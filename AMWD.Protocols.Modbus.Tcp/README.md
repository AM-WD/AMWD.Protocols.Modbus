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

Console.WriteLine($"The voltage between L1 and N is: {voltage:N2}V");
```


## Sources

- Protocol Specification: [v1.1b3]
- Modbus TCP/IP: [v1.0b]


---

Published under MIT License (see [**tl;dr**Legal])



[v1.1b3]:         https://modbus.org/docs/Modbus_Application_Protocol_V1_1b3.pdf
[v1.0b]:          https://modbus.org/docs/Modbus_Messaging_Implementation_Guide_V1_0b.pdf
[**tl;dr**Legal]: https://www.tldrlegal.com/license/mit-license
