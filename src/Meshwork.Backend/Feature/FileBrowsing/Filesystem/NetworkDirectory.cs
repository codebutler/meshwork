//
// NetworkDirectory.cs
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2009 FileFind.net (http://filefind.net)
//

using System.Collections.Generic;
using Meshwork.Backend.Core;

namespace Meshwork.Backend.Feature.FileBrowsing.Filesystem
{
	public class NetworkDirectory : AbstractDirectory
	{
		Network m_Network;

		public NetworkDirectory (Network network)
		{
			m_Network = network;
		}

		public Network Network {
			get { return m_Network; }
		}

		public override IDirectory[] Directories {
			get {
				var directories = new List<NodeDirectory>();
				foreach (var node in m_Network.Nodes.Values) {
					if (node != m_Network.LocalNode)
						directories.Add(node.Directory);				
				}
				return directories.ToArray();
			}
		}

		public override int DirectoryCount {
			get {
				 return m_Network.Nodes.Count - 1;
			}
		}

		public override int FileCount {
			get { return 0; }
		}

		public override IFile[] Files {
			get { return new IFile[0]; }
		}

		public override string Name {
			get { return m_Network.NetworkID; }
		}

		public override IDirectory Parent {
			get { return Network.Core.FileSystem.RootDirectory; }
		}		
	}
}