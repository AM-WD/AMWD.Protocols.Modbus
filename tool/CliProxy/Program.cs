using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AMWD.Common.Cli;
using AMWD.Protocols.Modbus.Common.Contracts;
using AMWD.Protocols.Modbus.Common.Protocols;
using AMWD.Protocols.Modbus.Serial;
using AMWD.Protocols.Modbus.Tcp;

namespace AMWD.Protocols.Modbus.CliProxy
{
	internal class Program
	{
		#region General options

		private static Option _helpOption;
		private static Option _debugOption;

		private static Option _serverOption;
		private static Option _clientOption;

		private static Option _clientProtocolOption;

		#endregion General options

		#region Server options

		private static Option _serverSerialBaudOption;
		private static Option _serverSerialDataBitsOption;
		private static Option _serverSerialStopBitsOption;
		private static Option _serverSerialParityOption;
		private static Option _serverSerialDeviceOption;

		private static Option _serverTcpHostOption;
		private static Option _serverTcpPortOption;

		#endregion Server options

		#region Client options

		private static Option _clientSerialBaudOption;
		private static Option _clientSerialDataBitsOption;
		private static Option _clientSerialStopBitsOption;
		private static Option _clientSerialParityOption;
		private static Option _clientSerialDeviceOption;
		private static Option _clientSerialSoftEnableOption;

		private static Option _clientTcpHostOption;
		private static Option _clientTcpPortOption;

		#endregion Client options

		private static async Task<int> Main(string[] args)
		{
			if (!ParseArguments(args))
			{
				Console.Error.WriteLine("Could not parse arguments.");
				return 1;
			}

			if (_helpOption.IsSet)
			{
				PrintHelp();
				return 0;
			}

			using var cts = new CancellationTokenSource();
			Console.CancelKeyPress += (_, e) =>
			{
				cts.Cancel();
				e.Cancel = true;
			};

			if (_debugOption.IsSet)
			{
				Console.Error.Write("Waiting for debugger ");
				while (!Debugger.IsAttached)
				{
					try
					{
						Console.Error.Write(".");
						await Task.Delay(1000, cts.Token);
					}
					catch (OperationCanceledException)
					{
						return 0;
					}
				}
				Console.Error.WriteLine();
			}

			try
			{
				using var client = CreateClient();
				Console.WriteLine(client);
				Console.WriteLine();

				if (_clientProtocolOption.IsSet)
				{
					switch (_clientProtocolOption.Value.ToLower())
					{
						case "ascii": client.Protocol = new AsciiProtocol(); break;
						case "rtu": client.Protocol = new RtuProtocol(); break;
						case "tcp": client.Protocol = new TcpProtocol(); break;
					}
				}
				using var proxy = CreateProxy(client);
				Console.WriteLine(proxy);
				Console.WriteLine();

				await proxy.StartAsync(cts.Token);
				try
				{
					Console.WriteLine("Running proxy. Press Ctrl+C to stop.");
					await Task.Delay(Timeout.Infinite, cts.Token);
				}
				finally
				{
					await proxy.StopAsync();
				}

				return 0;
			}
			catch (OperationCanceledException) when (cts.IsCancellationRequested)
			{
				return 0;
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"{ex.GetType().Name}: {ex.Message}");
				return 1;
			}
		}

