using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AMWD.Protocols.Modbus.Common.Contracts;
using AMWD.Protocols.Modbus.Common.Protocols;
using AMWD.Protocols.Modbus.Tcp.Utils;

namespace AMWD.Protocols.Modbus.Tcp
{
	/// <summary>
	/// The default Modbus TCP connection.
	/// </summary>
	public class ModbusTcpConnection : IModbusConnection
	{
		#region Fields

		private string _hostname;
		private int _port;

		private bool _isDisposed;
		private readonly CancellationTokenSource _disposeCts = new();

		private readonly SemaphoreSlim _clientLock = new(1, 1);
		private readonly TcpClientWrapper _client = new();
		private readonly Timer _idleTimer;

		private readonly Task _processingTask;
		private readonly AsyncQueue<RequestQueueItem> _requestQueue = new();

		#endregion Fields

		/// <summary>
		/// Initializes a new instance of the <see cref="ModbusTcpConnection"/> class.
		/// </summary>
		public ModbusTcpConnection()
		{
			_idleTimer = new Timer(OnIdleTimer);
			_processingTask = ProcessAsync(_disposeCts.Token);
		}

		#region Properties

		/// <inheritdoc/>
		public string Name => "TCP";

		/// <inheritdoc/>
		public  virtual TimeSpan IdleTimeout { get; set; } = TimeSpan.FromSeconds(6);

		/// <summary>
		/// The DNS name of the remote host to which the connection is intended to.
		/// </summary>
		public virtual string Hostname
		{
			get => _hostname;
			set
			{
				if (string.IsNullOrWhiteSpace(value))
					throw new ArgumentNullException(nameof(value));

				_hostname = value;
			}
		}

		/// <summary>
		/// The port number of the remote host to which the connection is intended to.
		/// </summary>
		public virtual int Port
		{
			get => _port;
			set
			{
				if (value < 1 || ushort.MaxValue < value)
					throw new ArgumentOutOfRangeException(nameof(value));

				_port = value;
			}
		}

		/// <summary>
		/// Gets or sets the receive time out value of the connection.
		/// </summary>
		public virtual TimeSpan ReadTimeout
		{
			get => TimeSpan.FromMilliseconds(_client.ReceiveTimeout);
			set => _client.ReceiveTimeout = (int)value.TotalMilliseconds;
		}

		/// <summary>
		/// Gets or sets the send time out value of the connection.
		/// </summary>
		public virtual TimeSpan WriteTimeout
		{
			get => TimeSpan.FromMilliseconds(_client.SendTimeout);
			set => _client.SendTimeout = (int)value.TotalMilliseconds;
		}

		/// <summary>
		/// Gets or sets the maximum time until the connect attempt is given up.
		/// </summary>
		public virtual TimeSpan ConnectTimeout { get; set; } = TimeSpan.MaxValue;

		#endregion Properties

		/// <inheritdoc/>
		public void Dispose()
		{
			if (_isDisposed)
				return;

			_isDisposed = true;
			_disposeCts.Cancel();

			_idleTimer.Dispose();

			try
			{
				_processingTask.Wait();
				_processingTask.Dispose();
			}
			catch
			{ /* keep it quiet */ }

			OnIdleTimer(null);

			_client.Dispose();
			_clientLock.Dispose();

			while (_requestQueue.TryDequeue(out var item))
			{
				item.CancellationTokenRegistration.Dispose();
				item.CancellationTokenSource.Dispose();
				item.TaskCompletionSource.TrySetException(new ObjectDisposedException(GetType().FullName));
			}

			_disposeCts.Dispose();
			GC.SuppressFinalize(this);
		}

		#region Request processing

		/// <inheritdoc/>
		public Task<IReadOnlyList<byte>> InvokeAsync(IReadOnlyList<byte> request, Func<IReadOnlyList<byte>, bool> validateResponseComplete, CancellationToken cancellationToken = default)
		{
#if NET8_0_OR_GREATER
			ObjectDisposedException.ThrowIf(_isDisposed, this);
#else
			if (_isDisposed)
				throw new ObjectDisposedException(GetType().FullName);
#endif

			if (request == null || request.Count < 1)
				throw new ArgumentNullException(nameof(request));

#if NET8_0_OR_GREATER
			ArgumentNullException.ThrowIfNull(validateResponseComplete);
#else
			if (validateResponseComplete == null)
				throw new ArgumentNullException(nameof(validateResponseComplete));
#endif

			var item = new RequestQueueItem
			{
				Request = [.. request],
				ValidateResponseComplete = validateResponseComplete,
				TaskCompletionSource = new(),
				CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
			};

			item.CancellationTokenRegistration = item.CancellationTokenSource.Token.Register(() =>
			{
				_requestQueue.Remove(item);
				item.CancellationTokenSource.Dispose();
				item.TaskCompletionSource.TrySetCanceled(cancellationToken);
				item.CancellationTokenRegistration.Dispose();
			});

			_requestQueue.Enqueue(item);
			return item.TaskCompletionSource.Task;
		}

