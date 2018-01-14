//
// IRemoteDirectoryItem.cs
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2009 Meshwork Authors
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
