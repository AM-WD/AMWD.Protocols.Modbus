using System;

namespace AMWD.Protocols.Modbus.Common.Events
{
	/// <summary>
	/// Represents the register written event arguments.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class RegisterWrittenEventArgs : EventArgs
	{
		internal RegisterWrittenEventArgs(byte unitId, ushort address, byte highByte, byte lowByte)
		{
			UnitId = unitId;
			Address = address;
			HighByte = highByte;
			LowByte = lowByte;

			Value = new[] { highByte, lowByte }.GetBigEndianUInt16();
		}

		/// <summary>
		/// Gets or sets the unit id.
		/// </summary>
		public byte UnitId { get; }

		/// <summary>
		/// Gets or sets the address of the register.
		/// </summary>
		public ushort Address { get; }

		/// <summary>
		/// Gets or sets the value of the register.
		/// </summary>
		public ushort Value { get; }

		/// <summary>
		/// Gets or sets the high byte of the register.
		/// </summary>
		public byte HighByte { get; }

		/// <summary>
		/// Gets or sets the low byte of the register.
		/// </summary>
		public byte LowByte { get; }
	}
}
