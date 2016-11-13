//
// TrustedNodeInfo.cs: A trusted node
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2005 FileFind.net (http://filefind.net)
//

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Meshwork.Backend.Core.Destination;
using Meshwork.Backend.Core.Protocol;

namespace Meshwork.Backend.Core
{
	public class TrustedNodeInfo
	{
	    public TrustedNodeInfo()
	    {

	    }

	    public TrustedNodeInfo(PublicKey publicKey)
	    {
	        Identifier = publicKey.Nickname;
	        NodeId = publicKey.Fingerprint;
	        PublicKey = publicKey.Key;
	    }

	    public TrustedNodeInfo(
	        string identifier,
	        string nodeId,
	        string publicKey)
	    {
	        Identifier = identifier;
	        NodeId = nodeId;
	        PublicKey = publicKey;
	    }

	    public string Identifier { get; set; }

	    public string NodeId { get; set; }

	    public bool AllowProfile { get; set; } = true;

	    public bool AllowNetworkInfo { get; set; } = true;

	    public bool AllowSharedFiles { get; set; } = true;

	    public bool AllowAutoConnect { get; set; } = true;

	    public bool AllowConnect { get; set; } = true;

	    public string PublicKey { get; set; }

	    public DateTime? LastConnected { get; set; }

	    public IList<DestinationInfo> DestinationInfos { get; private set; } = new List<DestinationInfo>();

	    // FIXME: Cache this again...
	    public RSACryptoServiceProvider CreateCrypto()
	    {
	        var crypto = new RSACryptoServiceProvider(new CspParameters());
	        crypto.FromXmlString(PublicKey);
	        return crypto;
	    }

	    public IDestination[] GetDestinations(Core core) {
            var result = new List<IDestination>();
            foreach (var info in DestinationInfos) {
                if (info.IsSupported(core)) {
                    info.CreateAndAddDestination(core, result);
                }
            }
            return result.ToArray();
		}

		public IDestination GetFirstConnectableDestination(Core core) {
            var destinations = GetConnectableDestinations(core);
            return destinations.Length == 0 ? null : destinations[0];
		}

		/// <summary>Get a list of destinations that we can connect to.</summary>
		public IDestination[] GetConnectableDestinations(Core core) {
            return DestinationManager.GetConnectableDestinations(GetDestinations(core));
		}

	    public void Update(NodeInfo nodeInfo)
	    {
	        Identifier = nodeInfo.NickName;
	        DestinationInfos = new List<DestinationInfo>(nodeInfo.DestinationInfos).AsReadOnly();
	    }
	}
}
