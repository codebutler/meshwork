using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using System.Xml.Serialization;

using FileFind;
using FileFind.Meshwork;
using FileFind.Meshwork.Collections;
using FileFind.Meshwork.Exceptions;
using FileFind.Meshwork.Destination;
using FileFind.Serialization;

namespace FileFind.Meshwork.DaemonClient
{
	[XmlInclude (typeof(NetworkInfo))]
	public class Settings : SettingsBase
	{
		public static Settings ReadSettings (string fileName)
		{
			if (File.Exists (fileName) == true) {
				string settingsText = File.ReadAllText (fileName);
				Settings result = (Settings)Xml.DeSerialize (settingsText, typeof(Settings));
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

		public override string DataPath {
			get {
				return dataPath;
			}
			set {
				dataPath = value;
			}
		} 

		public override void SaveSettings ()
		{
			lock (moo) {
				File.WriteAllText(FileName, Xml.Serialize(this));
			}
		}

		public override bool FirstRun {
			get {
				return false; // FIXME: !!
			}
		}
	}
}
