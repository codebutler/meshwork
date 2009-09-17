//
// IPlatform.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007 FileFind.net (http://filefind.net)
//

using System;
using System.Net;
using System.Net.Sockets;

namespace FileFind.Meshwork
{
	public interface IPlatform
	{
		InterfaceAddress[] GetInterfaceAddresses();
		
		string UserName {
			get;
		}
		
		string RealName {
			get;
		}

		string VersionInfo {
			get;
		}
	}

	// XXX: move this!
	public class InterfaceAddress
	{
		int       interfaceIndex;
		int       prefixLength;
		string    name;
		IPAddress address;
		IPAddress subnetMask;

		public InterfaceAddress (int interfaceIndex, string name, IPAddress address, int prefixLength)
		{
			if (address.AddressFamily != AddressFamily.InterNetworkV6) {
				throw new ArgumentException("address", "Must be IPv6");
			}

			this.prefixLength = prefixLength;

			this.interfaceIndex = interfaceIndex;
			this.name = name;
			this.address = address;
		}

		public InterfaceAddress (int interfaceIndex, string name, IPAddress address, IPAddress subnetMask)
		{
			if (address.AddressFamily != AddressFamily.InterNetwork) {
				throw new ArgumentException("address", "Must be IPv4");
			}

			
			if (subnetMask == null)
				throw new ArgumentNullException("subnetMask");
			
			this.interfaceIndex = interfaceIndex;
			this.name = name;
			this.address = address;
			this.subnetMask = subnetMask;
		}
		
		public string Name {
			get {
				return name;
			}
		}

		public IPAddress Address {
			get {
				return address;
			}
		}

		public IPAddress SubnetMask {
			get {
				if (address.AddressFamily == AddressFamily.InterNetwork)
					return subnetMask;
				else
					throw new Exception("Subnet mask not supported for this type of address");
			}
		}
		
		public int InterfaceIndex {
			get {
				return interfaceIndex;
			}
		}
		
		public int IPv6PrefixLength {
			get {
				return prefixLength;
			}
		}
	}
}
