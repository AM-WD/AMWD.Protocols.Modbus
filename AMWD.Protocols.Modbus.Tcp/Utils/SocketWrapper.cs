using System;
using System.Net.Sockets;

namespace AMWD.Protocols.Modbus.Tcp.Utils
{
	/// <inheritdoc cref="Socket" />
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	internal class SocketWrapper(Socket socket) : IDisposable
	{
		private readonly Socket _socket = socket;

		/// <inheritdoc  cref="Socket.DualMode" />
		public virtual bool DualMode
		{
			get => _socket.DualMode;
			set => _socket.DualMode = value;
		}

		/// <inheritdoc  cref="Socket.IsBound" />
		public virtual bool IsBound
			=> _socket.IsBound;

		public virtual void Dispose()
			=> _socket.Dispose();
	}
}
