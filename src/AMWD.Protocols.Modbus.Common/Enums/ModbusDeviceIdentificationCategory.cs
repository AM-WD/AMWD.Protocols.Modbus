using System.ComponentModel;

namespace AMWD.Protocols.Modbus.Common
{
	/// <summary>
	/// List of known categories for Modbus device identification.
	/// </summary>
	public enum ModbusDeviceIdentificationCategory : byte
	{
		/// <summary>
		/// The basic information. These are mandatory.
		/// </summary>
		[Description("Basic Device Identification")]
		Basic = 0x01,

		/// <summary>
		/// The regular information. These are optional.
		/// </summary>
		[Description("Regular Device Identification")]
		Regular = 0x02,

		/// <summary>
		/// The extended information. These are optional too.
		/// </summary>
		[Description("Extended Device Identification")]
		Extended = 0x03,

		/// <summary>
		/// Request to a specific identification object.
		/// </summary>
		[Description("Request to a specific identification object")]
		Individual = 0x04
	}
}
