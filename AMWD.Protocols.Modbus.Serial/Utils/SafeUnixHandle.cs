using System;
using System.Runtime.InteropServices;
#if NETSTANDARD
using System.Runtime.ConstrainedExecution;
using System.Security.Permissions;
#endif

namespace AMWD.Protocols.Modbus.Serial.Utils
{
	/// <summary>
	/// Implements a safe handle for unix systems.
	/// <br/>
	/// Found on https://stackoverflow.com/a/10388107
	/// </summary>
#if NETSTANDARD
	[SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
	[SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
#endif
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	internal sealed class SafeUnixHandle : SafeHandle
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SafeUnixHandle"/> class.
		/// </summary>
#if NETSTANDARD
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif
		private SafeUnixHandle()
			: base(new IntPtr(-1), true)
		{ }

		public override bool IsInvalid
			=> handle == new IntPtr(-1);

		protected override bool ReleaseHandle()
			=> UnsafeNativeMethods.Close(handle) != -1;
	}
}
