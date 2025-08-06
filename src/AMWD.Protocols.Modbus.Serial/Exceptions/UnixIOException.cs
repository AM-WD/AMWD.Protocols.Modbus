using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using AMWD.Protocols.Modbus.Serial.Utils;
#if NETSTANDARD
using System.Security.Permissions;
#endif

namespace AMWD.Protocols.Modbus.Serial
{
	/// <summary>
	/// Represents a unix specific IO exception.
	/// </summary>
	/// <remarks>
	/// See StackOverflow answer: <see href="https://stackoverflow.com/a/10388107"/>.
	/// </remarks>
	[Serializable]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class UnixIOException : ExternalException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UnixIOException"/> class.
		/// </summary>
#if NETSTANDARD
		[SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
#endif
		public UnixIOException()
			: this(Marshal.GetLastWin32Error())
		{ }

		/// <summary>
		/// Initializes a new instance of the <see cref="UnixIOException"/> class.
		/// </summary>
		/// <param name="errorCode">The native error code.</param>
#if NETSTANDARD
		[SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
#endif
		public UnixIOException(int errorCode)
			: this(GetErrorMessage(errorCode), errorCode)
		{ }

		/// <summary>
		/// Initializes a new instance of the <see cref="UnixIOException"/> class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
#if NETSTANDARD
		[SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
#endif
		public UnixIOException(string message)
			: base(message)
		{ }

		/// <inheritdoc/>
		public UnixIOException(string message, Exception inner)
			: base(message, inner)
		{ }

		/// <inheritdoc/>
		public UnixIOException(string message, int errorCode)
			: base(message, errorCode)
		{ }

#if ! NET8_0_OR_GREATER
		/// <inheritdoc/>
		protected UnixIOException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{ }
#endif

		private static string GetErrorMessage(int errorCode)
		{
			try
			{
				nint ptr = UnsafeNativeMethods.StrError(errorCode);
				return Marshal.PtrToStringAnsi(ptr);
			}
			catch
			{
				return $"Unknown error: 0x{errorCode:x}";
			}
		}
	}
}
