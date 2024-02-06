using System.Collections.Generic;

namespace AMWD.Protocols.Modbus.Common.Contracts
{
	/// <summary>
	/// A definition of the capabilities an implementation of the Modbus protocol version should have.
	/// </summary>
	public interface IModbusProtocol
	{
		/// <summary>
		/// Gets the protocol type name.
		/// </summary>
		string Name { get; }

		#region Read

		/// <summary>
		/// Serializes a read request for <see cref="Coil"/>s.
		/// </summary>
		/// <param name="unitId">The unit id.</param>
		/// <param name="startAddress">The starting address.</param>
		/// <param name="count">The number of coils to read.</param>
		/// <returns>The <see langword="byte"/>s to send.</returns>
		IReadOnlyList<byte> SerializeReadCoils(byte unitId, ushort startAddress, ushort count);

		/// <summary>
		/// Deserializes a read response for <see cref="Coil"/>s.
		/// </summary>
		/// <param name="response">The <see langword="byte"/>s received.</param>
		/// <returns>A list of <see cref="Coil"/>s.</returns>
		IReadOnlyList<Coil> DeserializeReadCoils(IReadOnlyList<byte> response);

		/// <summary>
		/// Serializes a read request for <see cref="DiscreteInput"/>s.
		/// </summary>
		/// <param name="unitId">The unit id.</param>
		/// <param name="startAddress">The starting address.</param>
		/// <param name="count">The number of discrete inputs to read.</param>
		/// <returns>The <see langword="byte"/>s to send.</returns>
		IReadOnlyList<byte> SerializeReadDiscreteInputs(byte unitId, ushort startAddress, ushort count);

		/// <summary>
		/// Deserializes a read response for <see cref="DiscreteInput"/>s.
		/// </summary>
		/// <param name="response">The <see langword="byte"/>s received.</param>
		/// <returns>A list of <see cref="DiscreteInput"/>s.</returns>
		IReadOnlyList<DiscreteInput> DeserializeReadDiscreteInputs(IReadOnlyList<byte> response);

		/// <summary>
		/// Serializes a read request for <see cref="HoldingRegister"/>s.
		/// </summary>
		/// <param name="unitId">The unit id.</param>
		/// <param name="startAddress">The starting address.</param>
		/// <param name="count">The number of holding registers to read.</param>
		/// <returns>The <see langword="byte"/>s to send.</returns>
		IReadOnlyList<byte> SerializeReadHoldingRegisters(byte unitId, ushort startAddress, ushort count);

		/// <summary>
		/// Deserializes a read response for <see cref="HoldingRegister"/>s.
		/// </summary>
		/// <param name="response">The <see langword="byte"/>s received.</param>
		/// <returns>A list of <see cref="HoldingRegister"/>s.</returns>
		IReadOnlyList<HoldingRegister> DeserializeReadHoldingRegisters(IReadOnlyList<byte> response);

		/// <summary>
		/// Serializes a read request for <see cref="InputRegister"/>s.
		/// </summary>
		/// <param name="unitId">The unit id.</param>
		/// <param name="startAddress">The starting address.</param>
		/// <param name="count">The number of input registers to read.</param>
		/// <returns>The <see langword="byte"/>s to send.</returns>
		IReadOnlyList<byte> SerializeReadInputRegisters(byte unitId, ushort startAddress, ushort count);

		/// <summary>
		/// Deserializes a read response for <see cref="InputRegister"/>s.
		/// </summary>
		/// <param name="response">The <see langword="byte"/>s received.</param>
		/// <returns>A list of <see cref="InputRegister"/>s.</returns>
		IReadOnlyList<InputRegister> DeserializeReadInputRegisters(IReadOnlyList<byte> response);

		#endregion Read

		#region Write

		/// <summary>
		/// Serializes a write request for a single <see cref="Coil"/>.
		/// </summary>
		/// <param name="unitId">The unit id.</param>
		/// <param name="coil">The coil to write.</param>
		/// <returns>The <see langword="byte"/>s to send.</returns>
		IReadOnlyList<byte> SerializeWriteSingleCoil(byte unitId, Coil coil);

		/// <summary>
		/// Deserializes a write response for a single <see cref="Coil"/>.
		/// </summary>
		/// <param name="response">The <see langword="byte"/>s received.</param>
		/// <returns>Should be the coil itself, as the response is an echo of the request.</returns>
		Coil DeserializeWriteSingleCoil(IReadOnlyList<byte> response);

		/// <summary>
		/// Serializes a write request for a single <see cref="HoldingRegister"/>.
		/// </summary>
		/// <param name="unitId">The unit id.</param>
		/// <param name="register">The holding register to write.</param>
		/// <returns>The <see langword="byte"/>s to send.</returns>
		IReadOnlyList<byte> SerializeWriteSingleHoldingRegister(byte unitId, HoldingRegister register);

		/// <summary>
		/// Deserializes a write response for a single <see cref="HoldingRegister"/>.
		/// </summary>
		/// <param name="response">The <see langword="byte"/>s received.</param>
		/// <returns>Should be the holding register itself, as the response is an echo of the request.</returns>
		HoldingRegister DeserializeWriteSingleHoldingRegister(IReadOnlyList<byte> response);

		/// <summary>
		/// Serializes a write request for multiple <see cref="Coil"/>s.
		/// </summary>
		/// <param name="unitId">The unit id.</param>
		/// <param name="coils">The coils to write.</param>
		/// <returns>The <see langword="byte"/>s to send.</returns>
		IReadOnlyList<byte> SerializeWriteMultipleCoils(byte unitId, IReadOnlyList<Coil> coils);

		/// <summary>
		/// Deserializes a write response for multiple <see cref="Coil"/>s.
		/// </summary>
		/// <param name="response">The <see langword="byte"/>s received.</param>
		/// <returns>A tuple containting the first address and the number of coils written.</returns>
		(ushort FirstAddress, ushort NumberOfCoils) DeserializeWriteMultipleCoils(IReadOnlyList<byte> response);

		/// <summary>
		/// Serializes a write request for multiple <see cref="HoldingRegister"/>s.
		/// </summary>
		/// <param name="unitId">The unit id.</param>
		/// <param name="registers">The holding registers to write.</param>
		/// <returns>The <see langword="byte"/>s to send.</returns>
		IReadOnlyList<byte> SerializeWriteMultipleHoldingRegisters(byte unitId, IReadOnlyList<HoldingRegister> registers);

		/// <summary>
		/// Deserializes a write response for multiple <see cref="HoldingRegister"/>s.
		/// </summary>
		/// <param name="response">The <see langword="byte"/>s received.</param>
		/// <returns>A tuple containting the first address and the number of holding registers written.</returns>
		(ushort FirstAddress, ushort NumberOfRegisters) DeserializeWriteMultipleHoldingRegisters(IReadOnlyList<byte> response);

		#endregion Write

		#region Control

		/// <summary>
		/// Checks whether the receive response bytes are complete to deserialize the response.
		/// </summary>
		/// <param name="responseBytes">The already received response bytes.</param>
		/// <returns><see langword="true"/> when the response is complete, otherwise <see langword="false"/>.</returns>
		bool CheckResponseComplete(IReadOnlyList<byte> responseBytes);

		/// <summary>
		/// Validates the response against the request and throws <see cref="ModbusException"/>s if necessary.
		/// </summary>
		/// <param name="request">The serialized request.</param>
		/// <param name="response">The received response.</param>
		void ValidateResponse(IReadOnlyList<byte> request, IReadOnlyList<byte> response);

		#endregion Control
	}
}
