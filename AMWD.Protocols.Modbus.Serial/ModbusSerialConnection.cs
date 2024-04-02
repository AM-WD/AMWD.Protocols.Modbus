using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AMWD.Protocols.Modbus.Common.Contracts;
using AMWD.Protocols.Modbus.Common.Protocols;
using AMWD.Protocols.Modbus.Common.Utils;
using AMWD.Protocols.Modbus.Serial.Enums;
using AMWD.Protocols.Modbus.Serial.Utils;

namespace AMWD.Protocols.Modbus.Serial
{
	/// <summary>
	/// The default Modbus Serial connection.
	/// </summary>
	public class ModbusSerialConnection : IModbusConnection
	{
		#region Fields

		private bool _isDisposed;
		private readonly CancellationTokenSource _disposeCts = new();

		private readonly SemaphoreSlim _portLock = new(1, 1);
		private readonly SerialPortWrapper _serialPort;
		private readonly Timer _idleTimer;

		private readonly Task _processingTask;
		private readonly AsyncQueue<RequestQueueItem> _requestQueue = new();

		// Only required to cover all logic branches on unit tests.
		private bool _isUnitTest = false;

		#endregion Fields

		/// <summary>
		/// Initializes a new instance of the <see cref="ModbusSerialConnection"/> class.
		/// </summary>
		public ModbusSerialConnection()
		{
			_serialPort = new SerialPortWrapper
			{
				BaudRate = (int)BaudRate.Baud19200,
				DataBits = 8,
				Handshake = Handshake.None,
				Parity = Parity.Even,
				ReadTimeout = 1000,
				RtsEnable = false,
				StopBits = StopBits.One,
				WriteTimeout = 1000,
			};

			_idleTimer = new Timer(OnIdleTimer);
			_processingTask = ProcessAsync(_disposeCts.Token);
		}

		#region Properties

		/// <inheritdoc/>
		public string Name => "Serial";

		/// <inheritdoc/>
		public virtual TimeSpan IdleTimeout { get; set; } = TimeSpan.FromSeconds(6);

		/// <inheritdoc/>
		public virtual TimeSpan ConnectTimeout { get; set; } = TimeSpan.MaxValue;

		/// <inheritdoc/>
		public virtual TimeSpan ReadTimeout
		{
			get => TimeSpan.FromMilliseconds(_serialPort.ReadTimeout);
			set => _serialPort.ReadTimeout = (int)value.TotalMilliseconds;
		}

		/// <inheritdoc/>
		public virtual TimeSpan WriteTimeout
		{
			get => TimeSpan.FromMilliseconds(_serialPort.WriteTimeout);
			set => _serialPort.WriteTimeout = (int)value.TotalMilliseconds;
		}

		/// <summary>
		/// Gets or sets a value indicating whether the RS485 driver has to be enabled via software switch.
		/// </summary>
		public virtual bool DriverEnabledRS485 { get; set; }

		/// <summary>
		/// Gets or sets a wait-time between requests.
		/// </summary>
		public virtual TimeSpan InterRequestDelay { get; set; } = TimeSpan.Zero;

		#region SerialPort Properties

		/// <inheritdoc cref="SerialPort.PortName" />
		public virtual string PortName
		{
			get => _serialPort.PortName;
			set => _serialPort.PortName = value;
		}

		/// <summary>
		/// Gets or sets the serial baud rate.
		/// </summary>
		public virtual BaudRate BaudRate
		{
			get => (BaudRate)_serialPort.BaudRate;
			set => _serialPort.BaudRate = (int)value;
		}

		/// <inheritdoc cref="SerialPort.DataBits" />
		/// <remarks>
		/// Should be 7 for ASCII mode and 8 for RTU mode.
		/// </remarks>
		public virtual int DataBits
		{
			get => _serialPort.DataBits;
			set => _serialPort.DataBits = value;
		}

		/// <inheritdoc cref="SerialPort.Handshake" />
		public virtual Handshake Handshake
		{
			get => _serialPort.Handshake;
			set => _serialPort.Handshake = value;
		}

