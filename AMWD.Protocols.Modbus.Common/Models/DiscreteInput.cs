namespace AMWD.Protocols.Modbus.Common
{
	/// <summary>
	/// Represents a discrete input.
	/// </summary>
	public class DiscreteInput : ModbusObject
	{
		/// <inheritdoc/>
		public override ModbusObjectType Type => ModbusObjectType.DiscreteInput;

		/// <summary>
		/// Gets or sets a value indicating whether the discrete input is on or off.
		/// </summary>
		public bool Value => HighByte == 0xFF;

		/// <inheritdoc/>
		public override string ToString()
			=> $"Discrete Input #{Address} | {(Value ? "ON" : "OFF")}";
	}
}
