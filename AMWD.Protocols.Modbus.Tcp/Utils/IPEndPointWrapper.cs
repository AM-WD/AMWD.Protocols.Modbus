using System.Net;

namespace AMWD.Protocols.Modbus.Tcp.Utils
{
	internal class IPEndPointWrapper
	{
		private IPEndPoint _ipEndPoint;

		public IPEndPointWrapper(EndPoint endPoint)
		{
			_ipEndPoint = (IPEndPoint)endPoint;
		}

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
