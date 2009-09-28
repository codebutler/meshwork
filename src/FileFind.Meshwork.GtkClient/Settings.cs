//
// Settings.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006-2008 FileFind.net (http://filefind.net)
//

using System;
using System.Linq;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Xml.Serialization;
using System.IO;

using FileFind;
using FileFind.Meshwork;
using FileFind.Meshwork.Collections;
using FileFind.Meshwork.Exceptions;
using FileFind.Meshwork.Search;
using FileFind.Meshwork.Destination;
using FileFind.Serialization;

namespace FileFind.Meshwork.GtkClient
{
	public class Settings : SettingsBase
	{
		bool firstRun = false;

		// Overrided config path?
		static string configPath = null;

		public static void OverrideConfigPath(string newPath) 
		{
			configPath = newPath;
		}

		public static string ConfigurationDirectory {
			get {
				if (configPath == null) {

					string confDir = null;

					if (Environment.OSVersion.Platform == PlatformID.Unix) {
						confDir = Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".config");
					} else {
						// This is only for windows actually
						confDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
					}

					confDir = Path.Combine(confDir, "FileFind.net");
					confDir = Path.Combine(confDir, "Meshwork");

					if (Directory.Exists(confDir) == false) {
						Directory.CreateDirectory(confDir);
					}
					
					return confDir;
				} else {
					return configPath;
				}
			}
		}

		public static Settings ReadSettings ()
		{
			if (File.Exists(FileName) == true) {
				string settingsText = FileFind.Common.ReadAllText (FileName);
				Settings result = (Settings)Xml.DeSerialize (settingsText, typeof(Settings));
				
				foreach (var networkInfo in result.Networks) {
					foreach (var key in networkInfo.TrustedNodes.Keys.ToArray()) {
						var info = networkInfo.TrustedNodes[key];
						if (String.IsNullOrEmpty(info.NodeID)) {
							LoggingService.LogWarning("Ignored TrustedNodeInfo with bad public key.");
							networkInfo.TrustedNodes.Remove(key);
						}
					}
				}
				
				return result;
			} else {
				return null;
			}
		}

		[XmlIgnore]
		public bool FirstRun { 
			get {
				return firstRun;
			}
			set {
				firstRun = value;
			}
		}

		private static string FileName {
			get {
				return Path.Combine(ConfigurationDirectory, "meshwork.conf");
			}
		}
	
		public Point WindowPosition = new Point();
		public Size WindowSize = new Size();
		
		
		public DateTime LastUsed;

		public override string ClientName {
			get {
				return "Title";
			}
		}

		public override string ClientVersion {
			get {
				return "Version";
			}
		}
	
		public override string OperatingSystem {
			get {
				return Environment.OSVersion.Platform.ToString() + " " + Environment.OSVersion.Version.ToString();
			}
		}
		
		public bool FlashWindows = true;

		public bool ChatPrependTitlebar = true;
		public bool ChatShowTimestamp = true;
			
		public bool MessageWindowShowSignature = true;
		public Size MessageWindowSize = Size.Empty;
		public int MessageInputFieldHeight = 0;

		public ArrayList RecentConnections = new ArrayList();
		
		public bool UseGroupsInMap = true;
		public bool ShowAllGroupsInMap = false;
		public bool ShowStatusBar = true;
		public bool ShowToolbar = true;
	
		public Size EditMemoWindowSize = Size.Empty;
		public Size ViewMemoWindowSize = Size.Empty;
		
		public bool StartInTray = false;
		public bool ShareHiddenFiles = false;

		[XmlIgnore]
		public override string DataPath {
			get {
				return Settings.ConfigurationDirectory;
			}
			set {
				//throw new InvalidOperationException();
			}
		}

		object saveLock = new object();
		
		public override void SaveSettings ()
		{
			if (firstRun) {
				throw new InvalidOperationException("Cannot save if FirstRun is true");
			}
			lock (saveLock) {
				FileFind.Common.WriteToFile(FileName, Xml.Serialize(this));
			}
		}
	}
}
