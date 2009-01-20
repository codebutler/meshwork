//
// Node.cs: A node on the network.
// 
// Author:
//   Eric Butler <eric@filefind.net>
//
//   (C) 2005-2008 FileFind.net (http://filefind.net/)
//

using System.Xml.Serialization;
using System.Security.Cryptography;
using System;
using Org.Mentalis.Security.Cryptography;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using FileFind;
using FileFind.Meshwork;
using FileFind.Meshwork.Collections;
using FileFind.Meshwork.Exceptions;
using FileFind.Meshwork.Filesystem;
using FileFind.Meshwork.Destination;

namespace FileFind.Meshwork 
{
	public class Node : FileFind.Meshwork.Object
	{
		//TODO: This needs to all be dynamic..

		// 256-bit aes:
		const int keySize = 16;
		const int ivSize = 16;
		byte[] keyBytes;
		byte[] ivBytes;

		string nickName = String.Empty;
		long avatarSize = 0;
		string email = String.Empty;
		bool verified = false;
		string realName = String.Empty;
		long sharedFiles = 0;
		long sharedBytes = 0;
		string clientName = String.Empty;
		string clientVersion = String.Empty;
		string operatingSystem = String.Empty;
		bool remotelyUntrusted = false;
		bool sentKeyExchange = false;
		bool remoteHasKey = false;
		bool localHasKey = false;
		DiffieHellmanManaged diffieHellman;
		SymmetricAlgorithm alg;
		string sessionKeyDataHash = String.Empty;
		Network network;
		private string nodeID;

		public Node(Network network, string nodeID)
		{
			if (network == null) {
				throw new ArgumentNullException("network");
			}
			
			if (nodeID.Length != 32) {
				throw new ArgumentException("Invaid NodeID specified.");
			}

			this.nodeID = nodeID;
			this.network = network;

			alg = new RijndaelManaged ();
			diffieHellman = new DiffieHellmanManaged();
		}

		public string NickName {
			get {
				return nickName;
			}
			internal set {
				if (value == null || value.Length == 0) {
					throw new ArgumentException ("You must specify a nickname.");
				}

				nickName = value;
			}
		}

		internal DiffieHellmanManaged DiffieHellman {
			get {
				return diffieHellman;
			}
		}

		public long AvatarSize {
			get {
				return avatarSize;
			}
			// XXX: Make this internal
			set {
				avatarSize = value;
			}
		}

		public string Email {
			get {
				return email;
			}
			internal set {
				email = value;
			}
		}

		public string RealName {
			get {
				return realName;
			}
			internal set {
				realName = value;
			}
		}

		public long Files {
			get {
				if (nodeID == Core.MyNodeID) {
					return Core.FileSystem.YourTotalFiles;
				} else {
					return sharedFiles;
				}
			}
			internal set {
				if (nodeID == Core.MyNodeID) {
					throw new InvalidOperationException();
				}
				sharedFiles = value;
			}
		}

		public long Bytes {
			get {
				if (nodeID == Core.MyNodeID) {
					return Core.FileSystem.YourTotalBytes;
				} else {
					return sharedBytes;
				}
			} 
			internal set {
				if (nodeID == Core.MyNodeID) {
					throw new InvalidOperationException();
				}
				sharedBytes = value;
			}
		}

		public string ClientName {
			get {
				return clientName;
			}
			internal set {
				clientName = value;
			}
		}

		public string ClientVersion {
			get {
				return clientVersion;
			}
			internal set {
				clientVersion = value;
			}
		}

		public string OperatingSystem {
			get {
				return operatingSystem;
			}
			internal set {
				operatingSystem = value;
			}
		}

		public bool RemotelyUntrusted {
			get {
				return remotelyUntrusted;
			}
			internal set {
				remotelyUntrusted = value;
			}
		}

		public bool SentKeyExchange {
			get {
				return sentKeyExchange;
			}
			internal set {
				sentKeyExchange = value;
			}
		}

		public bool RemoteHasKey {
			get {
				return remoteHasKey;
			}
			internal set {
				remoteHasKey = value;
			}
		}

		public bool LocalHasKey {
			get {
				return localHasKey;
			}
		}
		
		public bool Verified {
			get {
				if (Core.IsLocalNode(this))
					return true;
				else
					return verified;
			}
			internal set {
				verified = value;
			}
		}
		
		public string SessionKeyDataHash {
			get {
				return sessionKeyDataHash;
			}
			internal set {
				sessionKeyDataHash = value;
			}
		}

		public bool FinishedKeyExchange	{
			get {
				return (RemoteHasKey == true & LocalHasKey == true);
			}
		}

		public string NodeID {
			get {
				return nodeID;
			}
		}

		public TrustedNodeInfo GetTrustedNode ()
		{
			if (network.TrustedNodes.ContainsKey(this.NodeID)) {
				return network.TrustedNodes[this.NodeID];
			} else {
				return null;
			}
		}

		public string GetAmountSharedString()
		{
			return String.Format ("{0} Files ({1})",
			                      FileFind.Common.FormatNumber(this.Files),
			                      FileFind.Common.FormatBytes(this.Bytes));
		}

		public override string ToString()
		{
			if (NickName != "") {
				return NickName;
			} else {
				return NodeID;
			}
		}

		public Network Network {
			get {
				return network;
			}
		}

		public bool IsMe {
			get {
				return (nodeID == Core.MyNodeID);
			}
		}

