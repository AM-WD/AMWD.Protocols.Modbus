using System;
using System.Collections.Generic;
using System.Linq;
using AMWD.Protocols.Modbus.Common.Contracts;

namespace AMWD.Protocols.Modbus.Common.Protocols
{
	/// <summary>
	/// Default implementation of the Modbus RTU protocol.
	/// </summary>
	public class RtuProtocol : IModbusProtocol
	{
		#region Constants

		/// <summary>
		/// The minimum allowed unit id specified by the Modbus SerialLine protocol.
		/// </summary>
		/// <remarks>
		/// <strong>INFORMATION:</strong>
		/// <br/>
		/// Reading the specification, the minimum allowed unit ID would be <strong>1</strong>.
		/// <br/>
		/// As of other implementations seen, this limit is <em>not</em> enforced!
		/// </remarks>
		public const byte MIN_UNIT_ID = 0x01;

		/// <summary>
		/// The maximum allowed unit id specified by the Modbus SerialLine protocol.
		/// </summary>
		/// <remarks>
		/// Reading the specification, the max allowed unit id would be <strong>247</strong>!
		/// </remarks>
		public const byte MAX_UNIT_ID = 0xF7;

		/// <summary>
		/// The minimum allowed read count specified by the Modbus SerialLine protocol.
		/// </summary>
		public const ushort MIN_READ_COUNT = 0x01;

		/// <summary>
		/// The minimum allowed write count specified by the Modbus SerialLine protocol.
		/// </summary>
		public const ushort MIN_WRITE_COUNT = 0x01;

		/// <summary>
		/// The maximum allowed read count for discrete values specified by the Modbus SerialLine protocol.
		/// </summary>
		public const ushort MAX_DISCRETE_READ_COUNT = 0x07D0; // 2000

		/// <summary>
		/// The maximum allowed write count for discrete values specified by the Modbus SerialLine protocol.
		/// </summary>
		public const ushort MAX_DISCRETE_WRITE_COUNT = 0x07B0; // 1968

		/// <summary>
		/// The maximum allowed read count for registers specified by the Modbus SerialLine protocol.
		/// </summary>
		public const ushort MAX_REGISTER_READ_COUNT = 0x007D; // 125

		/// <summary>
		/// The maximum allowed write count for registers specified by the Modbus SerialLine protocol.
		/// </summary>
		public const ushort MAX_REGISTER_WRITE_COUNT = 0x007B; // 123

		/// <summary>
		/// The maximum allowed ADU length in bytes.
		/// </summary>
		/// <remarks>
		/// A Modbus frame consists of a PDU (protcol data unit) and additional protocol addressing / error checks.
		/// The whole data frame is called ADU (application data unit).
		/// </remarks>
		public const int MAX_ADU_LENGTH = 256; // bytes

		#endregion Constants

		/// <inheritdoc/>
		public string Name => "RTU";

		#region Read

		/// <inheritdoc/>
		public IReadOnlyList<byte> SerializeReadCoils(byte unitId, ushort startAddress, ushort count)
		{
			if (count < MIN_READ_COUNT || MAX_DISCRETE_READ_COUNT < count)
				throw new ArgumentOutOfRangeException(nameof(count));

			if (ushort.MaxValue < (startAddress + count - 1))
				throw new ArgumentOutOfRangeException(nameof(count), $"Combination of {nameof(startAddress)} and {nameof(count)} exceeds the addressation limit of {ushort.MaxValue}");

			byte[] request = new byte[8];

			// Unit Id
			request[0] = unitId;

			// Function code
			request[1] = (byte)ModbusFunctionCode.ReadCoils;

			// Starting address
			byte[] addrBytes = startAddress.ToBigEndianBytes();
			request[2] = addrBytes[0];
			request[3] = addrBytes[1];

			// Quantity
			byte[] countBytes = count.ToBigEndianBytes();
			request[4] = countBytes[0];
			request[5] = countBytes[1];

			// CRC
			byte[] crc = CRC16(request, 0, 6);
			request[6] = crc[0];
			request[7] = crc[1];

			return request;
		}