		/// <inheritdoc cref="SerialPort.Parity" />
		/// <remarks>
		/// From the Specs:
		/// <br/>
		/// <see cref="Parity.Even"/> is recommended and therefore the default value.
		/// <br/>
		/// If you use <see cref="Parity.None"/>, <see cref="StopBits.Two"/> is required,
		/// otherwise <see cref="StopBits.One"/> should work fine.
		/// </remarks>
		public virtual Parity Parity
		{
			get => _serialPort.Parity;
			set => _serialPort.Parity = value;
		}

		/// <inheritdoc cref="SerialPort.RtsEnable" />
		public virtual bool RtsEnable
		{
			get => _serialPort.RtsEnable;
			set => _serialPort.RtsEnable = value;
		}

		/// <inheritdoc cref="SerialPort.StopBits" />
		/// <remarks>
		/// From the Specs:
		/// <br/>
		/// Should be <see cref="StopBits.One"/> for <see cref="Parity.Even"/> or <see cref="Parity.Odd"/> and
		/// <br/>
		/// should be <see cref="StopBits.Two"/> for <see cref="Parity.None"/>.
		/// </remarks>
		public virtual StopBits StopBits
		{
			get => _serialPort.StopBits;
			set => _serialPort.StopBits = value;
		}

		#endregion SerialPort Properties

		#endregion Properties

		/// <summary>
		/// Releases all managed and unmanaged resources used by the <see cref="IModbusConnection"/>.
		/// </summary>
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

			_serialPort.Dispose();
			_portLock.Dispose();

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
					await _portLock.WaitAsync(linkedCts.Token).ConfigureAwait(false);
					try
					{
						// Ensure connection is up
						await AssertConnection(linkedCts.Token).ConfigureAwait(false);

						await _serialPort.WriteAsync(item.Request, linkedCts.Token).ConfigureAwait(false);

						linkedCts.Token.ThrowIfCancellationRequested();

						var bytes = new List<byte>();
						byte[] buffer = new byte[RtuProtocol.MAX_ADU_LENGTH];

						do
						{
							int readCount = await _serialPort.ReadAsync(buffer, 0, buffer.Length, linkedCts.Token).ConfigureAwait(false);
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
						_portLock.Release();
						_idleTimer.Change(IdleTimeout, Timeout.InfiniteTimeSpan);

						await Task.Delay(InterRequestDelay, cancellationToken).ConfigureAwait(false);
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

		// Has to be called within _portLock!
		private async Task AssertConnection(CancellationToken cancellationToken)
		{
			if (_serialPort.IsOpen)
				return;

			int delay = 1;
			int maxDelay = 60;

			var startTime = DateTime.UtcNow;
			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					_serialPort.Close();
					_serialPort.ResetRS485DriverStateFlags();

					if (DriverEnabledRS485 && (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || _isUnitTest))
					{
						var flags = _serialPort.GetRS485DriverStateFlags();
						flags |= RS485Flags.Enabled;
						flags &= ~RS485Flags.RxDuringTx;
						_serialPort.ChangeRS485DriverStateFlags(flags);
					}

					using var connectTask = Task.Run(_serialPort.Open);
					if (await Task.WhenAny(connectTask, Task.Delay(ReadTimeout, cancellationToken)) == connectTask)
					{
						await connectTask;
						if (_serialPort.IsOpen)
							return;
					}

					throw new IOException();
				}
				catch (IOException) when (ConnectTimeout == TimeSpan.MaxValue || DateTime.UtcNow.Subtract(startTime) < ConnectTimeout)
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
				_portLock.Wait(_disposeCts.Token);
				try
				{
					if (!_serialPort.IsOpen)
						return;

					_serialPort.Close();
					_serialPort.ResetRS485DriverStateFlags();
				}
				finally
				{
					_portLock.Release();
				}
			}
			catch
			{ /* keep it quiet */ }
		}

		#endregion Connection handling
	}
}
