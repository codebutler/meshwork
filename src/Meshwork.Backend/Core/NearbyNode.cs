//
// NearbyNode.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2006 Meshwork Authors
//

using System.Net;

namespace Meshwork.Backend.Core
{
	public class NearbyNode 
	{
		string    networkId;
		string    nodeId;
		string    nickname;
		IPAddress address;
		int port;

		public NearbyNode (string networkId, string nodeId,
		                   string nickname,  IPAddress address,
				   int port)
		{
			this.networkId = networkId;
			this.nodeId = nodeId;
			this.address = address;
			this.port = port;
			this.nickname = nickname;
		}

		/*
		public void AddAddress (IPAddress address)
		{
			if (address.AddressFamily == AddressFamily.InterNetwork) {
				if (IPv4Address.Equals (IPAddress.None))
					IPv4Address = address;
				else
					throw new Exception ("This node already has an IPv4 address!");

				return;
			}

			if (address.AddressFamily == AddressFamily.InterNetworkV6) {
				if (IPv6Address.Equals (IPAddress.None))
					IPv6Address = address;
				else
					throw new Exception ("This node already has an IPv6 address!");

			}

		}
		*/
		
		public string NetworkId {
			get {
				return networkId;
			}
		}

		public string NodeId {
			get {
				return nodeId;
			}
		}

		public string NickName {
			get {
				return nickname;
			}
		}

		public IPAddress Address {
			get {
				return address;
			}
		}

		public int Port {
			get {
				return port;
			}
		}

		/*
		public IPAddress IPv4Address {
			get {
				if (IPv4Address.Equals (IPAddress.None) == false)
					return IPv4Address;
				else
					return null;
			}
		}
		
		public IPAddress IPv6Address {
			get {
				if (IPv6Address.Equals (IPAddress.None) == false)
					return IPv6Address;
				else
					return null;
			}
		}
		*/
	}
}
