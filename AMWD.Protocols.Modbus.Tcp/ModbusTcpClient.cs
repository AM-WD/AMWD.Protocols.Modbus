using System;
using AMWD.Protocols.Modbus.Common.Contracts;
using AMWD.Protocols.Modbus.Common.Protocols;

namespace AMWD.Protocols.Modbus.Tcp
{
	/// <summary>
	/// Default implementation of a Modbus TCP client.
	/// </summary>
	public class ModbusTcpClient : ModbusClientBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ModbusTcpClient"/> class with a hostname and port number.
		/// </summary>
		/// <param name="hostname">The DNS name of the remote host to which the connection is intended to.</param>
		/// <param name="port">The port number of the remote host to which the connection is intended to.</param>
		public ModbusTcpClient(string hostname, int port = 502)
			: this(new ModbusTcpConnection { Hostname = hostname, Port = port })
		{ }

		/// <summary>
		/// Initializes a new instance of the <see cref="ModbusTcpClient"/> class with a specific <see cref="IModbusConnection"/>.
		/// </summary>
		/// <param name="connection">The <see cref="IModbusConnection"/> responsible for invoking the requests.</param>
		public ModbusTcpClient(IModbusConnection connection)
			: this(connection, true)
		{ }

		/// <summary>
		/// Initializes a new instance of the <see cref="ModbusClientBase"/> class with a specific <see cref="IModbusConnection"/>.
		/// </summary>
		/// <param name="connection">The <see cref="IModbusConnection"/> responsible for invoking the requests.</param>
		/// <param name="disposeConnection">
		/// <see langword="true"/> if the connection should be disposed of by Dispose(),
		/// <see langword="false"/> otherwise if you inted to reuse the connection.
		/// </param>
		public ModbusTcpClient(IModbusConnection connection, bool disposeConnection)
			: base(connection, disposeConnection)
		{
			Protocol = new TcpProtocol();
		}

		/// <inheritdoc/>
		public override IModbusProtocol Protocol { get; set; }

		/// <inheritdoc cref="ModbusTcpConnection.Hostname"/>
		public string Hostname
		{
			get
			{
				if (connection is ModbusTcpConnection tcpConnection)
					return tcpConnection.Hostname;

				return default;
			}
			set
			{
				if (connection is ModbusTcpConnection tcpConnection)
					tcpConnection.Hostname = value;
			}
		}

		/// <inheritdoc cref="ModbusTcpConnection.Port"/>
		public int Port
		{
			get
			{
				if (connection is ModbusTcpConnection tcpConnection)
					return tcpConnection.Port;

				return default;
			}
			set
			{
				if (connection is ModbusTcpConnection tcpConnection)
					tcpConnection.Port = value;
			}
		}

		/// <inheritdoc cref="ModbusTcpConnection.ReadTimeout"/>
		public TimeSpan ReadTimeout
		{
			get
			{
				if (connection is ModbusTcpConnection tcpConnection)
					return tcpConnection.ReadTimeout;

				return default;
			}
			set
			{
				if (connection is ModbusTcpConnection tcpConnection)
					tcpConnection.ReadTimeout = value;
			}
		}

		/// <inheritdoc cref="ModbusTcpConnection.WriteTimeout"/>
		public TimeSpan WriteTimeout
		{
			get
			{
				if (connection is ModbusTcpConnection tcpConnection)
					return tcpConnection.WriteTimeout;

				return default;
			}
			set
			{
				if (connection is ModbusTcpConnection tcpConnection)
					tcpConnection.WriteTimeout = value;
			}
		}

		/// <inheritdoc cref="ModbusTcpConnection.ConnectTimeout"/>
		public TimeSpan ReconnectTimeout
		{
			get
			{
				if (connection is ModbusTcpConnection tcpConnection)
					return tcpConnection.ConnectTimeout;

				return default;
			}
			set
			{
				if (connection is ModbusTcpConnection tcpConnection)
					tcpConnection.ConnectTimeout = value;
			}
		}

		/// <inheritdoc cref="ModbusTcpConnection.IdleTimeout"/>
		public TimeSpan IdleTimeout
		{
			get
			{
				if (connection is ModbusTcpConnection tcpConnection)
					return tcpConnection.IdleTimeout;

				return default;
			}
			set
			{
				if (connection is ModbusTcpConnection tcpConnection)
					tcpConnection.IdleTimeout = value;
			}
		}
	}
}
