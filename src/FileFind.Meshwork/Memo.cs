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

namespace FileFind.Meshwork
{
	public class Memo
	{
		//public ArrayList FileLinks = new ArrayList();
		bool unread = true;
		byte[] signature;
		string writtenByNodeID;
		string id;
		string subject;
		string text;
		Network network;
		DateTime createdOn;

		public Memo (Network network, FileFind.Meshwork.Protocol.MemoInfo memoInfo) : this (network)
		{
			id = memoInfo.ID;
			writtenByNodeID = memoInfo.FromNodeID;
			createdOn = memoInfo.CreatedOn;
			signature = memoInfo.Signature;
			subject = memoInfo.Subject;
			text = memoInfo.Text;
		}

		public Memo (Network network)
		{
			this.network = network;
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
			set {
				createdOn = value;
			}
		}

		public string WrittenByNodeID {
			get {
				return writtenByNodeID;
			}
			set {
				writtenByNodeID = value.Trim().ToLower();
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
				id = Common.MD5 (network.CreateMessageID());
			}
			byte[] buf = System.Text.Encoding.UTF8.GetBytes (CreateSignString());
			signature = Core.CryptoProvider.SignData (buf, new SHA1CryptoServiceProvider());
		}

		public bool Verify ()
		{
			TrustedNodeInfo remoteNode = network.TrustedNodes[WrittenByNodeID];
			byte[] buf = System.Text.Encoding.UTF8.GetBytes (CreateSignString());
			return remoteNode.Crypto.VerifyData (buf, new SHA1CryptoServiceProvider(), signature);
		}

		//XXX: Ewwww
		private string CreateSignString()
		{
			byte[] tmpsig = signature;
			signature = null;
			string returnMe = writtenByNodeID + id + Subject + Text;
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
