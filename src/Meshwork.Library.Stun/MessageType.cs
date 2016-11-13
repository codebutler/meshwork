namespace Meshwork.Library.Stun
{
	public enum MessageType
	{
		BindingRequest = 0x0001,
		BindingResponse = 0x0101,
		BindingErrorResponse = 0x0111,
		SharedSecretRequest = 0x0002,
		SharedSecretResponse = 0x0102,
		SharedSecretErrorResponse = 0x0112
		
	}
}
