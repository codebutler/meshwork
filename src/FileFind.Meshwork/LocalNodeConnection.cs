//
// LocalNodeConnection.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.Timers;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using System.Net;
using FileFind;
using FileFind.Meshwork;
using FileFind.Meshwork.Collections;
using FileFind.Meshwork.Exceptions;
using FileFind.Meshwork.Transport;
using FileFind.Meshwork.Protocol;

namespace FileFind.Meshwork
{
	public delegate void LocalNodeConnectionEventHandler (LocalNodeConnection connection);
	public delegate void LocalNodeConnectionErrorEventHandler(LocalNodeConnection connection, Exception ex);

	public class LocalNodeConnection : INodeConnection, IMeshworkOperation
	{
		private TrustedNodeInfo remoteNodeInfo;
		private Node thisNodeRemote;
		private bool readySent = false;

		private System.Timers.Timer pingTimer;
		private System.Timers.Timer timeoutTimer;

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
		
		public bool Incoming {
			get {
				return transport.Incoming;
			}
		}
		
		public ITransport Transport {
			get {
				return transport;
			}
		}
		
		public bool ReadySent {
			get {
				return readySent;
			}
		}
	
		public double Latency {
			get {
				return latency;
			}
		}

		public Node NodeLocal {
			get {
				return transport.Network.LocalNode;
			}
			set {
				throw new InvalidOperationException ("You cannot set this property.");
			}
		}

