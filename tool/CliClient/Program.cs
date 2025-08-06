using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AMWD.Common.Cli;
using AMWD.Protocols.Modbus.Common;
using AMWD.Protocols.Modbus.Common.Contracts;
using AMWD.Protocols.Modbus.Common.Protocols;
using AMWD.Protocols.Modbus.Serial;
using AMWD.Protocols.Modbus.Tcp;

namespace AMWD.Protocols.Modbus.CliClient
{
	internal class Program
	{
		// General
		private static string _target;
		private static Option _helpOption;
		private static Option _debugOption;

		private static Option _protocolOption;
		private static Option _addressOption;
		private static Option _referenceOption;
		private static Option _countOption;
		private static Option _typeOption;
		private static Option _intervalOption;
		private static Option _timeoutOption;
		private static Option _onceOption;

		// Serial
		private static Option _baudOption;
		private static Option _dataBitsOption;
		private static Option _stopBitsOption;
		private static Option _parityOption;
		private static Option _softSwitchOption;

		// TCP
		private static Option _portOption;

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

			if (string.IsNullOrWhiteSpace(_target))
			{
				Console.Error.WriteLine("No serial port or tcp host specified.");
				return 1;
			}

			if (!_typeOption.IsSet)
			{
				Console.Error.WriteLine("No type specified.");
				return 1;
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

			using var client = CreateClient();

			if (_protocolOption.IsSet)
			{
				switch (_protocolOption.Value.ToLower())
				{
					case "ascii": client.Protocol = new AsciiProtocol(); break;
					case "rtu": client.Protocol = new RtuProtocol(); break;
					case "tcp": client.Protocol = new TcpProtocol(); break;
				}
			}

			byte deviceAddress = 1;
			if (_addressOption.IsSet && byte.TryParse(_addressOption.Value, out byte addressValue))
				deviceAddress = addressValue;

			ushort reference = 0;
			if (_referenceOption.IsSet && ushort.TryParse(_referenceOption.Value, out ushort referenceValue))
				reference = referenceValue;

			ushort count = 1;
			if (_countOption.IsSet && ushort.TryParse(_countOption.Value, out ushort countValue))
				count = countValue;

			int interval = 1000;
			if (_intervalOption.IsSet && int.TryParse(_intervalOption.Value, out int intervalValue))
				interval = intervalValue;

			bool runOnce = _onceOption.IsSet;

			do
			{
				try
				{
					if (_typeOption.Value.ToLower() == "id")
					{
						runOnce = true;

						var deviceIdentification = await client.ReadDeviceIdentificationAsync(deviceAddress, ModbusDeviceIdentificationCategory.Regular, cancellationToken: cts.Token);
						Console.WriteLine(deviceIdentification);
					}
					else if (_typeOption.Value.ToLower() == "coil")
					{
						var coils = await client.ReadCoilsAsync(deviceAddress, reference, count, cts.Token);
						foreach (var coil in coils)
							Console.WriteLine($"  Coil {coil.Address}: {coil.Value}");
					}
					else if (_typeOption.Value.ToLower() == "discrete")
					{
						var discreteInputs = await client.ReadDiscreteInputsAsync(deviceAddress, reference, count, cts.Token);
						foreach (var discreteInput in discreteInputs)
							Console.WriteLine($"  Discrete Input {discreteInput.Address}: {discreteInput.Value}");
					}
					else if (_typeOption.Value.StartsWith("input", StringComparison.OrdinalIgnoreCase))
					{
						string type = _typeOption.Value.ToLower().Split(':').Last();
						switch (type)
						{
							case "hex":
								{
									var registers = await client.ReadInputRegistersAsync(deviceAddress, reference, count, cts.Token);
									for (int i = 0; i < count; i++)
									{
										var register = registers[i];
										Console.WriteLine($"  Input Register {register.Address}: {register.HighByte:X2} {register.LowByte:X2}");
									}
								}
								break;

							case "i8":
								{
									var registers = await client.ReadInputRegistersAsync(deviceAddress, reference, count, cts.Token);
									for (int i = 0; i < count; i++)
									{
										var register = registers[i];
										Console.WriteLine($"  Input Register {register.Address}: {register.GetSByte()}");
									}
								}
								break;
							case "i16":
								{
									var registers = await client.ReadInputRegistersAsync(deviceAddress, reference, count, cts.Token);
									for (int i = 0; i < count; i++)
									{
										var register = registers[i];
										Console.WriteLine($"  Input Register {register.Address}: {register.GetInt16()}");
									}
								}
								break;
							case "i32":
								{
									int cnt = count * 2;
									var registers = await client.ReadInputRegistersAsync(deviceAddress, reference, (ushort)cnt, cts.Token);
									for (int i = 0; i < cnt; i += 2)
									{
										var subRegisters = registers.Skip(i).Take(2);
										Console.WriteLine($"  Input Register {subRegisters.First().Address}: {subRegisters.GetInt32()}");
									}
								}
								break;
							case "i64":
								{
									int cnt = count * 4;
									var registers = await client.ReadInputRegistersAsync(deviceAddress, reference, (ushort)cnt, cts.Token);
									for (int i = 0; i < cnt; i += 4)
									{
										var subRegisters = registers.Skip(i).Take(4);
										Console.WriteLine($"  Input Register {subRegisters.First().Address}: {subRegisters.GetInt64()}");
									}
								}
								break;

							case "u8":
								{
									var registers = await client.ReadInputRegistersAsync(deviceAddress, reference, count, cts.Token);
									for (int i = 0; i < count; i++)
									{
										var register = registers[i];
										Console.WriteLine($"  Input Register {register.Address}: {register.GetByte()}");
									}
								}
								break;
							case "u16":
								{
									var registers = await client.ReadInputRegistersAsync(deviceAddress, reference, count, cts.Token);
									for (int i = 0; i < count; i++)
									{
										var register = registers[i];
										Console.WriteLine($"  Input Register {register.Address}: {register.GetUInt16()}");
									}
								}
								break;
							case "u32":
								{
									int cnt = count * 2;
									var registers = await client.ReadInputRegistersAsync(deviceAddress, reference, (ushort)cnt, cts.Token);
									for (int i = 0; i < cnt; i += 2)
									{
										var subRegisters = registers.Skip(i).Take(2);
										Console.WriteLine($"  Input Register {subRegisters.First().Address}: {subRegisters.GetUInt32()}");
									}
								}
								break;
							case "u64":
								{
									int cnt = count * 4;
									var registers = await client.ReadInputRegistersAsync(deviceAddress, reference, (ushort)cnt, cts.Token);
									for (int i = 0; i < cnt; i += 4)
									{
										var subRegisters = registers.Skip(i).Take(4);
										Console.WriteLine($"  Input Register {subRegisters.First().Address}: {subRegisters.GetUInt64()}");
									}
								}
								break;

							case "f32":
								{
									int cnt = count * 2;
									var registers = await client.ReadInputRegistersAsync(deviceAddress, reference, (ushort)cnt, cts.Token);
									for (int i = 0; i < cnt; i += 2)
									{
										var subRegisters = registers.Skip(i).Take(2);
										Console.WriteLine($"  Input Register {subRegisters.First().Address}: {subRegisters.GetSingle()}");
									}
								}
								break;
							case "f64":
								{
									int cnt = count * 4;
									var registers = await client.ReadInputRegistersAsync(deviceAddress, reference, (ushort)cnt, cts.Token);
									for (int i = 0; i < cnt; i += 4)
									{
										var subRegisters = registers.Skip(i).Take(4);
										Console.WriteLine($"  Input Register {subRegisters.First().Address}: {subRegisters.GetDouble()}");
									}
								}
								break;
						}
					}
					else if (_typeOption.Value.StartsWith("holding", StringComparison.OrdinalIgnoreCase))
					{
						string type = _typeOption.Value.ToLower().Split(':').Last();
						switch (type)
						{
							case "hex":
								{
									var registers = await client.ReadHoldingRegistersAsync(deviceAddress, reference, count, cts.Token);
									for (int i = 0; i < count; i++)
									{
										var register = registers[i];
										Console.WriteLine($"  Holding Register {register.Address}: {register.HighByte:X2} {register.LowByte:X2}");
									}
								}
								break;

							case "i8":
								{
									var registers = await client.ReadHoldingRegistersAsync(deviceAddress, reference, count, cts.Token);
									for (int i = 0; i < count; i++)
									{
										var register = registers[i];
										Console.WriteLine($"  Holding Register {register.Address}: {register.GetSByte()}");
									}
								}
								break;
							case "i16":
								{
									var registers = await client.ReadHoldingRegistersAsync(deviceAddress, reference, count, cts.Token);
									for (int i = 0; i < count; i++)
									{
										var register = registers[i];
										Console.WriteLine($"  Holding Register {register.Address}: {register.GetInt16()}");
									}
								}
								break;
							case "i32":
								{
									int cnt = count * 2;
									var registers = await client.ReadHoldingRegistersAsync(deviceAddress, reference, (ushort)cnt, cts.Token);
									for (int i = 0; i < cnt; i += 2)
									{
										var subRegisters = registers.Skip(i).Take(2);
										Console.WriteLine($"  Holding Register {subRegisters.First().Address}: {subRegisters.GetInt32()}");
									}
								}
								break;
							case "i64":
								{
									int cnt = count * 4;
									var registers = await client.ReadHoldingRegistersAsync(deviceAddress, reference, (ushort)cnt, cts.Token);
									for (int i = 0; i < cnt; i += 4)
									{
										var subRegisters = registers.Skip(i).Take(4);
										Console.WriteLine($"  Holding Register {subRegisters.First().Address}: {subRegisters.GetInt64()}");
									}
								}
								break;

							case "u8":
								{
									var registers = await client.ReadHoldingRegistersAsync(deviceAddress, reference, count, cts.Token);
									for (int i = 0; i < count; i++)
									{
										var register = registers[i];
										Console.WriteLine($"  Holding Register {register.Address}: {register.GetByte()}");
									}
								}
								break;
							case "u16":
								{
									var registers = await client.ReadHoldingRegistersAsync(deviceAddress, reference, count, cts.Token);
									for (int i = 0; i < count; i++)
									{
										var register = registers[i];
										Console.WriteLine($"  Holding Register {register.Address}: {register.GetUInt16()}");
									}
								}
								break;
							case "u32":
								{
									int cnt = count * 2;
									var registers = await client.ReadHoldingRegistersAsync(deviceAddress, reference, (ushort)cnt, cts.Token);
									for (int i = 0; i < cnt; i += 2)
									{
										var subRegisters = registers.Skip(i).Take(2);
										Console.WriteLine($"  Holding Register {subRegisters.First().Address}: {subRegisters.GetUInt32()}");
									}
								}
								break;
							case "u64":
								{
									int cnt = count * 4;
									var registers = await client.ReadHoldingRegistersAsync(deviceAddress, reference, (ushort)cnt, cts.Token);
									for (int i = 0; i < cnt; i += 4)
									{
										var subRegisters = registers.Skip(i).Take(4);
										Console.WriteLine($"  Holding Register {subRegisters.First().Address}: {subRegisters.GetUInt64()}");
									}
								}
								break;

							case "f32":
								{
									int cnt = count * 2;
									var registers = await client.ReadHoldingRegistersAsync(deviceAddress, reference, (ushort)cnt, cts.Token);
									for (int i = 0; i < cnt; i += 2)
									{
										var subRegisters = registers.Skip(i).Take(2);
										Console.WriteLine($"  Holding Register {subRegisters.First().Address}: {subRegisters.GetSingle()}");
									}
								}
								break;
							case "f64":
								{
									int cnt = count * 4;
									var registers = await client.ReadHoldingRegistersAsync(deviceAddress, reference, (ushort)cnt, cts.Token);
									for (int i = 0; i < cnt; i += 4)
									{
										var subRegisters = registers.Skip(i).Take(4);
										Console.WriteLine($"  Holding Register {subRegisters.First().Address}: {subRegisters.GetDouble()}");
									}
								}
								break;
						}
					}
					else
					{
						Console.Error.WriteLine($"Unknown type: {_typeOption.Value}");
						return 1;
					}

					Console.WriteLine();
					await Task.Delay(interval, cts.Token);
				}
				catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
				{
					return 0;
				}
				catch (Exception ex)
				{
					Console.WriteLine($"  {ex.GetType().Name}: {ex.Message}");
				}
			}
			while (!runOnce && !cts.Token.IsCancellationRequested);

			return 0;
		}

