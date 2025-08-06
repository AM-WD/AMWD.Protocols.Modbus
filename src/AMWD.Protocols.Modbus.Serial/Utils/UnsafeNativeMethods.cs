using System;
using System.Runtime.InteropServices;
#if NETSTANDARD
using System.Runtime.ConstrainedExecution;
#endif

namespace AMWD.Protocols.Modbus.Serial.Utils
{
	/// <summary>
	/// Definitions of the unsafe system methods.
	/// <br/>
	/// Found on https://stackoverflow.com/a/10388107
	/// </summary>
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	internal static class UnsafeNativeMethods
	{
		/// <summary>
		/// A flag for <see cref="Open(string, uint)"/>.
		/// </summary>
		internal const int O_RDWR = 2;

		/// <summary>
		/// A flag for <see cref="Open(string, uint)"/>.
		/// </summary>
		internal const int O_NOCTTY = 256;

		/// <summary>
		/// A flag for <see cref="IoCtl(SafeUnixHandle, uint, ref SerialRS485)"/>.
		/// </summary>
		internal const uint TIOCGRS485 = 0x542E;

		/// <summary>
		/// A flag for <see cref="IoCtl(SafeUnixHandle, uint, ref SerialRS485)"/>.
		/// </summary>
		internal const uint TIOCSRS485 = 0x542F;

		/// <summary>
		/// Opens a handle to a defined path (serial port).
		/// </summary>
		/// <param name="path">The path to open the handle.</param>
		/// <param name="flag">The flags for the handle.</param>
		[DllImport("libc", EntryPoint = "open", SetLastError = true)]
		internal static extern SafeUnixHandle Open(string path, uint flag);

		/// <summary>
		/// Performs an ioctl request to the open handle.
		/// </summary>
		/// <param name="handle">The handle.</param>
		/// <param name="request">The request.</param>
		/// <param name="serialRs485">The serial rs485 data structure to use.</param>
		[DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
		internal static extern int IoCtl(SafeUnixHandle handle, uint request, ref SerialRS485 serialRs485);

		/// <summary>
		/// Closes an open handle.
		/// </summary>
		/// <param name="handle">The handle.</param>
#if NETSTANDARD
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif
		[DllImport("libc", EntryPoint = "close", SetLastError = true)]
		internal static extern int Close(IntPtr handle);

		/// <summary>
		/// Converts the given error number (errno) into a readable string.
		/// </summary>
		/// <param name="errno">The error number.</param>
		[DllImport("libc", EntryPoint = "strerror", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr StrError(int errno);
	}
}
