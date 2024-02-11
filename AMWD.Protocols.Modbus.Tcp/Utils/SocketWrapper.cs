using System;
using System.Net.Sockets;

namespace AMWD.Protocols.Modbus.Tcp.Utils
{
	/// <inheritdoc cref="Socket"/>
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	internal class SocketWrapper : IDisposable
	{
		[Obsolete("Constructor only for mocking on UnitTests!")]
		public SocketWrapper()
		{ }

		public SocketWrapper(Socket socket)
		{
			Client = socket;
		}

		public virtual Socket Client { get; }

		/// <inheritdoc cref="Socket.Dispose()"/>
		public virtual void Dispose()
			=> Client.Dispose();

		/// <inheritdoc cref="Socket.IOControl(IOControlCode, byte[], byte[])"/>
		public virtual int IOControl(IOControlCode ioControlCode, byte[] optionInValue, byte[] optionOutValue)
			=> Client.IOControl(ioControlCode, optionInValue, optionOutValue);

#if NET6_0_OR_GREATER
		/// <inheritdoc cref="Socket.SetSocketOption(SocketOptionLevel, SocketOptionName, bool)"/>
		public virtual void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, bool optionValue)
			=> Client.SetSocketOption(optionLevel, optionName, optionValue);

		/// <inheritdoc cref="Socket.SetSocketOption(SocketOptionLevel, SocketOptionName, int)"/>
		public virtual void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue)
			=> Client.SetSocketOption(optionLevel, optionName, optionValue);
#endif
	}
}
