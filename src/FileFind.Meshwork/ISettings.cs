//
// ISettings: Interface for frontend settings classes to implement
//
// Author
//   Eric Butler <eric@filefind.net>
//
// (C) 2005-2008 FileFind.net (http://filefind.net)
//

using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using FileFind.Meshwork.Search;
using FileFind.Meshwork.Destination;

namespace FileFind.Meshwork
{
	public interface ISettings
	{
		string CompletedDownloadDir {
			get;
		}
		
		string IncompleteDownloadDir {
			get;
		}
		
		RSAParameters EncryptionParameters {
			get;
		}

		List<NetworkInfo> Networks {
			get;
		}

		List<DestinationInfo> SavedDestinationInfos {
			get;
		}

		string NickName {
			get;
		}

		string RealName {
			get;
		}

		string Email {
			get;
		}

		string ClientName {
			get;
		}

		string ClientVersion {
			get;
		}

		string OperatingSystem {
			get;
		}

		string DataPath {
			get;
		}

		List<string> SharedDirectories {
			get;
		}

		DateTime LastShareScan {
			get;
			set;
		}

		List<string> Plugins {
			get;
		}

		int AutoConnectCount {
			get;
		}

		FileSearchGroup SavedSearches {
			get;
		}

		int TcpListenPort {
			get;
		}

		bool TcpListenPortOpen {
			get;
		}

		string StunServer {
			get;
		}

		bool DetectInternetIPOnStart {
			get;
		}

		int IPv6LinkLocalInterfaceIndex {
			get;
		}

		int GlobalDownloadSpeedLimit {
			get;
			set;
		}

		int GlobalUploadSpeedLimit {
			get;
			set;
		}

		bool EnableGlobalDownloadSpeedLimit {
			get;
			set;
		}

		bool EnableGlobalUploadSpeedLimit {
			get;
			set;
		}

		void SaveSettings ();
		void SyncNetworkInfo ();
		void SyncNetworkInfoAndSave ();
	}
}