		/// <inheritdoc/>
		public IReadOnlyList<Coil> DeserializeReadCoils(IReadOnlyList<byte> response)
		{
			int baseOffset = 3;
			if (response[2] != response.Count - baseOffset - 2) // -2 for CRC
				throw new ModbusException("Coil byte count does not match.");

			int count = response[2] * 8;
			var coils = new List<Coil>();
			for (int i = 0; i < count; i++)
			{
				int bytePosition = i / 8;
				int bitPosition = i % 8;

				int value = response[baseOffset + bytePosition] & (1 << bitPosition);
				coils.Add(new Coil
				{
					Address = (ushort)i,
					Value = value > 0
				});
			}

			return coils;
		}

		/// <inheritdoc/>
		public IReadOnlyList<byte> SerializeReadDiscreteInputs(byte unitId, ushort startAddress, ushort count)
		{
			if (count < MIN_READ_COUNT || MAX_DISCRETE_READ_COUNT < count)
				throw new ArgumentOutOfRangeException(nameof(count));

			if (ushort.MaxValue < (startAddress + count - 1))
				throw new ArgumentOutOfRangeException(nameof(count), $"Combination of {nameof(startAddress)} and {nameof(count)} exceeds the addressation limit of {ushort.MaxValue}");

			byte[] request = new byte[8];

			// Unit Id
			request[0] = unitId;

			// Function code
			request[1] = (byte)ModbusFunctionCode.ReadDiscreteInputs;

			// Starting address
			byte[] addrBytes = startAddress.ToBigEndianBytes();
			request[2] = addrBytes[0];
			request[3] = addrBytes[1];

			// Quantity
			byte[] countBytes = count.ToBigEndianBytes();
			request[4] = countBytes[0];
			request[5] = countBytes[1];

			// CRC
			byte[] crc = CRC16(request, 0, 6);
			request[6] = crc[0];
			request[7] = crc[1];

			return request;
		}

		/// <inheritdoc/>
		public IReadOnlyList<DiscreteInput> DeserializeReadDiscreteInputs(IReadOnlyList<byte> response)
		{
			int baseOffset = 3;
			if (response[2] != response.Count - baseOffset - 2) // -2 for CRC
				throw new ModbusException("Discrete input byte count does not match.");

			int count = response[2] * 8;
			var discreteInputs = new List<DiscreteInput>();
			for (int i = 0; i < count; i++)
			{
				int bytePosition = i / 8;
				int bitPosition = i % 8;

				int value = response[baseOffset + bytePosition] & (1 << bitPosition);
				discreteInputs.Add(new DiscreteInput
				{
					Address = (ushort)i,
					HighByte = (byte)(value > 0 ? 0xFF : 0x00)
				});
			}

			return discreteInputs;
		}

		/// <inheritdoc/>
		public IReadOnlyList<byte> SerializeReadHoldingRegisters(byte unitId, ushort startAddress, ushort count)
		{
			if (count < MIN_READ_COUNT || MAX_REGISTER_READ_COUNT < count)
				throw new ArgumentOutOfRangeException(nameof(count));

			if (ushort.MaxValue < (startAddress + count - 1))
				throw new ArgumentOutOfRangeException(nameof(count), $"Combination of {nameof(startAddress)} and {nameof(count)} exceeds the addressation limit of {ushort.MaxValue}");

			byte[] request = new byte[8];

			// Unit Id
			request[0] = unitId;

			// Function code
			request[1] = (byte)ModbusFunctionCode.ReadHoldingRegisters;

			// Starting address
			byte[] addrBytes = startAddress.ToBigEndianBytes();
			request[2] = addrBytes[0];
			request[3] = addrBytes[1];

			// Quantity
			byte[] countBytes = count.ToBigEndianBytes();
			request[4] = countBytes[0];
			request[5] = countBytes[1];

			// CRC
			byte[] crc = CRC16(request, 0, 6);
			request[6] = crc[0];
			request[7] = crc[1];

			return request;
		}

		/// <inheritdoc/>
		public IReadOnlyList<HoldingRegister> DeserializeReadHoldingRegisters(IReadOnlyList<byte> response)
		{
			int baseOffset = 3;
			if (response[2] != response.Count - baseOffset - 2)
				throw new ModbusException("Holding register byte count does not match.");

			int count = response[2] / 2;
			var holdingRegisters = new List<HoldingRegister>();
			for (int i = 0; i < count; i++)
			{
				holdingRegisters.Add(new HoldingRegister
				{
					Address = (ushort)i,
					HighByte = response[baseOffset + i * 2],
					LowByte = response[baseOffset + i * 2 + 1]
				});
			}

			return holdingRegisters;
		}

