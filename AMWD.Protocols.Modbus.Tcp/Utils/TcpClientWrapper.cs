using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AMWD.Protocols.Modbus.Tcp.Utils
{
	/// <inheritdoc cref="TcpClient" />
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	internal class TcpClientWrapper : IDisposable
	{
		private readonly TcpClient _client = new();

		/// <inheritdoc cref="TcpClient.ReceiveTimeout" />
		public virtual int ReceiveTimeout
		{
			get => _client.ReceiveTimeout;
			set => _client.ReceiveTimeout = value;
		}

		/// <inheritdoc cref="TcpClient.SendTimeout" />
		public virtual int SendTimeout
		{
			get => _client.SendTimeout;
			set => _client.SendTimeout = value;
		}

		/// <inheritdoc cref="TcpClient.Connected" />
		public virtual bool Connected => _client.Connected;

		/// <inheritdoc cref="TcpClient.Client" />
		public virtual SocketWrapper Client
		{
			get => new(_client.Client);
			set => _client.Client = value.Client;
		}

		/// <inheritdoc cref="TcpClient.Close" />
		public virtual void Close()
			=> _client.Close();

#if NET6_0_OR_GREATER
		/// <inheritdoc cref="TcpClient.ConnectAsync(IPAddress, int, CancellationToken)" />
		public virtual Task ConnectAsync(IPAddress address, int port, CancellationToken cancellationToken)
			=> _client.ConnectAsync(address, port, cancellationToken).AsTask();
#else

		/// <inheritdoc cref="TcpClient.ConnectAsync(IPAddress, int)" />
		public virtual Task ConnectAsync(IPAddress address, int port)
			=> _client.ConnectAsync(address, port);

#endif

		/// <inheritdoc cref="TcpClient.Dispose()" />
		public virtual void Dispose()
			=> _client.Dispose();

		/// <inheritdoc cref="TcpClient.GetStream" />
		public virtual NetworkStreamWrapper GetStream()
			=> new(_client.GetStream());
	}
}
