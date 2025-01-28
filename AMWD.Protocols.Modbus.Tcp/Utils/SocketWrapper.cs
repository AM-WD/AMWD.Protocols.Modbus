using System;
using System.Net.Sockets;

namespace AMWD.Protocols.Modbus.Tcp.Utils
{
	internal class SocketWrapper : IDisposable
	{
		private Socket _socket;

		public SocketWrapper(Socket socket)
		{
			_socket = socket;
		}

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
