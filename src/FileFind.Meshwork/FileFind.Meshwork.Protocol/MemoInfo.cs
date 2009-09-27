//
// MemoInfo.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;

namespace FileFind.Meshwork.Protocol
{
	[Serializable]
	public struct MemoInfo
	{
		public MemoInfo (Memo memo)
		{
			this.ID = memo.ID;
			this.FromNodeID = memo.Node.NodeID;
			this.CreatedOn = memo.CreatedOn;
			this.Signature = memo.Signature;
			this.Subject = memo.Subject;
			this.Text = memo.Text;
		}

		public string ID;
		public string FromNodeID;
		public DateTime CreatedOn;
		public byte[] Signature;
		public string Subject;
		public string Text;
	}
}
