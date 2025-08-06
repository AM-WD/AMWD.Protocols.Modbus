namespace AMWD.Protocols.Modbus.Common
{
	/// <summary>
	/// List of known object ids for Modbus device identification.
	/// </summary>
	public enum ModbusDeviceIdentificationObject : byte
	{
		/// <summary>
		/// The vendor name (mandatory).
		/// </summary>
		VendorName = 0x00,

		/// <summary>
		/// The product code (mandatory).
		/// </summary>
		ProductCode = 0x01,

		/// <summary>
		/// The version in major, minor and revision number (mandatory).
		/// </summary>
		MajorMinorRevision = 0x02,

		/// <summary>
		/// The vendor URL (optional).
		/// </summary>
		VendorUrl = 0x03,

		/// <summary>
		/// The product name (optional).
		/// </summary>
		ProductName = 0x04,

		/// <summary>
		/// The model name (optional).
		/// </summary>
		ModelName = 0x05,

		/// <summary>
		/// The application name (optional).
		/// </summary>
		UserApplicationName = 0x06
	}
}
