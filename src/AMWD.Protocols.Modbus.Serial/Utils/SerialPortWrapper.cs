using System;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using AMWD.Protocols.Modbus.Serial.Enums;

namespace AMWD.Protocols.Modbus.Serial.Utils
{
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	internal class SerialPortWrapper : IDisposable
	{
		#region Fields

		private readonly SerialPort _serialPort = new();

		private bool _driverStateChanged = false;
		private RS485Flags _initialFlags = 0;

		#endregion Fields

		#region Constructor

		public SerialPortWrapper()
		{
			_serialPort.DataReceived += (sender, e) => DataReceived?.Invoke(this, e);
			_serialPort.PinChanged += (sender, e) => PinChanged?.Invoke(this, e);
			_serialPort.ErrorReceived += (sender, e) => ErrorReceived?.Invoke(this, e);
		}

		#endregion Constructor

		#region Events

		/// <inheritdoc cref="SerialPort.DataReceived"/>
		public virtual event SerialDataReceivedEventHandler DataReceived;

		/// <inheritdoc cref="SerialPort.PinChanged"/>
		public virtual event SerialPinChangedEventHandler PinChanged;

		/// <inheritdoc cref="SerialPort.ErrorReceived"/>
		public virtual event SerialErrorReceivedEventHandler ErrorReceived;

		#endregion Events

		#region Properties

		/// <inheritdoc cref="SerialPort.Handshake"/>
		public virtual Handshake Handshake
		{
			get => _serialPort.Handshake;
			set => _serialPort.Handshake = value;
		}

		/// <inheritdoc cref="SerialPort.DataBits"/>
		public virtual int DataBits
		{
			get => _serialPort.DataBits;
			set => _serialPort.DataBits = value;
		}

		/// <inheritdoc cref="SerialPort.IsOpen"/>
		public virtual bool IsOpen
			=> _serialPort.IsOpen;

		/// <inheritdoc cref="SerialPort.PortName"/>
		public virtual string PortName
		{
			get => _serialPort.PortName;
			set => _serialPort.PortName = value;
		}

		/// <inheritdoc cref="SerialPort.ReadTimeout"/>
		public virtual int ReadTimeout
		{
			get => _serialPort.ReadTimeout;
			set => _serialPort.ReadTimeout = value;
		}

		/// <inheritdoc cref="SerialPort.RtsEnable"/>
		public virtual bool RtsEnable
		{
			get => _serialPort.RtsEnable;
			set => _serialPort.RtsEnable = value;
		}

		/// <inheritdoc cref="SerialPort.StopBits"/>
		public virtual StopBits StopBits
		{
			get => _serialPort.StopBits;
			set => _serialPort.StopBits = value;
		}

		/// <inheritdoc cref="SerialPort.WriteTimeout"/>
		public virtual int WriteTimeout
		{
			get => _serialPort.WriteTimeout;
			set => _serialPort.WriteTimeout = value;
		}

		/// <inheritdoc cref="SerialPort.Parity"/>
		public virtual Parity Parity
		{
			get => _serialPort.Parity;
			set => _serialPort.Parity = value;
		}

		/// <inheritdoc cref="SerialPort.BytesToWrite"/>
		public virtual int BytesToWrite
			=> _serialPort.BytesToWrite;

		/// <inheritdoc cref="SerialPort.BaudRate"/>
		public virtual int BaudRate
		{
			get => _serialPort.BaudRate;
			set => _serialPort.BaudRate = value;
		}

		/// <inheritdoc cref="SerialPort.BytesToRead"/>
		public virtual int BytesToRead
			=> _serialPort.BytesToRead;

		#endregion Properties

		#region Methods

		/// <inheritdoc cref="SerialPort.Close"/>
		public virtual void Close()
			=> _serialPort.Close();

		/// <inheritdoc cref="SerialPort.Open"/>
		public virtual void Open()
			=> _serialPort.Open();

		/// <inheritdoc cref="SerialPort.Read(byte[], int, int)"/>
		public virtual int Read(byte[] buffer, int offset, int count)
			=> _serialPort.Read(buffer, offset, count);

		/// <inheritdoc cref="SerialPort.Write(byte[], int, int)"/>
		public virtual void Write(byte[] buffer, int offset, int count)
			=> _serialPort.Write(buffer, offset, count);

		/// <inheritdoc cref="SerialPort.Dispose"/>
		public virtual void Dispose()
			=> _serialPort.Dispose();

		#endregion Methods

		#region Extensions

		/// <summary>
		/// Asynchronously reads a sequence of bytes from the current serial port, advances the
		/// position within the stream by the number of bytes read, and monitors cancellation
		/// requests.
		/// </summary>
		/// <remarks>
		/// There seems to be a bug with the async stream implementation on Windows.
		/// <br/>
		/// See this StackOverflow answer: <see href="https://stackoverflow.com/a/54610437/11906695" />.
		/// </remarks>
		/// <param name="buffer">The buffer to write the data into.</param>
		/// <param name="offset">The byte offset in buffer at which to begin writing data from the serial port.</param>
		/// <param name="count">The maximum number of bytes to read.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
		/// <returns>
		/// A task that represents the asynchronous read operation. The value of the TResult
		/// parameter contains the total number of bytes read into the buffer. The result
		/// value can be less than the number of bytes requested if the number of bytes currently
		/// available is less than the requested number, or it can be 0 (zero) if the end
		/// of the stream has been reached.
		/// </returns>
		public virtual async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
		{
			using var cts = new CancellationTokenSource(_serialPort.ReadTimeout);
			using var reg = cancellationToken.Register(cts.Cancel);

			var ctr = default(CancellationTokenRegistration);
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				// The async stream implementation on windows seems a bit broken.
				// So this will ensure the task to return to the caller.
				ctr = cts.Token.Register(_serialPort.DiscardInBuffer);
			}

			try
			{
				return await _serialPort.BaseStream.ReadAsync(buffer, offset, count, cts.Token);
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				cancellationToken.ThrowIfCancellationRequested();
				return 0;
			}
			catch (OperationCanceledException) when (cts.IsCancellationRequested)
			{
				throw new TimeoutException("No bytes read within the ReadTimeout.");
			}
			catch (IOException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
			{
				throw new TimeoutException("No bytes read within the ReadTimeout.");
			}
			finally
			{
				ctr.Dispose();
			}
		}

		/// <summary>
		/// Asynchronously writes a sequence of bytes to the current serial port, advances the
		/// current position within this stream by the number of bytes written, and monitors
		/// cancellation requests.
		/// </summary>
		/// <remarks>
		/// There seems to be a bug with the async stream implementation on Windows.
		/// <br/>
		/// See this StackOverflow answer: <see href="https://stackoverflow.com/a/54610437/11906695" />
		/// </remarks>
		/// <param name="buffer">The buffer to write the data from.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
		/// <returns></returns>
		public virtual async Task WriteAsync(byte[] buffer, CancellationToken cancellationToken = default)
		{
			using var cts = new CancellationTokenSource(_serialPort.WriteTimeout);
			using var reg = cancellationToken.Register(cts.Cancel);

			var ctr = default(CancellationTokenRegistration);
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				// The async stream implementation on windows seems a bit broken.
				// So this will ensure the task to return to the caller.
				ctr = cts.Token.Register(_serialPort.DiscardOutBuffer);
			}

			try
			{
#if NET6_0_OR_GREATER
				await _serialPort.BaseStream.WriteAsync(buffer, cts.Token);
#else
				await _serialPort.BaseStream.WriteAsync(buffer, 0, buffer.Length, cts.Token);
#endif
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}
			catch (OperationCanceledException) when (cts.IsCancellationRequested)
			{
				throw new TimeoutException("No bytes written within the WriteTimeout.");
			}
			catch (IOException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
			{
				throw new TimeoutException("No bytes written within the WriteTimeout.");
			}
			finally
			{
				ctr.Dispose();
			}
		}

		internal virtual void ChangeRS485DriverStateFlags(RS485Flags flags)
		{
			if (_driverStateChanged)
				throw new InvalidOperationException("The RS485 driver state has already been changed.");

			_driverStateChanged = true;
			_initialFlags = GetRS485DriverStateFlags();
			ChangeRS485DriverStateFlagsInternal(flags);
		}

		internal virtual void ResetRS485DriverStateFlags()
		{
			if (!_driverStateChanged)
				return;

			ChangeRS485DriverStateFlagsInternal(_initialFlags);
			_driverStateChanged = false;
			_initialFlags = 0;
		}

		internal virtual RS485Flags GetRS485DriverStateFlags()
		{
			var rs485 = new SerialRS485();
			SafeUnixHandle handle = null;

			try
			{
				handle = UnsafeNativeMethods.Open(PortName, UnsafeNativeMethods.O_RDWR | UnsafeNativeMethods.O_NOCTTY);
				if (UnsafeNativeMethods.IoCtl(handle, UnsafeNativeMethods.TIOCGRS485, ref rs485) == -1)
					throw new UnixIOException();
			}
			finally
			{
				handle?.Dispose();
			}

			return rs485.Flags;
		}

		private void ChangeRS485DriverStateFlagsInternal(RS485Flags flags)
		{
			var rs485 = new SerialRS485();
			SafeUnixHandle handle = null;

			try
			{
				handle = UnsafeNativeMethods.Open(PortName, UnsafeNativeMethods.O_RDWR | UnsafeNativeMethods.O_NOCTTY);
				if (UnsafeNativeMethods.IoCtl(handle, UnsafeNativeMethods.TIOCGRS485, ref rs485) == -1)
					throw new UnixIOException();
			}
			finally
			{
				handle?.Dispose();
			}

			rs485.Flags = flags;
			try
			{
				handle = UnsafeNativeMethods.Open(PortName, UnsafeNativeMethods.O_RDWR | UnsafeNativeMethods.O_NOCTTY);
				if (UnsafeNativeMethods.IoCtl(handle, UnsafeNativeMethods.TIOCSRS485, ref rs485) == -1)
					throw new UnixIOException();
			}
			finally
			{
				handle?.Dispose();
			}
		}

		#endregion Extensions
	}
}
