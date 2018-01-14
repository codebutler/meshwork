//
// NodeDirectory.cs
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2009 Meshwork Authors
//

using Meshwork.Backend.Core;

namespace Meshwork.Backend.Feature.FileBrowsing.Filesystem
{
	public class NodeDirectory : RemoteDirectory
	{
		Node m_Node;
		
		internal NodeDirectory (Core.Core core, Node node)
		    : base (core, PathUtil.Join(node.Network.Directory.FullPath, node.NodeID))
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
