# Getting Started

To begin, you need at least the [Common] package.

In this package you'll find everything you need to implement you own client as the package contains the protocol implementations (`TCP`, `RTU` and `ASCII`).

The [`ModbusClientBase`](~/api/AMWD.Protocols.Modbus.Common.Contracts.ModbusClientBase.yml) is the place, where most of the magic happens.
In this base client you have all known (and implemented) methods to request a device.

The Protocol implementations are the other magic place to be, as there the request will be converted into bits and bytes, before they get transfered.


## Using a TCP client

To use a TCP Modbus client, you need the [Common] package and the [TCP] package installed.

```cs
using AMWD.Protocols.Modbus.Common;
using AMWD.Protocols.Modbus.Common.Contracts;
using AMWD.Protocols.Modbus.Common.Protocols;
using AMWD.Protocols.Modbus.Tcp;

namespace ConsoleApp;

internal class Program
{
	string hostname = "modbus-device.internal";
	int port = 502;

	byte unitId = 5;
	ushort startAddress = 19000;
	ushort count = 2;

	using var client = new ModbusTcpClient(hostname, port);
	await client.ConnectAsync(CancellationToken.None);

	var holdingRegisters = await client.ReadHoldingRegistersAsync(unitId, startAddress, count);
	float voltage = holdingRegisters.GetSingle();

	Console.WriteLine($"The voltage of the device #{unitId} between L1 and N is {voltage:N2}V.");
}
```

This will automatically create a TCP client using the TCP protocol.
If you want to change the protocol sent over TCP, you can specify it:

```cs
// [...] other code

using var client = new ModbusTcpClient(hostname, port)
{
	Protocol = new RtuProtocol()
};

// [...] other code
```


## Using a Serial client

To use a Serial Modbus client, you need the [Common] package and the [Serial] package installed.

```cs
using AMWD.Protocols.Modbus.Common;
using AMWD.Protocols.Modbus.Common.Contracts;
using AMWD.Protocols.Modbus.Common.Protocols;
using AMWD.Protocols.Modbus.Serial;

namespace ConsoleApp;

internal class Program
{
	string serialPort = "/dev/ttyUSB0";

	byte unitId = 5;
	ushort startAddress = 19000;
	ushort count = 2;

	using var client = new ModbusSerialClient(serialPort);
	await client.ConnectAsync(CancellationToken.None);

	var holdingRegisters = await client.ReadHoldingRegistersAsync(unitId, startAddress, count);
	float voltage = holdingRegisters.GetSingle();

	Console.WriteLine($"The voltage of the device #{unitId} between L1 and N is {voltage:N2}V.");
}
```

This will automatically create a Serial client using the RTU protocol.
If you want to change the protocol sent over serial line, you can specify it:

```cs
// [...] other code

using var client = new ModbusSerialClient(serialPort)
{
	Protocol = new AsciiProtocol()
};

// [...] other code
```


[Common]: https://www.nuget.org/packages/AMWD.Protocols.Modbus.Common
[Serial]: https://www.nuget.org/packages/AMWD.Protocols.Modbus.Serial
[TCP]: https://www.nuget.org/packages/AMWD.Protocols.Modbus.Tcp