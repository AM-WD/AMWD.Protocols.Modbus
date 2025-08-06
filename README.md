# Modbus Protocol for .NET

Here you can find a basic implementation of the Modbus protocol.

![NuGet Version](https://shields.io/nuget/v/AMWD.Protocols.Modbus.Common?style=flat&logo=nuget)
![Test Coverage](https://git.am-wd.de/am-wd/amwd.protocols.modbus/badges/main/coverage.svg?style=flat)

## Overview

The project is divided into multiple parts.

To be mentioned at the beginning:    
Only the clients are build very modular to fit any requirement reached on the first implementation back in 2018 ([see here]).    
The server implementations will only cover their defaults!


### [Common]

Here you'll find all the common interfaces and base implementations for Modbus.

For example the default protocol versions: `TCP`, `RTU` and `ASCII`.

With this package you'll have anything you need to create your own client implementations.


### [Serial]

This package contains some wrappers and implementations for the serial protocol.
So you can use it out of the box to communicate via serial line ports / devices.


### [TCP]

This package contains the default implementations for network communication via TCP.
It uses a specific TCP connection implementation and plugs all things from the Common package together.


---

Published under [MIT License] (see [choose a license])    
[![Buy me a Coffee](https://shields.io/badge/PayPal-Buy_me_a_Coffee-yellow?style=flat&logo=paypal)](https://link.am-wd.de/donate)



[see here]:         https://github.com/andreasAMmueller/Modbus
[Common]:           src/AMWD.Protocols.Modbus.Common/README.md
[Serial]:           src/AMWD.Protocols.Modbus.Serial/README.md
[TCP]:              src/AMWD.Protocols.Modbus.Tcp/README.md
[MIT License]:      LICENSE.txt
[choose a license]: https://choosealicense.com/licenses/mit/
