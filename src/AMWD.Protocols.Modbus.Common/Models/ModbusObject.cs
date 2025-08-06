#if NET6_0_OR_GREATER
using System;
#endif

namespace AMWD.Protocols.Modbus.Common
{
	/// <summary>
	/// Represents the base of all Modbus specific objects.
	/// </summary>
	public abstract class ModbusObject
	{
		/// <summary>
		/// Gets the type of the object.
		/// </summary>
		public abstract ModbusObjectType Type { get; }

		/// <summary>
		/// Gets or sets the address of the object.
		/// </summary>
		public ushort Address { get; set; }

		/// <summary>
		/// Gets or sets the high byte of the value.
		/// </summary>
		public byte HighByte { get; set; }

		/// <summary>
		/// Gets or sets the low byte of the value.
		/// </summary>
		public byte LowByte { get; set; }

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (obj is not ModbusObject mo)
				return false;

			return Type == mo.Type
				&& Address == mo.Address
				&& HighByte == mo.HighByte
				&& LowByte == mo.LowByte;
		}

		/// <inheritdoc/>
		[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		public override int GetHashCode()
		{
#if NET6_0_OR_GREATER
			return HashCode.Combine(Type, Address, HighByte, LowByte);
#else
			return Type.GetHashCode()
				^ Address.GetHashCode()
				^ HighByte.GetHashCode()
				^ LowByte.GetHashCode();
#endif
		}
	}
}
