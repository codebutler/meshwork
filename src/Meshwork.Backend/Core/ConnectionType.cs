//
// ConnectionType.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System.Collections;

namespace Meshwork.Backend.Core
{
	public static class ConnectionType 
	{
		static Hashtable friendlyNames = new Hashtable ();
		static ConnectionType ()
		{
			friendlyNames.Add (NodeConnection, "Node Connection");
			friendlyNames.Add (TransferConnection, "Transfer Connection");
		}
		
		public static readonly ulong NodeConnection = 0x01;
		public static readonly ulong TransferConnection = 0x02;
		
		public static string GetFriendlyName (ulong connectionType)
		{
		    if (friendlyNames.ContainsKey (connectionType))
				return friendlyNames [connectionType].ToString ();
		    return "Unknown";
		}
	}
}
