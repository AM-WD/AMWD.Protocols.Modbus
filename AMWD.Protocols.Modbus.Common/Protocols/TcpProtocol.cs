﻿using System;
using System.Collections.Generic;
using System.Linq;
using AMWD.Protocols.Modbus.Common.Contracts;

namespace AMWD.Protocols.Modbus.Common.Protocols
{
	/// <summary>
	/// Default implementation of the Modbus TCP protocol.
	/// </summary>
	public class TcpProtocol : IModbusProtocol
	{
		#region Fields

		private readonly object _lock = new();
		private ushort _transactionId = 0x0000;

		#endregion Fields

		#region Constants

		/// <summary>
		/// The minimum allowed unit id specified by the Modbus TCP protocol.
		/// </summary>
		public const byte MIN_UNIT_ID = 0x00;

		/// <summary>
		/// The maximum allowed unit id specified by the Modbus TCP protocol.
		/// </summary>
		public const byte MAX_UNIT_ID = 0xFF;

		/// <summary>
		/// The minimum allowed read count specified by the Modbus TCP protocol.
		/// </summary>
		public const ushort MIN_READ_COUNT = 0x01;

		/// <summary>
		/// The minimum allowed write count specified by the Modbus TCP protocol.
		/// </summary>
		public const ushort MIN_WRITE_COUNT = 0x01;

		/// <summary>
		/// The maximum allowed read count for discrete values specified by the Modbus TCP protocol.
		/// </summary>
		public const ushort MAX_DISCRETE_READ_COUNT = 0x07D0; // 2000

		/// <summary>
		/// The maximum allowed write count for discrete values specified by the Modbus TCP protocol.
		/// </summary>
		public const ushort MAX_DISCRETE_WRITE_COUNT = 0x07B0; // 1968

		/// <summary>
		/// The maximum allowed read count for registers specified by the Modbus TCP protocol.
		/// </summary>
		public const ushort MAX_REGISTER_READ_COUNT = 0x007D; // 125

		/// <summary>
		/// The maximum allowed write count for registers specified by the Modbus TCP protocol.
		/// </summary>
		public const ushort MAX_REGISTER_WRITE_COUNT = 0x007B; // 123

		/// <summary>
		/// The maximum allowed ADU length in bytes.
		/// </summary>
		/// <remarks>
		/// A Modbus frame consists of a PDU (protcol data unit) and additional protocol addressing / error checks.
		/// The whole data frame is called ADU (application data unit).
		/// </remarks>
		public const int MAX_ADU_LENGTH = 260; // bytes

		#endregion Constants

		/// <inheritdoc/>
		public string Name => "TCP";

		/// <summary>
		/// Gets or sets a value indicating whether to disable the transaction id usage.
		/// </summary>
		public bool DisableTransactionId { get; set; }

		#region Read

		/// <inheritdoc/>
		public IReadOnlyList<byte> SerializeReadCoils(byte unitId, ushort startAddress, ushort count)
		{
			if (count < MIN_READ_COUNT || MAX_DISCRETE_READ_COUNT < count)
				throw new ArgumentOutOfRangeException(nameof(count));

			if (ushort.MaxValue < (startAddress + count - 1))
				throw new ArgumentOutOfRangeException(nameof(count), $"Combination of {nameof(startAddress)} and {nameof(count)} exceeds the addressation limit of {ushort.MaxValue}");

			byte[] request = new byte[12];

			byte[] header = GetHeader(6);
			Array.Copy(header, 0, request, 0, header.Length);

			// Unit id
			request[6] = unitId;

			// Function code
			request[7] = (byte)ModbusFunctionCode.ReadCoils;

			// Starting address
			var addrBytes = startAddress.ToBigEndianBytes();
			request[8] = addrBytes[0];
			request[9] = addrBytes[1];

			// Quantity
			var countBytes = count.ToBigEndianBytes();
			request[10] = countBytes[0];
			request[11] = countBytes[1];

			return request;
		}

		/// <inheritdoc/>
		public IReadOnlyList<Coil> DeserializeReadCoils(IReadOnlyList<byte> response)
		{
			int baseOffset = 9;
			if (response[8] != response.Count - baseOffset)
				throw new ModbusException("Coil byte count does not match.");

			int count = response[8] * 8;
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

			byte[] request = new byte[12];

			byte[] header = GetHeader(6);
			Array.Copy(header, 0, request, 0, header.Length);

			// Unit id
			request[6] = unitId;

			// Function code
			request[7] = (byte)ModbusFunctionCode.ReadDiscreteInputs;

			// Starting address
			var addrBytes = startAddress.ToBigEndianBytes();
			request[8] = addrBytes[0];
			request[9] = addrBytes[1];

			// Quantity
			var countBytes = count.ToBigEndianBytes();
			request[10] = countBytes[0];
			request[11] = countBytes[1];

			return request;
		}