		public TrustedNodeInfo RemoteNodeInfo {
                       get {
                               return remoteNodeInfo;
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

		public LocalNodeConnection (ITransport transport)
		{
			if (transport == null) {
				throw new ArgumentNullException ("transport");
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
			pingTimer = new System.Timers.Timer (30000);
			pingTimer.Enabled = false;
			pingTimer.AutoReset = false;
			pingTimer.Elapsed += new ElapsedEventHandler (PingTimerElapsed);

			// Timeout if no pong after a minute
			timeoutTimer = new System.Timers.Timer (60000);
			timeoutTimer.Enabled = false;
			timeoutTimer.AutoReset = false;
			timeoutTimer.Elapsed += new ElapsedEventHandler (TimeOutTimerElapsed);
		}


	 	internal void SendMessage (Message message)
		{
			if (connectionState == ConnectionState.Disconnected) {
				throw new InvalidOperationException("Not connected");
			}

			// XXX: Clean up this validation
			if (message.From == transport.Network.LocalNode.NodeID && message.To != String.Empty
			    && Network.InsecureMessageTypes.IndexOf(message.Type) == -1) {
				if (!transport.Network.TrustedNodes.ContainsKey(message.To)) {
					throw new Exception("You cannot send messages to untrusted nodes." + 
							Environment.NewLine + "   Type: " + message.Type + 
							Environment.NewLine + "   From: " + message.From + 
							Environment.NewLine + "   To: " + message.To + 
							Environment.NewLine + "   MessageID: " + message.MessageID);
				}
			}

			AsyncCallback messageSentCallback = new AsyncCallback(MessageSent);

			SentMessageInfo info = new SentMessageInfo ();

			byte[] messageBytes = message.GetAssembledData();
			transport.BeginSendMessage(messageBytes, messageSentCallback, info);

			info.Connection = this;
			info.Message = message;
			Core.RaiseMessageSent(info);
		}

		private void MessageSent(IAsyncResult asyncResult)
		{
			try {
				transport.EndSendMessage(asyncResult);

				SentMessageInfo info = (SentMessageInfo)asyncResult.AsyncState;
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
				this.SendMessage (transport.Network.MessageBuilder.CreateMyKeyMessage (null));
			}
		}

		private void ReceiveMessage()
		{
			AsyncCallback receiveDataCallback = new AsyncCallback(ReceivedMessage);
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
				
				this.ConnectionState = ConnectionState.Disconnected;

				transport.Network.Connections.Remove(this);
			
				if (ex != null) {					
					LoggingService.LogError("Error in connection with " + this.RemoteAddress, ex);
					if (ConnectionError != null)
						ConnectionError(this, ex);
				}
				
				LoggingService.LogInfo("Connection to {0} closed.", this.RemoteAddress.ToString());
				
				if (ConnectionClosed != null) {
					ConnectionClosed(this);
				}

				transport.Network.Cleanup();
			
				if (NodeRemote != null) { 
					transport.Network.SendBroadcast(transport.Network.MessageBuilder.CreateConnectionDownMessage(NodeLocal, this.NodeRemote), this.NodeRemote);
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
				this.SendMessage (transport.Network.MessageBuilder.CreateReadyMessage(NodeRemote));
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
			
			LoggingService.LogInfo("Connection to {0} is ready.", this.NodeRemote.NickName);
			
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
				
				byte[] messageData = transport.EndReceiveMessage(result);
				
				// Get the next one!
				ReceiveMessage();

				Message message = Message.Parse(transport.Network, messageData);

				ReceivedMessageInfo info = new ReceivedMessageInfo ();
				info.Connection = this;
				info.Message = message;
				Core.RaiseMessageReceived(info);

				if (remoteNodeInfo == null) {

					KeyInfo key = (KeyInfo) message.Content;
					
					RSACryptoServiceProvider provider = new RSACryptoServiceProvider ();
					provider.FromXmlString (key.Key);
					string nodeID = FileFind.Common.MD5 (provider.ToXmlString (false));
				
					if (nodeID.ToLower () == transport.Network.LocalNode.NodeID.ToLower ()) {
						throw new Exception ("You cannot connect to yourself!");
					}
					
					if (!transport.Network.TrustedNodes.ContainsKey(nodeID)) {

						// XXX: Include this somewhere else. In fact, it might make sense to add
						// a KeyReceived event to LocalNodeConnection instead of using Network's.
						// key.Info = (transport.RemoteEndPoint as IPEndPoint).Address.ToString ();
						
						bool acceptKey = transport.Network.RaiseReceivedKey (null, key);
						if (acceptKey) {
							TrustedNodeInfo trustedNode = new TrustedNodeInfo();
							trustedNode.NodeID = nodeID;
							trustedNode.Identifier = String.Format("[{0}]", nodeID);
							trustedNode.EncryptionParameters = provider.ExportParameters(false);														
							transport.Network.AddTrustedNode(trustedNode);
							Core.Settings.SyncTrustedNodes();
							Core.Settings.SaveSettings();
						} else {
							throw new ConnectNotTrustedException ();
						}
					}
					
					if (transport.Network.TrustedNodes[nodeID].AllowConnect == false)
						throw new ConnectNotAllowedException(nodeID);
					
					foreach (LocalNodeConnection connection in transport.Network.GetLocalConnections()) {
						if (connection != this)
							if (connection.NodeRemote != null) 
								if (connection.NodeRemote.NodeID == nodeID)
									throw new Exception ("Already connected!");
					}

					remoteNodeInfo = transport.Network.TrustedNodes[nodeID];
					if (!transport.Network.Nodes.ContainsKey(RemoteNodeInfo.NodeID)) {
						Node node = new Node (transport.Network, RemoteNodeInfo.NodeID);
						node.NickName = RemoteNodeInfo.Identifier;
						node.Verified = true;
						transport.Network.AddNode(node);
						transport.Network.RaiseUserOnline(node);
					}
					this.NodeRemote = transport.Network.Nodes[RemoteNodeInfo.NodeID];

					if (transport.Incoming == true) {
						this.SendMessage(transport.Network.MessageBuilder.CreateMyKeyMessage (null));
					} else {
						//ConnectionState = ConnectionState.Authenticating;
						RaiseConnectionInfoChanged ();

						Message m = transport.Network.MessageBuilder.CreateAuthMessage (this, RemoteNodeInfo);
						SendMessage(m);
					}

				} else {
					ThreadPool.QueueUserWorkItem(new WaitCallback(transport.Network.ProcessMessage), info);
				}
			} catch (Exception ex) {
				Disconnect(ex);
			}
		}
	}

	public class SentMessageInfo : MessageInfo
	{
		bool sent = false;

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
			this.Message = message;
			this.Connection = connection;
		}

		protected void OnChanged ()
		{
			if (Changed != null) {
				Changed(this, EventArgs.Empty);
			}
		}
	}
}
