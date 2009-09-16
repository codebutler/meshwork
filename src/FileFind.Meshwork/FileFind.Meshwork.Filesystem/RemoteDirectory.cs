//
// RemoteDirectory.cs
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2009 FileFind.net (http://filefind.net)
//

using System;

namespace FileFind.Meshwork.Filesystem
{
	public class RemoteDirectory : AbstractDirectory, IRemoteDirectoryItem
	{
		IDirectory m_Parent;
		Node m_Node;

		public RemoteDirectory (IDirectory parent, Node node)
		{
			m_Parent = parent;
			m_Node = node;
		}

		public Node Node {
			get { return m_Node; }
		}
		
		public Network Network {
			get { return m_Node.Network; }
		}

		public bool Requested {
			get { return false; }
		}

		public override IDirectory[] Directories {
			get {
				return new IDirectory[0];
			}
		}

		public override int DirectoryCount {
			get {
				return 0;
			}
		}

		public override int FileCount {
			get {
				return 0;
			}
		}

		public override IFile[] Files {
			get {
				return new IFile[0];
			}
		}

		public override string Name {
			get {
				return null;
			}
		}
		
		public override IDirectory Parent {
			get {
				return m_Parent;
			}
		}
	}
}