		/// <inheritdoc/>
		public IReadOnlyList<DiscreteInput> DeserializeReadDiscreteInputs(IReadOnlyList<byte> response)
		{
			int baseOffset = 9;
			if (response[8] != response.Count - baseOffset)
				throw new ModbusException("Discrete input byte count does not match.");

			int count = response[8] * 8;
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

			byte[] request = new byte[12];

			byte[] header = GetHeader(6);
			Array.Copy(header, 0, request, 0, header.Length);

			// Unit id
			request[6] = unitId;

			// Function code
			request[7] = (byte)ModbusFunctionCode.ReadHoldingRegisters;

			// Starting address
			var addrBytes = startAddress.ToBigEndianBytes();
			request[8] = addrBytes[0];
			request[9] = addrBytes[1];

			// Quantity
			var countBytes = count.ToBigEndianBytes();
			request[10] = countBytes[0];
			request[11] = countBytes[1];

			return request;
		}

		/// <inheritdoc/>
		public IReadOnlyList<HoldingRegister> DeserializeReadHoldingRegisters(IReadOnlyList<byte> response)
		{
			int baseOffset = 9;
			if (response[8] != response.Count - baseOffset)
				throw new ModbusException("Holding register byte count does not match.");

			int count = response[8] / 2;
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

			byte[] request = new byte[12];

			byte[] header = GetHeader(6);
			Array.Copy(header, 0, request, 0, header.Length);

			// Unit id
			request[6] = unitId;

			// Function code
			request[7] = (byte)ModbusFunctionCode.ReadInputRegisters;

			// Starting address
			var addrBytes = startAddress.ToBigEndianBytes();
			request[8] = addrBytes[0];
			request[9] = addrBytes[1];

			// Quantity
			var countBytes = count.ToBigEndianBytes();
			request[10] = countBytes[0];
			request[11] = countBytes[1];

			return request;
		}

		/// <inheritdoc/>
		public IReadOnlyList<InputRegister> DeserializeReadInputRegisters(IReadOnlyList<byte> response)
		{
			int baseOffset = 9;
			if (response[8] != response.Count - baseOffset)
				throw new ModbusException("Input register byte count does not match.");

			int count = response[8] / 2;
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

			byte[] request = new byte[11];

			byte[] header = GetHeader(5);
			Array.Copy(header, 0, request, 0, header.Length);

			// Unit id
			request[6] = unitId;

			// Function code
			request[7] = (byte)ModbusFunctionCode.EncapsulatedInterface;

			// Modbus Encapsulated Interface: Read Device Identification (MEI Type)
			request[8] = 0x0E;

			// The category type (basic, regular, extended, individual)
			request[9] = (byte)category;
			request[10] = (byte)objectId;

			return request;
		}

		/// <inheritdoc/>
		public DeviceIdentificationRaw DeserializeReadDeviceIdentification(IReadOnlyList<byte> response)
		{
			if (response[8] != 0x0E)
				throw new ModbusException("The MEI type does not match");

			if (!Enum.IsDefined(typeof(ModbusDeviceIdentificationCategory), response[9]))
				throw new ModbusException("The category type does not match");

			var deviceIdentification = new DeviceIdentificationRaw
			{
				AllowsIndividualAccess = (response[10] & 0x80) == 0x80,
				MoreRequestsNeeded = response[11] == 0xFF,
				NextObjectIdToRequest = response[12],
			};

			int baseOffset = 14;
			while (baseOffset < response.Count)
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

			byte[] request = new byte[12];

			byte[] header = GetHeader(6);
			Array.Copy(header, 0, request, 0, header.Length);

			// Unit id
			request[6] = unitId;

			// Function code
			request[7] = (byte)ModbusFunctionCode.WriteSingleCoil;

			var addrBytes = coil.Address.ToBigEndianBytes();
			request[8] = addrBytes[0];
			request[9] = addrBytes[1];

			request[10] = coil.HighByte;
			request[11] = coil.LowByte;

			return request;
		}

