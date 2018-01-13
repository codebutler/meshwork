using System;
using System.Runtime.InteropServices;

namespace Meshwork.Platform.Linux
{
	public static class LibC
	{
		public const int AF_INET6 = 10;
		public const int AF_INET = 2;

		[DllImport("libc")]
		public static extern int if_nametoindex(string ifname);

		[DllImport("libc")]
		public static extern int getifaddrs(out IntPtr ifap);

		[DllImport("libc")]
		public static extern void freeifaddrs(IntPtr ifap);

		[DllImport("libc")] // Linux
		public static extern int prctl(int option, byte[] arg2, IntPtr arg3, IntPtr arg4, IntPtr arg5);

		[DllImport("libc")] // BSD
		public static extern void setproctitle(byte[] fmt, byte[] str_arg);
	}

	[StructLayout(LayoutKind.Explicit)]
	struct ifa_ifu
	{
		[FieldOffset(0)]
		public IntPtr ifu_broadaddr;

		[FieldOffset(0)]
		public IntPtr ifu_dstaddr;
	}

	struct ifaddrs
	{
		public IntPtr ifa_next;
		public string ifa_name;
		public uint ifa_flags;
		public IntPtr ifa_addr;
		public IntPtr ifa_netmask;
		public ifa_ifu ifa_ifu;
		public IntPtr ifa_data;
	}

	struct sockaddr_in
	{
		public ushort sin_family;
		public ushort sin_port;
		public uint sin_addr;
	}

	struct sockaddr_in6
	{
		public ushort sin6_family;   /* AF_INET6 */
		public ushort sin6_port;     /* Transport layer port # */
		public uint sin6_flowinfo; /* IPv6 flow information */
		public in6_addr sin6_addr;     /* IPv6 address */
		public uint sin6_scope_id; /* scope id (new in RFC2553) */
	}

	struct in6_addr
	{
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
		public byte[] u6_addr8;
	}
}
