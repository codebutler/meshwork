//
// IDirectoryItem.cs: An item in a directory in the Meshwork virtual filesystem.
// 
// Author:
//   Eric Butler <eric@codebutler.com>
//
//   (C) 2005-2006 Meshwork Authors
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
