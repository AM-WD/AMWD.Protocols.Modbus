# Modbus Protocol for .NET

Here you can find a basic implementation of the Modbus protocol.

## Overview

The project is divided into four parts.

To be mentioned at the beginning:    
Only the clients are build very modular to fit any requirement reached on the first implementation back in 2018 ([see here]).    
The server implementations will only cover their defaults!


### [Common]

Here you'll find all the common interfaces and base implementations for Modbus.

For example the default protocol versions: `TCP`, `RTU` and `ASCII`.

With this package you'll have anything you need to create your own client implementations.


### [Proxy]

The package contains a TCP and a RTU server implementation as proxy which contains a client of your choice to connect to.


### [Serial]

This package contains some wrappers and implementations for the serial protocol.
So you can use it out of the box to communicate via serial line ports / devices.


### [TCP]

This package contains the default implementations for network communication via TCP.
It uses a specific TCP connection implementation and plugs all things from the Common package together.


---

Published under [MIT License] (see [choose a license])    
[![Buy me a Coffee](https://shields.am-wd.de/badge/PayPal-Buy_me_a_Coffee-yellow?style=flat&logo=paypal)](https://link.am-wd.de/donate)
[![built with Codeium](https://codeium.com/badges/main)](https://link.am-wd.de/codeium)



[see here]:         https://github.com/andreasAMmueller/Modbus
[Common]:           AMWD.Protocols.Modbus.Common/README.md
[Proxy]:            AMWD.Protocols.Modbus.Proxy/README.md
[Serial]:           AMWD.Protocols.Modbus.Serial/README.md
[TCP]:              AMWD.Protocols.Modbus.Tcp/README.md
[MIT License]:      LICENSE.txt
[choose a license]: https://choosealicense.com/licenses/mit/