		/// <inheritdoc/>
		public IReadOnlyList<byte> SerializeReadInputRegisters(byte unitId, ushort startAddress, ushort count)
		{
			if (count < MIN_READ_COUNT || MAX_REGISTER_READ_COUNT < count)
				throw new ArgumentOutOfRangeException(nameof(count));

			if (ushort.MaxValue < (startAddress + count - 1))
				throw new ArgumentOutOfRangeException(nameof(count), $"Combination of {nameof(startAddress)} and {nameof(count)} exceeds the addressation limit of {ushort.MaxValue}");

			byte[] request = new byte[8];

			// Unit Id
			request[0] = unitId;

			// Function code
			request[1] = (byte)ModbusFunctionCode.ReadInputRegisters;

			// Starting address
			byte[] addrBytes = startAddress.ToBigEndianBytes();
			request[2] = addrBytes[0];
			request[3] = addrBytes[1];

			// Quantity
			byte[] countBytes = count.ToBigEndianBytes();
			request[4] = countBytes[0];
			request[5] = countBytes[1];

			// CRC
			byte[] crc = CRC16(request, 0, 6);
			request[6] = crc[0];
			request[7] = crc[1];

			return request;
		}

		/// <inheritdoc/>
		public IReadOnlyList<InputRegister> DeserializeReadInputRegisters(IReadOnlyList<byte> response)
		{
			int baseOffset = 3;
			if (response[2] != response.Count - baseOffset - 2)
				throw new ModbusException("Input register byte count does not match.");

			int count = response[2] / 2;
			var inputRegisters = new List<InputRegister>();
			for (int i = 0; i < count; i++)
			{
				inputRegisters.Add(new InputRegister
				{
					Address = (ushort)i,
					HighByte = response[baseOffset + i * 2],
					LowByte = response[baseOffset + i * 2 + 1]
				});
			}

			return inputRegisters;
		}

		/// <inheritdoc/>
		public IReadOnlyList<byte> SerializeReadDeviceIdentification(byte unitId, ModbusDeviceIdentificationCategory category, ModbusDeviceIdentificationObject objectId)
		{
			if (!Enum.IsDefined(typeof(ModbusDeviceIdentificationCategory), category))
				throw new ArgumentOutOfRangeException(nameof(category));

			byte[] request = new byte[7];

			// Unit Id
			request[0] = unitId;

			// Function code
			request[1] = (byte)ModbusFunctionCode.EncapsulatedInterface;

			// Modbus Encapsulated Interface: Read Device Identification (MEI Type)
			request[2] = 0x0E;

			// The category type (basic, regular, extended, individual)
			request[3] = (byte)category;
			request[4] = (byte)objectId;

			// CRC
			byte[] crc = CRC16(request, 0, 5);
			request[5] = crc[0];
			request[6] = crc[1];

			return request;
		}

		/// <inheritdoc/>
		public DeviceIdentificationRaw DeserializeReadDeviceIdentification(IReadOnlyList<byte> response)
		{
			if (response[2] != 0x0E)
				throw new ModbusException("The MEI type does not match");

			if (!Enum.IsDefined(typeof(ModbusDeviceIdentificationCategory), response[3]))
				throw new ModbusException("The category type does not match");

			var deviceIdentification = new DeviceIdentificationRaw
			{
				AllowsIndividualAccess = (response[4] & 0x80) == 0x80,
				MoreRequestsNeeded = response[5] == 0xFF,
				NextObjectIdToRequest = response[6],
			};

			int baseOffset = 8;
			while (baseOffset < response.Count - 2) // -2 for CRC
			{
				byte objectId = response[baseOffset];
				byte length = response[baseOffset + 1];

				byte[] data = response.Skip(baseOffset + 2).Take(length).ToArray();

				deviceIdentification.Objects.Add(objectId, data);
				baseOffset += 2 + length;
			}

			return deviceIdentification;
		}

		#endregion Read

		#region Write

