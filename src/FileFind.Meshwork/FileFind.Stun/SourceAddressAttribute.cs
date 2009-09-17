namespace FileFind.Stun
{
	public class SourceAddressAttribute : AddressAttributeBase
	{
		public SourceAddressAttribute () : base (MessageAttributeType.SourceAddress)
		{
		}

		public SourceAddressAttribute (byte[] data) : base (MessageAttributeType.SourceAddress, data)
		{
		}
	}
}
