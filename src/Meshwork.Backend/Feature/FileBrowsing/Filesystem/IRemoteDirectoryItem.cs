//
// IRemoteDirectoryItem.cs
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2009 FileFind.net (http://filefind.net)
//

using Meshwork.Backend.Core;

namespace Meshwork.Backend.Feature.FileBrowsing.Filesystem
{
	public interface IRemoteDirectoryItem
	{
		Network Network {
			get;
		}
		
		Node Node {
			get;
		}
		
		string RemoteFullPath {
			get;
		}
	}
}
