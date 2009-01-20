//
// ChatRoom.cs: A Meshwork chat room
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2005-2006 FileFind.net (http://filefind.net)
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Security.Cryptography;
using FileFind;
using FileFind.Meshwork;
using FileFind.Meshwork.Exceptions;
using FileFind.Collections;

namespace FileFind.Meshwork
{
	public class ChatRoom : FileFind.Meshwork.Object
	{
		string name;
		string passwordTest;
		string password;
		Dictionary<string, Node> users = new Dictionary<string, Node>();
		Network network;

		public ChatRoom (Network network, string name)
		{
			this.network = network;
			this.name = name;
		}

		public Network Network {
			get {
				return network;
			}
		}

		public string Name {
			get {
				return name;
			}
		}

		public IDictionary<string, Node> Users {
			get {
				return new ReadOnlyDictionary<string, Node>(users);
			}
		}

		internal void AddUser (Node node)
		{
			users.Add(node.NodeID, node);
		}

		internal void RemoveUser (Node node)
		{
			users.Remove(node.NodeID);
		}

		public string Password {
			get {
				return password;
			}
			internal set {
				password = value;
			}
		}

		public bool HasPassword {
			get {
				return !String.IsNullOrEmpty(password) || !String.IsNullOrEmpty(passwordTest);
			}
		}

		internal string PasswordTest {
			get {
				return passwordTest;
			}
			set {
				passwordTest = value;
			}
		}

		public bool InRoom {
			get {
				return users.ContainsKey(Core.MyNodeID);
			}
		}

		public bool TestPassword (string password)
		{
			if (String.IsNullOrEmpty(password)) {
				return false;
			}

			return (passwordTest == Common.SHA512Str(password));
		}
	}
}
