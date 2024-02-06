using System.ComponentModel;

namespace AMWD.Protocols.Modbus.Common
{
	/// <summary>
	/// List of Modbus exception codes.
	/// </summary>
	public enum ModbusErrorCode : byte
	{
		/// <summary>
		/// No error.
		/// </summary>
		[Description("No error")]
		NoError = 0x00,

		/// <summary>
		/// Function code not valid/supported.
		/// </summary>
		[Description("Illegal function")]
		IllegalFunction = 0x01,

		/// <summary>
		/// Data address not in range.
		/// </summary>
		[Description("Illegal data address")]
		IllegalDataAddress = 0x02,

		/// <summary>
		/// The data value to set is not valid.
		/// </summary>
		[Description("Illegal data value")]
		IllegalDataValue = 0x03,

		/// <summary>
		/// Slave device produced a failure.
		/// </summary>
		[Description("Slave device failure")]
		SlaveDeviceFailure = 0x04,

		/// <summary>
		/// Ack
		/// </summary>
		[Description("Acknowledge")]
		Acknowledge = 0x05,

		/// <summary>
		/// Slave device is working on another task.
		/// </summary>
		[Description("Slave device busy")]
		SlaveDeviceBusy = 0x06,

		/// <summary>
		/// nAck
		/// </summary>
		[Description("Negative acknowledge")]
		NegativeAcknowledge = 0x07,

		/// <summary>
		/// Momory Parity Error.
		/// </summary>
		[Description("Memory parity error")]
		MemoryParityError = 0x08,

		/// <summary>
		/// Gateway of the device could not be reached.
		/// </summary>
		[Description("Gateway path unavailable")]
		GatewayPath = 0x0A,

		/// <summary>
		/// Gateway device did no respond.
		/// </summary>
		[Description("Gateway target device failed to respond")]
		GatewayTargetDevice = 0x0B
	}
}