		/// <inheritdoc/>
		public IReadOnlyList<byte> SerializeWriteSingleCoil(byte unitId, Coil coil)
		{
#if NET8_0_OR_GREATER
			ArgumentNullException.ThrowIfNull(coil);
#else
			if (coil == null)
				throw new ArgumentNullException(nameof(coil));
#endif

			byte[] request = new byte[8];

			// Unit ID
			request[0] = unitId;

			// Function code
			request[1] = (byte)ModbusFunctionCode.WriteSingleCoil;

			byte[] addrBytes = coil.Address.ToBigEndianBytes();
			request[2] = addrBytes[0];
			request[3] = addrBytes[1];

			request[4] = coil.HighByte;
			request[5] = coil.LowByte;

			// CRC
			byte[] crc = CRC16(request, 0, 6);
			request[6] = crc[0];
			request[7] = crc[1];

			return request;
		}

		/// <inheritdoc/>
		public Coil DeserializeWriteSingleCoil(IReadOnlyList<byte> response)
		{
			return new Coil
			{
				Address = response.ToArray().GetBigEndianUInt16(2),
				HighByte = response[4],
				LowByte = response[5]
			};
		}

		/// <inheritdoc/>
		public IReadOnlyList<byte> SerializeWriteSingleHoldingRegister(byte unitId, HoldingRegister register)
		{
#if NET8_0_OR_GREATER
			ArgumentNullException.ThrowIfNull(register);
#else
			if (register == null)
				throw new ArgumentNullException(nameof(register));
#endif

			byte[] request = new byte[8];

			// Unit Id
			request[0] = unitId;

			// Function code
			request[1] = (byte)ModbusFunctionCode.WriteSingleRegister;

			byte[] addrBytes = register.Address.ToBigEndianBytes();
			request[2] = addrBytes[0];
			request[3] = addrBytes[1];

			request[4] = register.HighByte;
			request[5] = register.LowByte;

			// CRC
			byte[] crc = CRC16(request, 0, 6);
			request[6] = crc[0];
			request[7] = crc[1];

			return request;
		}

		/// <inheritdoc/>
		public HoldingRegister DeserializeWriteSingleHoldingRegister(IReadOnlyList<byte> response)
		{
			return new HoldingRegister
			{
				Address = response.ToArray().GetBigEndianUInt16(2),
				HighByte = response[4],
				LowByte = response[5]
			};
		}

		/// <inheritdoc/>
		public IReadOnlyList<byte> SerializeWriteMultipleCoils(byte unitId, IReadOnlyList<Coil> coils)
		{
#if NET8_0_OR_GREATER
			ArgumentNullException.ThrowIfNull(coils);
#else
			if (coils == null)
				throw new ArgumentNullException(nameof(coils));
#endif

			var orderedList = coils.OrderBy(c => c.Address).ToList();
			if (orderedList.Count < MIN_WRITE_COUNT || MAX_DISCRETE_WRITE_COUNT < orderedList.Count)
				throw new ArgumentOutOfRangeException(nameof(coils), $"At least {MIN_WRITE_COUNT} or max. {MAX_DISCRETE_WRITE_COUNT} coils can be written at once.");

			int addrCount = coils.Select(c => c.Address).Distinct().Count();
			if (orderedList.Count != addrCount)
				throw new ArgumentException("One or more duplicate coils found.", nameof(coils));

			ushort firstAddress = orderedList.First().Address;
			ushort lastAddress = orderedList.Last().Address;

			if (firstAddress + orderedList.Count - 1 != lastAddress)
				throw new ArgumentException("Gap in coil list found.", nameof(coils));

			byte byteCount = (byte)Math.Ceiling(orderedList.Count / 8.0);
			byte[] request = new byte[9 + byteCount];

			request[0] = unitId;

			request[1] = (byte)ModbusFunctionCode.WriteMultipleCoils;

			byte[] addrBytes = firstAddress.ToBigEndianBytes();
			request[2] = addrBytes[0];
			request[3] = addrBytes[1];

			byte[] countBytes = ((ushort)orderedList.Count).ToBigEndianBytes();
			request[4] = countBytes[0];
			request[5] = countBytes[1];

			request[6] = byteCount;

			int baseOffset = 7;
			for (int i = 0; i < orderedList.Count; i++)
			{
				int bytePosition = i / 8;
				int bitPosition = i % 8;

				if (orderedList[i].Value)
				{
					byte bitMask = (byte)(1 << bitPosition);
					request[baseOffset + bytePosition] |= bitMask;
				}
			}

			// CRC
			byte[] crc = CRC16(request, 0, request.Length - 2);
			request[request.Length - 2] = crc[0];
			request[request.Length - 1] = crc[1];

			return request;
		}

