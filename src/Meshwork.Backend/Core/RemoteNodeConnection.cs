//
// RemoteNodeConnection.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using Meshwork.Backend.Core.Protocol;

namespace Meshwork.Backend.Core {
	public class RemoteNodeConnection : INodeConnection
	{
		private Network parentNetwork;
		private Node    thisNodeLocal;
		private Node    thisNodeRemote;

		internal RemoteNodeConnection(Network theNetwork)
		{
			parentNetwork = theNetwork;
		}

		internal RemoteNodeConnection(Network theNetwork, ConnectionInfo info)
		{
			parentNetwork = theNetwork;
			thisNodeLocal = parentNetwork.Nodes[info.SourceNodeID];
			thisNodeRemote = parentNetwork.Nodes[info.DestNodeID];
		}

		public ConnectionState ConnectionState {
			get {
				return ConnectionState.Remote;
			}
			set {
			}
		}

		public Node NodeLocal {
			get {
				return thisNodeLocal;
			}
			set {
				thisNodeLocal = value;
			}
		}

		public Node NodeRemote {
			get {
				return thisNodeRemote;
			}
			set {
				thisNodeRemote = value;
			}
		}
	}
}
