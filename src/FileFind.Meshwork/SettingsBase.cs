//
// SettingsBase.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using FileFind.Meshwork.Destination;
using FileFind.Meshwork.Search;

namespace FileFind.Meshwork
{
	public abstract class SettingsBase : ISettings
	{
		string                email                       = String.Empty;
		string                nickName                    = String.Empty;
		string                name                        = String.Empty;
		string                stunServer                  = SettingsBase.DefaultStunServer;
		string                completedDownloadDir        = String.Empty;
		string                incompleteDownloadDir       = String.Empty;
		int                   autoConnectCount            = 2;
		DateTime              lastShareScan               = DateTime.MinValue;
		List<NetworkInfo>     networks                    = new List<NetworkInfo> ();
		List<string>          plugins                     = new List<string>();
		FileSearchGroup       searches                    = new FileSearchGroup();
		List<DestinationInfo> destinationInfos            = new List<DestinationInfo>();
		int                   tcpListenPort               = FileFind.Meshwork.Transport.TcpTransport.DefaultPort;
		bool                  tcpListenPortOpen           = false;
		bool                  detectInternetIPOnStart     = true;
		int                   ipv6LinkLocalInterfaceIndex = -1;		
		List<string>          sharedDirectories           = new List<string>();
		RSAParameters         encryptionParameters;
		int                   globalUploadSpeedLimit         = 10;
		int                   globalDownloadSpeedLimit       = 80;
		bool                  enableGlobalUploadSpeedLimit   = false;
		bool                  enableGlobalDownloadSpeedLimit = false;

		public static readonly string DefaultStunServer = "stun.filefind.net";
		
		public abstract string ClientName {
			get;
		}

		public abstract string ClientVersion {
			get;
		}

		public abstract string OperatingSystem {
			get;
		}

		public abstract string DataPath {
			get;
			set;
		}

		public abstract void SaveSettings ();

		public List<NetworkInfo> Networks {
			get {
				return networks;
			}
			set {
				networks = value;
			}
		}

		public string CompletedDownloadDir {
			get {
				return completedDownloadDir;
			}
			set {
				completedDownloadDir = value;
			}
		}
		
 		public string IncompleteDownloadDir {
			get {
				return incompleteDownloadDir;
			}
			set {
				incompleteDownloadDir = value;
			}
		}
		
		public string Email {
			get {
				return email;
			}
			set {
				email = value;
			}
		}

		public string NickName {
			get {
				return nickName;
			}
			set {
				nickName = value;
			}
		}
		
	 	public string RealName {
			get {
				return name;
			}
			set {
				name = value;
			}
		}

		public int AutoConnectCount {
			get {
				return autoConnectCount;
			}
			set {
				autoConnectCount = value;
			}
		}

		public List<string> Plugins {
			get {
				return plugins;
			}
		}

		public List<DestinationInfo> SavedDestinationInfos {
			get {
				return destinationInfos;
			}
		}

		public RSAParameters EncryptionParameters {
			get {
				return encryptionParameters;
			}
			set {
				encryptionParameters = value;
			}
		}
		
		public string StunServer {
			get {
				return stunServer;
			}
			set {
				stunServer = value;
			}
		}

		public DateTime LastShareScan {
			get {
				return lastShareScan;
			}
			set {
				lastShareScan = value;
			}
		}

		public FileSearchGroup SavedSearches {
			get {
				return searches;
			}
		}

		public int TcpListenPort {
			get {
				return tcpListenPort;
			}
			set {
				tcpListenPort = value;
			}
		}

		public bool TcpListenPortOpen {
			get {
				return tcpListenPortOpen;
			}
			set {
				tcpListenPortOpen = value;
			}
		}

		public bool DetectInternetIPOnStart {
			get {
				return detectInternetIPOnStart;
			}
			set {
				detectInternetIPOnStart = value;
			}
		}

		public int IPv6LinkLocalInterfaceIndex  {
			get {
				return ipv6LinkLocalInterfaceIndex;
			}
			set {
				ipv6LinkLocalInterfaceIndex = value;
			}
		}
	
		public List<string> SharedDirectories {
			get {
				return sharedDirectories;
			}
			set {
				sharedDirectories = value;
			}
		}
	
		public int GlobalDownloadSpeedLimit {
			get {
				return globalDownloadSpeedLimit;
			}
			set {
				globalDownloadSpeedLimit = value;
			}
		}

		public int GlobalUploadSpeedLimit {
			get {
				return globalUploadSpeedLimit;
			}
			set {
				globalUploadSpeedLimit = value;
			}
		}

		public bool EnableGlobalDownloadSpeedLimit {
			get {
				return enableGlobalDownloadSpeedLimit;
			}
			set {
				enableGlobalDownloadSpeedLimit = value;
			}
		}

		public bool EnableGlobalUploadSpeedLimit {
			get {
				return enableGlobalUploadSpeedLimit;
			}
			set {
				enableGlobalUploadSpeedLimit = value;
			}
		}

		public void SyncTrustedNodes ()
		{
			foreach (NetworkInfo info in networks) {
				foreach (Network network in Core.Networks) {
					if (network.NetworkID == info.NetworkID) {
						info.TrustedNodes.Clear();
						foreach (TrustedNodeInfo tni in network.TrustedNodes.Values) {
							info.TrustedNodes.Add(tni.NodeID, tni);
						}
						break;
					}
				}
			}
		}
	
		public void SyncTrustedNodesAndSave ()
		{
			SyncTrustedNodes();
			SaveSettings();
		}
	}
}
