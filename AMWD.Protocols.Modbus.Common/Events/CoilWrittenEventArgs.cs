using System;

namespace AMWD.Protocols.Modbus.Common.Events
{
	/// <summary>
	/// Represents the coil written event arguments.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class CoilWrittenEventArgs : EventArgs
	{
		/// <summary>
		/// Gets or sets the unit id.
		/// </summary>
		public byte UnitId { get; set; }

		/// <summary>
		/// Gets or sets the coil address.
		/// </summary>
		public ushort Address { get; set; }

		/// <summary>
		/// Gets or sets the coil value.
		/// </summary>
		public bool Value { get; set; }
	}
}
