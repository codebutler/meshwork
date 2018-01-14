//
// OSX.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using Mono.Unix;

namespace Meshwork.Platform.MacOS
{
	public class OSXPlatform : IPlatform
	{
		public string UserName {
			get {
				var user = UnixUserInfo.GetRealUser();
				return user.UserName;
			}
		}
		
		public string RealName {
			get {
				var user = UnixUserInfo.GetRealUser();
				if (user.RealName != null) {
					return user.RealName;
				}
			    return UserName;
			}
		}
		
		public string VersionInfo {
			get {
				// XXX: Find the OSX version
				return "OSX";
			}
		}

		public InterfaceAddress[] GetInterfaceAddresses ()
		{
			var result = new List<InterfaceAddress>();

			IntPtr ifap;

			if (getifaddrs(out ifap) != 0) {
				throw new SystemException("getifaddrs() failed");
			}

			try {
				var next = ifap;
					
				while (next != IntPtr.Zero) {
					var addr = (ifaddrs) Marshal.PtrToStructure(next, typeof(ifaddrs));

					var name = addr.ifa_name;

					if (addr.ifa_addr != IntPtr.Zero) {
						var index = if_nametoindex(name);

						var sockaddr = (sockaddr_in) Marshal.PtrToStructure(addr.ifa_addr, typeof(sockaddr_in));


						if (sockaddr.sin_family == AF_INET6) {
							var nmask = (sockaddr_in6) Marshal.PtrToStructure(addr.ifa_netmask, typeof(sockaddr_in6));
							var prefixLength = GetIPv6PrefixLength(nmask.sin6_addr.u6_addr8);

							var sockaddr6 = (sockaddr_in6) Marshal.PtrToStructure(addr.ifa_addr, typeof(sockaddr_in6));
							var address = new IPAddress(sockaddr6.sin6_addr.u6_addr8, sockaddr6.sin6_scope_id);
							var info = new InterfaceAddress(index, name, address, prefixLength);
							result.Add(info);
						} else if (sockaddr.sin_family == AF_INET) {
							var netmaskaddr = (sockaddr_in)Marshal.PtrToStructure(addr.ifa_netmask, typeof(sockaddr_in));
							var netmask = new IPAddress(netmaskaddr.in_addr);
							var address = new IPAddress(sockaddr.in_addr);
							var info = new InterfaceAddress(index, name, address, netmask);
							result.Add(info);
						}
					}
					next = addr.ifa_next;
				}
			} finally {
				freeifaddrs(ifap);
			}

			return result.ToArray();
		}

	    public void SetProcessName(string name)
	    {

	    }

		public string OSName { 
			get {
				return "macOS";
			}
		}

	    private int GetIPv6PrefixLength (byte[] value)
		{
			var bit  = 0;
			var b    = 0;
			var plen = 0;

			/* Figure out how many full bytes are in the mask */
			for (b = 0; b < value.Length; b++) {
				if (value[b] != 0xff) {
					break;
				}
				plen += 8;
			}

			if (b == value.Length) {
				return plen;
			}

			/* Now figure out how many bits are in the last byte */
			for (bit = 7; bit != 0; bit --) {
				if ((value[b] & (1 << bit)) == 0) {
					break;
				}
				plen ++;
			}

			return plen;
		}

		const int AF_INET6 = 30;
		const int AF_INET  = 2;
	
		[DllImport ("libc")]
		static extern int getifaddrs (out IntPtr ifap);

		[DllImport ("libc")]
		static extern void freeifaddrs (IntPtr ifap);

		[DllImport("libc")]
		static extern int if_nametoindex(string ifname);

		struct ifaddrs
		{
			public IntPtr  ifa_next;
			public string  ifa_name;
			public uint    ifa_flags;
			public IntPtr  ifa_addr;
			public IntPtr  ifa_netmask;
			public IntPtr  ifa_dstaddr;
			// void             *ifa_data;         /* Address specific data */
		}

		struct sockaddr_in
		{
			public byte   sin_len;
			public byte   sin_family;
			public ushort sin_port;
			public uint   in_addr;
			// char            sin_zero[8];
		}

		struct sockaddr_in6
		{
			public byte     sin6_len;
			public byte     sin6_family;
			public ushort   sin6_port;
			public uint     sin6_flowinfo;
			public in6_addr sin6_addr;
			public uint     sin6_scope_id;
		}

		struct in6_addr
		{
			[MarshalAs (UnmanagedType.ByValArray, SizeConst=16)]
			public byte[] u6_addr8;
		}
	}
}