		private static bool ParseArguments(string[] args)
		{
			var cmdLine = new CommandLineParser();

			_helpOption = cmdLine.RegisterOption("help").Alias("h");
			_debugOption = cmdLine.RegisterOption("debug");

			// General Options
			_protocolOption = cmdLine.RegisterOption("protocol", 1).Alias("m");
			_addressOption = cmdLine.RegisterOption("address", 1).Alias("a");
			_referenceOption = cmdLine.RegisterOption("reference", 1).Alias("r");
			_countOption = cmdLine.RegisterOption("count", 1).Alias("c");
			_typeOption = cmdLine.RegisterOption("type", 1).Alias("t");
			_intervalOption = cmdLine.RegisterOption("interval", 1).Alias("i");
			_timeoutOption = cmdLine.RegisterOption("timeout", 1).Alias("o");
			_onceOption = cmdLine.RegisterOption("once").Alias("1");

			// Serial Options
			_baudOption = cmdLine.RegisterOption("baud", 1).Alias("b");
			_dataBitsOption = cmdLine.RegisterOption("data-bits", 1).Alias("d");
			_stopBitsOption = cmdLine.RegisterOption("stop-bits", 1).Alias("s");
			_parityOption = cmdLine.RegisterOption("parity", 1).Alias("p");
			_softSwitchOption = cmdLine.RegisterOption("enable-rs485");

			// TCP Options
			_portOption = cmdLine.RegisterOption("port", 1).Alias("p");

			try
			{
				cmdLine.Parse(args);
				_target = cmdLine.FreeArguments.FirstOrDefault();
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
			Console.WriteLine($"Usage: {typeof(Program).Assembly.GetName().Name} [OPTIONS] <serial-port>|<tcp-host>");
			Console.WriteLine();
			Console.WriteLine("Serial Port:");
			Console.WriteLine("  COM1, COM2, ...                  on Windows");
			Console.WriteLine("  /dev/ttyS0, /dev/ttyUSB0, ...    on Linux");
			Console.WriteLine();
			Console.WriteLine("TCP Host:");
			Console.WriteLine("  192.168.x.y        as IPv4");
			Console.WriteLine("  fd00:1234:x:y::z   as IPv6");
			Console.WriteLine();
			Console.WriteLine("General Options:");
			Console.WriteLine("  -h, --help");
			Console.WriteLine("    Shows this help message.");
			Console.WriteLine();
			Console.WriteLine("  --debug");
			Console.WriteLine("    Waits for a debugger to attach before starting.");
			Console.WriteLine();
			Console.WriteLine("  -m, --protocol <ascii|rtu|tcp>");
			Console.WriteLine("    Select which protocol to use.");
			Console.WriteLine();
			Console.WriteLine("  -a, --address #");
			Console.WriteLine("    The slave/device address. 1-247 for serial, 0-255 for TCP. Default: 1");
			Console.WriteLine();
			Console.WriteLine("  -r, --reference #");
			Console.WriteLine("    The start reference to read from. 0-65535. Default: 0");
			Console.WriteLine();
			Console.WriteLine("  -c, --count #");
			Console.WriteLine("    The number of values to read. Default: 1");
			Console.WriteLine();
			Console.WriteLine("  -t, --type <coil|discrete>");
			Console.WriteLine("    Reads a discrete value (bool): Coil or Discrete Input.");
			Console.WriteLine();
			Console.WriteLine("  -t, --type input:<kind>");
			Console.WriteLine("    Reads an input register. Kind: (e.g. i32)");
			Console.WriteLine("      hex = print as HEX representation");
			Console.WriteLine("      i   =   signed integer (8, 16, 32, 64)");
			Console.WriteLine("      u   = unsigned integer (8, 16, 32, 64)");
			Console.WriteLine("      f   = floating point   (32, 64)");
			Console.WriteLine();
			Console.WriteLine("  -t, --type holding:<kind>");
			Console.WriteLine("    Reads a holding register. Kind: (e.g. i32)");
			Console.WriteLine("      hex = print as HEX representation");
			Console.WriteLine("      i   =   signed integer (8, 16, 32, 64)");
			Console.WriteLine("      u   = unsigned integer (8, 16, 32, 64)");
			Console.WriteLine("      f   = floating point   (32, 64)");
			Console.WriteLine();
			Console.WriteLine("  -t, --type id");
			Console.WriteLine("    Tries to read the device identification (Fn 43, Regular).");
			Console.WriteLine("    This option implies --once.");
			Console.WriteLine();
			Console.WriteLine("  -i, --interval #");
			Console.WriteLine("    The polling interval in milliseconds. Default: 1000");
			Console.WriteLine();
			Console.WriteLine("  -o, --timeout #");
			Console.WriteLine("    The timeout in milliseconds. Default: 1000");
			Console.WriteLine();
			Console.WriteLine("  -1, --once");
			Console.WriteLine("    Just query once, no interval polling.");
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine("Serial Options:");
			Console.WriteLine("  -b, --baud #");
			Console.WriteLine("    The baud rate (e.g. 9600). Default: 19200");
			Console.WriteLine();
			Console.WriteLine("  -d, --databits #");
			Console.WriteLine("    The number of data bits (7/8 for ASCII, otherwise 8). Default: 8");
			Console.WriteLine();
			Console.WriteLine("  -s, --stopbits #");
			Console.WriteLine("    The number of stop bits (1/2). Default: 1");
			Console.WriteLine();
			Console.WriteLine("  -p, --parity <none|odd|even>");
			Console.WriteLine("    The kind of parity. Default: even");
			Console.WriteLine();
			Console.WriteLine("  --enable-rs485");
			Console.WriteLine("    Enables the RS485 software switch for serial adapters capable of RS232 and RS485.");
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine("TCP Options:");
			Console.WriteLine("  -p, --port #");
			Console.WriteLine("    The TCP port of the remote device. Default: 502");
			Console.WriteLine();
		}

		private static bool IsSerialTarget()
		{
			return OperatingSystem.IsWindows()
				? _target.StartsWith("COM", StringComparison.OrdinalIgnoreCase)
				: _target.StartsWith("/dev/", StringComparison.OrdinalIgnoreCase);
		}

		private static ModbusClientBase CreateClient()
		{
			int timeout = 1000;
			if (_timeoutOption.IsSet && int.TryParse(_timeoutOption.Value, out int timeoutValue))
				timeout = timeoutValue;

			if (IsSerialTarget())
			{
				BaudRate baudRate = BaudRate.Baud19200;
				if (_baudOption.IsSet && int.TryParse(_baudOption.Value, out int baudRateValue))
					baudRate = (BaudRate)baudRateValue;

				int dataBits = 8;
				if (_dataBitsOption.IsSet && int.TryParse(_dataBitsOption.Value, out int dataBitsValue))
					dataBits = dataBitsValue;

				StopBits stopBits = StopBits.One;
				if (_stopBitsOption.IsSet && float.TryParse(_stopBitsOption.Value, out float stopBitsValue))
				{
					switch (stopBitsValue)
					{
						case 1.0f: stopBits = StopBits.One; break;
						case 1.5f: stopBits = StopBits.OnePointFive; break;
						case 2.0f: stopBits = StopBits.Two; break;
					}
				}

				Parity parity = Parity.Even;
				if (_parityOption.IsSet)
				{
					switch (_parityOption.Value.ToLower())
					{
						case "none": parity = Parity.None; break;
						case "odd": parity = Parity.Odd; break;
						case "even": parity = Parity.Even; break;
					}
				}

				bool enableRs485 = _softSwitchOption.IsSet;

				var client = new ModbusSerialClient(_target)
				{
					BaudRate = baudRate,
					DataBits = dataBits,
					StopBits = stopBits,
					Parity = parity,

					ReadTimeout = TimeSpan.FromMilliseconds(timeout),
					WriteTimeout = TimeSpan.FromMilliseconds(timeout),

					DriverEnabledRS485 = enableRs485
				};

				Console.WriteLine(client);
				return client;
			}
			else
			{
				int port = 502;
				if (_portOption.IsSet && int.TryParse(_portOption.Value, out int portValue))
					port = portValue;

				var client = new ModbusTcpClient(_target)
				{
					Port = port,

					ReadTimeout = TimeSpan.FromMilliseconds(timeout),
					WriteTimeout = TimeSpan.FromMilliseconds(timeout),
				};

				Console.WriteLine(client);
				return client;
			}
		}
	}
}
