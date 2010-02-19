//
// SettingsBase.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using System;
using System.Linq;
using System.Xml.Serialization;
using System.Security.Cryptography;
using System.Collections.Generic;
using FileFind.Meshwork.Destination;
using FileFind.Meshwork.Search;
using FileFind.Meshwork.Protocol;
using FileFind.Meshwork.Security;

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
		string[]              sharedDirectories           = new string [0];
		string                key                            = null;
		string                keyData                        = null;
		byte[]                salt                           = null;
		int                   globalUploadSpeedLimit         = 10;
		int                   globalDownloadSpeedLimit       = 80;
		bool                  enableGlobalUploadSpeedLimit   = false;
		bool                  enableGlobalDownloadSpeedLimit = false;

		public static readonly string DefaultStunServer = "stun.ekiga.net";
				
		public abstract bool FirstRun {
			get;
		}

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
		
		[XmlElement("KeyData")]
		public System.Xml.XmlCDataSection KeyData {
			get {
				var doc = new System.Xml.XmlDocument();
				return doc.CreateCDataSection(this.keyData);
			}
			set {
				if (value == null || String.IsNullOrEmpty(value.Value))
					throw new ArgumentNullException("value");
				    
				this.keyData = value.Value;
				if (!KeyEncrypted)
					this.key = this.keyData;
			}
		}
		
		[XmlElement]
		public string SaltData {
			get {
				return Convert.ToBase64String(this.salt);
			}
			set {
				this.salt = Convert.FromBase64String(value);
			}
		}
		
		[XmlIgnore]
		public bool KeyEncrypted {
			get { return !this.keyData.StartsWith("<RSAKeyValue>"); }
		}
		
		public bool CheckKeyPassword (string password)
		{
			if (!KeyEncrypted)
				throw new InvalidOperationException("Key is not encrypted");
			if (String.IsNullOrEmpty(password))
				throw new ArgumentNullException("password");
			
			try {
				string d = Encryption.PasswordDecrypt(password, this.keyData, this.salt);
			 	return d.StartsWith("<RSAKeyValue>");
			} catch (Exception ex) {
				return false;
			}
		}
		
		public bool UnlockKey (string password)
		{
			if (!KeyEncrypted)
				throw new InvalidOperationException("Key is not encrypted");
			if (this.key != null)
				throw new InvalidOperationException("Key has already been unlocked");
			if (String.IsNullOrEmpty(password))
				throw new ArgumentNullException("password");
		
			try {
				string d = Encryption.PasswordDecrypt(password, this.keyData, this.salt);
				if (d.StartsWith("<RSAKeyValue>")) {
					this.key = d;
					return true;
				}
				return false;
			} catch (Exception ex) {
				return false;
			}
		}
		
		public void ChangeKeyPassword (string newPassword)
		{
			if (String.IsNullOrEmpty(this.keyData) || String.IsNullOrEmpty(this.key))
				throw new InvalidOperationException();
			
			if (!String.IsNullOrEmpty(newPassword)) {
				this.keyData = Encryption.PasswordEncrypt(newPassword, this.key, this.salt);
			} else {
				this.keyData = this.key;
			}
			
			if (!FirstRun)
				SaveSettings();
		}
		
		public RSAParameters EncryptionParameters 
		{
			get {
				if (this.key == null)
					throw new InvalidOperationException();
				var rsa = new RSACryptoServiceProvider();
				rsa.FromXmlString(this.key);
				return rsa.ExportParameters(true);
			}
		}
		
		public bool KeyUnlocked {
			get {
				if (!KeyEncrypted)
					throw new InvalidOperationException("Key is not encrypted");
				return (this.key != null);
			}
		}
		
		public bool HasKey {
			get {
				return !String.IsNullOrEmpty(this.keyData);
			}
		}
		
		public void SetKey (string key)
		{
			if (HasKey)
				throw new InvalidOperationException();
			if (!key.StartsWith("<RSAKeyValue"))
				throw new ArgumentException("Invalid key");
			if (String.IsNullOrEmpty(key))
				throw new ArgumentNullException("key");
			this.key = key;
			this.keyData = key;
			this.salt = new byte[32];
			RandomNumberGenerator.Create().GetBytes(this.salt);
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

		public int IPv6LinkLocalInterfaceIndex {
			get {
				return ipv6LinkLocalInterfaceIndex;
			}
			set {
				ipv6LinkLocalInterfaceIndex = value;
			}
		}
	
		public string[] SharedDirectories {
			get {
				return sharedDirectories;
			}
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				
				if (sharedDirectories != null && !sharedDirectories.SequenceEqual(value)) {
					lastShareScan = DateTime.MinValue;
				}
				
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

		public void SyncNetworkInfo ()
		{
			foreach (NetworkInfo info in networks) {
				foreach (Network network in Core.Networks) {
					if (network.NetworkID == info.NetworkID) {
						info.TrustedNodes.Clear();
						foreach (TrustedNodeInfo tni in network.TrustedNodes.Values) {
							info.TrustedNodes.Add(tni.NodeID, tni);
						}
						
						info.Memos.Clear();
						foreach (Memo memo in network.Memos) {
							if (Core.IsLocalNode(memo.Node)) {
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
	
		public void SyncNetworkInfoAndSave ()
		{
			SyncNetworkInfo();
			SaveSettings();
		}
	}
}
