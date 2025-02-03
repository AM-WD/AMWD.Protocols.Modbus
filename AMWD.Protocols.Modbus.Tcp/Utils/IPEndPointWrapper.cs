using System.Net;
using System.Net.Sockets;

namespace AMWD.Protocols.Modbus.Tcp.Utils
{
	/// <inheritdoc cref="IPEndPoint" />
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	internal class IPEndPointWrapper(EndPoint endPoint)
	{
		private readonly IPEndPoint _ipEndPoint = (IPEndPoint)endPoint;

		#region Properties

		/// <inheritdoc  cref="IPEndPoint.Address"/>
		public virtual IPAddress Address
		{
			get => _ipEndPoint.Address;
			set => _ipEndPoint.Address = value;
		}

		/// <inheritdoc  cref="IPEndPoint.Port"/>
		public virtual int Port
		{
			get => _ipEndPoint.Port;
			set => _ipEndPoint.Port = value;
		}

		#endregion Properties
	}
}
