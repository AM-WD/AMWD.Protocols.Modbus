using System;

namespace AMWD.Protocols.Modbus.Common.Events
{
	/// <summary>
	/// Represents the coil written event arguments.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class CoilWrittenEventArgs : EventArgs
	{
		internal CoilWrittenEventArgs(byte unitId, ushort address, bool value)
		{
			UnitId = unitId;
			Address = address;
			Value = value;
		}

		/// <summary>
		/// Gets or sets the unit id.
		/// </summary>
		public byte UnitId { get; }

		/// <summary>
		/// Gets or sets the coil address.
		/// </summary>
		public ushort Address { get; }

		/// <summary>
		/// Gets or sets the coil value.
		/// </summary>
		public bool Value { get; }
	}
}
