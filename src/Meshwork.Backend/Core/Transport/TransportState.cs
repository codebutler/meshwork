//
// TransportState.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

namespace Meshwork.Backend.Core.Transport
{
	public enum TransportState
	{
		Waiting,
		Connecting,
		Securing,
		Connected,
		Disconnected
	}
}
