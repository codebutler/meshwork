//
// Linux.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006-2008 FileFind.net (http://filefind.net)
//

using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Globalization;
using System.IO;

namespace FileFind.Meshwork
{
	public class LinuxPlatform : IPlatform
	{
		const int AF_INET6 = 10;
		const int AF_INET = 2;

		[DllImport("libc")]
		static extern int if_nametoindex(string ifname);

		[DllImport ("libc")]
		static extern int getifaddrs (out IntPtr ifap);

		[DllImport ("libc")]
		static extern void freeifaddrs (IntPtr ifap);

		[StructLayout(LayoutKind.Explicit)]
		struct ifa_ifu
		{ 		
			[FieldOffset (0)]
			public IntPtr ifu_broadaddr; 

			[FieldOffset (0)]
			public IntPtr ifu_dstaddr; 
		}
       
		struct ifaddrs
		{
			public IntPtr  ifa_next; 
			public string  ifa_name; 
			public uint    ifa_flags; 
			public IntPtr  ifa_addr; 
			public IntPtr  ifa_netmask; 
			public ifa_ifu ifa_ifu; 
			public IntPtr  ifa_data; 
		}
		
		struct sockaddr_in
		{
			public ushort sin_family;
			public ushort sin_port;
			public uint   sin_addr;
		}

		struct sockaddr_in6
		{
			public ushort   sin6_family;   /* AF_INET6 */
			public ushort   sin6_port;     /* Transport layer port # */
			public uint     sin6_flowinfo; /* IPv6 flow information */
			public in6_addr sin6_addr;     /* IPv6 address */
			public uint     sin6_scope_id; /* scope id (new in RFC2553) */
		}

		struct in6_addr
		{
			[MarshalAs (UnmanagedType.ByValArray, SizeConst=16)]
			public byte[] u6_addr8;
		}

		public string UserName {
			get {
				Mono.Unix.UnixUserInfo user = Mono.Unix.UnixUserInfo.GetRealUser();
				return user.UserName;
			}
		}
		
		public string RealName {
			get {
				Mono.Unix.UnixUserInfo user = Mono.Unix.UnixUserInfo.GetRealUser();
				if (user.RealName != null) {
					if (user.RealName.IndexOf (",") > -1) {
						return user.RealName.Substring (0, user.RealName.IndexOf(","));
					} else {
						return user.RealName;
					}
				} else {
					return String.Empty;
				}
			}
		}
		
		public InterfaceAddress[] GetInterfaceAddresses ()
		{
			IntPtr ifap;

			if (getifaddrs(out ifap) != 0) {
				throw new SystemException("getifaddrs() failed");
			}

			List<InterfaceAddress> result = new List<InterfaceAddress>();

			try {
				IntPtr next = ifap;
				
				while (next != IntPtr.Zero) {
					ifaddrs addr = (ifaddrs) Marshal.PtrToStructure(next, typeof(ifaddrs));
					
					string name = addr.ifa_name;
					
					if (addr.ifa_addr != IntPtr.Zero) {
						sockaddr_in sockaddr = (sockaddr_in) Marshal.PtrToStructure(addr.ifa_addr, typeof(sockaddr_in));
						
						int index = if_nametoindex(name);

						if (sockaddr.sin_family == AF_INET6) {
							sockaddr_in6 sockaddr6 = (sockaddr_in6) Marshal.PtrToStructure(addr.ifa_addr, typeof(sockaddr_in6));
							IPAddress address = new IPAddress(sockaddr6.sin6_addr.u6_addr8, sockaddr6.sin6_scope_id);
							InterfaceAddress info = new InterfaceAddress(index, name, address, GetPrefixLength(name, address));
							result.Add(info);
						} else if (sockaddr.sin_family == AF_INET) {
							sockaddr_in netmaskaddr = (sockaddr_in)Marshal.PtrToStructure(addr.ifa_netmask, typeof(sockaddr_in));
							IPAddress netmask = new IPAddress(netmaskaddr.sin_addr);
							IPAddress address = new IPAddress(sockaddr.sin_addr);
							InterfaceAddress info = new InterfaceAddress(index, name, address, netmask);
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

		public string VersionInfo {
			get {
				// XXX: Query lsb_release for more details.
				return "Linux";
			}
		}

		// There is a way to get this using netlink, but I cant figure it out.
		private int GetPrefixLength(string interfaceName, IPAddress address)
		{
			string text = File.ReadAllText("/proc/net/if_inet6");
			foreach (string line in text.Split('\n')) {
				if (line.Length == 53) {
					IPAddress ip = new IPAddress(Common.StringToBytes(line.Substring(0, 32)));
					string n = line.Substring(44).Trim();
					int prefixLength = Int32.Parse(line.Substring(36, 2), NumberStyles.HexNumber);

					if (ip.Equals(address) && n == interfaceName) {
						return prefixLength;
					}
				}
			}
			return -1;
		}
	}
}
