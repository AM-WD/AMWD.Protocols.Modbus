using System;
using System.IO.Ports;
using AMWD.Protocols.Modbus.Common.Contracts;
using AMWD.Protocols.Modbus.Common.Protocols;

namespace AMWD.Protocols.Modbus.Serial
{
	/// <summary>
	/// Default implementation of a Modbus serial line client.
	/// </summary>
	public class ModbusSerialClient : ModbusClientBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ModbusSerialClient"/> class with a port name.
		/// </summary>
		/// <param name="portName">The name of the serial port to use.</param>
		public ModbusSerialClient(string portName)
			: this(new ModbusSerialConnection { PortName = portName })
		{ }

		/// <summary>
		/// Initializes a new instance of the <see cref="ModbusSerialClient"/> class with a specific <see cref="IModbusConnection"/>.
		/// </summary>
		/// <param name="connection">The <see cref="IModbusConnection"/> responsible for invoking the requests.</param>
		public ModbusSerialClient(IModbusConnection connection)
			: this(connection, true)
		{ }

		/// <summary>
		/// Initializes a new instance of the <see cref="ModbusSerialClient"/> class with a specific <see cref="IModbusConnection"/>.
		/// </summary>
		/// <param name="connection">The <see cref="IModbusConnection"/> responsible for invoking the requests.</param>
		/// <param name="disposeConnection">
		/// <see langword="true"/> if the connection should be disposed of by Dispose(),
		/// <see langword="false"/> otherwise if you inted to reuse the connection.
		/// </param>
		public ModbusSerialClient(IModbusConnection connection, bool disposeConnection)
			: base(connection, disposeConnection)
		{
			Protocol = new RtuProtocol();
		}

		/// <inheritdoc cref="SerialPort.GetPortNames" />
		public static string[] AvailablePortNames => SerialPort.GetPortNames();

		/// <inheritdoc/>
		public override IModbusProtocol Protocol { get; set; }

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

		/// <inheritdoc cref="ModbusSerialConnection.DriverEnabledRS485"/>
		public bool DriverEnabledRS485
		{
			get
			{
				if (connection is ModbusSerialConnection serialConnection)
					return serialConnection.DriverEnabledRS485;

				return default;
			}
			set
			{
				if (connection is ModbusSerialConnection serialConnection)
					serialConnection.DriverEnabledRS485 = value;
			}
		}

		/// <inheritdoc cref="ModbusSerialConnection.InterRequestDelay"/>
		public TimeSpan InterRequestDelay
		{
			get
			{
				if (connection is ModbusSerialConnection serialConnection)
					return serialConnection.InterRequestDelay;

				return default;
			}
			set
			{
				if (connection is ModbusSerialConnection serialConnection)
					serialConnection.InterRequestDelay = value;
			}
		}

		/// <inheritdoc cref="ModbusSerialConnection.PortName"/>
		public string PortName
		{
			get
			{
				if (connection is ModbusSerialConnection serialConnection)
					return serialConnection.PortName;

				return default;
			}
			set
			{
				if (connection is ModbusSerialConnection serialConnection)
					serialConnection.PortName = value;
			}
		}

		/// <inheritdoc cref="ModbusSerialConnection.BaudRate"/>
		public BaudRate BaudRate
		{
			get
			{
				if (connection is ModbusSerialConnection serialConnection)
					return serialConnection.BaudRate;

				return default;
			}
			set
			{
				if (connection is ModbusSerialConnection serialConnection)
					serialConnection.BaudRate = value;
			}
		}

		/// <inheritdoc cref="ModbusSerialConnection.DataBits"/>
		public int DataBits
		{
			get
			{
				if (connection is ModbusSerialConnection serialConnection)
					return serialConnection.DataBits;

				return default;
			}
			set
			{
				if (connection is ModbusSerialConnection serialConnection)
					serialConnection.DataBits = value;
			}
		}

		/// <inheritdoc cref="ModbusSerialConnection.Handshake"/>
		public Handshake Handshake
		{
			get
			{
				if (connection is ModbusSerialConnection serialConnection)
					return serialConnection.Handshake;

				return default;
			}
			set
			{
				if (connection is ModbusSerialConnection serialConnection)
					serialConnection.Handshake = value;
			}
		}

		/// <inheritdoc cref="ModbusSerialConnection.Parity"/>
		public Parity Parity
		{
			get
			{
				if (connection is ModbusSerialConnection serialConnection)
					return serialConnection.Parity;

				return default;
			}
			set
			{
				if (connection is ModbusSerialConnection serialConnection)
					serialConnection.Parity = value;
			}
		}

		/// <inheritdoc cref="ModbusSerialConnection.RtsEnable"/>
		public bool RtsEnable
		{
			get
			{
				if (connection is ModbusSerialConnection serialConnection)
					return serialConnection.RtsEnable;

				return default;
			}
			set
			{
				if (connection is ModbusSerialConnection serialConnection)
					serialConnection.RtsEnable = value;
			}
		}

		/// <inheritdoc cref="ModbusSerialConnection.StopBits"/>
		public StopBits StopBits
		{
			get
			{
				if (connection is ModbusSerialConnection serialConnection)
					return serialConnection.StopBits;

				return default;
			}
			set
			{
				if (connection is ModbusSerialConnection serialConnection)
					serialConnection.StopBits = value;
			}
		}
	}
}
