//
// TCPDestination.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using FileFind.Meshwork.Transport;

namespace FileFind.Meshwork.Destination
{
	public class TCPIPv4DestinationSource : TCPIPDestinationSource
	{
		public override Type DestinationType { 
			get {
				return typeof(TCPIPv4Destination);
			}
		}

		public override Type ListenerType {
			get {
				return typeof(TcpTransportListener);
			}
		}
	}

	public class TCPIPv6DestinationSource : TCPIPDestinationSource
	{
		public override Type DestinationType {
			get {
				return typeof(TCPIPv6Destination);
			}
		}

		public override Type ListenerType {
			get {
				// We piggyback on TCPIPv4DestinationSource's listener.
				return null;
			}
		}
	}

	public abstract class TCPIPDestinationSource : IDestinationSource
	{
		List<IDestination> destinations = new List<IDestination>();

		public event DestinationEventHandler DestinationAdded;
		public event DestinationEventHandler DestinationRemoved;

		int listenPort = TcpTransport.DefaultPort;

		public IList<IDestination> Destinations {
			get { 
				return destinations.AsReadOnly();
			}
		}

		public abstract Type DestinationType {
			get;
		}

		public abstract Type ListenerType {
			get;
		}

		public int ListenPort {
			get {
				return listenPort;
			}
			set {
				listenPort = value;
			}
		}

		public TCPIPDestinationSource ()
		{
			listenPort = Core.Settings.TcpListenPort;

			// XXX: Use NetworkManager to support IP changes,
			// etc. without restarting Meshwork.

			InterfaceAddress[] addresses = Core.OS.GetInterfaceAddresses();
			foreach (InterfaceAddress address in addresses) {

				// Ignore loopback.
				if (IPAddress.IsLoopback(address.Address))
					continue;
				
				// Only include addresses of the correct type.
				if (this is TCPIPv4DestinationSource && address.Address.AddressFamily != AddressFamily.InterNetwork)
					continue;
				else if (this is TCPIPv6DestinationSource && address.Address.AddressFamily != AddressFamily.InterNetworkV6)
					continue;

				bool isOpenExternally = false;
				if (!Common.IsInternalIP(address.Address)) {
					// XXX: Check for firewall. For now, we assume true because 99.99% of the
					// world is behind a NAT, the remaining 0.01% probably know what they are doing.
					isOpenExternally = true;
				}

				IDestination destination = null;

				if (this is TCPIPv6DestinationSource) {
					destination = (IDestination)Activator.CreateInstance(this.DestinationType, new object[] {address.IPv6PrefixLength, address.Address, (uint)listenPort, isOpenExternally});
				} else {
					destination = (IDestination)Activator.CreateInstance(this.DestinationType, new object[] {address.Address, (uint)listenPort, isOpenExternally});
				}

				destinations.Add(destination);

				if (DestinationAdded != null) {
					DestinationAdded(destination);
				}
			}
		}

		public void Update ()
		{
			throw new NotImplementedException();
		}
	}

	public class TCPIPv6Destination : IPv6Destination
	{	
		static TCPIPv6Destination ()
		{
			DestinationTypeFriendlyNames.RegisterFriendlyName(typeof(TCPIPv6Destination), "TCP (IPv6)");
		}

		public TCPIPv6Destination (DestinationInfo info)
		{
			base.ip = IPAddress.Parse(info.Data[0]);
			base.prefixLength = Int32.Parse(info.Data[1]);
			base.port = UInt32.Parse(info.Data[2]);

			if (base.IsExternal) {
				base.isOpenExternally = info.IsOpenExternally;
			}
		}

		public TCPIPv6Destination (int prefixLength, IPAddress ip, uint port, bool isOpenExternally) : base (prefixLength, ip, port, isOpenExternally)
		{
		}

		public override ITransport CreateTransport (ulong connectionType)
		{
			return new TcpTransport(base.IPAddress, (int)base.Port, connectionType);
		}

		public override string ToString ()
		{
			return String.Format("[{0}/{1}]:{2}", this.IPAddress.ToString(), this.PrefixLength, this.Port.ToString());
		}
	}

	public class TCPIPv4Destination : IPv4Destination
	{
		static TCPIPv4Destination ()
		{
			DestinationTypeFriendlyNames.RegisterFriendlyName(typeof(TCPIPv4Destination), "TCP");
		}

		public TCPIPv4Destination (DestinationInfo info)
		{
			base.ip   = IPAddress.Parse(info.Data[0]);
			base.port = UInt32.Parse(info.Data[1]);

			if (base.IsExternal) {
				base.isOpenExternally = info.IsOpenExternally;
			}
		}

		public TCPIPv4Destination (IPAddress ip, uint port, bool isOpenExternally) : base (ip, port, isOpenExternally)
		{
		}

		public override ITransport CreateTransport (ulong connectionType)
		{
			return new TcpTransport(base.IPAddress, (int)base.Port, connectionType);
		}

		public override string ToString ()
		{
			return String.Format("{0}:{1}", this.IPAddress.ToString(), this.Port.ToString());
		}
	}
}
