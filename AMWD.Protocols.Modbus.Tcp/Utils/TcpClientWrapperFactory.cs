using System;
using System.Net.Sockets;

namespace AMWD.Protocols.Modbus.Tcp.Utils
{
	/// <summary>
	/// Factory for creating <see cref="TcpClientWrapper"/> instances.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	internal class TcpClientWrapperFactory
	{
		/// <summary>
		/// Creates a new instance of <see cref="TcpClientWrapper"/>.
		/// </summary>
		/// <param name="addressFamily">The <see cref="AddressFamily"/> of the <see cref="TcpClient"/> to use.</param>
		/// <param name="readTimeout">The read timeout.</param>
		/// <param name="writeTimeout">The write timeout.</param>
		/// <returns>A new <see cref="TcpClientWrapper"/> instance.</returns>
		public virtual TcpClientWrapper Create(AddressFamily addressFamily, TimeSpan readTimeout, TimeSpan writeTimeout)
		{
			var client = new TcpClientWrapper(addressFamily)
			{
				ReceiveTimeout = (int)readTimeout.TotalMilliseconds,
				SendTimeout = (int)writeTimeout.TotalMilliseconds
			};
			return client;
		}
	}
}
