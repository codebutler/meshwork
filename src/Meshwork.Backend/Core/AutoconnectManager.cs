//
// AutoconnectManager.cs: Automatically keep a specified number of connections open
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2005-2008 FileFind.net (http://filefind.net)
//

using System;
using System.Collections.Generic;

namespace Meshwork.Backend.Core
{
	internal sealed class AutoconnectManager 
	{
		List<TrustedNodeInfo> nodeList = new List<TrustedNodeInfo> ();

		Network network;
		int connectionCount;
		
		NetworkLocalNodeConnectionEventHandler connectingToHandler;
		NetworkLocalNodeConnectionEventHandler incomingConnectionHandler;
		LocalNodeConnectionEventHandler        connectionReadyHandler;
		LocalNodeConnectionEventHandler        connectionClosedHandler;

		public AutoconnectManager (Network network, int connectionCount)
		{
			this.network = network;

			connectingToHandler       = NewConnection;
			incomingConnectionHandler = NewConnection;
			connectionReadyHandler    = OnConnectionReady;
			connectionClosedHandler   = OnConnectionClosed;

			// The number of connections to try to keep open
			this.connectionCount = connectionCount;
		}

		public void Start ()
		{
			network.ConnectingTo += connectingToHandler;
			network.NewIncomingConnection += incomingConnectionHandler;

			foreach (var info in network.TrustedNodes.Values) {
				if (IsGoodNode(info)) {
					nodeList.Add(info);
				}
			}

			nodeList.Sort(new NodeSuccessComparer());

			ConnectIfNeeded();
		}

		public void Stop ()
		{
			network.ConnectingTo -= connectingToHandler;
			network.NewIncomingConnection -= incomingConnectionHandler;

			// XXX: Remove handlers from all connections?
		}

		public int ConnectionCount {
			get {
				return connectionCount;
			}
			set {
				connectionCount = value;
				ConnectIfNeeded();
			}
		}

		private void NewConnection (Network network, LocalNodeConnection connection) 
		{
			connection.ConnectionReady += connectionReadyHandler;
			connection.ConnectionClosed += connectionClosedHandler;
		}

		private void OnConnectionReady (LocalNodeConnection connection)
		{
			var tnode = connection.NodeRemote.GetTrustedNode();
			lock (nodeList) {
				if (nodeList.Contains (tnode)) {
					nodeList.Remove (tnode);
					nodeList.Sort(new NodeSuccessComparer());
				}
			}
		}

		private void OnConnectionClosed (LocalNodeConnection connection)
		{
			connection.ConnectionReady -= connectionReadyHandler;
			connection.ConnectionClosed -= connectionClosedHandler;

			if (connection.RemoteNodeInfo != null && IsGoodNode(connection.RemoteNodeInfo)) {
				nodeList.Add(connection.RemoteNodeInfo);
				nodeList.Sort(new NodeSuccessComparer());
			}

			ConnectIfNeeded();
		}

		private void ConnectIfNeeded()
		{
			var totalConnections = network.LocalConnections.Length;
			if (totalConnections < connectionCount) {
				for (var x = 0; x < (connectionCount - totalConnections); x ++) {
					if (nodeList.Count != 0) {
						var node = (TrustedNodeInfo) GetNode ();
						try {	
							var destination = node.GetFirstConnectableDestination(network.Core);
							if (destination != null) {
								var transport = destination.CreateTransport(ConnectionType.NodeConnection);
								network.ConnectTo(transport);
							}
						} catch (Exception ex) {
							LoggingService.LogError("AutoconnectManager: Error while trying to connect", ex);
						}
					} else {
						// Nothing left, I give up! :(
						network.ConnectingTo -= connectingToHandler;
						network.NewIncomingConnection -= incomingConnectionHandler;
						LoggingService.LogDebug("AutoconnectManager: Nothing left to connect to.");
						return;
					}
				}
			}
		}
	
		private object GetNode ()
		{
			object result = null;
			lock (nodeList) {
				result = nodeList [0];
				nodeList.RemoveAt (0);
			}
			return result;
		}

		private bool IsGoodNode (TrustedNodeInfo node)
		{
			if (node == null) {
				throw new ArgumentNullException("node");
			}

			return (node.AllowConnect && node.AllowAutoConnect &&
			        node.GetConnectableDestinations(network.Core) != null);
		}

		private class NodeSuccessComparer : IComparer<TrustedNodeInfo>
		{
			public int Compare (TrustedNodeInfo firstNode, TrustedNodeInfo secondNode)
			{
			    if (firstNode.LastConnected < secondNode.LastConnected)
					return -1;
			    if (firstNode.LastConnected == secondNode.LastConnected)
			        return 0;
			    return 1;
			}
		}
	}
}
