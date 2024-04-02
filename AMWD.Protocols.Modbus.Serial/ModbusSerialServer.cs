using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AMWD.Protocols.Modbus.Common;
using AMWD.Protocols.Modbus.Common.Events;
using AMWD.Protocols.Modbus.Common.Models;
using AMWD.Protocols.Modbus.Common.Protocols;

namespace AMWD.Protocols.Modbus.Serial
{
	/// <summary>
	/// A basic implementation of a Modbus serial line server.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class ModbusSerialServer : IDisposable
	{
		#region Fields

		private bool _isDisposed;

		private SerialPort _serialPort;
		private CancellationTokenSource _stopCts;

		private readonly ReaderWriterLockSlim _deviceListLock = new();
		private readonly Dictionary<byte, ModbusDevice> _devices = [];

		#endregion Fields

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ModbusSerialServer"/> class.
		/// </summary>
		/// <param name="portName">The name of the serial port to use.</param>
		/// <param name="baudRate">The baud rate of the serial port (Default: 19.200).</param>
		public ModbusSerialServer(string portName, BaudRate baudRate = BaudRate.Baud19200)
		{
			if (string.IsNullOrWhiteSpace(portName))
				throw new ArgumentNullException(nameof(portName));

			if (!Enum.IsDefined(typeof(BaudRate), baudRate))
				throw new ArgumentOutOfRangeException(nameof(baudRate));

			if (!ModbusSerialClient.AvailablePortNames.Contains(portName))
				throw new ArgumentException($"The serial port ({portName}) is not available.", nameof(portName));

			_serialPort = new SerialPort
			{
				PortName = portName,
				BaudRate = (int)baudRate,
				Handshake = Handshake.None,
				DataBits = 8,
				ReadTimeout = 1000,
				RtsEnable = false,
				StopBits = StopBits.One,
				WriteTimeout = 1000,
				Parity = Parity.Even
			};
		}

		#endregion Constructors

		#region Events

		/// <summary>
		/// Occurs when a <see cref="Coil"/> is written.
		/// </summary>
		public event EventHandler<CoilWrittenEventArgs> CoilWritten;

		/// <summary>
		/// Occurs when a <see cref="HoldingRegister"/> is written.
		/// </summary>
		public event EventHandler<RegisterWrittenEventArgs> RegisterWritten;

		#endregion Events

		#region Properties

		/// <inheritdoc cref="SerialPort.PortName"/>
		public string PortName => _serialPort.PortName;

		/// <summary>
		/// Gets or sets the baud rate of the serial port.
		/// </summary>
		public BaudRate BaudRate
		{
			get => (BaudRate)_serialPort.BaudRate;
			set => _serialPort.BaudRate = (int)value;
		}

		/// <inheritdoc cref="SerialPort.Handshake"/>
		public Handshake Handshake
		{
			get => _serialPort.Handshake;
			set => _serialPort.Handshake = value;
		}

		/// <inheritdoc cref="SerialPort.DataBits"/>
		public int DataBits
		{
			get => _serialPort.DataBits;
			set => _serialPort.DataBits = value;
		}

		/// <inheritdoc cref="SerialPort.IsOpen"/>
		public bool IsOpen => _serialPort.IsOpen;

		/// <summary>
		/// Gets or sets the <see cref="TimeSpan"/> before a time-out occurs when a read operation does not finish.
		/// </summary>
		public TimeSpan ReadTimeout
		{
			get => TimeSpan.FromMilliseconds(_serialPort.ReadTimeout);
			set => _serialPort.ReadTimeout = (int)value.TotalMilliseconds;
		}

		/// <inheritdoc cref="SerialPort.RtsEnable"/>
		public bool RtsEnable
		{
			get => _serialPort.RtsEnable;
			set => _serialPort.RtsEnable = value;
		}

		/// <inheritdoc cref="SerialPort.StopBits"/>
		public StopBits StopBits
		{
			get => _serialPort.StopBits;
			set => _serialPort.StopBits = value;
		}

		/// <summary>
		/// Gets or sets the <see cref="TimeSpan"/> before a time-out occurs when a write operation does not finish.
		/// </summary>
		public TimeSpan WriteTimeout
		{
			get => TimeSpan.FromMilliseconds(_serialPort.WriteTimeout);
			set => _serialPort.WriteTimeout = (int)value.TotalMilliseconds;
		}

		/// <inheritdoc cref="SerialPort.Parity"/>
		public Parity Parity
		{
			get => _serialPort.Parity;
			set => _serialPort.Parity = value;
		}

		#endregion Properties

		#region Control Methods

		/// <summary>
		/// Starts the server.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token used to propagate notification that this operation should be canceled.</param>
		public Task StartAsync(CancellationToken cancellationToken = default)
		{
			Assertions();

			_stopCts?.Cancel();
			_serialPort.Close();
			_serialPort.DataReceived -= OnDataReceived;

			_stopCts?.Dispose();
			_stopCts = new CancellationTokenSource();

			_serialPort.DataReceived += OnDataReceived;
			_serialPort.Open();

			return Task.CompletedTask;
		}

		/// <summary>
		/// Stops the server.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token used to propagate notification that this operation should be canceled.</param>
		public Task StopAsync(CancellationToken cancellationToken = default)
		{
			Assertions();
			return StopAsyncInternal(cancellationToken);
		}

		private Task StopAsyncInternal(CancellationToken cancellationToken)
		{
			_stopCts.Cancel();

			_serialPort.Close();
			_serialPort.DataReceived -= OnDataReceived;

			return Task.CompletedTask;
		}

		/// <summary>
		/// Releases all managed and unmanaged resources used by the <see cref="ModbusSerialServer"/>.
		/// </summary>
		public void Dispose()
		{
			if (_isDisposed)
				return;

			_isDisposed = true;

			StopAsyncInternal(CancellationToken.None).Wait();

			_deviceListLock.Dispose();
			_devices.Clear();
		}

		private void Assertions()
		{
#if NET8_0_OR_GREATER
			ObjectDisposedException.ThrowIf(_isDisposed, this);
#else
			if (_isDisposed)
				throw new ObjectDisposedException(GetType().FullName);
#endif
		}

		#endregion Control Methods

		#region Client Handling

		private void OnDataReceived(object _, SerialDataReceivedEventArgs evArgs)
		{
			try
			{
				var requestBytes = new List<byte>();
				do
				{
					byte[] buffer = new byte[RtuProtocol.MAX_ADU_LENGTH];
					int count = _serialPort.Read(buffer, 0, buffer.Length);
					requestBytes.AddRange(buffer.Take(count));

					_stopCts.Token.ThrowIfCancellationRequested();
				}
				while (_serialPort.BytesToRead > 0);

				_stopCts.Token.ThrowIfCancellationRequested();
				byte[] responseBytes = HandleRequest(requestBytes.ToArray());
				if (responseBytes == null)
					return;

				_stopCts.Token.ThrowIfCancellationRequested();
				_serialPort.Write(responseBytes, 0, responseBytes.Length);
			}
			catch
			{ /* keep it quiet */ }
		}

		#endregion Client Handling

		#region Request Handling

		private byte[] HandleRequest(byte[] requestBytes)
		{
			byte[] recvCrc = requestBytes.Skip(requestBytes.Length - 2).ToArray();
			byte[] calcCrc = RtuProtocol.CRC16(requestBytes, 0, requestBytes.Length - 2);
			if (!recvCrc.SequenceEqual(calcCrc))
				return null;

			using (_deviceListLock.GetReadLock())
			{
				// No response is sent, if the device is not known
				if (!_devices.TryGetValue(requestBytes[0], out var device))
					return null;

				switch ((ModbusFunctionCode)requestBytes[1])
				{
					case ModbusFunctionCode.ReadCoils:
						return HandleReadCoils(device, requestBytes);

					case ModbusFunctionCode.ReadDiscreteInputs:
						return HandleReadDiscreteInputs(device, requestBytes);

					case ModbusFunctionCode.ReadHoldingRegisters:
						return HandleReadHoldingRegisters(device, requestBytes);

					case ModbusFunctionCode.ReadInputRegisters:
						return HandleReadInputRegisters(device, requestBytes);

					case ModbusFunctionCode.WriteSingleCoil:
						return HandleWriteSingleCoil(device, requestBytes);

					case ModbusFunctionCode.WriteSingleRegister:
						return HandleWriteSingleRegister(device, requestBytes);

					case ModbusFunctionCode.WriteMultipleCoils:
						return HandleWriteMultipleCoils(device, requestBytes);

					case ModbusFunctionCode.WriteMultipleRegisters:
						return HandleWriteMultipleRegisters(device, requestBytes);

					case ModbusFunctionCode.EncapsulatedInterface:
						return HandleEncapsulatedInterface(requestBytes);

					default: // unknown function
						{
							byte[] responseBytes = new byte[5];
							Array.Copy(requestBytes, 0, responseBytes, 0, 2);

							// Mark as error
							responseBytes[1] |= 0x80;

							responseBytes[2] = (byte)ModbusErrorCode.IllegalFunction;

							SetCrc(responseBytes);
							return responseBytes;
						}
				}
			}
		}

		private static byte[] HandleReadCoils(ModbusDevice device, byte[] requestBytes)
		{
			if (requestBytes.Length < 8)
				return null;

			var responseBytes = new List<byte>();
			responseBytes.AddRange(requestBytes.Take(2));

			ushort firstAddress = requestBytes.GetBigEndianUInt16(2);
			ushort count = requestBytes.GetBigEndianUInt16(4);

			if (TcpProtocol.MIN_READ_COUNT < count || count < TcpProtocol.MAX_DISCRETE_READ_COUNT)
			{
				responseBytes[1] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.IllegalDataValue);

				AddCrc(responseBytes);
				return [.. responseBytes];
			}

			if (firstAddress + count > ushort.MaxValue)
			{
				responseBytes[1] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.IllegalDataAddress);

				AddCrc(responseBytes);
				return [.. responseBytes];
			}

