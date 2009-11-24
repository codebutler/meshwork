//
// Memo.cs: A memo
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2005-2006 FileFind.net (http://filefind.net)
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Security;
using System.Security.Cryptography;
using FileFind;
using FileFind.Meshwork;
using FileFind.Meshwork.Collections;
using FileFind.Meshwork.Exceptions;
using FileFind.Meshwork.Protocol;

namespace FileFind.Meshwork
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
			this.id = memoInfo.ID;
			this.node = network.Nodes[memoInfo.FromNodeID];
			this.createdOn = memoInfo.CreatedOn;
			this.signature = memoInfo.Signature;
			this.subject = memoInfo.Subject;
			this.text = memoInfo.Text;
		}

		public Memo (Network network)
		{
			this.network = network;
			this.node = network.Nodes[Core.MyNodeID];
			this.createdOn = DateTime.Now;
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
			byte[] buf = System.Text.Encoding.UTF8.GetBytes (CreateSignString());
			signature = Core.CryptoProvider.SignData (buf, new SHA1CryptoServiceProvider());
		}

		public bool Verify ()
		{
			TrustedNodeInfo remoteNode = network.TrustedNodes[this.node.NodeID];
			byte[] buf = System.Text.Encoding.UTF8.GetBytes (CreateSignString());
			return remoteNode.Crypto.VerifyData (buf, new SHA1CryptoServiceProvider(), signature);
		}

		//XXX: Ewwww
		private string CreateSignString()
		{
			byte[] tmpsig = signature;
			signature = null;
			string returnMe = node.NodeID + id + Subject + createdOn.ToString() + Text;
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
