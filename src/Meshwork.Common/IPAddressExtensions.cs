// http://blogs.msdn.com/knom/archive/2008/12/31/ip-address-calculations-with-c-subnetmasks-networks.aspx

using System;
using System.Net;

namespace Meshwork.Common
{
	public static class IPAddressExtensions
	{
		public static IPAddress GetBroadcastAddress (this IPAddress address, IPAddress subnetMask)
		{
			var ipAdressBytes = address.GetAddressBytes();
			var subnetMaskBytes = subnetMask.GetAddressBytes();
			
			if (ipAdressBytes.Length != subnetMaskBytes.Length)
				throw new ArgumentException("Lengths of IP address and subnet mask do not match.");
			
			var broadcastAddress = new byte[ipAdressBytes.Length];
			for (var i = 0; i < broadcastAddress.Length; i++) {
				broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
			}
			return new IPAddress(broadcastAddress);
		}

		public static IPAddress GetNetworkAddress (this IPAddress address, IPAddress subnetMask)
		{
			var ipAdressBytes = address.GetAddressBytes();
			var subnetMaskBytes = subnetMask.GetAddressBytes();
			
			if (ipAdressBytes.Length != subnetMaskBytes.Length)
				throw new ArgumentException("Lengths of IP address and subnet mask do not match.");
			
			var broadcastAddress = new byte[ipAdressBytes.Length];
			for (var i = 0; i < broadcastAddress.Length; i++) {
				broadcastAddress[i] = (byte)(ipAdressBytes[i] & (subnetMaskBytes[i]));
			}
			return new IPAddress(broadcastAddress);
		}

		public static bool IsInSameSubnet (this IPAddress address2, IPAddress address, IPAddress subnetMask)
		{
			IPAddress network1 = address.GetNetworkAddress(subnetMask);
			IPAddress network2 = address2.GetNetworkAddress(subnetMask);
			
			return network1.Equals(network2);
		}
	}

}
