namespace FileFind.Stun
{
	public class ChangeRequestAttribute : MessageAttribute
	{
		public ChangeRequestAttribute (bool changeIP, bool changePort) : base (MessageAttributeType.ChangeRequest)
		{
			byte[] valueBuffer = new byte [4];
			base.Value = valueBuffer;
		}
	}
}
