//
// Settings.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006-2008 FileFind.net (http://filefind.net)
//

using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Linq;
using Meshwork.Backend.Core;
using Meshwork.Common.Serialization;

namespace Meshwork.Client.GtkClient
{
	public class Settings : SettingsBase
	{
	    // Overrided config path?
		static string configPath;

	    private static string FileName => Path.Combine(ConfigurationDirectory, "meshwork.conf");

	    public static void OverrideConfigPath(string newPath)
		{
			configPath = newPath;
			
			if (Directory.Exists(configPath) == false) {
				Directory.CreateDirectory(configPath);
			}
		}

		public static string ConfigurationDirectory {
			get
			{
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
				}
			    return configPath;
			}
		}

		public static Settings ReadSettings ()
		{
		    if (File.Exists(FileName)) {
				string settingsText = File.ReadAllText(FileName);
				Settings result = (Settings)Json.Deserialize(settingsText, typeof(Settings));
				
				foreach (var networkInfo in result.Networks) {
					foreach (var key in networkInfo.TrustedNodes.Keys.ToArray()) {
						var info = networkInfo.TrustedNodes[key];
						if (string.IsNullOrEmpty(info.NodeId)) {
							LoggingService.LogWarning("Ignored TrustedNodeInfo with bad public key.");
							networkInfo.TrustedNodes.Remove(key);
						}
					}
				}
				
				return result;
			}
		    return null;
		}

	    [DontSerialize]
		public override bool FirstRun { get; set; } = false;

	    public Point WindowPosition = new Point();
		public Size WindowSize = new Size();

		public DateTime LastUsed;

		public override string ClientName { get; } = "Title";

	    public override string ClientVersion { get; } = "Version";

	    public override string OperatingSystem { get; } =
	        $"{Environment.OSVersion.Platform} {Environment.OSVersion.Version}";

		public ArrayList RecentConnections = new ArrayList();
		
		public bool ShowStatusBar = true;
		public bool ShowToolbar = true;
	
		public bool StartInTray = false;
		public bool ShareHiddenFiles = false;

	    [DontSerialize]
	    public override string DataPath
	    {
	        get { return ConfigurationDirectory; }
	        set { }
	    }

	    object saveLock = new object();
		
		public override void SaveSettings ()
		{
			if (FirstRun) {
				throw new InvalidOperationException("Cannot save if FirstRun is true");
			}
			lock (saveLock) {
				File.WriteAllText(FileName, Json.Serialize(this));
			}
		}
	}
}
