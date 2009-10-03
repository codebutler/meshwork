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
		PublicKey m_PublicKey;
		RSACryptoServiceProvider m_Crypto;
		string m_NodeId;
		string m_Identifier;
		List<DestinationInfo> m_DestinationInfos = new List<DestinationInfo>();

		public TrustedNodeInfo ()
		{
			// For the serializer
		}

		public TrustedNodeInfo (PublicKey key)
		{
			this.Identifier = key.Nickname;
			this.PublicKey = key;
		}

		public string Identifier {
			get { return m_Identifier; }
			set {
				m_Identifier = value;
				if (m_PublicKey != null)
					m_PublicKey.Nickname = value;
			}
		}

		// XXX: Replace these with properties.
		public bool AllowProfile = true;
		public bool AllowNetworkInfo = true;
		public bool AllowSharedFiles = true;
		public DateTime LastConnected = new DateTime(0);
		public bool AllowAutoConnect = true;
		public bool AllowConnect = true;

		public string NodeID {
			get { return m_NodeId; }
		}

		[XmlIgnore]
		public PublicKey PublicKey {
			get { return m_PublicKey; }
			set {
				if (value != null) {
					var crypto = new RSACryptoServiceProvider(new CspParameters());
					crypto.FromXmlString(value.Key);
					string nodeId = Common.MD5(value.Key);
					m_PublicKey = value;
					m_PublicKey.Nickname = m_Identifier;
					m_Crypto = crypto;
					m_NodeId = nodeId;
				} else {
					m_PublicKey = null;
					m_Crypto = null;
					m_NodeId = null;
				}
			}
		}

		[XmlElement("PublicKey")]
		public System.Xml.XmlCDataSection PublicKeyData {
			get {
				var doc = new System.Xml.XmlDocument();
				return doc.CreateCDataSection(this.PublicKey.Key);
			}
			set {
				try {
					this.PublicKey = new PublicKey(value.Value);
				} catch (Exception ex) {
					LoggingService.LogError("Error loading TrustedNodeInfo", ex);
					this.PublicKey = null;
				}
			}
		}

		[XmlIgnore]
		public RSACryptoServiceProvider Crypto {
			get {
				if (m_Crypto == null)
					throw new Exception("No Crypto object for " + Identifier + "!");
				return m_Crypto;
			}
		}

		public List<DestinationInfo> DestinationInfos {
			get { return m_DestinationInfos; }
		}

		[XmlIgnore]
		public IDestination[] Destinations {
			get {
				List<IDestination> result = new List<IDestination>();
				foreach (DestinationInfo info in m_DestinationInfos) {
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
				
				result.Sort(delegate(IDestination a, IDestination b) {
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
