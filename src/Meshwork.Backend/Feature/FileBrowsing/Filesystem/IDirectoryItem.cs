//
// IDirectoryItem.cs: An item in a directory in the Meshwork virtual filesystem.
// 
// Author:
//   Eric Butler <eric@filefind.net>
//
//   (C) 2005-2006 FileFind.net (http://filefind.net/)
//

namespace Meshwork.Backend.Feature.FileBrowsing.Filesystem
{
	public interface IDirectoryItem
	{
		string Name {
			get; 
		}

		long Size {
			get;
		}

		string Type {
			get; 
		}

		IDirectory Parent {
			get;
		}

		string FullPath {
			get;
		}
	}
}
