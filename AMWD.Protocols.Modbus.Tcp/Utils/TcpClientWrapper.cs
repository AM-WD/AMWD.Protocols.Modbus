using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AMWD.Protocols.Modbus.Tcp.Utils
{
	/// <inheritdoc cref="TcpClient" />
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	internal class TcpClientWrapper(AddressFamily addressFamily) : IDisposable
	{
		#region Fields

		private readonly TcpClient _client = new(addressFamily);

		#endregion Fields

		#region Properties

		/// <inheritdoc cref="TcpClient.Connected" />
		public virtual bool Connected => _client.Connected;

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

		#endregion Properties

		#region Methods

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

		/// <inheritdoc cref="TcpClient.GetStream" />
		public virtual NetworkStreamWrapper GetStream()
			=> new(_client.GetStream());

		#endregion Methods

		#region IDisposable

		/// <inheritdoc cref="TcpClient.Dispose()" />
		public virtual void Dispose()
			=> _client.Dispose();

		#endregion IDisposable
	}
}
