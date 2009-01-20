//
// IPDestination.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using System;
using System.Net;
using System.Net.Sockets;
using FileFind.Meshwork.Transport;

namespace FileFind.Meshwork.Destination
{
	public abstract class IPv6Destination : IPDestination
	{
		protected int prefixLength;

		public IPv6Destination (int prefixLength, IPAddress ip, uint port, bool isOpenExternally) : base (ip, port, isOpenExternally)
		{
			if (ip.AddressFamily != AddressFamily.InterNetworkV6) {
				throw new ArgumentException("ip must be IPv6");
			}

			this.prefixLength = prefixLength;
		}

		protected IPv6Destination () 
		{

		}

		public string NetworkPrefix {
			get {
				return IPv6Util.GetNetworkPrefix(prefixLength, ip);
			}
		}

		public int PrefixLength {
			get {
				return prefixLength;
			}
		}

		public override bool CanConnect {
			get {
				if (IsExternal) {
					if (Common.SupportsIPv6) { // XXX: Only if supports ipv6 and has a public ip. see preferences dialog.
						return base.IsOpenExternally;
					} else {
						return false;
					}
				} else {
					// Can't connect to link-local addresses if no interface is set.
					if (Core.Settings.IPv6LinkLocalInterfaceIndex == -1) {
						return false;
					}
				
					foreach (IDestination destination in Core.DestinationManager.Destinations) {

						// If this is an IPv6 address, we can connect only if
						// one of our Destinations is also IPv6 and has the
						// same network prefix (excluding link-local).
						if (destination is IPv6Destination && destination.IsExternal) {
							IPv6Destination ipv6Destination = (IPv6Destination)destination;
							if (this.NetworkPrefix == ((IPv6Destination)destination).NetworkPrefix) {
								return true;
							}
		
						// In many (most?) cases, two nodes on the same LAN will
						// have only link-local IPv6 addresses. If we have a
						// matching external IPv4 address (i.e., we are behind
						// the same IPv4 NAT router), then we can connect.
						} else if (destination is IPv4Destination && destination.IsExternal) {
							IPAddress myAddress = ((IPv4Destination)destination).IPAddress;
							foreach (IDestination d in parentList) {
								if (d.IsExternal && d is IPv4Destination && myAddress.Equals(((IPv4Destination)d).IPAddress)) {
									return true;
								}
							}
						}
					}
					return false;
				}
			}
		}

		public override DestinationInfo CreateDestinationInfo()
		{
			DestinationInfo info = new DestinationInfo();
			info.IsOpenExternally = this.IsOpenExternally;
			info.TypeName = this.GetType().ToString();
			info.Data = new string[] { ip.ToString(), prefixLength.ToString(), port.ToString() };
			return info;
		}
	}

	public abstract class IPv4Destination : IPDestination
	{
		public IPv4Destination (IPAddress ip, uint port, bool isOpenExternally) : base (ip, port, isOpenExternally)
		{
			if (ip.AddressFamily != AddressFamily.InterNetwork) {
				throw new ArgumentException(String.Format("ip must be IPv4 (was {0})", ip));
			}
		}

		protected IPv4Destination () 
		{

		}

		public override bool CanConnect {
			get {
				if (IsExternal) {
					return base.IsOpenExternally;
				} else {
					// If this is an IPv4 address, we can connect only
					// if one of our external Destinations matches another one
					// of their (external) Destinations. This means that we both
					// have the same external IP address (both are behind the
					// same NAT router). Under certain situations, a NAT'd
					// network may have multiple public IP Addresses. We do not
					// currently support this case.
					
					foreach (IDestination destination in Core.DestinationManager.Destinations) {
						if (destination is IPv4Destination && destination.IsExternal) {
							IPAddress myAddress = ((IPv4Destination)destination).IPAddress;
							foreach (IDestination d in parentList) {
								if (d.IsExternal && d is IPv4Destination && myAddress.Equals(((IPv4Destination)d).IPAddress)) {
									return true;
								}
							}
						}
					}

					return false;
				}
			}
		}
		
		public override DestinationInfo CreateDestinationInfo()
		{
			DestinationInfo info = new DestinationInfo();
			info.IsOpenExternally = this.IsOpenExternally;
			info.TypeName = this.GetType().ToString();
			info.Data = new string[] { ip.ToString(), port.ToString() };
			return info;
		}
	}

	public abstract class IPDestination : DestinationBase
	{
		protected IPAddress ip;
		protected uint      port;

		public IPDestination (IPAddress ip, uint port, bool isOpenExternally)
		{
			this.ip = ip;
			this.port = port;

			base.isOpenExternally = isOpenExternally;
		}

		protected IPDestination ()
		{

		}

		public override bool IsExternal {
			get {
				return !Common.IsInternalIP(this.IPAddress);
			}
		}

		public IPAddress IPAddress {
			get {
				return ip;
			}
		}

		public uint Port {
			get {
				return port;
			}
		}
	}
}
