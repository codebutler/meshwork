//
// NodeDirectory.cs
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2009 FileFind.net (http://filefind.net)
//

using System;

namespace FileFind.Meshwork.Filesystem
{
	public class NodeDirectory : RemoteDirectory
	{
		public NodeDirectory (Node node) : base(node.Network.Directory, node.NodeID, node)
		{
			
		}
	}
}
