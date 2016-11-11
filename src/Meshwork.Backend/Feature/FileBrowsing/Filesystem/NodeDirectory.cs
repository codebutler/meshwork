//
// NodeDirectory.cs
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2009 FileFind.net (http://filefind.net)
//

using Meshwork.Backend.Core;

namespace Meshwork.Backend.Feature.FileBrowsing.Filesystem
{
	public class NodeDirectory : RemoteDirectory
	{
		Node m_Node;
		
		internal NodeDirectory (Node node) : base (PathUtil.Join(node.Network.Directory.FullPath, node.NodeID))
		{
			m_Node = node;
		}
				
		public override Node Node {
			get {
				return m_Node;
			}
		}
	}
}
