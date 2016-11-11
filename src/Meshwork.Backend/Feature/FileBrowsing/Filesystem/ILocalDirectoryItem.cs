//
// ILocalDirectoryItem.cs
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2009 FileFind.net (http://filefind.net)
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
