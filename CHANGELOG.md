# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

_no changes_


## [v0.3.1] (2024-06-28)

### Fixed

- Issues with range validation on several lines of code in server implementations.


## [v0.3.0] (2024-05-31)

### Added

- New `AMWD.Protocols.Modbus.Proxy` package, that contains the server implementations as proxies

### Changed

- Renamed `ModbusSerialServer` to `ModbusRtuServer` to clearify the protocol that is used
- Made `Protocol` property of `ModbusClientBase` non-abstract

### Fixed

- Issue with missing client on TCP connection when using default constructor (seems that `AddressFamily.Unknown` caused the problem)


## [v0.2.0] (2024-04-02)

First "final" re-implementation


## v0.1.0 (2022-08-28)

Was a first shot of a re-implementation... Was deleted and re-written again.    
So this tag is only here for documentation purposes of the NuGet Gallery.



[Unreleased]: https://github.com/AM-WD/AMWD.Protocols.Modbus/compare/v0.3.1...HEAD
[v0.3.1]: https://github.com/AM-WD/AMWD.Protocols.Modbus/compare/v0.3.0...v0.3.1
[v0.3.0]: https://github.com/AM-WD/AMWD.Protocols.Modbus/compare/v0.2.0...v0.3.0
[v0.2.0]: https://github.com/AM-WD/AMWD.Protocols.Modbus/tree/v0.2.0