		/// <inheritdoc/>
		public (ushort FirstAddress, ushort NumberOfCoils) DeserializeWriteMultipleCoils(IReadOnlyList<byte> response)
		{
			ushort firstAddress = response.ToArray().GetBigEndianUInt16(2);
			ushort numberOfCoils = response.ToArray().GetBigEndianUInt16(4);

			return (firstAddress, numberOfCoils);
		}

		/// <inheritdoc/>
		public IReadOnlyList<byte> SerializeWriteMultipleHoldingRegisters(byte unitId, IReadOnlyList<HoldingRegister> registers)
		{
#if NET8_0_OR_GREATER
			ArgumentNullException.ThrowIfNull(registers);
#else
			if (registers == null)
				throw new ArgumentNullException(nameof(registers));
#endif

			var orderedList = registers.OrderBy(c => c.Address).ToList();
			if (orderedList.Count < MIN_WRITE_COUNT || MAX_REGISTER_WRITE_COUNT < orderedList.Count)
				throw new ArgumentOutOfRangeException(nameof(registers), $"At least {MIN_WRITE_COUNT} or max. {MAX_REGISTER_WRITE_COUNT} holding registers can be written at once.");

			int addrCount = registers.Select(c => c.Address).Distinct().Count();
			if (orderedList.Count != addrCount)
				throw new ArgumentException("One or more duplicate holding registers found.", nameof(registers));

			ushort firstAddress = orderedList.First().Address;
			ushort lastAddress = orderedList.Last().Address;

			if (firstAddress + orderedList.Count - 1 != lastAddress)
				throw new ArgumentException("Gap in holding register list found.", nameof(registers));

			byte byteCount = (byte)(orderedList.Count * 2);
			byte[] request = new byte[9 + byteCount];

			request[0] = unitId;
			request[1] = (byte)ModbusFunctionCode.WriteMultipleRegisters;

			byte[] addrBytes = firstAddress.ToBigEndianBytes();
			request[2] = addrBytes[0];
			request[3] = addrBytes[1];

			byte[] countBytes = ((ushort)orderedList.Count).ToBigEndianBytes();
			request[4] = countBytes[0];
			request[5] = countBytes[1];

			request[6] = byteCount;

			int baseOffset = 7;
			for (int i = 0; i < orderedList.Count; i++)
			{
				request[baseOffset + 2 * i] = orderedList[i].HighByte;
				request[baseOffset + 2 * i + 1] = orderedList[i].LowByte;
			}

			// CRC
			byte[] crc = CRC16(request, 0, request.Length - 2);
			request[request.Length - 2] = crc[0];
			request[request.Length - 1] = crc[1];

			return request;
		}

		/// <inheritdoc/>
		public (ushort FirstAddress, ushort NumberOfRegisters) DeserializeWriteMultipleHoldingRegisters(IReadOnlyList<byte> response)
		{
			ushort firstAddress = response.ToArray().GetBigEndianUInt16(2);
			ushort numberOfRegisters = response.ToArray().GetBigEndianUInt16(4);

			return (firstAddress, numberOfRegisters);
		}

		#endregion Write

		#region Validation

		/// <inheritdoc/>
		public bool CheckResponseComplete(IReadOnlyList<byte> responseBytes)
		{
			// Minimum requirement => Unit ID, Function code and at least 2x CRC
			if (responseBytes.Count < 4)
				return false;

			// Response is error response
			if ((responseBytes[1] & 0x80) == 0x80)
			{
				// Unit ID, Function Code, ErrorCode, 2x CRC
				if (responseBytes.Count < 5)
					return false;

				// On error, skip any other evaluation
				return true;
			}

			// Read responses
			// - 0x01 Read Coils
			// - 0x02 Read Discrete Inputs
			// - 0x03 Read Holding Registers
			// - 0x04 Read Input Registers
			// do have a "following bytes" at position 3
			if (new[] { 0x01, 0x02, 0x03, 0x04 }.Contains(responseBytes[1]))
			{
				// Unit ID, Function Code, ByteCount, 2x CRC and length of ByteCount
				if (responseBytes.Count < 5 + responseBytes[2])
					return false;
			}

			// - 0x05 Write Single Coil
			// - 0x06 Write Single Register
			// - 0x0F Write Multiple Coils
			// - 0x10 Write Multiple Registers
			if (new[] { 0x05, 0x06, 0x0F, 0x10 }.Contains(responseBytes[1]))
			{
				// Write Single => Unit ID, Function code, 2x Address, 2x Value, 2x CRC
				// Write Multi  => Unit ID, Function code, 2x Address, 2x QuantityWritten, 2x CRC
				if (responseBytes.Count < 8)
					return false;
			}

			// 0x2B Read Device Identification
			if (responseBytes[1] == 0x2B)
			{
				// [0] 1x Unit ID
				// [1] 1x Function code
				// [2] 1x MEI Type
				// [3] 1x Category
				// [4] 1x Conformity Level
				// [5] 1x More Follows
				// [6] 1x Next Object ID
				// [7] 1x NumberOfObjects
				// ----- repeat NumberOfObjects times
				// 1x Object ID
				// 1x length N
				// Nx data
				// -----
				// 2x CRC

				if (responseBytes.Count < 8)
					return false;

				byte numberOfObjects = responseBytes[7];
				if (numberOfObjects == 0)
				{
					if (responseBytes.Count < 10)
						return false;

					return true;
				}

				int offset = 8;
				for (int i = 0; i < numberOfObjects; i++)
				{
					offset++; // object id
					byte length = responseBytes[offset++];
					offset += length; // data

					// 2x CRC or next object ID and length
					if (responseBytes.Count < offset + 2)
						return false;
				}
			}

			return true;
		}

