# Modbus Protocol for .NET | Common

This package contains all basic tools to build your own clients.

### Contracts

**IModbusConnection**    
This is the interface used on the base client to communicate with the remote device.
If you want to use a custom connection type, you should implement this interface yourself.

**IModbusProtocol**    
If you want to speak a custom type of protocol with the clients, you can implement this interface.

**ModbusBaseClient**    
This abstract base client contains all the basic methods and handlings required to communicate via Modbus Protocol.
The packages `AMWD.Protocols.Modbus.Serial` _(in progress)_ and `AMWD.Protocols.Modbus.Tcp` _(in progress)_ have specific derived implementations to match the communication types.


### Enums

Here you have all typed enumerables defined by the Modbus Protocol.


### Extensions

To convert the Modbus specific types to usable values and vice-versa, there are some extensions.

- Decimal extensions for `float` (single) and `double`
- Signed extensions for signed integer values as `sbyte`, `short` (int16), `int` (int32) and `long` (int64)
- Unsigned extensions for unsigned integer values as `byte`, `ushort` (uint16), `uint` (uint32) and `ulong` (uint64)
- Some other extensions for `string` and `bool`


### Models

The different types handled by the Modbus Protocol.

- Coil
- Discrete Input
- Holding Register
- Input Register

In addition, you'll find the `DeviceIdentification` there.    
It is used for a "special" function called "Read Device Identification" (0x2B / 43) not supported by all devices.

The `ModbusDevice` is used for the server implementations in the derived packages.


### Protocols

Here you have the specific default implementations for the Modbus Protocol.

- ASCII _(in progress)_
- RTU _(in progress)_
- TCP


---

Published under MIT License (see [**tl;dr**Legal](https://www.tldrlegal.com/license/mit-license))
