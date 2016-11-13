//
// LocalNodeConnection.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.Net;
using System.Threading;
using System.Timers;
using Meshwork.Backend.Core.Protocol;
using Meshwork.Backend.Core.Transport;
using Meshwork.Common.Serialization;
using Timer = System.Timers.Timer;

namespace Meshwork.Backend.Core
{
	public delegate void LocalNodeConnectionEventHandler (LocalNodeConnection connection);
	public delegate void LocalNodeConnectionErrorEventHandler(LocalNodeConnection connection, Exception ex);

	public class LocalNodeConnection : INodeConnection, IMeshworkOperation
	{
		private TrustedNodeInfo remoteNodeInfo;
		private Node thisNodeRemote;
		private bool readySent;

		private Timer pingTimer;
		private Timer timeoutTimer;

		private DateTime pingSent;
		private double latency;

		private ITransport transport;

		ConnectionState connectionState;

		public ConnectionState ConnectionState {
			get {
				return connectionState;
			}
			set {
				connectionState = value;
			}
		}
		
		public string RemoteAddress {
			get {
				return (transport.RemoteEndPoint as IPEndPoint).Address.ToString ();
			}
		}

		public bool Incoming => transport.Incoming;

	    public ITransport Transport => transport;

	    public bool ReadySent => readySent;

	    public double Latency => latency;

	    public Node NodeLocal {
			get {
				return transport.Network.LocalNode;
			}
			set {
				throw new InvalidOperationException ("You cannot set this property.");
			}
		}

		public TrustedNodeInfo RemoteNodeInfo => remoteNodeInfo;

	    public Node NodeRemote {
			get {
				return thisNodeRemote;
			}
			set {
				thisNodeRemote = value;
			}
		}

		public LocalNodeConnection (ITransport transport)
		{
			if (transport == null) {
				throw new ArgumentNullException (nameof(transport));
			}

			if (transport.Network == null) {
				throw new ArgumentException ("transport.Network cannot be null");
			}
			
			this.transport = transport;
			transport.Connected += OnTransportConnected;
			transport.Disconnected += OnTransportDisconnected;
			Construct ();
		}
		
		private void Construct ()
		{
			// Ping every 30 seconds
			pingTimer = new Timer (30000);
			pingTimer.Enabled = false;
			pingTimer.AutoReset = false;
			pingTimer.Elapsed += PingTimerElapsed;

			// Timeout if no pong after a minute
			timeoutTimer = new Timer (60000);
			timeoutTimer.Enabled = false;
			timeoutTimer.AutoReset = false;
			timeoutTimer.Elapsed += TimeOutTimerElapsed;
		}


	 	internal void SendMessage (Message message)
		{
			if (connectionState == ConnectionState.Disconnected) {
				throw new InvalidOperationException("Not connected");
			}

			// XXX: Clean up this validation
			if (message.From == transport.Network.LocalNode.NodeID && message.To != string.Empty
			    && Network.InsecureMessageTypes.IndexOf(message.Type) == -1) {
				if (!transport.Network.TrustedNodes.ContainsKey(message.To)) {
					throw new Exception("You cannot send messages to untrusted nodes." + 
							Environment.NewLine + "   Type: " + message.Type + 
							Environment.NewLine + "   From: " + message.From + 
							Environment.NewLine + "   To: " + message.To + 
							Environment.NewLine + "   MessageID: " + message.MessageID);
				}
			}

			AsyncCallback messageSentCallback = MessageSent;

			var info = new SentMessageInfo ();

			var messageBytes = message.GetAssembledData();
			transport.BeginSendMessage(messageBytes, messageSentCallback, info);

			info.Connection = this;
			info.Message = message;

		    LoggingService.LogDebug("SEND: " + Json.Serialize(message.Content));

			transport.Network.Core.RaiseMessageSent(info);
		}

		private void MessageSent(IAsyncResult asyncResult)
		{
			try {
				transport.EndSendMessage(asyncResult);

				var info = (SentMessageInfo)asyncResult.AsyncState;
				info.Sent = true;

			} catch (Exception ex) {
				if (connectionState != ConnectionState.Disconnected) {
					Disconnect(ex);
				} else {
					LoggingService.LogWarning("Tried to send a message after being disconnected");
				}
			}
		}
		
