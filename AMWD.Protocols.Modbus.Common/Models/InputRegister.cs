using System;

namespace AMWD.Protocols.Modbus.Common
{
	/// <summary>
	/// Represents a input register.
	/// </summary>
	public class InputRegister : ModbusObject
	{
		/// <inheritdoc/>
		public override ModbusObjectType Type => ModbusObjectType.InputRegister;

		/// <summary>
		/// Gets or sets the value of the input register.
		/// </summary>
		public ushort Value
		{
			get
			{
				byte[] blob = [HighByte, LowByte];
				blob.SwapNetworkOrder();
				return BitConverter.ToUInt16(blob, 0);
			}
		}

		/// <inheritdoc/>
		public override string ToString()
			=> $"Input Register #{Address} | {Value} | HI: {HighByte:X2}, LO: {LowByte:X2}";
	}
}
