//
// IFile.cs
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2009 Meshwork Authors
//

using System.Collections.Generic;

namespace Meshwork.Backend.Feature.FileBrowsing.Filesystem
{
	public interface IFile : IDirectoryItem
	{
		string InfoHash {
			get;
		}
		
		string SHA1 {
			get;
		}
		
		string[] Pieces {
			get;
		}
		
		int PieceLength {
			get;
		}
		
		Dictionary<string, string> Metadata {
			get;
		}
	}
	
	public enum FileType
	{
		Audio,
		Video,
		Image,
		Document,
		Other
	}
}
