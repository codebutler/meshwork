using System;
using System.Collections.Generic;
using System.IO;
using Meshwork.Backend.Core;
using Meshwork.Common.Serialization;

namespace Meshwork.Client.Console
{
	public class Settings : SettingsBase
	{
		public static Settings ReadSettings (string fileName)
		{
			if (File.Exists (fileName)) {
				var settingsText = File.ReadAllText (fileName);
				var result = (Settings)Json.Deserialize(settingsText, typeof(Settings));
				result.FileName = fileName;
				return result;
			} else {
				throw new Exception("Settings file not found: " + fileName);
			}
		}

	    string fileName;
		string dataPath;
		List<string> adminIDs = new List<string>();
		object moo = new object();
		string avatarFile;

	    public string AvatarFile {
			get {
				return avatarFile;
			}
			set {
				avatarFile = value;
			}
		}

		public List<string> AdminIDs {
			get {
				return adminIDs;
			}
			set {
				adminIDs = value;
			}
		}

		public override string ClientName {
			get {
				return "Meshwork Daemon client";
			}
		}

		public override string ClientVersion {
			get {
				return "0.1";
			}
		}

		public override string OperatingSystem {
			get {
				return "Linux";
			}
		}

		public string FileName {
			get {
				return fileName;
			}
			set {
				fileName = value;
			}
		}

	    [DontSerialize]
		public override string DataPath {
			get {
				return dataPath;
			}
	        set { dataPath = value; }
		}

		public override void SaveSettings ()
		{
			lock (moo) {
				File.WriteAllText(FileName, Json.Serialize(this));
			}
		}

	    [DontSerialize]
		public override bool FirstRun
	    {
	        get {
				return false; // FIXME: !!
			}
	        set
	        {
                // FIXME
	        }
	    }
	}
}
