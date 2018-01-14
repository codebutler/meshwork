//
// ChatRoom.cs: A Meshwork chat room
//
// Author:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2005-2006 Meshwork Authors
//

using System;
using System.Collections.Generic;
using Meshwork.Backend.Core.Protocol;
using Meshwork.Common;

namespace Meshwork.Backend.Core
{
	public class ChatRoom
	{
	    private string password;
	    private readonly Dictionary<string, Node> users = new Dictionary<string, Node>();

	    public ChatRoom (Network network, string id, string name)
		{
			Network = network;
			Id = id;
			Name = name;
		}

		internal ChatRoom (Network network, ChatRoomInfo info)
		{
			Network = network;
			Id = info.Id;
			Name = info.Name;
		}

	    [Obsolete]
	    public Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();

		public Network Network { get; }

	    public string Id { get; }

	    public string Name { get; }

	    public IDictionary<string, Node> Users => new ReadOnlyDictionary<string, Node>(users);

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
				if (!string.IsNullOrEmpty(value)) {
					if (!TestPassword(value))
						throw new ArgumentException("Invalid password");
					password = value;
				} else
					password = null;
			}
		}

		public bool HasPassword => Id != Common.Utils.SHA512Str(Name);

	    public bool InRoom => users.ContainsKey(Network.Core.MyNodeID);

	    public bool TestPassword (string password)
		{
			if (!HasPassword)
				return true;

			return (Id == Common.Utils.SHA512Str(Name + password));
		}
	}
}
