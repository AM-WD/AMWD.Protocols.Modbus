using System;
using System.Text;
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
		/// Initializes a new instance of the <see cref="ModbusTcpClient"/> class with a specific <see cref="IModbusConnection"/>.
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

		/// <inheritdoc cref="IModbusConnection.IdleTimeout"/>
		public TimeSpan IdleTimeout
		{
			get => connection.IdleTimeout;
			set => connection.IdleTimeout = value;
		}

		/// <inheritdoc cref="IModbusConnection.ConnectTimeout"/>
		public TimeSpan ConnectTimeout
		{
			get => connection.ConnectTimeout;
			set => connection.ConnectTimeout = value;
		}

		/// <inheritdoc cref="IModbusConnection.ReadTimeout"/>
		public TimeSpan ReadTimeout
		{
			get => connection.ReadTimeout;
			set => connection.ReadTimeout = value;
		}

		/// <inheritdoc cref="IModbusConnection.WriteTimeout"/>
		public TimeSpan WriteTimeout
		{
			get => connection.WriteTimeout;
			set => connection.WriteTimeout = value;
		}

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

		/// <inheritdoc/>
		public override string ToString()
		{
			var sb = new StringBuilder();

			sb.AppendLine($"TCP Client {Hostname}");
			sb.AppendLine($"  {nameof(Port)}:           {Port}");

			return sb.ToString();
		}
	}
}
