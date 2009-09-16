//
// ILocalDirectoryItem.cs
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2009 FileFind.net (http://filefind.net)
//

using System;

namespace FileFind.Meshwork.Filesystem
{
	public interface ILocalDirectoryItem : IDirectoryItem
	{
		int Id {
			get;
		}
		
		string LocalPath {
			get;
		}
		
		void Delete();
	}
}
