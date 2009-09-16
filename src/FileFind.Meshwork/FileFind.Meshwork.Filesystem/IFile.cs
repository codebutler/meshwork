//
// IFile.cs
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2009 FileFind.net (http://filefind.net)
//

using System;

namespace FileFind.Meshwork.Filesystem
{
	public interface IFile : IDirectoryItem
	{
		string InfoHash {
			get;
		}
		
		string[] Pieces {
			get;
		}
		
		int PieceLength {
			get;
		}
		
		void Reload();
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
