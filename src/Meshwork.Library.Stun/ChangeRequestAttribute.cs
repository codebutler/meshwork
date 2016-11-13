namespace Meshwork.Library.Stun
{
	public class ChangeRequestAttribute : MessageAttribute
	{
		public ChangeRequestAttribute (bool changeIP, bool changePort) : base (MessageAttributeType.ChangeRequest)
		{
			var valueBuffer = new byte [4];
			Value = valueBuffer;
		}
	}
}
