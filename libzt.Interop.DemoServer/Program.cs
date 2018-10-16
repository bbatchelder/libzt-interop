using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace libzt.Interop.DemoServer
{
    class Program
    {
        static void Main(string[] args)
        {
            ulong nwid = 0x35c192ce9bea6859;

            int err = 0;

            try
            {
                Console.WriteLine("Waiting for ZeroTier to come online...");

                if ((err = libzt.zts_startjoin(Environment.CurrentDirectory, nwid)) == 1)
                {
                    throw new Exception(string.Format("Failed to start/join network [{0:X}]", nwid));
                }

                Console.WriteLine("Joined network [{0:X}] as node [{1:X}].", nwid, libzt.zts_get_node_id());

                if ((err = libzt.zts_has_address(nwid)) != 1)
                {
                    throw new Exception("No ZeroTier address assigned.");
                }

                Console.WriteLine("ZeroTier address assigned.");

                int sockaddr_storage_size = Marshal.SizeOf(typeof(libzt.SOCKADDR_STORAGE));
                var sockaddr_storage_buffer = new byte[sockaddr_storage_size];
                GCHandle sockaddr_storage_handle = GCHandle.Alloc(sockaddr_storage_buffer, GCHandleType.Pinned);
                IntPtr addressStoragePtr = GCHandle.ToIntPtr(sockaddr_storage_handle);

                if ((err = libzt.zts_get_address(nwid, addressStoragePtr, (int)libzt.SockAddrFamily.Inet)) != 0)
                {
                    throw new Exception("Error calling zts_get_address.");
                }

                libzt.SOCKADDR_STORAGE addressStorage = Marshal.PtrToStructure<libzt.SOCKADDR_STORAGE>(addressStoragePtr);

                if (err == 0)
                {
                    var ipbytes = BitConverter.GetBytes(addressStorage.Data2[0]);
                    var ip = new IPAddress(ipbytes);
                    Console.WriteLine("ZeroTier address [{0}]", ip);
                }
                else
                {
                    throw new Exception("Problem getting address.");
                }

                sockaddr_storage_handle.Free();

                int fd = -1;

                if ((fd = libzt.zts_socket((int)libzt.SockAddrFamily.Inet, (int)libzt.SockType.Stream, 0)) < 0)
                {
                    throw new Exception("Error creating socket");
                }

                Console.WriteLine("Created socked [{0}]", fd);

                if ((err = libzt.zts_fcntl(fd, 0, 0)) < 0)
                {
                    Console.WriteLine("Error setting O_NONBLOCK on fd.");
                }

                string localAddrStr = "0.0.0.0";
                Int16 localPort = 8008;
                var addr = new libzt.SOCKADDR_IN();
                addr.Family = (byte)libzt.SockAddrFamily.Inet;
                addr.Port = (ushort)IPAddress.HostToNetworkOrder(localPort);
                addr.Addr = BitConverter.ToUInt32(IPAddress.Parse(localAddrStr).GetAddressBytes(), 0);

                int addrlen = Marshal.SizeOf(typeof(libzt.SOCKADDR_IN));
                var addrPtr = Marshal.AllocHGlobal(addrlen);
                Marshal.StructureToPtr(addr, addrPtr, false);

                Console.Write("Trying to bind to local port [{0}]...", localPort);
                if ((err = libzt.zts_bind(fd, addrPtr, addrlen)) < 0)
                {
                    throw new Exception(string.Format("Could not bind to local port [{0}].", localPort));
                }
                Console.WriteLine("Done.");

                Console.Write("Trying to put socket into listening state...");
                if ((err = libzt.zts_listen(fd, 3)) < 0)
                {
                    throw new Exception("Cannot put socket in listening state.");
                }
                Console.WriteLine("Done.");

                int accept_fd = -1;

                if ((accept_fd = libzt.zts_accept(fd, IntPtr.Zero, 0)) < 0)
                {
                    Console.WriteLine("Error accepting incoming connection.");
                }

                Console.WriteLine("Press any key to shutdown...");
                Console.ReadKey();

                Marshal.FreeHGlobal(addrPtr);

                libzt.zts_close(fd);
                libzt.zts_stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}
