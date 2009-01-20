//
// FileSearchGroup.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007 FileFind.net (http://filefind.net)
//

using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Collections;

namespace FileFind.Meshwork.Search
{
	public class FileSearchGroup
	{
		string name;
		List<FileSearch> searches;
		List<FileSearchGroup> groups;

		public FileSearchGroup ()
		{
			searches = new List<FileSearch>();
			groups = new List<FileSearchGroup>();
		}

		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}

		public List<FileSearchGroup> Groups {
			get {
				return groups;
			}
			set {
				groups = value;
			}
		}

		public List<FileSearch> Searches {
			get {
				return searches;
			}
			set {
				searches = value;
			}
		}
	}
}
