//
// ConnectionState.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

namespace Meshwork.Backend.Core
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
