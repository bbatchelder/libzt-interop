# libzt-interop
Working towards a fully tested .NET interop layer for libzt (ZeroTier).

Right now we have a very basic interop project that contains the needed structs, enums, and static functions to interop with libzt.

There is also a DemoServer project that implements a very basic single-threaded TCP server.

# Roadmap
Implement a multi-threaded proxy server (to expose services on a host onto the ZeroTier network)

Create a managed library that abstracts the interop code, so it more resembles working with C# sockets and streams.

# Scope
Right now UDP and IPv6 are low priority, but I do plan on addressing them at some point.
