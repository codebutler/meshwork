//
// IDirectory.cs
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2009 Meshwork Authors
//

namespace Meshwork.Backend.Feature.FileBrowsing.Filesystem
{
	public interface IDirectory : IDirectoryItem
	{
		IDirectory[] Directories 
		{
			get;
		}
		
		IFile[] Files
		{
			get;
		}
		
		int FileCount {
			get;
		}
		
		int DirectoryCount {
			get;
		}
			
		IDirectory GetSubdirectory (string name);
		IFile GetFile (string name);
	}
}
