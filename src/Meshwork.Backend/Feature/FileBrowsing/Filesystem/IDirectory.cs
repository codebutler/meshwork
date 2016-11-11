//
// IDirectory.cs
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2009 FileFind.net (http://filefind.net)
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
