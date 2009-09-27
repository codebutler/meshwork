//
// NetworkInfo.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006-2008 FileFind.net (http://filefind.net)
//

using System;
using System.Collections;
using System.Collections.Generic;
using FileFind.Collections;
using FileFind.Meshwork.Protocol;

namespace FileFind.Meshwork
{
	public class NetworkInfo
	{
		List<MemoInfo> memos = new List<MemoInfo>();		
		SerializableDictionary<string, TrustedNodeInfo> trustedNodes = new SerializableDictionary<string, TrustedNodeInfo>();
		string networkName;

		public SerializableDictionary<string, TrustedNodeInfo> TrustedNodes {
			get {
				return trustedNodes;
			}
			set { // Setter exists ONLY for XML Serializer. Do not use!
				trustedNodes = value;
			}
		}
		
		public List<MemoInfo> Memos {
			get {
				return memos;
			}
			set { // Setter exists ONLY for XML Serializer. Do not use!
				memos = value;
			}
		}

		public string NetworkName {
			get {
				return networkName;
			}
			set {
				networkName = value;
			}
		}

		public string NetworkID {
			get {	
				return Common.SHA512Str(networkName);
			}
		}

		public NetworkInfo Clone ()
		{
			NetworkInfo clone = new NetworkInfo();
			clone.NetworkName = this.NetworkName;

			foreach (KeyValuePair<string, TrustedNodeInfo> pair in this.TrustedNodes) {
				// XXX: pair.Value should be cloned too!
				clone.TrustedNodes.Add(pair.Key, pair.Value);
			}
			
			clone.Memos.AddRange(memos);

			return clone;
		}
	}
}
