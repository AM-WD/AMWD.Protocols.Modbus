# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- New `AMWD.Protocols.Modbus.Proxy` package, that contains the server implementations as proxies.

### Changed

- Renamed `ModbusSerialServer` to `ModbusRtuServer` so clearify the protocol, that is used.
- Made `Protocol` property of `ModbusClientBase` non-abstract.


## [v0.2.0] (2024-04-02)

First "final" re-implementation.


## v0.1.0 (2022-08-28)

Was a first shot of a re-implementation... Was deleted and re-written again.    
So this tag is only here for documentation purposes of the NuGet Gallery.



[Unreleased]: https://github.com/AM-WD/AMWD.Protocols.Modbus/compare/v0.2.0...HEAD
[v0.2.0]: https://github.com/AM-WD/AMWD.Protocols.Modbus/tree/v0.2.0
