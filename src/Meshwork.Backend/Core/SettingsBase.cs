//
// SettingsBase.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2008 Meshwork Authors
//

using System;
using System.Collections.Generic;
using System.Linq;
using Meshwork.Backend.Core.Destination;
using Meshwork.Backend.Core.Protocol;
using Meshwork.Backend.Core.Transport;
using Meshwork.Backend.Feature.FileSearch;

namespace Meshwork.Backend.Core
{
	public abstract class SettingsBase : ISettings
	{
	    string[] sharedDirectories = new string [0];

        public static readonly string DefaultStunServer = "stun.stunprotocol.org";

	    public abstract bool FirstRun {
			get; set; }

		public abstract string ClientName {
			get;
		}

		public abstract string ClientVersion {
			get;
		}

		public abstract string OperatingSystem {
			get;
		}

		public abstract string DataPath { get; set; }

	    public abstract void SaveSettings ();

		public List<NetworkInfo> Networks { get; set; } = new List<NetworkInfo> ();

	    public string CompletedDownloadDir { get; set; } = string.Empty;

	    public string IncompleteDownloadDir { get; set; } = string.Empty;

	    public string Email { get; set; } = string.Empty;

	    public string NickName { get; set; } = string.Empty;

	    public string RealName { get; set; } = string.Empty;

	    public int AutoConnectCount { get; set; } = 2;

	    public List<string> Plugins { get; } = new List<string>();

	    public List<DestinationInfo> SavedDestinationInfos { get; } = new List<DestinationInfo>();

	    public string PrivateKey
	    {
	        get;
	        set;
	    }

		public string StunServer { get; set; } = DefaultStunServer;

	    public DateTime LastShareScan { get; set; } = DateTime.MinValue;

	    public FileSearchGroup SavedSearches { get; } = new FileSearchGroup();

	    public int TcpListenPort { get; set; } = TcpTransport.DefaultPort;

	    public bool TcpListenPortOpen { get; set; }

	    public bool DetectInternetIPOnStart { get; set; } = true;

	    public int IPv6LinkLocalInterfaceIndex { get; set; } = -1;

	    public string[] SharedDirectories {
			get {
				return sharedDirectories;
			}
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				if (sharedDirectories != null && !sharedDirectories.SequenceEqual(value)) {
					LastShareScan = DateTime.MinValue;
				}

				sharedDirectories = value;
			}
		}

		public int GlobalDownloadSpeedLimit { get; set; } = 80;

	    public int GlobalUploadSpeedLimit { get; set; } = 10;

	    public bool EnableGlobalDownloadSpeedLimit { get; set; }

	    public bool EnableGlobalUploadSpeedLimit { get; set; }

	    public void SyncNetworkInfo (Core core)
		{
			foreach (var info in Networks) {
				foreach (var network in core.Networks) {
					if (network.NetworkID == info.NetworkId) {
						info.TrustedNodes.Clear();
						foreach (var tni in network.TrustedNodes.Values) {
							info.TrustedNodes.Add(tni.NodeId, tni);
						}
						
						info.Memos.Clear();
						foreach (var memo in network.Memos) {
							if (core.IsLocalNode(memo.Node)) {
								var memoInfo = new MemoInfo(memo);
								memoInfo.FromNodeID = null;
								info.Memos.Add(memoInfo);
							}
						}
						
						break;
					}
				}
			}
		}
	
		public void SyncNetworkInfoAndSave (Core core)
		{
			SyncNetworkInfo(core);
			SaveSettings();
		}
	}
}
