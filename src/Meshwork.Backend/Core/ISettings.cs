//
// ISettings: Interface for frontend settings classes to implement
//
// Author
//   Eric Butler <eric@codebutler.com>
//
// (C) 2005-2008 Meshwork Authors
//

using System;
using System.Collections.Generic;
using Meshwork.Backend.Core.Destination;
using Meshwork.Backend.Feature.FileSearch;

namespace Meshwork.Backend.Core
{
	public interface ISettings
	{
		bool FirstRun {
			get;
		}
		
		string CompletedDownloadDir {
			get;
		}
		
		string IncompleteDownloadDir {
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

		string[] SharedDirectories {
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

	    string PrivateKey
	    {
	        get;
	        set;
	    }

		void SaveSettings ();
		void SyncNetworkInfo (Core core);
		void SyncNetworkInfoAndSave (Core core);
	}
}
