using System;
using System.Net;
using System.Net.Sockets;

namespace Meshwork.Library.Stun
{
	public class AddressAttributeBase : MessageAttribute
	{
	    private readonly AddressFamily addressFamily;
	    private readonly int port;
	    private readonly IPAddress address;
		
		public AddressAttributeBase (MessageAttributeType type) : base (type)
		{

		}
		
		public AddressAttributeBase (MessageAttributeType type, byte[] data) : base (type)
		{
			byte[] familyBytes = new byte[1];
			Array.Copy (data, 1, familyBytes, 0, 1);

			if (familyBytes[0] != 0x01)
				throw new Exception ("Invalid network family!");
			else
				this.addressFamily = AddressFamily.InterNetwork;

			byte[] portBytes = new byte [2];
			Array.Copy (data, 2, portBytes, 0, 2);
			this.port = Utility.TwoBytesToInteger (portBytes);

			byte[] addressBytes = new byte [4];
			Array.Copy (data, 4, addressBytes, 0, 4);
			this.address = new IPAddress (BitConverter.ToInt32 (addressBytes, 0));
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

		public AddressFamily AddressFamily {
			get {
				return addressFamily;
			}
		}
	}
}
