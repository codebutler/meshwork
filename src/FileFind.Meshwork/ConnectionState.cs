//
// ConnectionState.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

namespace FileFind.Meshwork
{
	public enum ConnectionState
	{
		Connecting,
		Authenticating,
		Remote,
		Ready,
		Disconnected
	}
}
