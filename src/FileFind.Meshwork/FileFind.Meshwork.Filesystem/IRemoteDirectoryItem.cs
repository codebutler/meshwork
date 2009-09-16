//
// IRemoteDirectoryItem.cs
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2009 FileFind.net (http://filefind.net)
//

using System;

namespace FileFind.Meshwork
{
	public interface IRemoteDirectoryItem
	{
		Network Network {
			get;
		}
		
		Node Node {
			get;
		}
	}
}
