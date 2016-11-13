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
using Meshwork.Backend.Core.Destination;
using Meshwork.Backend.Feature.FileBrowsing.Filesystem;
using Meshwork.Common.Serialization;
using Org.Mentalis.Security.Cryptography;

namespace Meshwork.Backend.Core
{
	public class Node
	{
		const int keySize = 32;
		const int ivSize = 16;
		byte[] keyBytes;
		byte[] ivBytes;

		string nickName = string.Empty;
	    bool verified;
	    long sharedFiles;
		long sharedBytes;
	    SymmetricAlgorithm alg;

	    public Node (Network network, string nodeId)
		{
			if (network == null) {
				throw new ArgumentNullException(nameof(network));
			}

			if (nodeId.Length != 128) {
				throw new ArgumentException("Invalid NodeID specified.");
			}

			NodeID = nodeId;
			Network = network;

			alg = new RijndaelManaged();
			DiffieHellman = new DiffieHellmanManaged();

			if (nodeId != Network.Core.MyNodeID) {
				Directory = new NodeDirectory(Network.Core, this);
			}
		}

	    [Obsolete]
	    public Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();

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
			get
			{
			    if (NodeID == Network.Core.MyNodeID) {
					return Network.Core.FileSystem.YourTotalFiles;
				}
			    return sharedFiles;
			}
			internal set {
				if (NodeID == Network.Core.MyNodeID) {
					throw new InvalidOperationException();
				}
				sharedFiles = value;
			}
		}

		public long Bytes {
			get
			{
			    if (NodeID == Network.Core.MyNodeID) {
					return Network.Core.FileSystem.YourTotalBytes;
				}
			    return sharedBytes;
			}
			internal set {
				if (NodeID == Network.Core.MyNodeID) {
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

	    public bool SentKeyExchange { get; internal set; }

	    public bool RemoteHasKey { get; internal set; }

	    public bool LocalHasKey { get; private set; }

	    public bool Verified {
			get
			{
			    if (Network.Core.IsLocalNode(this))
					return true;
			    return verified;
			}
			internal set {
				verified = value;
			}
		}

		public string SessionKeyDataHash { get; internal set; } = string.Empty;

	    public bool FinishedKeyExchange	{
			get {
				return (RemoteHasKey & LocalHasKey);
			}
		}

		public string NodeID { get; }

	    public TrustedNodeInfo GetTrustedNode ()
	    {
	        if (Network.TrustedNodes.ContainsKey(NodeID)) {
				return Network.TrustedNodes[NodeID];
			}
	        return null;
	    }

		public string GetAmountSharedString()
		{
			return $"{Common.Utils.FormatNumber(Files)} Files ({Common.Utils.FormatBytes(Bytes)})";
		}

		public override string ToString()
		{
		    if (NickName != "") {
				return NickName;
			}
		    return NodeID;
		}

		public Network Network { get; }

	    public bool IsMe => (NodeID == Network.Core.MyNodeID);

	    public void CreateNewSessionKey()
		{
			if (FinishedKeyExchange == false) {

				// The logic elsewhere is to call this method unless RemoteHasKey == true.
				// That needs to be cleaned up, because this is pointless.
				if (SentKeyExchange) {
					//LogManager.Current.WriteToLog("CreateNewSessionKey() AGAIN for " + this.ToString() + "\n" + Environment.StackTrace);
					return;
				}

				try {
					LoggingService.LogInfo("Creating secure communication channel to {0}...", ToString());

					SentKeyExchange = true;

					var keyExchange = DiffieHellman.CreateKeyExchange();
					var m = Network.MessageBuilder.CreateNewSessionKeyMessage(this, keyExchange);
					var c = new AckMethod();
					c.args = new object[]{ this };
					c.Method += Network.NewSessionKeyReady;
					Network.AckMethods.Add(m.MessageID, c);
					Network.SendRoutedMessage(m);
				} catch (Exception ex) {
					LoggingService.LogError("Failed to create key exchange! Hopefully we will retry...");
					SentKeyExchange = false;
					throw ex;
				}
			} else {
				LoggingService.LogWarning("Why are we trying to CreateNewSessionKey for {0} when FinishedKeyExchange=True?", ToString());
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
				throw new Exception("No key for " + ToString());
			return alg.CreateDecryptor(keyBytes, ivBytes);
		}

		public ICryptoTransform CreateEncryptor()
		{
			if (keyBytes == null | ivBytes == null)
				throw new Exception("No key for " + ToString());
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
				foreach (var connection in Network.Connections) {
					if (connection is LocalNodeConnection)
					{
					    if (connection.NodeLocal == Network.LocalNode & connection.NodeRemote == this)
							return true;
					    if (connection.NodeRemote == Network.LocalNode & connection.NodeLocal == this)
					        return true;
					}
				}
				return false;
			}
		}


	/*	public ulong SendPing()
		{
			if (timeOfLastPing == 0) {
				ulong timestamp = FileFind.Utils.GetUnixTimestamp();
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
			var result = new List<INodeConnection>();
			foreach (var connection in Network.Connections) {
				if (connection.NodeLocal == this || connection.NodeRemote == this) {
					result.Add(connection);
				}
			}
			return result.ToArray();
		}

		public IList<DestinationInfo> DestinationInfos {
			get {
				if (IsMe) {
					return Network.Core.DestinationManager.DestinationInfos;
				}
			    var tnode = GetTrustedNode();
			    return tnode?.DestinationInfos;
			}
		}

	    [DontSerialize]
		public IDestination[] Destinations {
			get {
				if (IsMe) {
					return Network.Core.DestinationManager.Destinations;
				}
			    var tnode = GetTrustedNode();
			    return tnode?.GetDestinations(Network.Core);
			}
		}

	    [DontSerialize]
	    public IDestination FirstConnectableDestination {
			get {
				var destinations = ConnectableDestinations;
				if (destinations.Length == 0) {
					return null;
				}
			    return destinations[0];
			}
		}

		/// <summary>Get a list of destinations that we can connect to.</summary>
		[DontSerialize]
		public IDestination[] ConnectableDestinations {
			get {
				return DestinationManager.GetConnectableDestinations(Destinations);
			}
		}
	}
}
