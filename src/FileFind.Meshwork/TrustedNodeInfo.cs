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
		RSACryptoServiceProvider crypto;
		string nodeId;
		string identifier;
		List<DestinationInfo> destinationInfos = new List<DestinationInfo>();

		public TrustedNodeInfo () 
		{
			// For the serializer
		}

		public TrustedNodeInfo (PublicKey key)
		{
			this.Identifier = key.Identifier;
			this.PublicKey = key.Key;
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
		}
		
		[XmlIgnore]
		public string PublicKey {
			get {
				if (crypto != null)
					return crypto.ToXmlString(false);
				else
					return null;
			}
			set {
				if (value != null) {
					crypto = new RSACryptoServiceProvider(new CspParameters());
					crypto.FromXmlString(value);
					nodeId = Common.MD5(value);
				} else {
					crypto = null;
					nodeId = null;
				}
			}
		}
		
		[XmlElement("PublicKey")]
		public System.Xml.XmlCDataSection PublicKeyData {
			get {
				var doc = new System.Xml.XmlDocument();
				return doc.CreateCDataSection(this.PublicKey);
			}
			set {
				try {
					this.PublicKey = value.Value;
				} catch (Exception) {
					this.PublicKey = null;
				}
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
