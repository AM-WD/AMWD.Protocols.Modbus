using System.ComponentModel;

namespace AMWD.Protocols.Modbus.Common
{
	/// <summary>
	/// List of the Modbus function codes.
	/// </summary>
	public enum ModbusFunctionCode : byte
	{
		/// <summary>
		/// Read coils (Fn 1).
		/// </summary>
		[Description("Read Coils")]
		ReadCoils = 0x01,

		/// <summary>
		/// Read discrete inputs (Fn 2).
		/// </summary>
		[Description("Read Discrete Inputs")]
		ReadDiscreteInputs = 0x02,

		/// <summary>
		/// Reads holding registers (Fn 3).
		/// </summary>
		[Description("Read Holding Registers")]
		ReadHoldingRegisters = 0x03,

		/// <summary>
		/// Reads input registers (Fn 4).
		/// </summary>
		[Description("Read Input Registers")]
		ReadInputRegisters = 0x04,

		/// <summary>
		/// Writes a single coil (Fn 5).
		/// </summary>
		[Description("Write Single Coil")]
		WriteSingleCoil = 0x05,

		/// <summary>
		/// Writes a single register (Fn 6).
		/// </summary>
		[Description("Write Single Register")]
		WriteSingleRegister = 0x06,

		/// <summary>
		/// Writes multiple coils (Fn 15).
		/// </summary>
		[Description("Write Multiple Coils")]
		WriteMultipleCoils = 0x0F,

		/// <summary>
		/// Writes multiple registers (Fn 16).
		/// </summary>
		[Description("Write Multiple Registers")]
		WriteMultipleRegisters = 0x10,

		/// <summary>
		/// Tunnels service requests and method invocations (Fn 43).
		/// </summary>
		/// <remarks>
		/// This function code needs additional information about its type of request.
		/// </remarks>
		[Description("MODBUS Encapsulated Interface (MEI)")]
		EncapsulatedInterface = 0x2B
	}
}
