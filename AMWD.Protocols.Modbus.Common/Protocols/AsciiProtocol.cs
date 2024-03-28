using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AMWD.Protocols.Modbus.Common.Contracts;

namespace AMWD.Protocols.Modbus.Common.Protocols
{
	/// <summary>
	/// Default implementation of the Modbus ASCII protocol.
	/// </summary>
	public class AsciiProtocol : IModbusProtocol
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
		public const byte MAX_UNIT_ID = 0xFF;

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
		/// The maximum allowed ADU length in chars.
		/// </summary>
		/// <remarks>
		/// A Modbus frame consists of a PDU (protcol data unit) and additional protocol addressing / error checks.
		/// The whole data frame is called ADU (application data unit).
		/// </remarks>
		public const int MAX_ADU_LENGTH = 513; // chars in ASCII (so bytes in the end)

		#endregion Constants

		/// <inheritdoc/>
		public string Name => "ASCII";

		#region Read

		/// <inheritdoc/>
		public IReadOnlyList<byte> SerializeReadCoils(byte unitId, ushort startAddress, ushort count)
		{
			if (count < MIN_READ_COUNT || MAX_DISCRETE_READ_COUNT < count)
				throw new ArgumentOutOfRangeException(nameof(count));

			if (ushort.MaxValue < (startAddress + count - 1))
				throw new ArgumentOutOfRangeException(nameof(count), $"Combination of {nameof(startAddress)} and {nameof(count)} exceeds the addressation limit of {ushort.MaxValue}");

			// Unit Id and Function code
			string request = $":{unitId:X2}{(byte)ModbusFunctionCode.ReadCoils:X2}";

			// Starting address
			byte[] addrBytes = startAddress.ToBigEndianBytes();
			request += $"{addrBytes[0]:X2}{addrBytes[1]:X2}";

			// Quantity
			byte[] countBytes = count.ToBigEndianBytes();
			request += $"{countBytes[0]:X2}{countBytes[1]:X2}";

			// LRC
			string lrc = LRC(request);
			request += lrc;

			// CRLF
			request += "\r\n";

			return Encoding.ASCII.GetBytes(request);
		}

