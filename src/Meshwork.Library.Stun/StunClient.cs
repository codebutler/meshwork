using System;
using System.Net;
using System.Net.Sockets;

namespace Meshwork.Library.Stun
{
	public class StunClient
	{
        public static IPAddress GetExternalAddress (String stunServer)
		{
			var entry = Dns.GetHostEntry (stunServer);
			var endPoint = new IPEndPoint (entry.AddressList [0], 3478);
			var client = new UdpClient ();
			client.Connect (endPoint);

			var header = new MessageHeader ();
			header.MessageType = MessageType.BindingRequest;

			var bytes = header.GetBytes ();
			client.Send (bytes, bytes.Length);

			bytes = client.Receive (ref endPoint);

			header = new MessageHeader (bytes);
			if (header.MessageType == MessageType.BindingResponse) {
				foreach (var attribute in header.MessageAttributes) {
					if (attribute is MappedAddressAttribute) {
						return (attribute as AddressAttributeBase).Address;
					}
				}
				throw new Exception ("Resposne was missing Mapped-address!");
			}
		    throw new Exception ("Wrong response message!");
		}
	}
}
