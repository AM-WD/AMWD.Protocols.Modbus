# Modbus CLI client

This project contains a small CLI tool to test Modbus connections.

```
Usage: modbus-client [OPTIONS] <serial-port>|<tcp-host>

Serial Port:
  COM1, COM2, ...                  on Windows
  /dev/ttyS0, /dev/ttyUSB0, ...    on Linux

TCP Host:
  192.168.x.y        as IPv4
  fd00:1234:x:y::z   as IPv6

General Options:
  -h, --help
    Shows this help message.

  --debug
    Waits for a debugger to attach before starting.

  -m, --protocol <ascii|rtu|tcp>
    Select which protocol to use.

  -a, --address #
    The slave/device address. 1-247 for serial, 0-255 for TCP. Default: 1

  -r, --reference #
    The start reference to read from. 0-65535. Default: 0

  -c, --count #
    The number of values to read. Default: 1

  -t, --type <coil|discrete>
    Reads a discrete value (bool): Coil or Discrete Input.

  -t, --type input:<kind>
    Reads an input register. Kind: (e.g. i32)
      hex = print as HEX representation
      i   =   signed integer (8, 16, 32, 64)
      u   = unsigned integer (8, 16, 32, 64)
      f   = floating point   (32, 64)

  -t, --type holding:<kind>
    Reads a holding register. Kind: (e.g. i32)
      hex = print as HEX representation
      i   =   signed integer (8, 16, 32, 64)
      u   = unsigned integer (8, 16, 32, 64)
      f   = floating point   (32, 64)

  -t, --type id
    Tries to read the device identification (Fn 43, Regular).
    This option implies --once.

  -i, --interval #
    The polling interval in milliseconds. Default: 1000

  -o, --timeout #
    The timeout in milliseconds. Default: 1000

  -1, --once
    Just query once, no interval polling.


Serial Options:
  -b, --baud #
    The baud rate (e.g. 9600). Default: 19200

  -d, --databits #
    The number of data bits (7/8 for ASCII, otherwise 8). Default: 8

  -s, --stopbits #
    The number of stop bits (1/2). Default: 1

  -p, --parity <none|odd|even>
    The kind of parity. Default: even

  --enable-rs485
    Enables the RS485 software switch for serial adapters capable of RS232 and RS485.


TCP Options:
  -p, --port #
    The TCP port of the remote device. Default: 502
```


---

Published under MIT License (see [choose a license])



[choose a license]: https://choosealicense.com/licenses/mit/
