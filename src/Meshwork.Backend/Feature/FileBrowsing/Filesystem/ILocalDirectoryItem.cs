//
// ILocalDirectoryItem.cs
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2009 Meshwork Authors
//

namespace Meshwork.Backend.Feature.FileBrowsing.Filesystem
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
