# Introduction

During my training, I came into contact with the Modbus protocol.
The implementation I developed at that time was very cumbersome and rigid.
There were huge inheritance hierarchies and the design was very confusing.

In 2018, I wanted to do better and completely redesigned the library.
After changing companies, this library could be integrated and tested under real-world conditions.
This quickly led to new challenges and some specific requirements were implemented.
This was the first time that both TCP and the RTU protocol were fully implemented.

However, the structure of the library also revealed problems and was too rigid for the requirements.
Therefore, in 2024, there was a new development from scratch, which now exists and has already been tested by some eager people – THANK YOU SO MUCH!

The focus is, of course, on the development of the protocol and the clients. However, a server implementation (TCP/RTU) is also available.

For detailed changes of the current development, see the [CHANGELOG].


[CHANGELOG]: https://github.com/AM-WD/AMWD.Protocols.Modbus/blob/main/CHANGELOG.md