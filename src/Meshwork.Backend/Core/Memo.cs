//
// Memo.cs: A memo
//
// Author:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2005-2006 Meshwork Authors
//

using System;
using System.Security.Cryptography;
using System.Text;
using Meshwork.Backend.Core.Protocol;

namespace Meshwork.Backend.Core
{
	public class Memo
	{
		//public ArrayList FileLinks = new ArrayList();
		bool unread = true;
		byte[] signature;
		Node node;
		string id;
		string subject;
		string text;
		Network network;
		DateTime createdOn;

		public Memo (Network network, MemoInfo memoInfo) 
		{
			this.network = network;
			id = memoInfo.ID;
			node = network.Nodes[memoInfo.FromNodeID];
			createdOn = memoInfo.CreatedOn;
			signature = memoInfo.Signature;
			subject = memoInfo.Subject;
			text = memoInfo.Text;
		}

		public Memo (Network network)
		{
			this.network = network;
			node = network.Nodes[Network.Core.MyNodeID];
			createdOn = DateTime.Now;
		}

		public string Subject {
			get {
				return subject;
			}
			set {
				subject = value;
			}
		}

		public string Text {
			get {
				unread = false;
				return text;
			}
			set {
				text = value;
				unread = true;
			}
		}

		public DateTime CreatedOn {
			get {
				return createdOn;
			}
		}

		public Node Node {
			get {
				return node;
			}
		}

		public string ID {
			get {
				return id;
			}
		}

		public byte[] Signature {
			get {
				return signature;
			}
		}

		public bool Unread {
			get {
				return unread;
			}
		}

		public Network Network {
			get {
				return network;
			}
		}

		public void Sign ()
		{
			if (id == null) {
				id = Guid.NewGuid().ToString();
			}
			var buf = Encoding.UTF8.GetBytes (CreateSignString());
			signature = Network.Core.CryptoProvider.SignData (buf, new SHA1CryptoServiceProvider());
		}

		public bool Verify ()
		{
			var remoteNode = network.TrustedNodes[node.NodeID];
			var buf = Encoding.UTF8.GetBytes (CreateSignString());
			return remoteNode.CreateCrypto().VerifyData (buf, new SHA1CryptoServiceProvider(), signature);
		}

		//XXX: Ewwww
		private string CreateSignString()
		{
			var tmpsig = signature;
			signature = null;
			var returnMe = node.NodeID + id + Subject + createdOn + Text;
			signature = tmpsig;
			return returnMe;
		}
	}

	/*
	public class FileLink
	{
		public string FileName;
		public long FileSize;
		public string FilePath;
	}
	*/
}
