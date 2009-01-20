//
// TrustedNodeInfo.cs: A trusted node
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2005 FileFind.net (http://filefind.net)
//

using System;
using System.Net;
using System.Security.Cryptography;
using System.Xml.Serialization;
using System.Collections.Generic;
using FileFind;
using FileFind.Meshwork;
using FileFind.Meshwork.Collections;
using FileFind.Meshwork.Exceptions;
using FileFind.Meshwork.Destination;

namespace FileFind.Meshwork
{
	public class TrustedNodeInfo
	{	
		/* Private Variables */
		RSAParameters encryptionParameters;
		RSACryptoServiceProvider crypto;
		string nodeId = "";
		string identifier;
		List<DestinationInfo> destinationInfos = new List<DestinationInfo>();

		public TrustedNodeInfo () 
		{
			// For the serializer
		}

		public TrustedNodeInfo (PublicKey key)
		{
			RSACryptoServiceProvider r = new RSACryptoServiceProvider();
			r.FromXmlString(key.Key);

			this.Identifier = key.Identifier;
			this.NodeID = FileFind.Common.MD5(key.Key);
			this.EncryptionParameters = r.ExportParameters(false);
		}

		public TrustedNodeInfo (Node node, RSAParameters parameters)
		{
			if (node == null) {
				throw new ArgumentException ("node");
			}

			// TODO: Check for every field?
			if (parameters.Modulus == null)
				throw new Exception ("parameters cannot be null");

			this.nodeId = node.NodeID;
			this.Identifier = node.NickName;
			this.EncryptionParameters = parameters;
		}
		
		public string Identifier {
			get {
				return identifier;
			}
			set {
				identifier = value;
			}
		}

		// XXX: Replace these with properties.
		public bool AllowProfile = true;
		public bool AllowNetworkInfo = true;
		public bool AllowSharedFiles = true;
		public DateTime LastConnected = new DateTime (0);
		public bool AllowAutoConnect = true;
		public bool AllowConnect = true;

		public string NodeID {
			get {
				return nodeId;
			}
			set {
				nodeId = value.ToLower();
			}
		}

		[XmlElement]
		public RSAParameters EncryptionParameters {
			get {
				return encryptionParameters;
			}
			set {
				encryptionParameters = value;
				crypto = new RSACryptoServiceProvider(new CspParameters());
				crypto.ImportParameters(value);
			}
		}

		[XmlIgnore]
		public RSACryptoServiceProvider Crypto {
			get {
				if (crypto == null)
					throw new Exception ("No Crypto object for " + Identifier + "!");
				return crypto;
			}
		}

		[XmlIgnore]
		public string PublicKey {
			get {
				return Crypto.ToXmlString(false);
			}
		}

		public List<DestinationInfo> DestinationInfos {
			get {
				return destinationInfos;
			}
		}	
		
		[XmlIgnore]
		public IDestination[] Destinations {
			get {
				List<IDestination> result = new List<IDestination>();
				foreach (DestinationInfo info in destinationInfos) {
					if (info.Supported) {
						info.CreateAndAddDestination(result);
					}
				}
				return result.ToArray();
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
