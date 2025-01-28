# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Small CLI client for Modbus communication.
- Small CLI proxy to forward messages.
- `VirtualModbusClient` added to `AMWD.Protocols.Modbus.Common`.

### Changed

- The `ModbusTcpProxy.ReadWriteTimeout` has a default value of 100 seconds (same default as a `HttpClient` has).
- The `ModbusRtuProxy` moved from `AMWD.Protocols.Modbus.Proxy` to `AMWD.Protocols.Modbus.Serial`.
- The `ModbusTcpProxy` moved from `AMWD.Protocols.Modbus.Proxy` to `AMWD.Protocols.Modbus.Tcp`.
- Server implementations are proxies with a virtual Modbus client.

### Removed

- The `AMWD.Protocols.Modbus.Proxy` (introduced in [v0.3.0]) has been removed.

### Fixed

- Wrong _following bytes_ calculation in `ModbusTcpProxy`.
- Wrong processing of `WriteMultipleHoldingRegisters` for proxies.


## [v0.3.2] (2024-09-04)

### Added

- Build configuration for strong named assemblies.


## [v0.3.1] (2024-06-28)

### Fixed

- Issues with range validation on several lines of code in server implementations.


## [v0.3.0] (2024-05-31)

### Added

- New `AMWD.Protocols.Modbus.Proxy` package, that contains the server implementations as proxies.

### Changed

- Renamed `ModbusSerialServer` to `ModbusRtuServer` to clearify the protocol that is used.
- Made `Protocol` property of `ModbusClientBase` non-abstract.

### Fixed

- Issue with missing client on TCP connection when using default constructor (seems that `AddressFamily.Unknown` caused the problem).


## [v0.2.0] (2024-04-02)

First "final" re-implementation.


## v0.1.0 (2022-08-28)

Was a first shot of a re-implementation... Was deleted and re-written again.    
So this tag is only here for documentation purposes of the NuGet Gallery.



[Unreleased]: https://github.com/AM-WD/AMWD.Protocols.Modbus/compare/v0.3.2...HEAD
[v0.3.2]: https://github.com/AM-WD/AMWD.Protocols.Modbus/compare/v0.3.1...v0.3.2
[v0.3.1]: https://github.com/AM-WD/AMWD.Protocols.Modbus/compare/v0.3.0...v0.3.1
[v0.3.0]: https://github.com/AM-WD/AMWD.Protocols.Modbus/compare/v0.2.0...v0.3.0
[v0.2.0]: https://github.com/AM-WD/AMWD.Protocols.Modbus/tree/v0.2.0
