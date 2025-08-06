namespace AMWD.Protocols.Modbus.Common
{
	/// <summary>
	/// Represents a coil.
	/// </summary>
	public class Coil : ModbusObject
	{
		/// <inheritdoc/>
		public override ModbusObjectType Type => ModbusObjectType.Coil;

		/// <summary>
		/// Gets or sets a value indicating whether the coil is on or off.
		/// </summary>
		public bool Value
		{
			get => HighByte == 0xFF;
			set
			{
				HighByte = (byte)(value ? 0xFF : 0x00);
				LowByte = 0x00;
			}
		}

		/// <inheritdoc/>
		public override string ToString()
			=> $"Coil #{Address} | {(Value ? "ON" : "OFF")}";
	}
}
