namespace AMWD.Protocols.Modbus.Common
{
	/// <summary>
	/// List of specific types.
	/// </summary>
	public enum ModbusObjectType
	{
		/// <summary>
		/// The discrete value is a coil (read/write).
		/// </summary>
		Coil = 1,

		/// <summary>
		/// The discrete value is an input (read only).
		/// </summary>
		DiscreteInput = 2,

		/// <summary>
		/// The value is a holding register (read/write).
		/// </summary>
		HoldingRegister = 3,

		/// <summary>
		/// The value is an input register (read only).
		/// </summary>
		InputRegister = 4
	}
}
