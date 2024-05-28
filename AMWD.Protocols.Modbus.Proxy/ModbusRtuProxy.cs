using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AMWD.Protocols.Modbus.Common;
using AMWD.Protocols.Modbus.Common.Contracts;
using AMWD.Protocols.Modbus.Common.Protocols;
using AMWD.Protocols.Modbus.Serial;

namespace AMWD.Protocols.Modbus.Proxy
{
	/// <summary>
	/// Implements a Modbus serial line RTU server proxying all requests to a Modbus client of choice.
	/// </summary>
	public class ModbusRtuProxy : IDisposable
	{
		#region Fields

		private bool _isDisposed;

		private readonly SerialPort _serialPort;
		private CancellationTokenSource _stopCts;

		#endregion Fields

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ModbusRtuProxy"/> class.
		/// </summary>
		/// <param name="client">The <see cref="ModbusClientBase"/> used to request the remote device, that should be proxied.</param>
		/// <param name="portName">The name of the serial port to use.</param>
		/// <param name="baudRate">The baud rate of the serial port (Default: 19.200).</param>
		public ModbusRtuProxy(ModbusClientBase client, string portName, BaudRate baudRate = BaudRate.Baud19200)
		{
			Client = client ?? throw new ArgumentNullException(nameof(client));

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

		#region Properties

		/// <summary>
		/// Gets the Modbus client used to request the remote device, that should be proxied.
		/// </summary>
		public ModbusClientBase Client { get; }

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
		/// Releases all managed and unmanaged resources used by the <see cref="ModbusRtuProxy"/>.
		/// </summary>
		public void Dispose()
		{
			if (_isDisposed)
				return;

			_isDisposed = true;

			StopAsyncInternal(CancellationToken.None).Wait();

			_serialPort.Dispose();
			_stopCts?.Dispose();
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
				byte[] responseBytes = HandleRequest([.. requestBytes]);
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

			switch ((ModbusFunctionCode)requestBytes[1])
			{
				case ModbusFunctionCode.ReadCoils:
					return HandleReadCoilsAsync(requestBytes, _stopCts.Token).Result;

				case ModbusFunctionCode.ReadDiscreteInputs:
					return HandleReadDiscreteInputsAsync(requestBytes, _stopCts.Token).Result;

				case ModbusFunctionCode.ReadHoldingRegisters:
					return HandleReadHoldingRegistersAsync(requestBytes, _stopCts.Token).Result;

				case ModbusFunctionCode.ReadInputRegisters:
					return HandleReadInputRegistersAsync(requestBytes, _stopCts.Token).Result;

				case ModbusFunctionCode.WriteSingleCoil:
					return HandleWriteSingleCoilAsync(requestBytes, _stopCts.Token).Result;

				case ModbusFunctionCode.WriteSingleRegister:
					return HandleWriteSingleRegisterAsync(requestBytes, _stopCts.Token).Result;

				case ModbusFunctionCode.WriteMultipleCoils:
					return HandleWriteMultipleCoilsAsync(requestBytes, _stopCts.Token).Result;

				case ModbusFunctionCode.WriteMultipleRegisters:
					return HandleWriteMultipleRegistersAsync(requestBytes, _stopCts.Token).Result;

				case ModbusFunctionCode.EncapsulatedInterface:
					return HandleEncapsulatedInterfaceAsync(requestBytes, _stopCts.Token).Result;

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

		private async Task<byte[]> HandleReadCoilsAsync(byte[] requestBytes, CancellationToken cancellationToken)
		{
			if (requestBytes.Length < 8)
				return null;

			byte unitId = requestBytes[0];
			ushort firstAddress = requestBytes.GetBigEndianUInt16(2);
			ushort count = requestBytes.GetBigEndianUInt16(4);

			var responseBytes = new List<byte>();
			responseBytes.AddRange(requestBytes.Take(2));
			try
			{
				var coils = await Client.ReadCoilsAsync(unitId, firstAddress, count, cancellationToken);

				byte[] values = new byte[(int)Math.Ceiling(coils.Count / 8.0)];
				for (int i = 0; i < coils.Count; i++)
				{
					if (coils[i].Value)
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

		private async Task<byte[]> HandleReadDiscreteInputsAsync(byte[] requestBytes, CancellationToken cancellationToken)
		{
			if (requestBytes.Length < 8)
				return null;

			byte unitId = requestBytes[0];
			ushort firstAddress = requestBytes.GetBigEndianUInt16(2);
			ushort count = requestBytes.GetBigEndianUInt16(4);

			var responseBytes = new List<byte>();
			responseBytes.AddRange(requestBytes.Take(2));
			try
			{
				var discreteInputs = await Client.ReadDiscreteInputsAsync(unitId, firstAddress, count, cancellationToken);

				byte[] values = new byte[(int)Math.Ceiling(discreteInputs.Count / 8.0)];
				for (int i = 0; i < discreteInputs.Count; i++)
				{
					if (discreteInputs[i].Value)
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

		private async Task<byte[]> HandleReadHoldingRegistersAsync(byte[] requestBytes, CancellationToken cancellationToken)
		{
			if (requestBytes.Length < 8)
				return null;

			byte unitId = requestBytes[0];
			ushort firstAddress = requestBytes.GetBigEndianUInt16(2);
			ushort count = requestBytes.GetBigEndianUInt16(4);

			var responseBytes = new List<byte>();
			responseBytes.AddRange(requestBytes.Take(2));
			try
			{
				var holdingRegisters = await Client.ReadHoldingRegistersAsync(unitId, firstAddress, count, cancellationToken);

				byte[] values = new byte[holdingRegisters.Count * 2];
				for (int i = 0; i < holdingRegisters.Count; i++)
				{
					values[i * 2] = holdingRegisters[i].HighByte;
					values[i * 2 + 1] = holdingRegisters[i].LowByte;
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

		private async Task<byte[]> HandleReadInputRegistersAsync(byte[] requestBytes, CancellationToken cancellationToken)
		{
			if (requestBytes.Length < 8)
				return null;

			byte unitId = requestBytes[0];
			ushort firstAddress = requestBytes.GetBigEndianUInt16(2);
			ushort count = requestBytes.GetBigEndianUInt16(4);

			var responseBytes = new List<byte>();
			responseBytes.AddRange(requestBytes.Take(2));
			try
			{
				var inputRegisters = await Client.ReadInputRegistersAsync(unitId, firstAddress, count, cancellationToken);

				byte[] values = new byte[count * 2];
				for (int i = 0; i < count; i++)
				{
					values[i * 2] = inputRegisters[i].HighByte;
					values[i * 2 + 1] = inputRegisters[i].LowByte;
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

		private async Task<byte[]> HandleWriteSingleCoilAsync(byte[] requestBytes, CancellationToken cancellationToken)
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
				var coil = new Coil
				{
					Address = address,
					HighByte = requestBytes[4],
					LowByte = requestBytes[5],
				};

				bool isSuccess = await Client.WriteSingleCoilAsync(requestBytes[0], coil, cancellationToken);
				if (isSuccess)
				{
					// Response is an echo of the request
					responseBytes.AddRange(requestBytes.Skip(2).Take(4));
				}
				else
				{
					responseBytes[1] |= 0x80;
					responseBytes.Add((byte)ModbusErrorCode.SlaveDeviceFailure);
				}
			}
			catch
			{
				responseBytes[1] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.SlaveDeviceFailure);
			}

			AddCrc(responseBytes);
			return [.. responseBytes];
		}

		private async Task<byte[]> HandleWriteSingleRegisterAsync(byte[] requestBytes, CancellationToken cancellationToken)
		{
			if (requestBytes.Length < 8)
				return null;

			var responseBytes = new List<byte>();
			responseBytes.AddRange(requestBytes.Take(2));

			ushort address = requestBytes.GetBigEndianUInt16(2);
			try
			{
				var register = new HoldingRegister
				{
					Address = address,
					HighByte = requestBytes[4],
					LowByte = requestBytes[5]
				};

				bool isSuccess = await Client.WriteSingleHoldingRegisterAsync(requestBytes[0], register, cancellationToken);
				if (isSuccess)
				{
					// Response is an echo of the request
					responseBytes.AddRange(requestBytes.Skip(2).Take(4));
				}
				else
				{
					responseBytes[1] |= 0x80;
					responseBytes.Add((byte)ModbusErrorCode.SlaveDeviceFailure);
				}
			}
			catch
			{
				responseBytes[1] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.SlaveDeviceFailure);
			}

			AddCrc(responseBytes);
			return [.. responseBytes];
		}

		private async Task<byte[]> HandleWriteMultipleCoilsAsync(byte[] requestBytes, CancellationToken cancellationToken)
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
				var coils = new List<Coil>();
				for (int i = 0; i < count; i++)
				{
					int bytePosition = i / 8;
					int bitPosition = i % 8;

					ushort address = (ushort)(firstAddress + i);
					bool value = (requestBytes[baseOffset + bytePosition] & (1 << bitPosition)) > 0;

					coils.Add(new Coil
					{
						Address = address,
						HighByte = value ? (byte)0xFF : (byte)0x00
					});
				}

				bool isSuccess = await Client.WriteMultipleCoilsAsync(requestBytes[0], coils, cancellationToken);
				if (isSuccess)
				{
					// Response is an echo of the request
					responseBytes.AddRange(requestBytes.Skip(2).Take(4));
				}
				else
				{
					responseBytes[1] |= 0x80;
					responseBytes.Add((byte)ModbusErrorCode.SlaveDeviceFailure);
				}
			}
			catch
			{
				responseBytes[1] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.SlaveDeviceFailure);
			}

			AddCrc(responseBytes);
			return [.. responseBytes];
		}

		private async Task<byte[]> HandleWriteMultipleRegistersAsync(byte[] requestBytes, CancellationToken cancellationToken)
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
				var list = new List<HoldingRegister>();
				for (int i = 0; i < count; i++)
				{
					ushort address = (ushort)(firstAddress + i);

					list.Add(new HoldingRegister
					{
						Address = address,
						HighByte = requestBytes[baseOffset + i * 2],
						LowByte = requestBytes[baseOffset + i * 2 + 1]
					});

					bool isSuccess = await Client.WriteMultipleHoldingRegistersAsync(requestBytes[0], list, cancellationToken);
					if (isSuccess)
					{
						// Response is an echo of the request
						responseBytes.AddRange(requestBytes.Skip(2).Take(4));
					}
					else
					{
						responseBytes[1] |= 0x80;
						responseBytes.Add((byte)ModbusErrorCode.SlaveDeviceFailure);
					}
				}
			}
			catch
			{
				responseBytes[1] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.SlaveDeviceFailure);
			}

			AddCrc(responseBytes);
			return [.. responseBytes];
		}

		private async Task<byte[]> HandleEncapsulatedInterfaceAsync(byte[] requestBytes, CancellationToken cancellationToken)
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

			var firstObject = (ModbusDeviceIdentificationObject)requestBytes[4];
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
				var res = await Client.ReadDeviceIdentificationAsync(requestBytes[6], category, firstObject, cancellationToken);

				var bodyBytes = new List<byte>();

				// MEI, Category
				bodyBytes.AddRange(requestBytes.Skip(2).Take(2));

				// Conformity
				bodyBytes.Add((byte)category);
				if (res.IsIndividualAccessAllowed)
					bodyBytes[2] |= 0x80;

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
						maxObjectId = requestBytes[4];
						break;
				}

				byte numberOfObjects = 0;
				for (int i = requestBytes[4]; i <= maxObjectId; i++)
				{
					// Reserved
					if (0x07 <= i && i <= 0x7F)
						continue;

					byte[] objBytes = GetDeviceObject((byte)i, res);

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

		private byte[] GetDeviceObject(byte objectId, DeviceIdentification deviceIdentification)
		{
			var result = new List<byte> { objectId };
			switch ((ModbusDeviceIdentificationObject)objectId)
			{
				case ModbusDeviceIdentificationObject.VendorName:
					{
						byte[] bytes = Encoding.UTF8.GetBytes(deviceIdentification.VendorName);
						result.Add((byte)bytes.Length);
						result.AddRange(bytes);
					}
					break;

				case ModbusDeviceIdentificationObject.ProductCode:
					{
						byte[] bytes = Encoding.UTF8.GetBytes(deviceIdentification.ProductCode);
						result.Add((byte)bytes.Length);
						result.AddRange(bytes);
					}
					break;

				case ModbusDeviceIdentificationObject.MajorMinorRevision:
					{
						byte[] bytes = Encoding.UTF8.GetBytes(deviceIdentification.MajorMinorRevision);
						result.Add((byte)bytes.Length);
						result.AddRange(bytes);
					}
					break;

				case ModbusDeviceIdentificationObject.VendorUrl:
					{
						byte[] bytes = Encoding.UTF8.GetBytes(deviceIdentification.VendorUrl);
						result.Add((byte)bytes.Length);
						result.AddRange(bytes);
					}
					break;

				case ModbusDeviceIdentificationObject.ProductName:
					{
						byte[] bytes = Encoding.UTF8.GetBytes(deviceIdentification.ProductName);
						result.Add((byte)bytes.Length);
						result.AddRange(bytes);
					}
					break;

				case ModbusDeviceIdentificationObject.ModelName:
					{
						byte[] bytes = Encoding.UTF8.GetBytes(deviceIdentification.ModelName);
						result.Add((byte)bytes.Length);
						result.AddRange(bytes);
					}
					break;

				case ModbusDeviceIdentificationObject.UserApplicationName:
					{
						byte[] bytes = Encoding.UTF8.GetBytes(deviceIdentification.UserApplicationName);
						result.Add((byte)bytes.Length);
						result.AddRange(bytes);
					}
					break;

				default:
					{
						if (deviceIdentification.ExtendedObjects.ContainsKey(objectId))
						{
							byte[] bytes = deviceIdentification.ExtendedObjects[objectId];
							result.Add((byte)bytes.Length);
							result.AddRange(bytes);
						}
						else
						{
							result.Add(0x00);
						}
					}
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
	}
}
