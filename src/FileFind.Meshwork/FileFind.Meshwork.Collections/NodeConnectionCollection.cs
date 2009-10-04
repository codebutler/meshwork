//
// NodeConnectionCollection.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006-2008 FileFind.net (http://filefind.net)
//

using System;
using System.Collections;
using System.Collections.Generic;

namespace FileFind.Meshwork.Collections
{
	public class NodeConnectionCollection : List<INodeConnection>
	{
		Network network;

		public NodeConnectionCollection (Network network)
		{
			this.network = network;
		}

		public new void Add (INodeConnection c)
		{
			if (c.NodeLocal == null) {
				throw new Exception("Trying to add a connection with no NodeLocal object!!");
			}
			if (c is RemoteNodeConnection && c.NodeRemote == null) {
				throw new Exception("Trying to add a connection with no NodeRemote object!!");
			}
			if (c is RemoteNodeConnection && (!network.Nodes.ContainsKey(c.NodeRemote.NodeID))) {
				throw new Exception("Trying to add a connection with a NodeRemote thats not in network collection!! ('" + c.NodeRemote.NodeID + "')");
			}
			if (!network.Nodes.ContainsKey(c.NodeLocal.NodeID)) {
				throw new Exception("Trying to add a connection with a NodeLocal thats not in network collection!!");
			}
			base.Add(c);
		}
	}
}
