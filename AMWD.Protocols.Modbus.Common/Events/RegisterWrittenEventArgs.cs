using System;

namespace AMWD.Protocols.Modbus.Common.Events
{
	/// <summary>
	/// Represents the register written event arguments.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class RegisterWrittenEventArgs : EventArgs
	{
		/// <summary>
		/// Gets or sets the unit id.
		/// </summary>
		public byte UnitId { get; set; }

		/// <summary>
		/// Gets or sets the address of the register.
		/// </summary>
		public ushort Address { get; set; }

		/// <summary>
		/// Gets or sets the value of the register.
		/// </summary>
		public ushort Value { get; set; }

		/// <summary>
		/// Gets or sets the high byte of the register.
		/// </summary>
		public byte HighByte { get; set; }

		/// <summary>
		/// Gets or sets the low byte of the register.
		/// </summary>
		public byte LowByte { get; set; }
	}
}
