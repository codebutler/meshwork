//
// NetworkState.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.Collections;
using FileFind.Meshwork.Protocol;

namespace FileFind.Meshwork
{
	internal class NetworkState
	{
		ConnectionInfo[] knownConnections;
		ChatRoomInfo[] knownChatRooms;
		MemoInfo[] knownMemos;

		public NetworkState (HelloInfo info)
		{
			this.knownConnections = info.KnownConnections;
			this.knownChatRooms = info.KnownChatRooms;
			this.knownMemos = info.KnownMemos;
		}

		public NetworkState (NodeInfo info)
		{
			this.knownConnections = info.KnownConnections;
			this.knownChatRooms = info.KnownChatRooms;
			this.knownMemos = info.KnownMemos;
		}

		public ConnectionInfo[] KnownConnections {
			get {
				return knownConnections;
			}
		}

		public ChatRoomInfo[] KnownChatRooms {
			get {
				return knownChatRooms;
			}
		}

		public MemoInfo[] KnownMemos {
			get {
				return knownMemos;
			}
		}
	}
}
