//
// NetworkState.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using Meshwork.Backend.Core.Protocol;

namespace Meshwork.Backend.Core
{
	internal class NetworkState
	{
	    public NetworkState (HelloInfo info)
		{
			KnownConnections = info.KnownConnections;
			KnownChatRooms = info.KnownChatRooms;
			KnownMemos = info.KnownMemos;
		}

		public NetworkState (NodeInfo info)
		{
			KnownConnections = info.KnownConnections;
			KnownChatRooms = info.KnownChatRooms;
			KnownMemos = info.KnownMemos;
		}

		public ConnectionInfo[] KnownConnections { get; }

	    public ChatRoomInfo[] KnownChatRooms { get; }

	    public MemoInfo[] KnownMemos { get; }
	}
}