		/// <inheritdoc/>
		public IReadOnlyList<Coil> DeserializeReadCoils(IReadOnlyList<byte> response)
		{
			string responseMessage = Encoding.ASCII.GetString([.. response]).ToUpper();

			byte numBytes = HexToByte(responseMessage.Substring(5, 2));
			byte[] responsePayloadBytes = HexStringToByteArray(responseMessage.Substring(7, responseMessage.Length - 11));

			if (numBytes != responsePayloadBytes.Length)
				throw new ModbusException("Coil byte count does not match.");

			int count = numBytes * 8;
			var coils = new List<Coil>();
			for (int i = 0; i < count; i++)
			{
				int bytePosition = i / 8;
				int bitPosition = i % 8;

				int value = responsePayloadBytes[bytePosition] & (1 << bitPosition);
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

			// Unit Id and Function code
			string request = $":{unitId:X2}{(byte)ModbusFunctionCode.ReadDiscreteInputs:X2}";

			// Starting address
			byte[] addrBytes = startAddress.ToBigEndianBytes();
			request += $"{addrBytes[0]:X2}{addrBytes[1]:X2}";

			// Quantity
			byte[] countBytes = count.ToBigEndianBytes();
			request += $"{countBytes[0]:X2}{countBytes[1]:X2}";

			// LRC
			string lrc = LRC(request);
			request += lrc;

			// CRLF
			request += "\r\n";

			return Encoding.ASCII.GetBytes(request);
		}

		/// <inheritdoc/>
		public IReadOnlyList<DiscreteInput> DeserializeReadDiscreteInputs(IReadOnlyList<byte> response)
		{
			string responseMessage = Encoding.ASCII.GetString([.. response]).ToUpper();

			byte numBytes = HexToByte(responseMessage.Substring(5, 2));
			byte[] responsePayloadBytes = HexStringToByteArray(responseMessage.Substring(7, responseMessage.Length - 11));

			if (numBytes != responsePayloadBytes.Length)
				throw new ModbusException("Discrete input byte count does not match.");

			int count = numBytes * 8;
			var discreteInputs = new List<DiscreteInput>();
			for (int i = 0; i < count; i++)
			{
				int bytePosition = i / 8;
				int bitPosition = i % 8;

				int value = responsePayloadBytes[bytePosition] & (1 << bitPosition);
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

			string request = $":{unitId:X2}{(byte)ModbusFunctionCode.ReadHoldingRegisters:X2}";

			// Starting address
			byte[] addrBytes = startAddress.ToBigEndianBytes();
			request += $"{addrBytes[0]:X2}{addrBytes[1]:X2}";

			// Quantity
			byte[] countBytes = count.ToBigEndianBytes();
			request += $"{countBytes[0]:X2}{countBytes[1]:X2}";

			// LRC
			string lrc = LRC(request);
			request += lrc;

			// CRLF
			request += "\r\n";

			return Encoding.ASCII.GetBytes(request);
		}

		/// <inheritdoc/>
		public IReadOnlyList<HoldingRegister> DeserializeReadHoldingRegisters(IReadOnlyList<byte> response)
		{
			string responseMessage = Encoding.ASCII.GetString([.. response]).ToUpper();

			byte numBytes = HexToByte(responseMessage.Substring(5, 2));
			byte[] responsePayloadBytes = HexStringToByteArray(responseMessage.Substring(7, responseMessage.Length - 11));

			if (numBytes != responsePayloadBytes.Length)
				throw new ModbusException("Holding register byte count does not match.");

			int count = numBytes / 2;
			var holdingRegisters = new List<HoldingRegister>();
			for (int i = 0; i < count; i++)
			{
				holdingRegisters.Add(new HoldingRegister
				{
					Address = (ushort)i,
					HighByte = responsePayloadBytes[i * 2],
					LowByte = responsePayloadBytes[i * 2 + 1]
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

			string request = $":{unitId:X2}{(byte)ModbusFunctionCode.ReadInputRegisters:X2}";

			// Starting address
			byte[] addrBytes = startAddress.ToBigEndianBytes();
			request += $"{addrBytes[0]:X2}{addrBytes[1]:X2}";

			// Quantity
			byte[] countBytes = count.ToBigEndianBytes();
			request += $"{countBytes[0]:X2}{countBytes[1]:X2}";

			// LRC
			string lrc = LRC(request);
			request += lrc;

			// CRLF
			request += "\r\n";

			return Encoding.ASCII.GetBytes(request);
		}

		/// <inheritdoc/>
		public IReadOnlyList<InputRegister> DeserializeReadInputRegisters(IReadOnlyList<byte> response)
		{
			string responseMessage = Encoding.ASCII.GetString([.. response]).ToUpper();

			byte numBytes = HexToByte(responseMessage.Substring(5, 2));
			byte[] responsePayloadBytes = HexStringToByteArray(responseMessage.Substring(7, responseMessage.Length - 11));

			if (numBytes != responsePayloadBytes.Length)
				throw new ModbusException("Input register byte count does not match.");

			int count = numBytes / 2;
			var inputRegisters = new List<InputRegister>();
			for (int i = 0; i < count; i++)
			{
				inputRegisters.Add(new InputRegister
				{
					Address = (ushort)i,
					HighByte = responsePayloadBytes[i * 2],
					LowByte = responsePayloadBytes[i * 2 + 1]
				});
			}

			return inputRegisters;
		}

		/// <inheritdoc/>
		public IReadOnlyList<byte> SerializeReadDeviceIdentification(byte unitId, ModbusDeviceIdentificationCategory category, ModbusDeviceIdentificationObject objectId)
		{
			if (!Enum.IsDefined(typeof(ModbusDeviceIdentificationCategory), category))
				throw new ArgumentOutOfRangeException(nameof(category));

			// Unit Id, Function code and Modbus Encapsulated Interface: Read Device Identification (MEI Type)
			string request = $":{unitId:X2}{(byte)ModbusFunctionCode.EncapsulatedInterface:X2}0E";

			// The category type (basic, regular, extended, individual)
			request += $"{(byte)category:X2}{(byte)objectId:X2}";

			// LRC
			string lrc = LRC(request);
			request += lrc;

			// CRLF
			request += "\r\n";

			return Encoding.ASCII.GetBytes(request);
		}

		/// <inheritdoc/>
		public DeviceIdentificationRaw DeserializeReadDeviceIdentification(IReadOnlyList<byte> response)
		{
			string responseMessage = Encoding.ASCII.GetString([.. response]).ToUpper();

			if (responseMessage.Substring(5, 2) != "0E")
				throw new ModbusException("The MEI type does not match");

			byte category = HexToByte(responseMessage.Substring(7, 2));
			if (!Enum.IsDefined(typeof(ModbusDeviceIdentificationCategory), category))
				throw new ModbusException("The category type does not match");

			var deviceIdentification = new DeviceIdentificationRaw
			{
				AllowsIndividualAccess = (HexToByte(responseMessage.Substring(9, 2)) & 0x80) == 0x80,
				MoreRequestsNeeded = responseMessage.Substring(11, 2) == "FF",
				NextObjectIdToRequest = HexToByte(responseMessage.Substring(13, 2)),
			};

			byte[] responsePayloadBytes = HexStringToByteArray(responseMessage.Substring(15, responseMessage.Length - 19));

			int baseOffset = 1; // Skip number of objects
			while (baseOffset < responsePayloadBytes.Length)
			{
				byte objectId = responsePayloadBytes[baseOffset];
				byte length = responsePayloadBytes[baseOffset + 1];

				byte[] data = responsePayloadBytes.Skip(baseOffset + 2).Take(length).ToArray();

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

			// Unit Id and Function code
			string request = $":{unitId:X2}{(byte)ModbusFunctionCode.WriteSingleCoil:X2}";

			// Starting address
			byte[] addrBytes = coil.Address.ToBigEndianBytes();
			request += $"{addrBytes[0]:X2}{addrBytes[1]:X2}";

			// Value
			request += $"{coil.HighByte:X2}{coil.LowByte:X2}";

			// LRC
			string lrc = LRC(request);
			request += lrc;

			// CRLF
			request += "\r\n";

			return Encoding.ASCII.GetBytes(request);
		}

		/// <inheritdoc/>
		public Coil DeserializeWriteSingleCoil(IReadOnlyList<byte> response)
		{
			string responseMessage = Encoding.ASCII.GetString([.. response]).ToUpper();

			return new Coil
			{
				Address = HexStringToByteArray(responseMessage.Substring(5, 4)).GetBigEndianUInt16(),
				HighByte = HexToByte(responseMessage.Substring(9, 2)),
				LowByte = HexToByte(responseMessage.Substring(11, 2))
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

			// Unit Id and Function code
			string request = $":{unitId:X2}{(byte)ModbusFunctionCode.WriteSingleRegister:X2}";

			// Starting address
			byte[] addrBytes = register.Address.ToBigEndianBytes();
			request += $"{addrBytes[0]:X2}{addrBytes[1]:X2}";

			// Value
			request += $"{register.HighByte:X2}{register.LowByte:X2}";

			// LRC
			string lrc = LRC(request);
			request += lrc;

			// CRLF
			request += "\r\n";

			return Encoding.ASCII.GetBytes(request);
		}

		/// <inheritdoc/>
		public HoldingRegister DeserializeWriteSingleHoldingRegister(IReadOnlyList<byte> response)
		{
			string responseMessage = Encoding.ASCII.GetString([.. response]).ToUpper();

			return new HoldingRegister
			{
				Address = HexStringToByteArray(responseMessage.Substring(5, 4)).GetBigEndianUInt16(),
				HighByte = HexToByte(responseMessage.Substring(9, 2)),
				LowByte = HexToByte(responseMessage.Substring(11, 2))
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
			byte[] data = new byte[byteCount];
			for (int i = 0; i < orderedList.Count; i++)
			{
				int bytePosition = i / 8;
				int bitPosition = i % 8;

				if (orderedList[i].Value)
				{
					byte bitMask = (byte)(1 << bitPosition);
					data[bytePosition] |= bitMask;
				}
			}

			// Unit Id and Function code
			string request = $":{unitId:X2}{(byte)ModbusFunctionCode.WriteMultipleCoils:X2}";

			// Starting address
			byte[] addrBytes = firstAddress.ToBigEndianBytes();
			request += $"{addrBytes[0]:X2}{addrBytes[1]:X2}";

			// Quantity
			byte[] countBytes = ((ushort)orderedList.Count).ToBigEndianBytes();
			request += $"{countBytes[0]:X2}{countBytes[1]:X2}";

			// Byte count
			request += $"{byteCount:X2}";

			// Data
			request += string.Join("", data.Select(b => $"{b:X2}"));

			// LRC
			string lrc = LRC(request);
			request += lrc;

			// CRLF
			request += "\r\n";

			return Encoding.ASCII.GetBytes(request);
		}

		/// <inheritdoc/>
		public (ushort FirstAddress, ushort NumberOfCoils) DeserializeWriteMultipleCoils(IReadOnlyList<byte> response)
		{
			string responseMessage = Encoding.ASCII.GetString([.. response]).ToUpper();

			ushort firstAddress = HexStringToByteArray(responseMessage.Substring(5, 4)).GetBigEndianUInt16();
			ushort numberOfCoils = HexStringToByteArray(responseMessage.Substring(9, 4)).GetBigEndianUInt16();

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
			byte[] data = new byte[byteCount];
			for (int i = 0; i < orderedList.Count; i++)
			{
				data[2 * i] = orderedList[i].HighByte;
				data[2 * i + 1] = orderedList[i].LowByte;
			}

			// Unit Id and Function code
			string request = $":{unitId:X2}{(byte)ModbusFunctionCode.WriteMultipleRegisters:X2}";

			// Starting address
			byte[] addrBytes = firstAddress.ToBigEndianBytes();
			request += $"{addrBytes[0]:X2}{addrBytes[1]:X2}";

			// Quantity
			byte[] countBytes = ((ushort)orderedList.Count).ToBigEndianBytes();
			request += $"{countBytes[0]:X2}{countBytes[1]:X2}";

			// Byte count
			request += $"{byteCount:X2}";

			// Data
			request += string.Join("", data.Select(b => $"{b:X2}"));

			// LRC
			string lrc = LRC(request);
			request += lrc;

			// CRLF
			request += "\r\n";

			return Encoding.ASCII.GetBytes(request);
		}

		/// <inheritdoc/>
		public (ushort FirstAddress, ushort NumberOfRegisters) DeserializeWriteMultipleHoldingRegisters(IReadOnlyList<byte> response)
		{
			string responseMessage = Encoding.ASCII.GetString([.. response]).ToUpper();

			ushort firstAddress = HexStringToByteArray(responseMessage.Substring(5, 4)).GetBigEndianUInt16();
			ushort numberOfRegisters = HexStringToByteArray(responseMessage.Substring(9, 4)).GetBigEndianUInt16();

			return (firstAddress, numberOfRegisters);
		}

		#endregion Write

		#region Validation

		/// <inheritdoc/>
		public bool CheckResponseComplete(IReadOnlyList<byte> responseBytes)
		{
			if (responseBytes.Count < 3)
				return false;

			for (int i = responseBytes.Count - 2; i >= 0; i--)
			{
				// ASCII terminates with CR LF (\r\n)
				if (responseBytes[i] == 0x0D && responseBytes[i + 1] == 0x0A)
					return true;
			}

			return false;
		}

		/// <inheritdoc/>
		public void ValidateResponse(IReadOnlyList<byte> request, IReadOnlyList<byte> response)
		{
			string requestMessage = Encoding.ASCII.GetString([.. request]).ToUpper();
			string responseMessage = Encoding.ASCII.GetString([.. response]).ToUpper();

			// Check header
			if (!responseMessage.StartsWith(":"))
				throw new ModbusException("The protocol header is missing.");

			// Check trailer
			if (!responseMessage.EndsWith("\r\n"))
				throw new ModbusException("The protocol tail is missing.");

			string calculatedLrc = LRC(responseMessage, 1, responseMessage.Length - 5);
			string receivedLrc = responseMessage.Substring(responseMessage.Length - 4, 2);
			if (calculatedLrc != receivedLrc)
				throw new ModbusException("LRC check failed.");

			if (requestMessage.Substring(1, 2) != responseMessage.Substring(1, 2))
				throw new ModbusException("Unit Identifier does not match.");

			byte fnCode = HexToByte(responseMessage.Substring(3, 2));
			bool isError = (fnCode & 0x80) == 0x80;
			if (isError)
				fnCode = (byte)(fnCode ^ 0x80); // === fnCode & 0x7F

			if (requestMessage.Substring(3, 2) != fnCode.ToString("X2"))
				throw new ModbusException("Function code does not match.");

			if (isError)
				throw new ModbusException("Remote Error") { ErrorCode = (ModbusErrorCode)HexToByte(responseMessage.Substring(5, 2)) };

			if (new[] { 0x01, 0x02, 0x03, 0x04 }.Contains(fnCode))
			{
				// : ID FN NU DA XX \r\n
				byte charByteCount = HexToByte(responseMessage.Substring(5, 2));
				if (responseMessage.Length != charByteCount * 2 + 11)
					throw new ModbusException("Number of following bytes does not match.");
			}

			if (new[] { 0x05, 0x06, 0x0F, 0x10 }.Contains(fnCode))
			{
				// : ID FN 00 10 00 30 XX \r\n
				if (responseMessage.Length != 17)
					throw new ModbusException("Number of bytes does not match.");
			}

			// TODO: Do we want to check 0x2B too?
		}

		/// <summary>
		/// Calculate LRC for Modbus ASCII.
		/// </summary>
		/// <param name="message">The message chars.</param>
		/// <param name="start">The start index.</param>
		/// <param name="length">The number of bytes to calculate.</param>
		public static string LRC(string message, int start = 1, int? length = null)
		{
			if (string.IsNullOrWhiteSpace(message))
				throw new ArgumentNullException(nameof(message));

			if (start < 0 || start >= message.Length)
				throw new ArgumentOutOfRangeException(nameof(start));

			length ??= message.Length - start;

			if (length <= 0 || start + length > message.Length)
				throw new ArgumentOutOfRangeException(nameof(length));

			if (length % 2 != 0)
				throw new ArgumentException("The number of chars to calculate the LRC must be even.", nameof(length));

			string subStr = message.Substring(start, length.Value);

			// Step 1:
			// Add all bytes in the message, excluding the starting 'colon' and ending CRLF.
			// Add them into an 8–bit field, so that carries will be discarded.
			byte lrc = 0x00;
			foreach (byte b in HexStringToByteArray(subStr))
				lrc += b;

			// Step 2:
			// Subtract the final field value from FF hex (all 1's), to produce the ones-complement.
			byte oneComplement = (byte)(lrc ^ 0xFF);

			// Step 3:
			// Add 1 to produce the twos-complement.
			return ((byte)(oneComplement + 0x01)).ToString("X2");
		}

		#endregion Validation

		#region Private Helper

		private static byte[] HexStringToByteArray(string hexString)
		{
			return Enumerable
				.Range(0, hexString.Length)
				.Where(x => x % 2 == 0)
				.Select(x => HexToByte(hexString.Substring(x, 2)))
				.ToArray();
		}

		private static byte HexToByte(string hex)
			=> Convert.ToByte(hex, 16);

		#endregion Private Helper
	}
}
