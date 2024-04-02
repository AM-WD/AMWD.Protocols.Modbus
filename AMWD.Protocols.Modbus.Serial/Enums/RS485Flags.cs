using System;

namespace AMWD.Protocols.Modbus.Serial.Enums
{
	/// <summary>
	/// Defines the flags for the RS485 driver state.
	/// </summary>
	[Flags]
	internal enum RS485Flags
	{
		/// <summary>
		/// RS485 is enabled.
		/// </summary>
		Enabled = 1,

		/// <summary>
		/// RS485 uses RTS on send.
		/// </summary>
		RtsOnSend = 2,

		/// <summary>
		/// RS485 uses RTS after send.
		/// </summary>
		RtsAfterSend = 4,

		/// <summary>
		/// Receive during send (duplex).
		/// </summary>
		RxDuringTx = 16
	}
}