		public void Start ()
		{
			ReceiveMessage();

			if (transport.Incoming == false) {
				SendMessage (transport.Network.MessageBuilder.CreateMyKeyMessage (null));
			}
		}

		private void ReceiveMessage()
		{
			AsyncCallback receiveDataCallback = ReceivedMessage;
			transport.BeginReceiveMessage(receiveDataCallback, null);
		}
	
		private void OnTransportConnected (ITransport transport)
		{
			if (ConnectionConnected != null)
				ConnectionConnected(this);
		}
		
		private void OnTransportDisconnected (ITransport transport, Exception ex)
		{
			Disconnect (ex);
		}
		
		public void Disconnect () 
		{
			Disconnect (null);
		}

		public void Disconnect (Exception ex)
		{
			try {
				LoggingService.LogDebug("Local Node Connection Disconnect.");

				pingTimer.Stop ();
				timeoutTimer.Stop ();
				
				// theres an if statement in Disconnect() to prevent a loop
				transport.Disconnect ();
				
				ConnectionState = ConnectionState.Disconnected;

				transport.Network.RemoveConnection(this);
			
				if (ex != null) {					
					LoggingService.LogError("Error in connection with " + RemoteAddress, ex);
					if (ConnectionError != null)
						ConnectionError(this, ex);
				}
				
				LoggingService.LogInfo("Connection to {0} closed.", RemoteAddress);
				
				if (ConnectionClosed != null) {
					ConnectionClosed(this);
				}

				transport.Network.Cleanup();
			
				if (NodeRemote != null) { 
					transport.Network.SendBroadcast(transport.Network.MessageBuilder.CreateConnectionDownMessage(NodeLocal, NodeRemote), NodeRemote);
				}
			} catch (Exception exx) {
				LoggingService.LogError(exx);
			}
		}

		public event LocalNodeConnectionEventHandler ConnectionReady;
		public event LocalNodeConnectionEventHandler PongReceived;
		public event LocalNodeConnectionEventHandler ConnectionInfoChanged;
		public event LocalNodeConnectionEventHandler ConnectionClosed;
		public event LocalNodeConnectionEventHandler ConnectionConnected;
		public event LocalNodeConnectionErrorEventHandler ConnectionError;

		public void SendReady()
		{
			if (readySent == false) {
				SendMessage (transport.Network.MessageBuilder.CreateReadyMessage(NodeRemote));
				readySent = true;
			} else {
				throw new Exception ("`Ready' was already sent.");
			}
		}
		
		public void RaiseConnectionInfoChanged() {
			if (ConnectionInfoChanged != null) {
				ConnectionInfoChanged(this);
			}
		}

		internal void RaiseConnectionReady()
		{			
			connectionState = ConnectionState.Ready;
			
			LoggingService.LogInfo("Connection to {0} is ready.", NodeRemote.NickName);
			
			if (ConnectionReady != null) 
				ConnectionReady(this);
			
			Ping();			
		}

		private void PingTimerElapsed (object o, ElapsedEventArgs args)
		{
			Ping ();
		}

		private void Ping ()
		{
			pingSent = DateTime.Now;
			SendMessage(transport.Network.MessageBuilder.CreatePingMessage (NodeRemote, (ulong)pingSent.Ticks));
			timeoutTimer.Start ();
		}

		private void TimeOutTimerElapsed (object o, ElapsedEventArgs args)
		{
			Disconnect (new Exception ("Ping Timeout"));
		}
		
		internal void ReceivedPong (ulong timestamp)
		{
			timeoutTimer.Stop ();

			if (timestamp == (ulong)pingSent.Ticks) {
			
				latency = DateTime.Now.Subtract (pingSent).TotalMilliseconds;

				if (PongReceived != null)
					PongReceived (this);
	
				//pingSent = new DateTime (0);

				pingTimer.Start ();

			} else {
				Disconnect (new Exception ("Invalid PONG recieved!"));
			}
		}

