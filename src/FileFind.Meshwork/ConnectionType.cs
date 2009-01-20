//
// ConnectionType.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.Collections;

namespace FileFind.Meshwork
{
	public static class ConnectionType 
	{
		static Hashtable friendlyNames = new Hashtable ();
		static ConnectionType ()
		{
			friendlyNames.Add (ConnectionType.NodeConnection, "Node Connection");
			friendlyNames.Add (ConnectionType.TransferConnection, "Transfer Connection");
		}
		
		public static readonly ulong NodeConnection = 0x01;
		public static readonly ulong TransferConnection = 0x02;
		
		public static string GetFriendlyName (ulong connectionType)
		{
			if (friendlyNames.ContainsKey (connectionType) == true)
				return friendlyNames [connectionType].ToString ();
			else
				return "Unknown";
		}
	}
}
