# Modbus Protocol for .NET | Serial

The Modbus Serial protocol implementation.

## Example

A simple example which reads the voltage between L1 and N of a Janitza device.

```csharp
string serialPort = "COM5";

using var client = new ModbusSerialClient(serialPort);
await client.ConnectAsync(CancellationToken.None);

byte unitId = 5;
ushort startAddress = 19000;
ushort count = 2;

var registers = await client.ReadHoldingRegistersAsync(unitId, startAddress, count);
float voltage = registers.GetSingle();

Console.WriteLine($"The voltage of device #{unitId} between L1 and N is: {voltage:N2}V");
```

If you want to use the `ASCII` protocol instead, you can do this on initialization:

```csharp
// [...]

using var client = new ModbusSerialClient(serialPort)
{
	Protocol = new AsciiProtocol();
};

// [...]
```


## Sources

- Protocol Specification: [v1.1b3]
- Modbus Serial line: [v1.02]


---

Published under MIT License (see [**tl;dr**Legal])



[v1.1b3]:         https://modbus.org/docs/Modbus_Application_Protocol_V1_1b3.pdf
[v1.02]:          https://modbus.org/docs/Modbus_over_serial_line_V1_02.pdf
[**tl;dr**Legal]: https://www.tldrlegal.com/license/mit-license
