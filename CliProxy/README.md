# Modbus CLI proxy

This project contains a small CLI tool to proxy Modbus connections.

```
Usage: modbus-proxy --server <rtu|tcp> --client <rtu|tcp> [options]

General options:
  --help, -h
    Shows this help message.

  --debug
    Waits for a debugger to be attached before starting.


Server options:
  --server <rtu|tcp>
    Defines whether to use an RTU or an TCP proxy.

  --server-baud #
    The baud rate (e.g. 9600) to use for the RTU proxy. Default: 19200.

  --server-databits #
    The number of data bits. Default: 8.

  --server-device <device-port>
    The serial port to use (e.g. COM1, /dev/ttyS0).

  --server-parity <none|odd|even>
    The parity to use. Default: even.

  --server-stopbits #
    The number of stop bits. Default: 1.

  --server-host <address>
    The IP address to listen on. Default: 127.0.0.1.

  --server-port #
    The port to listen on. Default: 502.


Client options:
  --client <rtu|tcp>
    Defines whether to use an RTU or an TCP client.

  --client-protocol <ascii|rtu|tcp>
    Select which Modbus protocol to use.

  --client-baud #
    The baud rate (e.g. 9600) to use for the RTU client. Default: 19200.

  --client-databits #
    The number of data bits. Default: 8.

  --client-device <device-port>
    The serial port to use (e.g. COM1, /dev/ttyS0).

  --client-parity <none|odd|even>
    The parity to use. Default: even.

  --client-stopbits #
    The number of stop bits. Default: 1.

  --client-enable-rs485
    Enables the RS485 software switch for serial adapters capable of RS232 and RS485.

  --client-host <hostname>
    The host to connect to.

  --client-port #
    The port to connect to. Default: 502.
```


---

Published under MIT License (see [choose a license])



[choose a license]: https://choosealicense.com/licenses/mit/
