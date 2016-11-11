namespace Meshwork.Library.Stun
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
