using System;
using System.Net;
using System.Net.Sockets;

namespace Meshwork.Library.Stun
{
	public class StunClient
	{
		static string stunServer = "stun.ekiga.net";
		public static string StunServer {
			get {
				return stunServer;
			}
			set {
				stunServer = value;
			}
		}
		
		public static IPAddress GetExternalAddress ()
		{
			IPHostEntry entry = Dns.GetHostEntry (stunServer);
			IPEndPoint endPoint = new IPEndPoint (entry.AddressList [0], 3478);
			UdpClient client = new UdpClient ();
			client.Connect (endPoint);

			MessageHeader header = new MessageHeader ();
			header.MessageType = MessageType.BindingRequest;

			byte[] bytes = header.GetBytes ();
			client.Send (bytes, bytes.Length);

			bytes = client.Receive (ref endPoint);

			header = new MessageHeader (bytes);
			if (header.MessageType == MessageType.BindingResponse) {
				foreach (MessageAttribute attribute in header.MessageAttributes) {
					if (attribute is MappedAddressAttribute) {
						return (attribute as AddressAttributeBase).Address;
					}
				}
				throw new Exception ("Resposne was missing Mapped-address!");
			} else {
				throw new Exception ("Wrong response message!");
			}
		}
	}
}