		/// <inheritdoc/>
		public Coil DeserializeWriteSingleCoil(IReadOnlyList<byte> response)
		{
			return new Coil
			{
				Address = response.ToArray().GetBigEndianUInt16(8),
				HighByte = response[10],
				LowByte = response[11]
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

			byte[] request = new byte[12];

			byte[] header = GetHeader(6);
			Array.Copy(header, 0, request, 0, header.Length);

			// Unit id
			request[6] = unitId;

			// Function code
			request[7] = (byte)ModbusFunctionCode.WriteSingleRegister;

			var addrBytes = register.Address.ToBigEndianBytes();
			request[8] = addrBytes[0];
			request[9] = addrBytes[1];

			request[10] = register.HighByte;
			request[11] = register.LowByte;

			return request;
		}

		/// <inheritdoc/>
		public HoldingRegister DeserializeWriteSingleHoldingRegister(IReadOnlyList<byte> response)
		{
			return new HoldingRegister
			{
				Address = response.ToArray().GetBigEndianUInt16(8),
				HighByte = response[10],
				LowByte = response[11]
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
			byte[] request = new byte[13 + byteCount];

			byte[] header = GetHeader(byteCount + 7);
			Array.Copy(header, 0, request, 0, header.Length);

			// Unit id
			request[6] = unitId;

			// Function code
			request[7] = (byte)ModbusFunctionCode.WriteMultipleCoils;

			// Starting address
			var addrBytes = firstAddress.ToBigEndianBytes();
			request[8] = addrBytes[0];
			request[9] = addrBytes[1];

			// Quantity
			var countBytes = ((ushort)orderedList.Count).ToBigEndianBytes();
			request[10] = countBytes[0];
			request[11] = countBytes[1];

			// Byte count
			request[12] = byteCount;

			// Coils
			int baseOffset = 13;
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

			return request;
		}

		/// <inheritdoc/>
		public (ushort FirstAddress, ushort NumberOfCoils) DeserializeWriteMultipleCoils(IReadOnlyList<byte> response)
		{
			ushort firstAddress = response.ToArray().GetBigEndianUInt16(8);
			ushort numberOfCoils = response.ToArray().GetBigEndianUInt16(10);

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
			byte[] request = new byte[13 + byteCount];

			byte[] header = GetHeader(byteCount + 7);
			Array.Copy(header, 0, request, 0, header.Length);

			// Unit id
			request[6] = unitId;

			// Function code
			request[7] = (byte)ModbusFunctionCode.WriteMultipleRegisters;

			// Starting address
			var addrBytes = firstAddress.ToBigEndianBytes();
			request[8] = addrBytes[0];
			request[9] = addrBytes[1];

			// Quantity
			var countBytes = ((ushort)orderedList.Count).ToBigEndianBytes();
			request[10] = countBytes[0];
			request[11] = countBytes[1];

			// Byte count
			request[12] = byteCount;

			// Registers
			int baseOffset = 13;
			for (int i = 0; i < orderedList.Count; i++)
			{
				request[baseOffset + 2 * i] = orderedList[i].HighByte;
				request[baseOffset + 2 * i + 1] = orderedList[i].LowByte;
			}

			return request;
		}

		/// <inheritdoc/>
		public (ushort FirstAddress, ushort NumberOfRegisters) DeserializeWriteMultipleHoldingRegisters(IReadOnlyList<byte> response)
		{
			ushort firstAddress = response.ToArray().GetBigEndianUInt16(8);
			ushort numberOfRegisters = response.ToArray().GetBigEndianUInt16(10);

			return (firstAddress, numberOfRegisters);
		}

		#endregion Write

		#region Validation

		/// <inheritdoc/>
		public bool CheckResponseComplete(IReadOnlyList<byte> responseBytes)
		{
			// 2x Transaction Id
			// 2x Protocol Identifier
			// 2x Number of following bytes
			if (responseBytes.Count < 6)
				return false;

			ushort followingBytes = responseBytes.ToArray().GetBigEndianUInt16(4);
			if (responseBytes.Count < followingBytes + 6)
				return false;

			return true;
		}

		/// <inheritdoc/>
		public void ValidateResponse(IReadOnlyList<byte> request, IReadOnlyList<byte> response)
		{
			if (!DisableTransactionId)
			{
				if (request[0] != response[0] || request[1] != response[1])
					throw new ModbusException("Transaction Id does not match.");
			}

			if (request[2] != response[2] || request[3] != response[3])
				throw new ModbusException("Protocol Identifier does not match.");

			ushort count = response.ToArray().GetBigEndianUInt16(4);
			if (count != response.Count - 6)
				throw new ModbusException("Number of following bytes does not match.");

			if (request[6] != response[6])
				throw new ModbusException("Unit Identifier does not match.");

			byte fnCode = response[7];
			bool isError = (fnCode & 0x80) == 0x80;
			if (isError)
				fnCode = (byte)(fnCode ^ 0x80); // === fnCode & 0x7F

			if (request[7] != fnCode)
				throw new ModbusException("Function code does not match.");

			if (isError)
				throw new ModbusException("Remote Error") { ErrorCode = (ModbusErrorCode)response[8] };
		}

		#endregion Validation

		#region Private helpers

		private ushort GetNextTransacitonId()
		{
			if (DisableTransactionId)
				return 0x0000;

			lock (_lock)
			{
				if (_transactionId == ushort.MaxValue)
					_transactionId = 0x0000;
				else
					_transactionId++;

				return _transactionId;
			}
		}

		/// <summary>
		/// Generates the header for a Modbus request.
		/// </summary>
		/// <param name="followingBytes">The number of following bytes.</param>
		/// <returns>The header ready to copy to the request bytes (6 bytes).</returns>
		private byte[] GetHeader(int followingBytes)
		{
			byte[] header = new byte[6];

			// Transaction id
			ushort txId = GetNextTransacitonId();
			var txBytes = txId.ToBigEndianBytes();
			header[0] = txBytes[0];
			header[1] = txBytes[1];

			// Protocol identifier
			header[2] = 0x00;
			header[3] = 0x00;

			// Number of following bytes
			var countBytes = ((ushort)followingBytes).ToBigEndianBytes();
			header[4] = countBytes[0];
			header[5] = countBytes[1];

			return header;
		}

		#endregion Private helpers
	}
}
