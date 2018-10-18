using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace libzt.Interop.DemoServer
{
    class Program
    {
        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            ulong nwid = 0x35c192ce9bea6859;

            int err = 0;

            try
            {
                Console.WriteLine("Waiting for ZeroTier to come online...");

                GC.Collect();

                if ((err = libzt.zts_startjoin(Environment.CurrentDirectory, nwid)) == 1)
                {
                    throw new Exception(string.Format("Failed to start/join network [{0:X}]", nwid));
                }

                GC.Collect();

                Console.WriteLine("Joined network [{0:X}] as node [{1:X}].", nwid, libzt.zts_get_node_id());

                GC.Collect();

                if ((err = libzt.zts_has_address(nwid)) != 1)
                {
                    throw new Exception("No ZeroTier address assigned.");
                }

                GC.Collect();

                Console.WriteLine("ZeroTier address assigned.");

                //We allocate 4 times as much memory as we should need because somethin is screwy with the structure
                //and doing this prevents a crash due to memory corruption.
                IntPtr sockAddrPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(libzt.SOCKADDR_IN)) * 4);

                if ((err = libzt.zts_get_address(nwid, sockAddrPtr, (int)libzt.SockAddrFamily.Inet)) == 0)
                {
                    libzt.SOCKADDR_IN sockAddr = Marshal.PtrToStructure<libzt.SOCKADDR_IN>(sockAddrPtr);
                    var ipbytes = BitConverter.GetBytes(sockAddr.Addr);
                    var ip = new IPAddress(ipbytes);
                    Console.WriteLine("ZeroTier address [{0}]", ip);
                    Marshal.FreeHGlobal(sockAddrPtr);
                }
                else
                {
                    throw new Exception("Error calling zts_get_address.");
                }

                int numAddresses = 0;
                if((numAddresses = libzt.zts_get_num_assigned_addresses(nwid)) < 0)
                {
                    throw new Exception("Error calling zts_get_num_assigned_addresses");
                }

                Console.WriteLine("Bound to [{0}] ZeroTier addresses.", numAddresses);


                GC.Collect();

                int fd = -1;

                if ((fd = libzt.zts_socket((int)libzt.SockAddrFamily.Inet, (int)libzt.SockType.Stream, 0)) < 0)
                {
                    throw new Exception("Error creating socket");
                }

                Console.WriteLine("Created socket [{0}]", fd);

                GC.Collect();

                //Console.Write("Trying to set O_NONBLOCK flag on fd [{0}]...", fd);
                //if ((err = libzt.zts_fcntl(fd, (int)libzt.FCNTL_CMDS.F_SETFL, (int)libzt.FILE_ACCESS_MODES.O_NONBLOCK)) < 0)
                //{
                //    Console.WriteLine("Error setting O_NONBLOCK on fd.");
                //}
                //Console.WriteLine("Done.");

                //err = libzt.zts_fcntl(fd, (int)libzt.FCNTL_CMDS.F_GETFL, (int)libzt.FILE_ACCESS_MODES.O_NONBLOCK);
                //Console.WriteLine("Got back [{0}] from zts_fnctl trying to get the flag value for O_NONBLOCK on fd [{1}].", err, fd);

                string localAddrStr = "0.0.0.0";
                var ipAny = IPAddress.Parse(localAddrStr);

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

                GC.Collect();

                //Free unmanaged memory
                Marshal.FreeHGlobal(addrPtr);

                Console.Write("Trying to put socket into listening state...");
                if ((err = libzt.zts_listen(fd, 3)) < 0)
                {
                    throw new Exception("Cannot put socket in listening state.");
                }
                Console.WriteLine("Done.");

                GC.Collect();

                //Number of file descriptors to check when calling select().  Since fds are zero-based this should
                //be the highest value fd + 1.
                int nfds = 0;

                //Timeval struct to hold our timeout value when calling select().  Select() will return at the first sign of activity
                //or when the timeout elapses, whichever comes first.  Timeval holds both seconds and microseconds.
                var tv = new libzt.TIMEVAL();
                int msecs = 250;
                tv.tv_sec = msecs / 1000;
                tv.tv_usec = (msecs % 1000) * 1000;

                //File Descriptor Sets that we'll use when calling select.  In this example we really only care about the
                //read state, so that is the only set we populate.  The others are passed in empty.
                var readfds = new libzt.FDSET();
                var writefds = new libzt.FDSET();
                var exceptfds = new libzt.FDSET();

                //This FD set is used to track active client connections.  We'll use this to iterate through client sockets
                //and check if they are ready to be read from, and remove them from the set when the connection is closed.
                var handlerfds = new libzt.FDSET();

                //The message buffer used for receiving data.
                byte[] messageBuffer = new byte[32768];

                int fdret = -1;

                //Zero-out the list of handler FDs.
                libzt.FD_ZERO(ref handlerfds);

                Int64 loopCount = 0;
                Int64 byteCount = 0;
                Int64 tickCount = DateTime.Now.Ticks;
                Int64 tickInterval = 10000000;

                GC.Collect();

                while (true)
                {
                    loopCount++;

                    GC.Collect();

                    if (Console.KeyAvailable)
                        break;

                    //Clear all the fd sets
                    libzt.FD_ZERO(ref readfds);
                    libzt.FD_ZERO(ref writefds);
                    libzt.FD_ZERO(ref exceptfds);

                    //Add the main listener socket to readfds
                    libzt.FD_SET(fd, ref readfds);

                    //Reset nfds on each loop.  Important when client sockets start closing, so we don't poll
                    //dead sockets.
                    nfds = 1;

                    //Populate readfds with all our client sockets
                    for (int i=0; i<32; i++)
                    {
                        if (libzt.FD_ISSET(i, ref handlerfds) > 0)
                        {
                            libzt.FD_SET(i, ref readfds);

                            //Remember nfds must be the largest value fd + 1
                            if (nfds < (i + 1))
                                nfds = (i + 1);
                        }
                    }  

                    //Poll all the fds from 0 to nfds-1 that are also present in the various FD sets, returning prior to the timeout value;
                    int ret = libzt.zts_select(nfds, ref readfds, ref writefds, ref exceptfds, ref tv);

                    GC.Collect();

                    //If ret is zero then no sockets had the matching activity and the timeout elapsed.
                    //So all we can do is start the loop over again.
                    if (ret == 0)
                        continue;

                    //Service main listener socket if it is ready to be read.
                    if((fdret = libzt.FD_ISSET(fd, ref readfds)) > 0)
                    {
                        int hfd = -1;

                        if ((hfd = libzt.zts_accept(fd, IntPtr.Zero, 0)) < 0)
                        {
                            Console.WriteLine("Error accepting incoming connection.");
                        }

                        //Add this new client socket to our set of active sockets
                        Console.WriteLine("Accepted new connection [{0}]", hfd);
                        libzt.FD_SET(hfd, ref handlerfds);
                    }

                    int activeConnections = 0;

                    //Loop through the possible 32 file descriptors
                    for(int i=0; i<nfds; i++)
                    {
                        //If this fd is not an active client FD then continue loop
                        if (libzt.FD_ISSET(i, ref handlerfds) == 0)
                            continue;

                        activeConnections++;
                        int hfd = i;

                        //If fd is ready to be read, then read it.
                        if((fdret = libzt.FD_ISSET(hfd, ref readfds)) > 0)
                        {
                            //Have to clear the array or you will potentially get jumbled messages
                            Array.Clear(messageBuffer, 0, messageBuffer.Length);

                            err = libzt.zts_read(hfd, messageBuffer, messageBuffer.Length);

                            GC.Collect();

                            if (err > 0)
                            {
                                byteCount += err;
                                string msgStr = System.Text.Encoding.UTF8.GetString(messageBuffer, 0, err);
                                //Console.WriteLine("[{0}]: {1}", hfd, msgStr);
                                //Console.WriteLine("Connection [{0}]: [{1}] bytes received", hfd, byteCount);
                                
                            }
                            else if(err <= 0)
                            {
                                //If there was an error, close the socket and clear it from the handler FD set.
                                libzt.zts_close(hfd);
                                libzt.FD_CLR(hfd, ref handlerfds);
                                Console.WriteLine("Connection [{0}] closed by client [{1}].", hfd, byteCount);
                            }
                        }
                    }


                    Int64 ticksNow = DateTime.Now.Ticks;
                    if(((ticksNow - tickCount) / tickInterval) > 1)
                    {
                        tickCount = ticksNow;
                        Console.WriteLine("Received [{0:###,###,##0.00}] MBs via [{1}] connections", ((decimal)byteCount / (1024 * 1024)), activeConnections);
                    }
                }

                Console.ReadKey();

                //If we reached this point, a key has been pressed in the Console
                //indicating that we need to shut down and exit.
                Console.WriteLine("Closing sockets...");

                //Close all handler sockets
                for (int i = 0; i < 32; i++)
                {
                    if (libzt.FD_ISSET(i, ref handlerfds) > 0)
                    {
                        libzt.zts_close(i);
                        libzt.FD_CLR(i, ref handlerfds);
                    }
                }

                //Close main listening socket
                libzt.zts_close(fd);
                //Stop ZeroTier
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

        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject);
        }
    }
}