		private void ReceivedMessage(IAsyncResult result) 
		{
			try {
				if (connectionState == ConnectionState.Disconnected) {
					// Connection has been closed. Ignore the message.
					LoggingService.LogWarning("LocalNodeConnection: Ignored message received after connection was closed.");
					return;
				}
				
				var messageData = transport.EndReceiveMessage(result);
				
				if (messageData == null)
					return;
				
				// Get the next one!
				ReceiveMessage();

				Message message = null;
				string messageFrom = null;
				try {
					message = Message.Parse(transport.Network, messageData, out messageFrom);
				} catch (InvalidSignatureException ex) {
					if (string.IsNullOrEmpty(messageFrom) || messageFrom == remoteNodeInfo.NodeId) {
						throw ex;
					}
				    LoggingService.LogWarning("Ignored message with invalid signature from {0}", messageFrom);
				    return;
				}

				var info = new ReceivedMessageInfo ();
				info.Connection = this;
				info.Message = message;
			    transport.Network.Core.RaiseMessageReceived(info);

				if (remoteNodeInfo == null) {
					var keyInfo = (KeyInfo) message.Content;
				    var publicKey = new PublicKey(keyInfo.Info, keyInfo.Key);
				    var nodeId = publicKey.Fingerprint;

				    if (nodeId.ToLower () == transport.Network.LocalNode.NodeID.ToLower ()) {
						throw new Exception ("You cannot connect to yourself!");
					}

					if (!transport.Network.TrustedNodes.ContainsKey(nodeId)) {
						var acceptKey = transport.Network.RaiseReceivedKey (this, keyInfo);
						if (acceptKey)
						{
						    var trustedNode = new TrustedNodeInfo($"[{nodeId}]", nodeId, keyInfo.Key);
							transport.Network.AddTrustedNode(trustedNode);
						    transport.Network.Core.Settings.SyncNetworkInfoAndSave(transport.Network.Core);
						} else {
							throw new ConnectNotTrustedException ();
						}
					}

					if (transport.Network.TrustedNodes[nodeId].AllowConnect == false)
						throw new ConnectNotAllowedException(nodeId);

					foreach (var connection in transport.Network.LocalConnections) {
						if (connection != this && connection.NodeRemote != null && connection.NodeRemote.NodeID == nodeId)
							throw new Exception ("Already connected!");
					}

					remoteNodeInfo = transport.Network.TrustedNodes[nodeId];
					if (!transport.Network.Nodes.ContainsKey(RemoteNodeInfo.NodeId)) {
						var node = new Node (transport.Network, RemoteNodeInfo.NodeId);
						node.NickName = RemoteNodeInfo.Identifier;
						node.Verified = true;
						transport.Network.AddNode(node);
					}
					NodeRemote = transport.Network.Nodes[RemoteNodeInfo.NodeId];

					if (transport.Incoming) {
						SendMessage(transport.Network.MessageBuilder.CreateMyKeyMessage (null));
					} else {
						ConnectionState = ConnectionState.Authenticating;
						RaiseConnectionInfoChanged ();

						var m = transport.Network.MessageBuilder.CreateAuthMessage (this, RemoteNodeInfo);
						SendMessage(m);
					}

				} else {
					ThreadPool.QueueUserWorkItem(transport.Network.ProcessMessage, info);
				}
			} catch (Exception ex) {
				Disconnect(ex);
			}
		}
	}

	public class SentMessageInfo : MessageInfo
	{
		bool sent;

		public bool Sent {
			get {
				return sent;
			}
			set {
				sent = value;
				OnChanged();
			}
		}
	}

	public class ReceivedMessageInfo : MessageInfo
	{
	}

	public class MessageInfo
	{
		public LocalNodeConnection Connection;
		public Message Message;

		public event EventHandler Changed;

		public MessageInfo ()
		{
		}
		
		public MessageInfo (Message message, LocalNodeConnection connection)
		{
			Message = message;
			Connection = connection;
		}

		protected void OnChanged ()
		{
			if (Changed != null) {
				Changed(this, EventArgs.Empty);
			}
		}
	}
}
