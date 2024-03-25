using System;

namespace AMWD.Protocols.Modbus.Tcp.Events
{
	/// <summary>
	/// Represents the coil written event arguments.
	/// </summary>
	public class CoilWrittenEventArgs : EventArgs
	{
		/// <summary>
		/// Gets or sets the unit id.
		/// </summary>
		public byte UnitId { get; internal set; }

		/// <summary>
		/// Gets or sets the coil address.
		/// </summary>
		public ushort Address { get; internal set; }

		/// <summary>
		/// Gets or sets the coil value.
		/// </summary>
		public bool Value { get; internal set; }
	}
}
