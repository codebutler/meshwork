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
using FileFind.Meshwork.Protocol;
using FileFind.Meshwork.Exceptions;
using FileFind.Collections;

namespace FileFind.Meshwork
{
	public class ChatRoom : FileFind.Meshwork.Object
	{
		string id;
		string name;
		string password;
		Dictionary<string, Node> users = new Dictionary<string, Node>();
		Network network;

		public ChatRoom (Network network, string id, string name)
		{
			this.network = network;
			this.id = id;
			this.name = name;
		}
		
		internal ChatRoom (Network network, ChatRoomInfo info)
		{
			this.network = network;
			this.id = info.Id;
			this.name = info.Name;
		}

		public Network Network {
			get {
				return network;
			}
		}

		public string Id {
			get {
				return id;
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
				if (!String.IsNullOrEmpty(value)) {
					if (!TestPassword(value))
						throw new ArgumentException("Invalid password");
					password = value;
				} else
					password = null;
			}
		}

		public bool HasPassword {
			get {
				return this.id != Common.SHA512Str(this.name);
			}
		}

		public bool InRoom {
			get {
				return users.ContainsKey(Core.MyNodeID);
			}
		}

		public bool TestPassword (string password)
		{
			if (!HasPassword)
				return true;

			return (id == Common.SHA512Str(name + password));
		}
	}
}
