# Modbus Protocol for .NET | TCP

The Modbus TCP protocol implementation.

## Example

A simple example which reads the voltage between L1 and N of a Janitza device.

```csharp
string host = "modbus-device.internal";
int port = 502;

using var client = new ModbusTcpClient(host, port);
await client.ConnectAsync(CancellationToken.None);

byte unitId = 5;
ushort startAddress = 19000;
ushort count = 2;

var registers = await client.ReadHoldingRegistersAsync(unitId, startAddress, count);
float voltage = registers.GetSingle();

Console.WriteLine($"The voltage of device #{unitId} between L1 and N is: {voltage:N2}V");
```

If you have a device speaking `RTU` connected over `TCP`, you can use it as followed:

```csharp
// [...]

using var client = new ModbusTcpClient(host, port)
{
	Protocol = new RtuProtocol()
};

// [...]
```

## Sources

- Protocol Specification: [v1.1b3]
- Modbus TCP/IP: [v1.0b]


---

Published under MIT License (see [choose a license])



[v1.1b3]:           https://modbus.org/docs/Modbus_Application_Protocol_V1_1b3.pdf
[v1.0b]:            https://modbus.org/docs/Modbus_Messaging_Implementation_Guide_V1_0b.pdf
[choose a license]: https://choosealicense.com/licenses/mit/
