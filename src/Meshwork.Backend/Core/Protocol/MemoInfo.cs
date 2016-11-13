//
// MemoInfo.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;

namespace Meshwork.Backend.Core.Protocol
{
	public struct MemoInfo
	{
		public MemoInfo (Memo memo)
		{
			ID = memo.ID;
			FromNodeID = memo.Node.NodeID;
			CreatedOn = memo.CreatedOn;
			Signature = memo.Signature;
			Subject = memo.Subject;
			Text = memo.Text;
		}

		public string ID;
		public string FromNodeID;
		public DateTime CreatedOn;
		public byte[] Signature;
		public string Subject;
		public string Text;
	}
}