			try
			{
				byte[] values = new byte[(int)Math.Ceiling(count / 8.0)];
				for (int i = 0; i < count; i++)
				{
					ushort address = (ushort)(firstAddress + i);
					if (device.GetCoil(address).Value)
					{
						int byteIndex = i / 8;
						int bitIndex = i % 8;

						values[byteIndex] |= (byte)(1 << bitIndex);
					}
				}

				responseBytes.Add((byte)values.Length);
				responseBytes.AddRange(values);
			}
			catch
			{
				responseBytes[1] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.SlaveDeviceFailure);
			}

			AddCrc(responseBytes);
			return [.. responseBytes];
		}

		private static byte[] HandleReadDiscreteInputs(ModbusDevice device, byte[] requestBytes)
		{
			if (requestBytes.Length < 8)
				return null;

			var responseBytes = new List<byte>();
			responseBytes.AddRange(requestBytes.Take(2));

			ushort firstAddress = requestBytes.GetBigEndianUInt16(2);
			ushort count = requestBytes.GetBigEndianUInt16(4);

			if (TcpProtocol.MIN_READ_COUNT < count || count < TcpProtocol.MAX_DISCRETE_READ_COUNT)
			{
				responseBytes[1] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.IllegalDataValue);

				AddCrc(responseBytes);
				return [.. responseBytes];
			}

			if (firstAddress + count > ushort.MaxValue)
			{
				responseBytes[1] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.IllegalDataAddress);

				AddCrc(responseBytes);
				return [.. responseBytes];
			}

			try
			{
				byte[] values = new byte[(int)Math.Ceiling(count / 8.0)];
				for (int i = 0; i < count; i++)
				{
					ushort address = (ushort)(firstAddress + i);
					if (device.GetDiscreteInput(address).Value)
					{
						int byteIndex = i / 8;
						int bitIndex = i % 8;

						values[byteIndex] |= (byte)(1 << bitIndex);
					}
				}

				responseBytes.Add((byte)values.Length);
				responseBytes.AddRange(values);
			}
			catch
			{
				responseBytes[1] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.SlaveDeviceFailure);
			}

			AddCrc(responseBytes);
			return [.. responseBytes];
		}

		private static byte[] HandleReadHoldingRegisters(ModbusDevice device, byte[] requestBytes)
		{
			if (requestBytes.Length < 8)
				return null;

			var responseBytes = new List<byte>();
			responseBytes.AddRange(requestBytes.Take(2));

			ushort firstAddress = requestBytes.GetBigEndianUInt16(2);
			ushort count = requestBytes.GetBigEndianUInt16(4);

			if (TcpProtocol.MIN_READ_COUNT < count || count < TcpProtocol.MAX_REGISTER_READ_COUNT)
			{
				responseBytes[1] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.IllegalDataValue);

				AddCrc(responseBytes);
				return [.. responseBytes];
			}

			if (firstAddress + count > ushort.MaxValue)
			{
				responseBytes[1] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.IllegalDataAddress);

				AddCrc(responseBytes);
				return [.. responseBytes];
			}

			try
			{
				byte[] values = new byte[count * 2];
				for (int i = 0; i < count; i++)
				{
					ushort address = (ushort)(firstAddress + i);
					var register = device.GetHoldingRegister(address);

					values[i * 2] = register.HighByte;
					values[i * 2 + 1] = register.LowByte;
				}

				responseBytes.Add((byte)values.Length);
				responseBytes.AddRange(values);
			}
			catch
			{
				responseBytes[1] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.SlaveDeviceFailure);
			}

			AddCrc(responseBytes);
			return [.. responseBytes];
		}

		private static byte[] HandleReadInputRegisters(ModbusDevice device, byte[] requestBytes)
		{
			if (requestBytes.Length < 8)
				return null;

			var responseBytes = new List<byte>();
			responseBytes.AddRange(requestBytes.Take(2));

			ushort firstAddress = requestBytes.GetBigEndianUInt16(2);
			ushort count = requestBytes.GetBigEndianUInt16(4);

			if (TcpProtocol.MIN_READ_COUNT < count || count < TcpProtocol.MAX_REGISTER_READ_COUNT)
			{
				responseBytes[1] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.IllegalDataValue);

				AddCrc(responseBytes);
				return [.. responseBytes];
			}

			if (firstAddress + count > ushort.MaxValue)
			{
				responseBytes[1] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.IllegalDataAddress);

				AddCrc(responseBytes);
				return [.. responseBytes];
			}

			try
			{
				byte[] values = new byte[count * 2];
				for (int i = 0; i < count; i++)
				{
					ushort address = (ushort)(firstAddress + i);
					var register = device.GetInputRegister(address);

					values[i * 2] = register.HighByte;
					values[i * 2 + 1] = register.LowByte;
				}

				responseBytes.Add((byte)values.Length);
				responseBytes.AddRange(values);
			}
			catch
			{
				responseBytes[1] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.SlaveDeviceFailure);
			}

			AddCrc(responseBytes);
			return [.. responseBytes];
		}

		private byte[] HandleWriteSingleCoil(ModbusDevice device, byte[] requestBytes)
		{
			if (requestBytes.Length < 8)
				return null;

			var responseBytes = new List<byte>();
			responseBytes.AddRange(requestBytes.Take(2));

			ushort address = requestBytes.GetBigEndianUInt16(2);

			if (requestBytes[4] != 0x00 && requestBytes[4] != 0xFF)
			{
				responseBytes[1] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.IllegalDataValue);

				AddCrc(responseBytes);
				return [.. responseBytes];
			}

			try
			{
				device.SetCoil(new Coil
				{
					Address = address,
					HighByte = requestBytes[4]
				});

				// Response is an echo of the request
				responseBytes.AddRange(requestBytes.Skip(2).Take(4));

				// Notify that the coil was written
				Task.Run(() =>
				{
					try
					{
						CoilWritten?.Invoke(this, new CoilWrittenEventArgs
						{
							UnitId = device.Id,
							Address = address,
							Value = requestBytes[10] == 0xFF
						});
					}
					catch
					{
						// keep everything quiet
					}
				});
			}
			catch
			{
				responseBytes[1] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.SlaveDeviceFailure);
			}

			AddCrc(responseBytes);
			return [.. responseBytes];
		}

		private byte[] HandleWriteSingleRegister(ModbusDevice device, byte[] requestBytes)
		{
			if (requestBytes.Length < 8)
				return null;

			var responseBytes = new List<byte>();
			responseBytes.AddRange(requestBytes.Take(2));

			ushort address = requestBytes.GetBigEndianUInt16(2);
			ushort value = requestBytes.GetBigEndianUInt16(4);

			try
			{
				device.SetHoldingRegister(new HoldingRegister
				{
					Address = address,
					HighByte = requestBytes[4],
					LowByte = requestBytes[5]
				});

				// Response is an echo of the request
				responseBytes.AddRange(requestBytes.Skip(2).Take(4));

				// Notify that the register was written
				Task.Run(() =>
				{
					try
					{
						RegisterWritten?.Invoke(this, new RegisterWrittenEventArgs
						{
							UnitId = device.Id,
							Address = address,
							Value = value,
							HighByte = requestBytes[10],
							LowByte = requestBytes[11]
						});
					}
					catch
					{
						// keep everything quiet
					}
				});
			}
			catch
			{
				responseBytes[1] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.SlaveDeviceFailure);
			}

			AddCrc(responseBytes);
			return [.. responseBytes];
		}

		private byte[] HandleWriteMultipleCoils(ModbusDevice device, byte[] requestBytes)
		{
			if (requestBytes.Length < 9)
				return null;

			var responseBytes = new List<byte>();
			responseBytes.AddRange(requestBytes.Take(2));

			ushort firstAddress = requestBytes.GetBigEndianUInt16(2);
			ushort count = requestBytes.GetBigEndianUInt16(4);

			int byteCount = (int)Math.Ceiling(count / 8.0);
			if (requestBytes.Length < 9 + byteCount)
			{
				responseBytes[1] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.IllegalDataValue);

				AddCrc(responseBytes);
				return [.. responseBytes];
			}

			try
			{
				int baseOffset = 7;
				for (int i = 0; i < count; i++)
				{
					int bytePosition = i / 8;
					int bitPosition = i % 8;

					ushort address = (ushort)(firstAddress + i);
					bool value = (requestBytes[baseOffset + bytePosition] & (1 << bitPosition)) > 0;

					device.SetCoil(new Coil
					{
						Address = address,
						HighByte = value ? (byte)0xFF : (byte)0x00
					});

					// Notify that the coil was written
					Task.Run(() =>
					{
						try
						{
							CoilWritten?.Invoke(this, new CoilWrittenEventArgs
							{
								UnitId = device.Id,
								Address = address,
								Value = value
							});
						}
						catch
						{
							// keep everything quiet
						}
					});
				}

				responseBytes.AddRange(requestBytes.Skip(2).Take(4));
			}
			catch
			{
				responseBytes[1] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.SlaveDeviceFailure);
			}

			AddCrc(responseBytes);
			return [.. responseBytes];
		}

		private byte[] HandleWriteMultipleRegisters(ModbusDevice device, byte[] requestBytes)
		{
			if (requestBytes.Length < 9)
				return null;

			var responseBytes = new List<byte>();
			responseBytes.AddRange(requestBytes.Take(8));

			ushort firstAddress = requestBytes.GetBigEndianUInt16(2);
			ushort count = requestBytes.GetBigEndianUInt16(4);

			int byteCount = count * 2;
			if (requestBytes.Length < 9 + byteCount)
			{
				responseBytes[1] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.IllegalDataValue);

				AddCrc(responseBytes);
				return [.. responseBytes];
			}

			try
			{
				int baseOffset = 7;
				for (int i = 0; i < count; i++)
				{
					ushort address = (ushort)(firstAddress + i);

					device.SetHoldingRegister(new HoldingRegister
					{
						Address = address,
						HighByte = requestBytes[baseOffset + i * 2],
						LowByte = requestBytes[baseOffset + i * 2 + 1]
					});

					// Notify that the coil was written
					Task.Run(() =>
					{
						try
						{
							RegisterWritten?.Invoke(this, new RegisterWrittenEventArgs
							{
								UnitId = device.Id,
								Address = address,
								Value = requestBytes.GetBigEndianUInt16(baseOffset + i * 2),
								HighByte = requestBytes[baseOffset + i * 2],
								LowByte = requestBytes[baseOffset + i * 2 + 1]
							});
						}
						catch
						{
							// keep everything quiet
						}
					});
				}

				responseBytes.AddRange(requestBytes.Skip(2).Take(4));
			}
			catch
			{
				responseBytes[1] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.SlaveDeviceFailure);
			}

			AddCrc(responseBytes);
			return [.. responseBytes];
		}

		private byte[] HandleEncapsulatedInterface(byte[] requestBytes)
		{
			var responseBytes = new List<byte>();
			responseBytes.AddRange(requestBytes.Take(2));

			if (requestBytes[2] != 0x0E)
			{
				responseBytes[1] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.IllegalFunction);

				AddCrc(responseBytes);
				return [.. responseBytes];
			}

			if (0x06 < requestBytes[4] && requestBytes[4] < 0x80)
			{
				responseBytes[1] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.IllegalDataAddress);

				AddCrc(responseBytes);
				return [.. responseBytes];
			}

			var category = (ModbusDeviceIdentificationCategory)requestBytes[3];
			if (!Enum.IsDefined(typeof(ModbusDeviceIdentificationCategory), category))
			{
				responseBytes[1] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.IllegalDataValue);

				AddCrc(responseBytes);
				return [.. responseBytes];
			}

			try
			{
				var bodyBytes = new List<byte>();
				// MEI, Category
				bodyBytes.AddRange(requestBytes.Skip(2).Take(2));
				// Conformity
				bodyBytes.Add((byte)(category + 0x80));
				// More, NextId, NumberOfObjects
				bodyBytes.AddRange(new byte[3]);

				int maxObjectId;
				switch (category)
				{
					case ModbusDeviceIdentificationCategory.Basic:
						maxObjectId = 0x02;
						break;

					case ModbusDeviceIdentificationCategory.Regular:
						maxObjectId = 0x06;
						break;

					case ModbusDeviceIdentificationCategory.Extended:
						maxObjectId = 0xFF;
						break;

					default: // Individual
						{
							if (requestBytes[4] < 0x03)
								bodyBytes[2] = 0x81;
							else if (requestBytes[4] < 0x80)
								bodyBytes[2] = 0x82;
							else
								bodyBytes[2] = 0x83;

							maxObjectId = requestBytes[4];
						}

						break;
				}

				byte numberOfObjects = 0;
				for (int i = requestBytes[4]; i <= maxObjectId; i++)
				{
					// Reserved
					if (0x07 <= i && i <= 0x7F)
						continue;

					byte[] objBytes = GetDeviceObject((byte)i);

					// We need to split the response if it would exceed the max ADU size
					if (responseBytes.Count + bodyBytes.Count + objBytes.Length > RtuProtocol.MAX_ADU_LENGTH)
					{
						bodyBytes[3] = 0xFF;
						bodyBytes[4] = (byte)i;

						bodyBytes[5] = numberOfObjects;
						responseBytes.AddRange(bodyBytes);
						return [.. responseBytes];
					}

					bodyBytes.AddRange(objBytes);
					numberOfObjects++;
				}

				bodyBytes[5] = numberOfObjects;
				responseBytes.AddRange(bodyBytes);

				AddCrc(responseBytes);
				return [.. responseBytes];
			}
			catch
			{
				responseBytes[1] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.SlaveDeviceFailure);

				AddCrc(responseBytes);
				return [.. responseBytes];
			}
		}

		private byte[] GetDeviceObject(byte objectId)
		{
			var result = new List<byte> { objectId };
			switch ((ModbusDeviceIdentificationObject)objectId)
			{
				case ModbusDeviceIdentificationObject.VendorName:
					{
						byte[] bytes = Encoding.UTF8.GetBytes("AMWD");
						result.Add((byte)bytes.Length);
						result.AddRange(bytes);
					}
					break;

				case ModbusDeviceIdentificationObject.ProductCode:
					{
						byte[] bytes = Encoding.UTF8.GetBytes("AMWD-MBS-RTU");
						result.Add((byte)bytes.Length);
						result.AddRange(bytes);
					}
					break;

				case ModbusDeviceIdentificationObject.MajorMinorRevision:
					{
						string version = GetType().Assembly
							.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
							.InformationalVersion;

						byte[] bytes = Encoding.UTF8.GetBytes(version);
						result.Add((byte)bytes.Length);
						result.AddRange(bytes);
					}
					break;

				case ModbusDeviceIdentificationObject.VendorUrl:
					{
						byte[] bytes = Encoding.UTF8.GetBytes("https://github.com/AM-WD/AMWD.Protocols.Modbus");
						result.Add((byte)bytes.Length);
						result.AddRange(bytes);
					}
					break;

				case ModbusDeviceIdentificationObject.ProductName:
					{
						byte[] bytes = Encoding.UTF8.GetBytes("AM.WD Modbus Library");
						result.Add((byte)bytes.Length);
						result.AddRange(bytes);
					}
					break;

				case ModbusDeviceIdentificationObject.ModelName:
					{
						byte[] bytes = Encoding.UTF8.GetBytes("Serial Server");
						result.Add((byte)bytes.Length);
						result.AddRange(bytes);
					}
					break;

				case ModbusDeviceIdentificationObject.UserApplicationName:
					{
						byte[] bytes = Encoding.UTF8.GetBytes("Modbus RTU Server");
						result.Add((byte)bytes.Length);
						result.AddRange(bytes);
					}
					break;

				default:
					result.Add(0x00);
					break;
			}

			return [.. result];
		}

		private static void SetCrc(byte[] bytes)
		{
			byte[] crc = RtuProtocol.CRC16(bytes, 0, bytes.Length - 2);
			bytes[bytes.Length - 2] = crc[0];
			bytes[bytes.Length - 1] = crc[1];
		}

		private static void AddCrc(List<byte> bytes)
		{
			byte[] crc = RtuProtocol.CRC16(bytes);
			bytes.Add(crc[0]);
			bytes.Add(crc[1]);
		}

		#endregion Request Handling

		#region Device Handling

		/// <summary>
		/// Adds a new device to the server.
		/// </summary>
		/// <param name="unitId">The unit ID of the device.</param>
		/// <returns><see langword="true"/> if the device was added, <see langword="false"/> otherwise.</returns>
		public bool AddDevice(byte unitId)
		{
			Assertions();

			using (_deviceListLock.GetWriteLock())
			{
				if (_devices.ContainsKey(unitId))
					return false;

				_devices.Add(unitId, new ModbusDevice(unitId));
				return true;
			}
		}

		/// <summary>
		/// Removes a device from the server.
		/// </summary>
		/// <param name="unitId">The unit ID of the device.</param>
		/// <returns><see langword="true"/> if the device was removed, <see langword="false"/> otherwise.</returns>
		public bool RemoveDevice(byte unitId)
		{
			Assertions();

			using (_deviceListLock.GetWriteLock())
			{
				if (_devices.TryGetValue(unitId, out var device))
					device.Dispose();

				return _devices.Remove(unitId);
			}
		}

		/// <summary>
		/// Gets a <see cref="Coil"/> from the specified <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="unitId">The unit ID of the device.</param>
		/// <param name="address">The address of the coil.</param>
		public Coil GetCoil(byte unitId, ushort address)
		{
			Assertions();

			using (_deviceListLock.GetReadLock())
			{
				if (!_devices.TryGetValue(unitId, out var device))
					return null;

				return device.GetCoil(address);
			}
		}

		/// <summary>
		/// Sets a <see cref="Coil"/> to the specified <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="unitId">The unit ID of the device.</param>
		/// <param name="coil">The <see cref="Coil"/> to set.</param>
		public void SetCoil(byte unitId, Coil coil)
		{
			Assertions();

			using (_deviceListLock.GetReadLock())
			{
				if (!_devices.TryGetValue(unitId, out var device))
					return;

				device.SetCoil(coil);
			}
		}

		/// <summary>
		/// Gets a <see cref="DiscreteInput"/> from the specified <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="unitId">The unit ID of the device.</param>
		/// <param name="address">The address of the <see cref="DiscreteInput"/>.</param>
		public DiscreteInput GetDiscreteInput(byte unitId, ushort address)
		{
			Assertions();

			using (_deviceListLock.GetReadLock())
			{
				if (!_devices.TryGetValue(unitId, out var device))
					return null;

				return device.GetDiscreteInput(address);
			}
		}

		/// <summary>
		/// Sets a <see cref="DiscreteInput"/> to the specified <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="unitId">The unit ID of the device.</param>
		/// <param name="discreteInput">The <see cref="DiscreteInput"/> to set.</param>
		public void SetDiscreteInput(byte unitId, DiscreteInput discreteInput)
		{
			Assertions();

			using (_deviceListLock.GetReadLock())
			{
				if (!_devices.TryGetValue(unitId, out var device))
					return;

				device.SetDiscreteInput(discreteInput);
			}
		}

		/// <summary>
		/// Gets a <see cref="HoldingRegister"/> from the specified <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="unitId">The unit ID of the device.</param>
		/// <param name="address">The address of the <see cref="HoldingRegister"/>.</param>
		public HoldingRegister GetHoldingRegister(byte unitId, ushort address)
		{
			Assertions();

			using (_deviceListLock.GetReadLock())
			{
				if (!_devices.TryGetValue(unitId, out var device))
					return null;

				return device.GetHoldingRegister(address);
			}
		}

		/// <summary>
		/// Sets a <see cref="HoldingRegister"/> to the specified <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="unitId">The unit ID of the device.</param>
		/// <param name="holdingRegister">The <see cref="HoldingRegister"/> to set.</param>
		public void SetHoldingRegister(byte unitId, HoldingRegister holdingRegister)
		{
			Assertions();

			using (_deviceListLock.GetReadLock())
			{
				if (!_devices.TryGetValue(unitId, out var device))
					return;

				device.SetHoldingRegister(holdingRegister);
			}
		}

		/// <summary>
		/// Gets a <see cref="InputRegister"/> from the specified <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="unitId">The unit ID of the device.</param>
		/// <param name="address">The address of the <see cref="InputRegister"/>.</param>
		public InputRegister GetInputRegister(byte unitId, ushort address)
		{
			Assertions();

			using (_deviceListLock.GetReadLock())
			{
				if (!_devices.TryGetValue(unitId, out var device))
					return null;

				return device.GetInputRegister(address);
			}
		}

		/// <summary>
		/// Sets a <see cref="InputRegister"/> to the specified <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="unitId">The unit ID of the device.</param>
		/// <param name="inputRegister">The <see cref="InputRegister"/> to set.</param>
		public void SetInputRegister(byte unitId, InputRegister inputRegister)
		{
			Assertions();

			using (_deviceListLock.GetReadLock())
			{
				if (!_devices.TryGetValue(unitId, out var device))
					return;

				device.SetInputRegister(inputRegister);
			}
		}

		#endregion Device Handling
	}
}
