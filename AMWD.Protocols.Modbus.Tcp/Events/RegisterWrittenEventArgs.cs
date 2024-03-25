using System;

namespace AMWD.Protocols.Modbus.Tcp.Events
{
	/// <summary>
	/// Represents the register written event arguments.
	/// </summary>
	public class RegisterWrittenEventArgs : EventArgs
	{
		/// <summary>
		/// Gets or sets the unit id.
		/// </summary>
		public byte UnitId { get; internal set; }

		/// <summary>
		/// Gets or sets the address of the register.
		/// </summary>
		public ushort Address { get; internal set; }

		/// <summary>
		/// Gets or sets the value of the register.
		/// </summary>
		public ushort Value { get; internal set; }

		/// <summary>
		/// Gets or sets the high byte of the register.
		/// </summary>
		public byte HighByte { get; internal set; }

		/// <summary>
		/// Gets or sets the low byte of the register.
		/// </summary>
		public byte LowByte { get; internal set; }
	}
}