		private async Task ProcessAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					// Get next request to process
					var item = await _requestQueue.DequeueAsync(cancellationToken).ConfigureAwait(false);

					// Remove registration => already removed from queue
					item.CancellationTokenRegistration.Dispose();

					// Build combined cancellation token
					using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, item.CancellationTokenSource.Token);
					// Wait for exclusive access
					await _clientLock.WaitAsync(linkedCts.Token).ConfigureAwait(false);
					try
					{
						// Ensure connection is up
						await AssertConnection(linkedCts.Token).ConfigureAwait(false);

						var stream = _client.GetStream();
						await stream.FlushAsync(linkedCts.Token).ConfigureAwait(false);

#if NET6_0_OR_GREATER
						await stream.WriteAsync(item.Request, linkedCts.Token).ConfigureAwait(false);
#else
						await stream.WriteAsync(item.Request, 0, item.Request.Length, linkedCts.Token).ConfigureAwait(false);
#endif

						linkedCts.Token.ThrowIfCancellationRequested();

						var bytes = new List<byte>();
						byte[] buffer = new byte[TcpProtocol.MAX_ADU_LENGTH];

						do
						{
#if NET6_0_OR_GREATER
							int readCount = await stream.ReadAsync(buffer, linkedCts.Token).ConfigureAwait(false);
#else
							int readCount = await stream.ReadAsync(buffer, 0, buffer.Length, linkedCts.Token).ConfigureAwait(false);
#endif
							if (readCount < 1)
								throw new EndOfStreamException();

							bytes.AddRange(buffer.Take(readCount));

							linkedCts.Token.ThrowIfCancellationRequested();
						}
						while (!item.ValidateResponseComplete(bytes));

						item.TaskCompletionSource.TrySetResult(bytes);
					}
					catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
					{
						// Dispose() called
						item.TaskCompletionSource.TrySetCanceled(cancellationToken);
					}
					catch (OperationCanceledException) when (item.CancellationTokenSource.IsCancellationRequested)
					{
						// Cancellation requested by user
						item.TaskCompletionSource.TrySetCanceled(item.CancellationTokenSource.Token);
					}
					catch (Exception ex)
					{
						item.TaskCompletionSource.TrySetException(ex);
					}
					finally
					{
						_clientLock.Release();
						_idleTimer.Change(IdleTimeout, Timeout.InfiniteTimeSpan);
					}
				}
				catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
				{
					// Dispose() called while waiting for request item
				}
			}
		}

		#endregion Request processing

		#region Connection handling

		// Has to be called within _clientLock!
		private async Task AssertConnection(CancellationToken cancellationToken)
		{
			if (_client.Connected)
				return;

			int delay = 1;
			int maxDelay = 60;

			var ipAddresses = Resolve(Hostname);
			if (ipAddresses.Length == 0)
				throw new ApplicationException($"Could not resolve hostname '{Hostname}'");

			var startTime = DateTime.UtcNow;
			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					foreach (var ipAddress in ipAddresses)
					{
						_client.Close();

#if NET6_0_OR_GREATER
						using var connectTask = _client.ConnectAsync(ipAddress, Port, cancellationToken);
#else
						using var connectTask = _client.ConnectAsync(ipAddress, Port);
#endif
						if (await Task.WhenAny(connectTask, Task.Delay(ReadTimeout, cancellationToken)) == connectTask)
						{
							await connectTask;
							if (_client.Connected)
								return;
						}
					}

					throw new SocketException((int)SocketError.TimedOut);
				}
				catch (SocketException) when (ConnectTimeout == TimeSpan.MaxValue || DateTime.UtcNow.Subtract(startTime) < ConnectTimeout)
				{
					delay *= 2;
					if (delay > maxDelay)
						delay = maxDelay;

					try
					{
						await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken).ConfigureAwait(false);
					}
					catch
					{ /* keep it quiet */ }
				}
			}
		}

		private void OnIdleTimer(object _)
		{
			try
			{
				_clientLock.Wait(_disposeCts.Token);
				try
				{
					if (!_client.Connected)
						return;

					_client.Close();
				}
				finally
				{
					_clientLock.Release();
				}
			}
			catch
			{ /* keep it quiet */ }
		}

		#endregion Connection handling

		#region Helpers

		[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		private static IPAddress[] Resolve(string hostname)
		{
			if (string.IsNullOrWhiteSpace(hostname))
				return [];

			if (IPAddress.TryParse(hostname, out var address))
				return [address];

			try
			{
				return Dns.GetHostAddresses(hostname)
					.Where(a => a.AddressFamily == AddressFamily.InterNetwork || a.AddressFamily == AddressFamily.InterNetworkV6)
					.OrderBy(a => a.AddressFamily) // prefer IPv4
					.ToArray();
			}
			catch
			{
				return [];
			}
		}

		#endregion Helpers
	}
}
