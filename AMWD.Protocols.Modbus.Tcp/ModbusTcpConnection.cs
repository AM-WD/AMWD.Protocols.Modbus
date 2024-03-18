using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AMWD.Protocols.Modbus.Common.Contracts;
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
		private bool _isConnected;
		private readonly TcpClientWrapper _client = new();

		private CancellationTokenSource _disconnectCts;
		private Task _reconnectTask = Task.CompletedTask;
		private readonly SemaphoreSlim _reconnectLock = new(1, 1);

		private CancellationTokenSource _processingCts;
		private Task _processingTask = Task.CompletedTask;
		private readonly AsyncQueue<RequestQueueItem> _requestQueue = new();

		#endregion Fields

		#region Properties

		/// <inheritdoc/>
		public string Name => "TCP";

		/// <inheritdoc/>
		public bool IsConnected => _isConnected && _client.Connected;

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
		/// Gets or sets the maximum time until the reconnect is given up.
		/// </summary>
		public virtual TimeSpan ReconnectTimeout { get; set; } = TimeSpan.MaxValue;

		/// <summary>
		/// Gets or sets the interval in which a keep alive package should be sent.
		/// </summary>
		public virtual TimeSpan KeepAliveInterval { get; set; } = TimeSpan.Zero;

		#endregion Properties

		/// <inheritdoc/>
		public async Task ConnectAsync(CancellationToken cancellationToken = default)
		{
#if NET8_0_OR_GREATER
			ObjectDisposedException.ThrowIf(_isDisposed, this);
#else
			if (_isDisposed)
				throw new ObjectDisposedException(GetType().FullName);
#endif

			if (_disconnectCts != null)
			{
				await _reconnectTask;
				return;
			}

			_disconnectCts = new CancellationTokenSource();
			using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_disconnectCts.Token, cancellationToken);

			_reconnectTask = ReconnectInternalAsync(linkedCts.Token);
			await _reconnectTask.ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public Task DisconnectAsync(CancellationToken cancellationToken = default)
		{
#if NET8_0_OR_GREATER
			ObjectDisposedException.ThrowIf(_isDisposed, this);
#else
			if (_isDisposed)
				throw new ObjectDisposedException(GetType().FullName);
#endif
			if (_disconnectCts == null)
				return Task.CompletedTask;

			return DisconnectInternalAsync(cancellationToken);
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			if (_isDisposed)
				return;

			_isDisposed = true;
			DisconnectInternalAsync(CancellationToken.None).Wait();

			_client.Dispose();

			GC.SuppressFinalize(this);
		}

		/// <inheritdoc/>
		public Task<IReadOnlyList<byte>> InvokeAsync(IReadOnlyList<byte> request, Func<IReadOnlyList<byte>, bool> validateResponseComplete, CancellationToken cancellationToken = default)
		{
#if NET8_0_OR_GREATER
			ObjectDisposedException.ThrowIf(_isDisposed, this);
#else
			if (_isDisposed)
				throw new ObjectDisposedException(GetType().FullName);
#endif

			if (!IsConnected)
				throw new ApplicationException($"Connection is not open");

			if (request?.Count < 1)
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
				CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken),
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

		private async Task ReconnectInternalAsync(CancellationToken cancellationToken)
		{
			if (!_reconnectLock.Wait(0, cancellationToken))
				return;

			try
			{
				_isConnected = false;
				_processingCts?.Cancel();
				await _processingTask.ConfigureAwait(false);

				int delay = 1;
				int maxDelay = 60;

				var ipAddresses = Resolve(Hostname);
				if (ipAddresses.Count == 0)
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
								{
									_isConnected = true;

									_processingCts?.Dispose();
									_processingCts = new();
									_processingTask = ProcessAsync(_processingCts.Token);

									SetKeepAlive();
									return;
								}
							}
						}

						throw new SocketException((int)SocketError.TimedOut);
					}
					catch (SocketException) when (ReconnectTimeout == TimeSpan.MaxValue || DateTime.UtcNow.Subtract(startTime) < ReconnectTimeout)
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
			finally
			{
				_reconnectLock.Release();
			}
		}

		private async Task DisconnectInternalAsync(CancellationToken cancellationToken)
		{
			_disconnectCts?.Cancel();
			_processingCts?.Cancel();

			try
			{
				await _reconnectTask.ConfigureAwait(false);
				await _processingTask.ConfigureAwait(false);
			}
			catch
			{ /* keep it quiet */ }

			// Ensure that the client is closed
			await _reconnectLock.WaitAsync(cancellationToken).ConfigureAwait(false);
			try
			{
				_isConnected = false;
				_client.Close();
			}
			finally
			{
				_reconnectLock.Release();
			}

			_disconnectCts?.Dispose();
			_disconnectCts = null;

			_processingCts?.Dispose();
			_processingCts = null;

			while (_requestQueue.TryDequeue(out var item))
			{
				item.CancellationTokenRegistration.Dispose();
				item.CancellationTokenSource.Dispose();
				item.TaskCompletionSource.TrySetCanceled(CancellationToken.None);
			}
		}

		#region Processing

		private async Task ProcessAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				var item = await _requestQueue.DequeueAsync(cancellationToken).ConfigureAwait(false);
				item.CancellationTokenRegistration.Dispose();

				using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, item.CancellationTokenSource.Token);
				try
				{
					var stream = _client.GetStream();
					await stream.FlushAsync(linkedCts.Token).ConfigureAwait(false);

#if NET6_0_OR_GREATER
					await stream.WriteAsync(item.Request, linkedCts.Token).ConfigureAwait(false);
#else
					await stream.WriteAsync(item.Request, 0, item.Request.Length, linkedCts.Token).ConfigureAwait(false);
#endif

					linkedCts.Token.ThrowIfCancellationRequested();

					var bytes = new List<byte>();
					byte[] buffer = new byte[260];

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
					// DisconnectAsync() called
					item.TaskCompletionSource.TrySetCanceled(cancellationToken);
					return;
				}
				catch (OperationCanceledException) when (item.CancellationTokenSource.IsCancellationRequested)
				{
					item.TaskCompletionSource.TrySetCanceled(item.CancellationTokenSource.Token);
					continue;
				}
				catch (IOException ex)
				{
					item.TaskCompletionSource.TrySetException(ex);
					_reconnectTask = ReconnectInternalAsync(_disconnectCts.Token);
				}
				catch (SocketException ex)
				{
					item.TaskCompletionSource.TrySetException(ex);
					_reconnectTask = ReconnectInternalAsync(_disconnectCts.Token);
				}
				catch (TimeoutException ex)
				{
					item.TaskCompletionSource.TrySetException(ex);
					_reconnectTask = ReconnectInternalAsync(_disconnectCts.Token);
				}
				catch (InvalidOperationException ex)
				{
					item.TaskCompletionSource.TrySetException(ex);
					_reconnectTask = ReconnectInternalAsync(_disconnectCts.Token);
				}
				catch (Exception ex)
				{
					item.TaskCompletionSource.TrySetException(ex);
				}
			}
		}

		internal class RequestQueueItem
		{
			public byte[] Request { get; set; }

			public Func<IReadOnlyList<byte>, bool> ValidateResponseComplete { get; set; }

			public TaskCompletionSource<IReadOnlyList<byte>> TaskCompletionSource { get; set; }

			public CancellationTokenSource CancellationTokenSource { get; set; }

			public CancellationTokenRegistration CancellationTokenRegistration { get; set; }
		}

		#endregion Processing

		#region Helpers

		[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		private static List<IPAddress> Resolve(string hostname)
		{
			if (string.IsNullOrWhiteSpace(hostname))
				return [];

			if (IPAddress.TryParse(hostname, out var ipAddress))
				return [ipAddress];

			try
			{
				return Dns.GetHostAddresses(hostname)
					.Where(a => a.AddressFamily == AddressFamily.InterNetwork || a.AddressFamily == AddressFamily.InterNetworkV6)
					.OrderBy(a => a.AddressFamily) // Prefer IPv4
					.ToList();
			}
			catch
			{
				return [];
			}
		}

		[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		private void SetKeepAlive()
		{
#if NET6_0_OR_GREATER
			_client.Client?.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, KeepAliveInterval.TotalMilliseconds > 0);
			_client.Client?.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, (int)KeepAliveInterval.TotalSeconds);
			_client.Client?.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, (int)KeepAliveInterval.TotalSeconds);
#else
			// See: https://github.com/dotnet/runtime/issues/25555
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				return;

			bool isEnabled = KeepAliveInterval.TotalMilliseconds > 0;
			uint interval = KeepAliveInterval.TotalMilliseconds > uint.MaxValue
				? uint.MaxValue
				: (uint)KeepAliveInterval.TotalMilliseconds;
			int uIntSize = sizeof(uint);
			byte[] config = new byte[uIntSize * 3];

			Array.Copy(BitConverter.GetBytes(isEnabled ? 1U : 0U), 0, config, uIntSize * 0, uIntSize);
			Array.Copy(BitConverter.GetBytes(interval), 0, config, uIntSize * 1, uIntSize);
			Array.Copy(BitConverter.GetBytes(interval), 0, config, uIntSize * 2, uIntSize);
			_client.Client?.IOControl(IOControlCode.KeepAliveValues, config, null);
#endif
		}

		#endregion Helpers
	}
}
