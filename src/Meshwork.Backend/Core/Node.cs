//
// Node.cs: A node on the network.
//
// Author:
//   Eric Butler <eric@filefind.net>
//
//   (C) 2005-2008 FileFind.net (http://filefind.net/)
//

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Xml.Serialization;
using Meshwork.Backend.Core.Destination;
using Meshwork.Backend.Feature.FileBrowsing.Filesystem;
using Mono.Security.Cryptography;
using Object = Meshwork.Common.Object;

namespace Meshwork.Backend.Core
{
	public class Node : Object
	{
		const int keySize = 32;
		const int ivSize = 16;
		byte[] keyBytes;
		byte[] ivBytes;

		string nickName = string.Empty;
	    bool verified = false;
	    long sharedFiles = 0;
		long sharedBytes = 0;
	    SymmetricAlgorithm alg;

	    public Node (Network network, string nodeID)
		{
			if (network == null) {
				throw new ArgumentNullException("network");
			}

			if (nodeID.Length != 128) {
				throw new ArgumentException("Invalid NodeID specified.");
			}

			this.NodeID = nodeID;
			this.Network = network;

			alg = new RijndaelManaged();
			DiffieHellman = new DiffieHellmanManaged();

			if (nodeID != Core.MyNodeID) {
				Directory = new NodeDirectory(this);
			}
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

		internal DiffieHellmanManaged DiffieHellman { get; }

	    public long AvatarSize { get;
	        // XXX: Make this internal
	        set; } = 0;

	    public string Email { get; internal set; } = string.Empty;

	    public string RealName { get; internal set; } = string.Empty;

	    public long Files {
			get {
				if (NodeID == Core.MyNodeID) {
					return Core.FileSystem.YourTotalFiles;
				} else {
					return sharedFiles;
				}
			}
			internal set {
				if (NodeID == Core.MyNodeID) {
					throw new InvalidOperationException();
				}
				sharedFiles = value;
			}
		}

		public long Bytes {
			get {
				if (NodeID == Core.MyNodeID) {
					return Core.FileSystem.YourTotalBytes;
				} else {
					return sharedBytes;
				}
			}
			internal set {
				if (NodeID == Core.MyNodeID) {
					throw new InvalidOperationException();
				}
				sharedBytes = value;
			}
		}

		public NodeDirectory Directory { get; }

	    public string ClientName { get; internal set; } = string.Empty;

	    public string ClientVersion { get; internal set; } = string.Empty;

	    public string OperatingSystem { get; internal set; } = string.Empty;

	    public bool RemotelyUntrusted { get; internal set; } = false;

	    public bool SentKeyExchange { get; internal set; } = false;

	    public bool RemoteHasKey { get; internal set; } = false;

	    public bool LocalHasKey { get; private set; } = false;

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

		public string SessionKeyDataHash { get; internal set; } = string.Empty;

	    public bool FinishedKeyExchange	{
			get {
				return (RemoteHasKey == true & LocalHasKey == true);
			}
		}

		public string NodeID { get; }

	    public TrustedNodeInfo GetTrustedNode ()
		{
			if (Network.TrustedNodes.ContainsKey(this.NodeID)) {
				return Network.TrustedNodes[this.NodeID];
			} else {
				return null;
			}
		}

		public string GetAmountSharedString()
		{
			return $"{Common.Common.FormatNumber(this.Files)} Files ({Common.Common.FormatBytes(this.Bytes)})";
		}

		public override string ToString()
		{
			if (NickName != "") {
				return NickName;
			} else {
				return NodeID;
			}
		}

		public Network Network { get; }

	    public bool IsMe => (NodeID == Core.MyNodeID);

	    public void CreateNewSessionKey()
		{
			if (this.FinishedKeyExchange == false) {

				// The logic elsewhere is to call this method unless RemoteHasKey == true.
				// That needs to be cleaned up, because this is pointless.
				if (SentKeyExchange == true) {
					//LogManager.Current.WriteToLog("CreateNewSessionKey() AGAIN for " + this.ToString() + "\n" + Environment.StackTrace);
					return;
				}

				try {
					LoggingService.LogInfo("Creating secure communication channel to {0}...", this.ToString());

					SentKeyExchange = true;

					byte[] keyExchange = DiffieHellman.CreateKeyExchange();
					Message m = Network.MessageBuilder.CreateNewSessionKeyMessage(this, keyExchange);
					AckMethod c = new AckMethod();
					c.args = new object[]{ this };
					c.Method += new AckMethod.MethodEventHandler(Network.NewSessionKeyReady);
					Network.AckMethods.Add(m.MessageID, c);
					Network.SendRoutedMessage(m);
				} catch (Exception ex) {
					LoggingService.LogError("Failed to create key exchange! Hopefully we will retry...");
					SentKeyExchange = false;
					throw ex;
				}
			} else {
				LoggingService.LogWarning("Why are we trying to CreateNewSessionKey for {0} when FinishedKeyExchange=True?", this.ToString());
			}
		}

		internal void DecryptKeyExchange(byte[] keyExchangeBytes)
		{

			keyExchangeBytes = DiffieHellman.DecryptKeyExchange(keyExchangeBytes);

			keyBytes = new byte[keySize];
			ivBytes = new byte[ivSize];

			Array.Copy(keyExchangeBytes, 0, keyBytes, 0, keySize);
			Array.Copy(keyExchangeBytes, keySize, ivBytes, 0, ivSize);
			//deTransform = alg.CreateDecryptor(keyBytes, ivBytes);
			//enTransform = alg.CreateEncryptor(keyBytes, ivBytes);

			LocalHasKey = true;
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
			LocalHasKey = false;
			RemoteHasKey = false;
			SentKeyExchange = false;
			keyBytes = null;
			ivBytes = null;

			sharedBytes = 0;
			sharedFiles = 0;
		}

		public bool IsConnectedLocally {
			get {
				foreach (INodeConnection connection in Network.Connections) {
					if (connection is LocalNodeConnection) {
						if (connection.NodeLocal == Network.LocalNode & connection.NodeRemote == this)
							return true;
						else if (connection.NodeRemote == Network.LocalNode & connection.NodeLocal == this)
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
			foreach (INodeConnection connection in Network.Connections) {
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
				return DestinationManager.GetConnectableDestinations(this.Destinations);
			}
		}
	}
}
