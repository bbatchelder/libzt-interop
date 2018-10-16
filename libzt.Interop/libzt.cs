using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace libzt.Interop
{
    public class libzt
    {
        public enum SockAddrFamily
        {
            Inet = 2,
            Inet6 = 10
        }

        public enum SockType
        {
            Stream = 1,
            Datagram = 2
        }

        public enum FCNTL_CMDS
        {
            F_GETFL = 3,
            F_SETFL = 0
        }

        public enum FILE_ACCESS_MODES
        {
            O_NONBLOCK = 0,
            O_NDELAY = 1,
            O_RDONLY = 2,
            O_WRONLY = 4,
            O_RDWR = 6
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SOCKADDR_STORAGE
        {
            public byte Length;
            public byte Family;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[] Data1;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public uint[] Data2;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public uint[] Data3;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SOCKADDR
        {
            public ushort Family;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
            public byte[] Data;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SOCKADDR_IN
        {
            public byte Length;
            public byte Family;
            public ushort Port;
            public uint Addr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Zero;
        }

        [DllImport("libzt.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zts_startjoin(string path, ulong nwid);

        [DllImport("libzt.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern Int64 zts_get_node_id();

        [DllImport("libzt.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern long zts_get_peer_count();

        [DllImport("libzt.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void zts_stop();

        [DllImport("libzt.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zts_has_address(ulong nwid);

        [DllImport("libzt.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zts_get_address(ulong nwid, IntPtr addr, int address_family);

        [DllImport("libzt.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zts_socket(int socket_family, int socket_type, int protocol);

        [DllImport("libzt.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zts_close(int fd);

        [DllImport("libzt.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zts_bind(int fd, IntPtr addr, int addrlen);

        [DllImport("libzt.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zts_listen(int fd, int backlog);

        [DllImport("libzt.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zts_fcntl(int fd, int cmd, int flags);

        [DllImport("libzt.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zts_accept(int fd, IntPtr addr, int addrlen);
    }
}
