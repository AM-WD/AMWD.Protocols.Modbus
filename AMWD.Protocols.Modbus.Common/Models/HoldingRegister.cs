using System;

namespace AMWD.Protocols.Modbus.Common
{
	/// <summary>
	/// Represents a holding register.
	/// </summary>
	public class HoldingRegister : ModbusObject
	{
		/// <inheritdoc/>
		public override ModbusObjectType Type => ModbusObjectType.HoldingRegister;

		/// <summary>
		/// Gets or sets the value of the holding register.
		/// </summary>
		public ushort Value
		{
			get
			{
				byte[] blob = [HighByte, LowByte];
				blob.SwapNetworkOrder();
				return BitConverter.ToUInt16(blob, 0);
			}
			set
			{
				byte[] blob = BitConverter.GetBytes(value);
				blob.SwapNetworkOrder();

				HighByte = blob[0];
				LowByte = blob[1];
			}
		}

		/// <inheritdoc/>
		public override string ToString()
			=> $"Holding Register #{Address} | {Value} | HI: {HighByte:X2}, LO: {LowByte:X2}";
	}
}
