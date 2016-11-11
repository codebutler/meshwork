//
// DebugInfo.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

namespace Meshwork.Backend.Core
{
	public class DebugInfo
	{
		public string FromIP;
		public string FromID;
		public Message Content;
		public MessageDirection Direction;

		public DebugInfo() {
		}

		public DebugInfo(MessageDirection Direction, string FromID, string FromIP, Message Content) {
			this.FromID = FromID;
			this.FromIP = FromIP;
			this.Content = Content;
			this.Direction = Direction;
		}
		public enum MessageDirection {
			Incoming = 1,
			Outgoing = 2
		}
	}
}

