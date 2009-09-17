using System;
using System.Net;
using System.Net.Sockets;

namespace FileFind.Stun
{
	public class MappedAddressAttribute : AddressAttributeBase
	{


		public MappedAddressAttribute () : base (MessageAttributeType.MappedAddress)
		{
		}

		public MappedAddressAttribute (byte[] data) : base (MessageAttributeType.MappedAddress, data)
		{
			
		}
	}
}
