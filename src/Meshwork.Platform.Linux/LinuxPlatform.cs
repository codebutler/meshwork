//
// Linux.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006-2008 FileFind.net (http://filefind.net)
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using Mono.Unix;
using Mono.Unix.Native;

namespace Meshwork.Platform.Linux
{
	public class LinuxPlatform : IPlatform
	{
		public LinuxPlatform(string uname) {
			OSName = uname;
		}

	    public string GetOSName()
	    {
	        throw new NotImplementedException();
	    }

	    public string UserName {
			get {
				var user = UnixUserInfo.GetRealUser();
				return user.UserName;
			}
		}
		
		public string RealName {
			get {
				var user = UnixUserInfo.GetRealUser();
				if (user.RealName != null)
				{
				    if (user.RealName.IndexOf (",") > -1) {
						return user.RealName.Substring (0, user.RealName.IndexOf(","));
					}
				    return user.RealName;
				}
			    return string.Empty;
			}
		}
		
		public InterfaceAddress[] GetInterfaceAddresses ()
		{
			IntPtr ifap;

			if (LibC.getifaddrs(out ifap) != 0) {
				throw new SystemException("getifaddrs() failed");
			}

			var result = new List<InterfaceAddress>();

			try {
				var next = ifap;
				
				while (next != IntPtr.Zero) {
					var addr = (ifaddrs) Marshal.PtrToStructure(next, typeof(ifaddrs));
					
					var name = addr.ifa_name;
					
					if (addr.ifa_addr != IntPtr.Zero) {
						var sockaddr = (sockaddr_in) Marshal.PtrToStructure(addr.ifa_addr, typeof(sockaddr_in));
						
						var index = LibC.if_nametoindex(name);

						if (sockaddr.sin_family == LibC.AF_INET6) {
							var sockaddr6 = (sockaddr_in6) Marshal.PtrToStructure(addr.ifa_addr, typeof(sockaddr_in6));
							var address = new IPAddress(sockaddr6.sin6_addr.u6_addr8, sockaddr6.sin6_scope_id);
							var info = new InterfaceAddress(index, name, address, GetPrefixLength(name, address));
							result.Add(info);
						} else if (sockaddr.sin_family == LibC.AF_INET) {
							var netmaskaddr = (sockaddr_in)Marshal.PtrToStructure(addr.ifa_netmask, typeof(sockaddr_in));
							var netmask = new IPAddress(netmaskaddr.sin_addr);
							var address = new IPAddress(sockaddr.sin_addr);
							var info = new InterfaceAddress(index, name, address, netmask);
							result.Add(info);
						}
					}
					next = addr.ifa_next;
				}
			} finally {
				LibC.freeifaddrs(ifap);
			}

			return result.ToArray();
		}

	    public string OSName {
			get;
			private set;
	    }

	    public void SetProcessName (string name)
	    {
	        if (OSName == "Linux") {

	            if (LibC.prctl (15 /* PR_SET_NAME */, Encoding.ASCII.GetBytes (name + "\0"),
	                    IntPtr.Zero, IntPtr.Zero, IntPtr.Zero) != 0) {
	                throw new ApplicationException ("Error setting process name: " +
	                                                Stdlib.GetLastError ());
	            }
	        } else if (OSName == "FreeBSD") { // XXX: I'm not sure this is right
	            LibC.setproctitle (Encoding.ASCII.GetBytes ("%s\0"),
	                Encoding.ASCII.GetBytes (name + "\0"));
	        }
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
			var text = File.ReadAllText("/proc/net/if_inet6");
			foreach (var line in text.Split('\n')) {
				if (line.Length == 53) {
					var ip = new IPAddress(Common.Utils.StringToBytes(line.Substring(0, 32)));
					var n = line.Substring(44).Trim();
					var prefixLength = int.Parse(line.Substring(36, 2), NumberStyles.HexNumber);

					if (ip.Equals(address) && n == interfaceName) {
						return prefixLength;
					}
				}
			}
			return -1;
		}
	}
}
