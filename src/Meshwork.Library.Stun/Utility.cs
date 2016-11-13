namespace Meshwork.Library.Stun
{
	public class Utility 
	{
		public static byte[] IntegerToTwoBytes (int integer)
		{
			var result = new byte [2];
			result [0] = (byte) (integer >> 8);
			result [1] = (byte) integer;
			return result;
		}

		public static int TwoBytesToInteger (byte[] bytes)
		{
			return ((bytes[0] << 8) + bytes[1]);
		}
	}
}