		private static bool ParseArguments(string[] args)
		{
			var cmdLine = new CommandLineParser();

			#region General options

			_helpOption = cmdLine.RegisterOption("help").Alias("h");
			_debugOption = cmdLine.RegisterOption("debug");

			_serverOption = cmdLine.RegisterOption("server", 1); // TCP | RTU
			_clientOption = cmdLine.RegisterOption("client", 1); // TCP | RTU

			_clientProtocolOption = cmdLine.RegisterOption("client-protocol", 1);

			#endregion General options

			#region Server options

			_serverSerialBaudOption = cmdLine.RegisterOption("server-baud", 1);
			_serverSerialDataBitsOption = cmdLine.RegisterOption("server-databits", 1);
			_serverSerialDeviceOption = cmdLine.RegisterOption("server-device", 1);
			_serverSerialStopBitsOption = cmdLine.RegisterOption("server-stopbits", 1);
			_serverSerialParityOption = cmdLine.RegisterOption("server-parity", 1);

			_serverTcpHostOption = cmdLine.RegisterOption("server-host", 1);
			_serverTcpPortOption = cmdLine.RegisterOption("server-port", 1);

			#endregion Server options

			#region Client options

			_clientSerialBaudOption = cmdLine.RegisterOption("client-baud", 1);
			_clientSerialDataBitsOption = cmdLine.RegisterOption("client-databits", 1);
			_clientSerialDeviceOption = cmdLine.RegisterOption("client-device", 1);
			_clientSerialStopBitsOption = cmdLine.RegisterOption("client-stopbits", 1);
			_clientSerialParityOption = cmdLine.RegisterOption("client-parity", 1);
			_clientSerialSoftEnableOption = cmdLine.RegisterOption("client-enable-rs485");

			_clientTcpHostOption = cmdLine.RegisterOption("client-host", 1);
			_clientTcpPortOption = cmdLine.RegisterOption("client-port", 1);

			#endregion Client options

			try
			{
				cmdLine.Parse(args);
				return true;
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex.Message);
				return false;
			}
		}

		private static void PrintHelp()
		{
			Console.WriteLine($"Usage: {typeof(Program).Assembly.GetName().Name} --server <rtu|tcp> --client <rtu|tcp> [options]");
			Console.WriteLine();
			Console.WriteLine("General options:");
			Console.WriteLine("  --help, -h");
			Console.WriteLine("    Shows this help message.");
			Console.WriteLine();
			Console.WriteLine("  --debug");
			Console.WriteLine("    Waits for a debugger to be attached before starting.");
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine("Server options:");
			Console.WriteLine("  --server <rtu|tcp>");
			Console.WriteLine("    Defines whether to use an RTU or an TCP proxy.");
			Console.WriteLine();
			Console.WriteLine("  --server-baud #");
			Console.WriteLine("    The baud rate (e.g. 9600) to use for the RTU proxy. Default: 19200.");
			Console.WriteLine();
			Console.WriteLine("  --server-databits #");
			Console.WriteLine("    The number of data bits. Default: 8.");
			Console.WriteLine();
			Console.WriteLine("  --server-device <device-port>");
			Console.WriteLine("    The serial port to use (e.g. COM1, /dev/ttyS0).");
			Console.WriteLine();
			Console.WriteLine("  --server-parity <none|odd|even>");
			Console.WriteLine("    The parity to use. Default: even.");
			Console.WriteLine();
			Console.WriteLine("  --server-stopbits #");
			Console.WriteLine("    The number of stop bits. Default: 1.");
			Console.WriteLine();
			Console.WriteLine("  --server-host <address>");
			Console.WriteLine("    The IP address to listen on. Default: 127.0.0.1.");
			Console.WriteLine();
			Console.WriteLine("  --server-port #");
			Console.WriteLine("    The port to listen on. Default: 502.");
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine("Client options:");
			Console.WriteLine("  --client <rtu|tcp>");
			Console.WriteLine("    Defines whether to use an RTU or an TCP client.");
			Console.WriteLine();
			Console.WriteLine("  --client-protocol <ascii|rtu|tcp>");
			Console.WriteLine("    Select which Modbus protocol to use.");
			Console.WriteLine();
			Console.WriteLine("  --client-baud #");
			Console.WriteLine("    The baud rate (e.g. 9600) to use for the RTU client. Default: 19200.");
			Console.WriteLine();
			Console.WriteLine("  --client-databits #");
			Console.WriteLine("    The number of data bits. Default: 8.");
			Console.WriteLine();
			Console.WriteLine("  --client-device <device-port>");
			Console.WriteLine("    The serial port to use (e.g. COM1, /dev/ttyS0).");
			Console.WriteLine();
			Console.WriteLine("  --client-parity <none|odd|even>");
			Console.WriteLine("    The parity to use. Default: even.");
			Console.WriteLine();
			Console.WriteLine("  --client-stopbits #");
			Console.WriteLine("    The number of stop bits. Default: 1.");
			Console.WriteLine();
			Console.WriteLine("  --client-enable-rs485");
			Console.WriteLine("    Enables the RS485 software switch for serial adapters capable of RS232 and RS485.");
			Console.WriteLine();
			Console.WriteLine("  --client-host <hostname>");
			Console.WriteLine("    The host to connect to.");
			Console.WriteLine();
			Console.WriteLine("  --client-port #");
			Console.WriteLine("    The port to connect to. Default: 502.");
			Console.WriteLine();
		}

		private static ModbusClientBase CreateClient()
		{
			if (!_clientOption.IsSet)
				throw new ApplicationException("No client type specified.");

			BaudRate baudRate = BaudRate.Baud19200;
			if (_clientSerialBaudOption.IsSet && int.TryParse(_clientSerialBaudOption.Value, out int baudRateValue))
				baudRate = (BaudRate)baudRateValue;

			int dataBits = 8;
			if (_clientSerialDataBitsOption.IsSet && int.TryParse(_clientSerialDataBitsOption.Value, out int dataBitsValue))
				dataBits = dataBitsValue;

			StopBits stopBits = StopBits.One;
			if (_clientSerialStopBitsOption.IsSet && float.TryParse(_clientSerialStopBitsOption.Value, out float stopBitsValue))
			{
				switch (stopBitsValue)
				{
					case 1.0f: stopBits = StopBits.One; break;
					case 1.5f: stopBits = StopBits.OnePointFive; break;
					case 2.0f: stopBits = StopBits.Two; break;
				}
			}

			Parity parity = Parity.Even;
			if (_clientSerialParityOption.IsSet)
			{
				switch (_clientSerialParityOption.Value.ToLower())
				{
					case "none": parity = Parity.None; break;
					case "odd": parity = Parity.Odd; break;
					case "even": parity = Parity.Even; break;
				}
			}

			bool enableRs485 = _clientSerialSoftEnableOption.IsSet;

			int port = 502;
			if (_clientTcpPortOption.IsSet && ushort.TryParse(_clientTcpPortOption.Value, out ushort portValue))
				port = portValue;

			return _clientOption.Value.ToLower() switch
			{
				"rtu" => new ModbusSerialClient(_clientSerialDeviceOption.Value)
				{
					BaudRate = baudRate,
					DataBits = dataBits,
					StopBits = stopBits,
					Parity = parity,

					DriverEnabledRS485 = enableRs485
				},
				"tcp" => new ModbusTcpClient(_clientTcpHostOption.Value)
				{
					Port = port
				},
				_ => throw new ApplicationException($"Unknown client type: '{_clientOption.Value}'"),
			};
		}

		private static IModbusProxy CreateProxy(ModbusClientBase client)
		{
			if (!_serverOption.IsSet)
				throw new ApplicationException("No proxy type specified.");

			BaudRate baudRate = BaudRate.Baud19200;
			if (_serverSerialBaudOption.IsSet && int.TryParse(_serverSerialBaudOption.Value, out int baudRateValue))
				baudRate = (BaudRate)baudRateValue;

			int dataBits = 8;
			if (_serverSerialDataBitsOption.IsSet && int.TryParse(_serverSerialDataBitsOption.Value, out int dataBitsValue))
				dataBits = dataBitsValue;

			StopBits stopBits = StopBits.One;
			if (_serverSerialStopBitsOption.IsSet && float.TryParse(_serverSerialStopBitsOption.Value, out float stopBitsValue))
			{
				switch (stopBitsValue)
				{
					case 1.0f: stopBits = StopBits.One; break;
					case 1.5f: stopBits = StopBits.OnePointFive; break;
					case 2.0f: stopBits = StopBits.Two; break;
				}
			}

			Parity parity = Parity.Even;
			if (_serverSerialParityOption.IsSet)
			{
				switch (_serverSerialParityOption.Value.ToLower())
				{
					case "none": parity = Parity.None; break;
					case "odd": parity = Parity.Odd; break;
					case "even": parity = Parity.Even; break;
				}
			}

			int port = 502;
			if (_serverTcpPortOption.IsSet && ushort.TryParse(_serverTcpPortOption.Value, out ushort portValue))
				port = portValue;

			return _serverOption.Value.ToLower() switch
			{
				"rtu" => new ModbusRtuProxy(client, _serverSerialDeviceOption.Value)
				{
					BaudRate = baudRate,
					DataBits = dataBits,
					StopBits = stopBits,
					Parity = parity
				},
				"tcp" => new ModbusTcpProxy(client, IPAddress.Parse(_serverTcpHostOption.Value))
				{
					ListenPort = port
				},
				_ => throw new ApplicationException($"Unknown client type: '{_serverOption.Value}'"),
			};
		}
	}
}