		/// <inheritdoc/>
		public void ValidateResponse(IReadOnlyList<byte> request, IReadOnlyList<byte> response)
		{
			if (request[0] != response[0])
				throw new ModbusException("Unit Identifier does not match.");

			byte[] calculatedCrc16 = CRC16(response, 0, response.Count - 2);
			byte[] receivedCrc16 = [response[response.Count - 2], response[response.Count - 1]];

			if (calculatedCrc16[0] != receivedCrc16[0] || calculatedCrc16[1] != receivedCrc16[1])
				throw new ModbusException("CRC16 check failed.");

			byte fnCode = response[1];
			bool isError = (fnCode & 0x80) == 0x80;
			if (isError)
				fnCode = (byte)(fnCode ^ 0x80); // === fnCode & 0x7F

			if (request[1] != fnCode)
				throw new ModbusException("Function code does not match.");

			if (isError)
				throw new ModbusException("Remote Error") { ErrorCode = (ModbusErrorCode)response[2] };

			if (new[] { 0x01, 0x02, 0x03, 0x04 }.Contains(fnCode))
			{
				if (response.Count != 5 + response[2])
					throw new ModbusException("Number of following bytes does not match.");
			}

			if (new[] { 0x05, 0x06, 0x0F, 0x10 }.Contains(fnCode))
			{
				if (response.Count != 8)
					throw new ModbusException("Number of bytes does not match.");
			}

			// TODO: Do we want to check 0x2B too?
		}

		/// <summary>
		/// Calculate CRC16 for Modbus RTU.
		/// </summary>
		/// <remarks>
		/// The CRC 16 calculation algorithm is defined in the Modbus serial line specification.
		/// See <see href="https://modbus.org/docs/Modbus_over_serial_line_V1_02.pdf">Modbus over Serial Line v1.02</see>, Appendix B, page 40.
		/// </remarks>
		/// <param name="bytes">The message bytes.</param>
		/// <param name="start">The start index.</param>
		/// <param name="length">The number of bytes to calculate.</param>
		public static byte[] CRC16(IReadOnlyList<byte> bytes, int start = 0, int? length = null)
		{
			if (bytes == null || bytes.Count == 0)
				throw new ArgumentNullException(nameof(bytes));

			if (start < 0 || start >= bytes.Count)
				throw new ArgumentOutOfRangeException(nameof(start));

			length ??= bytes.Count - start;

			if (length <= 0 || start + length > bytes.Count)
				throw new ArgumentOutOfRangeException(nameof(length));

			byte lsb;
			ushort crc16 = 0xFFFF;
			for (int i = start; i < start + length; i++)
			{
				crc16 = (ushort)(crc16 ^ bytes[i]);
				for (int j = 0; j < 8; j++)
				{
					lsb = (byte)(crc16 & 0x0001);
					crc16 = (ushort)(crc16 >> 1);

					if (lsb == 0x01)
						crc16 = (ushort)(crc16 ^ 0xA001);
				}
			}

			return [(byte)crc16, (byte)(crc16 >> 8)];
		}

		#endregion Validation
	}
}