		public void CreateNewSessionKey()
		{
			if (this.FinishedKeyExchange == false) {

				// The logic elsewhere is to call this method unles RemoteHasKey == true.
				// That needs to be cleaned up, because this is pointless.
				if (sentKeyExchange == true) {
					//LogManager.Current.WriteToLog("CreateNewSessionKey() AGAIN for " + this.ToString() + "\n" + Environment.StackTrace);
					return;
				}

				try {
					LogManager.Current.WriteToLog("Creating secure communication channel to {0}...", this.ToString());

					sentKeyExchange = true;

					byte[] keyExchange = diffieHellman.CreateKeyExchange();
					Message m = network.MessageBuilder.CreateNewSessionKeyMessage(this, keyExchange);
					AckMethod c = new AckMethod();
					c.args = new object[]{ this };
					c.Method += new AckMethod.MethodEventHandler(network.NewSessionKeyReady);
					network.AckMethods.Add(m.MessageID, c);
					network.SendRoutedMessage(m);
				} catch (Exception ex) {
					LogManager.Current.WriteToLog("Failed to create key exchange! Hopefully we will retry...");
					sentKeyExchange = false;
					throw ex;
				}
			} else {
				LogManager.Current.WriteToLog("Why are we trying to CreateNewSessionKey for {0} when FinishedKeyExchange=True?", this.ToString());
			}
		}

		internal void DecryptKeyExchange(byte[] keyExchangeBytes)
		{

			keyExchangeBytes = diffieHellman.DecryptKeyExchange(keyExchangeBytes);

			keyBytes = new byte[keySize];
			ivBytes = new byte[ivSize];

			Array.Copy(keyExchangeBytes, 0, keyBytes, 0, keySize);
			Array.Copy(keyExchangeBytes, keySize, ivBytes, 0, ivSize);
			//deTransform = alg.CreateDecryptor(keyBytes, ivBytes);
			//enTransform = alg.CreateEncryptor(keyBytes, ivBytes);

			localHasKey = true;
		}

		public ICryptoTransform CreateDecryptor()
		{
			if (keyBytes == null | ivBytes == null)
				throw new Exception("No key for " + this.ToString());
			return alg.CreateDecryptor(keyBytes, ivBytes);
		}

		public ICryptoTransform CreateEncryptor()
		{
			if (keyBytes == null | ivBytes == null)
				throw new Exception("No key for " + this.ToString());
			return alg.CreateEncryptor(keyBytes, ivBytes);
		}

		public void ClearSessionKey()
		{
			localHasKey = false;
			remoteHasKey = false;
			sentKeyExchange = false;
			keyBytes = null;
			ivBytes = null;

			sharedBytes = 0;
			sharedFiles = 0;
		}
		
		public bool IsConnectedLocally {
			get {
				foreach (INodeConnection connection in network.Connections) {
					if (connection is LocalNodeConnection) {
						if (connection.NodeLocal == network.LocalNode & connection.NodeRemote == this) 
							return true;
						else if (connection.NodeRemote == network.LocalNode & connection.NodeLocal == this) 
							return true;
					}
				}
				return false;
			}
		}


	/*	public ulong SendPing()
		{
			if (timeOfLastPing == 0) {
				ulong timestamp = FileFind.Common.GetUnixTimestamp();
				network.SendRoutedMessage(network.MessageBuilder.CreatePingMessage(this, timestamp));
				timeOfLastPing = timestamp;
				return timestamp;
			} else {
				throw new Exception("Already waiting for a pong");
			}
		}
*/
		
	/*	public void SendReady ()
		{
			//TODO: Fix this ready foo
			//if (readySent == false) {
				network.SendRoutedMessage(network.MessageBuilder.CreateReadyMessage(this));
			//	readySent = true;
			//} else {
			//	throw new Exception ("`Ready' was already sent.");
			//}
		}*/

		internal INodeConnection[] GetConnections ()
		{
			List<INodeConnection> result = new List<INodeConnection>();
			foreach (INodeConnection connection in network.Connections) {
				if (connection.NodeLocal == this || connection.NodeRemote == this) {
					result.Add(connection);
				}
			}
			return result.ToArray();
		}

		public DestinationInfo[] DestinationInfos {
			get {
				if (IsMe) {
					return Core.DestinationManager.DestinationInfos;
				} else {
					TrustedNodeInfo tnode = GetTrustedNode();
					if (tnode != null) {
						return tnode.DestinationInfos.ToArray();
					} else {
						return null;
					}
				}
			}
		}	
		
		[XmlIgnore]
		public IDestination[] Destinations {
			get {
				if (IsMe) {
					return Core.DestinationManager.Destinations;
				} else {
					TrustedNodeInfo tnode = GetTrustedNode();
					if (tnode != null) {
						return tnode.Destinations;
					} else {
						return null;
					}
				}
			}
		}

		[XmlIgnore]
		public IDestination FirstConnectableDestination {
			get {
				IDestination[] destinations = this.ConnectableDestinations;
				if (destinations.Length == 0) {
					return null;
				} else {
					return destinations[0];
				}
			}
		}

		/// <summary>Get a list of destinations that we can connect to.</summary>
		[XmlIgnore]
		public IDestination[] ConnectableDestinations {
			get {
				List<IDestination> result = new List<IDestination>();

				foreach (IDestination d in this.Destinations) {
					if (d.CanConnect) {
						result.Add(d);
					}
				}

				result.Sort(delegate (IDestination a, IDestination b) {
					if (a.IsExternal && !b.IsExternal) {
						return 1;
					} else if (!a.IsExternal && b.IsExternal) {
						return -1;
					} else {
						return 0;
					}
				});
				
				return result.ToArray();
			}
		}
	}
}
