//
// IPv6Util.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006-2008 FileFind.net (http://filefind.net)
//

using System.Net;

namespace Meshwork.Common
{
	public class IPv6Util
	{
		public static string GetNetworkPrefix (int prefixLength, IPAddress address)
		{
			var bytes = address.GetAddressBytes ();
			var prefix = "";
			for (var y = 0; y < (prefixLength / 8); y += 2) {
				if (y > 0) prefix += ":";
				prefix += EndianBitConverter.ToString (bytes, y,2).Replace ("-","");
			}

			for (long y = (prefixLength / 8); y < bytes.Length; y+=2)
				prefix += ":0000";

			prefix += "/" + prefixLength;

			return prefix.ToLower();
		}
	}
}
