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

		public InterfaceAddress (int interfaceIndex, string name, IPAddress address, int prefixLength)
		{
			if (address.AddressFamily != AddressFamily.InterNetworkV6) {
				throw new ArgumentException("address", "Must be IPv6");
			}

			this.prefixLength = prefixLength;

			this.interfaceIndex = interfaceIndex;
			this.name           = name;
			this.address        = address;
		}

		public InterfaceAddress (int interfaceIndex, string name, IPAddress address)
		{
			if (address.AddressFamily != AddressFamily.InterNetwork) {
				throw new ArgumentException("address", "Must be IPv4");
			}

			this.interfaceIndex = interfaceIndex;
			this.name           = name;
			this.address        = address;
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
