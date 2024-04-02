using System.Runtime.InteropServices;
using AMWD.Protocols.Modbus.Serial.Enums;

namespace AMWD.Protocols.Modbus.Serial.Utils
{
	/// <summary>
	/// Represents the structure of the driver settings for RS485.
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Size = 32)]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	internal struct SerialRS485
	{
		/// <summary>
		/// The flags to change the driver state.
		/// </summary>
		public RS485Flags Flags;

		/// <summary>
		/// The delay in milliseconds before send.
		/// </summary>
		public uint RtsDelayBeforeSend;

		/// <summary>
		/// The delay in milliseconds after send.
		/// </summary>
		public uint RtsDelayAfterSend;
	}
}
