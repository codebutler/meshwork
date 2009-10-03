//
// RemoteDirectory.cs
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2009 FileFind.net (http://filefind.net)
//

using System;
using FileFind.Meshwork.Protocol;

namespace FileFind.Meshwork.Filesystem
{
	public class RemoteDirectory : AbstractDirectory, IRemoteDirectoryItem
	{
		IDirectory m_Parent;
		Node m_Node;
		
		string m_Name;

		RemoteDirectory[] m_SubDirectories;
		RemoteFile[]      m_Files;

		RemoteDirectoryState m_State;

		public RemoteDirectory (IDirectory parent, string name, Node node)
		{
			m_Parent = parent;
			m_Name = name;
			m_Node = node;

			UpdateFromCache();
		}

		public Node Node {
			get { return m_Node; }
		}
		
		public Network Network {
			get { return m_Node.Network; }
		}

		public string RemoteFullPath {
			get {
				return "/" + String.Join("/", this.FullPath.Split('/').Slice(3));
			}
		}
		
		public RemoteDirectoryState State {
			get { return m_State; }
		}

		public override IDirectory[] Directories {
			get {
				return m_SubDirectories;
			}
		}

		public override int DirectoryCount {
			get {
				return m_SubDirectories.Length;
			}
		}

		public override int FileCount {
			get {
				return m_Files.Length;
			}
		}

		public override IFile[] Files {
			get {
				return m_Files;
			}
		}

		public override string Name {
			get {
				return m_Name;
			}
		}
		
		public override IDirectory Parent {
			get {
				return m_Parent;
			}
		}

		public void Update()
		{
			m_State = RemoteDirectoryState.ContentsRequested;
			m_Node.Network.RequestDirectoryListing(this);
		}

		// FIXME: Get rid of this once cache works
		internal void UpdateFromInfo (SharedDirectoryInfo info)
		{
			var newDirectories = new RemoteDirectory[info.Directories.Length];
			for (int x = 0; x < info.Directories.Length; x++)
			{
				newDirectories[x] = new RemoteDirectory(this, info.Directories[x], m_Node);
			}
			m_SubDirectories = newDirectories;

			var newFiles = new RemoteFile[info.Files.Length];
			for (int x = 0; x < info.Files.Length; x++)
			{
				newFiles[x] = new RemoteFile(this, info.Files[x]);
			}
			m_Files = newFiles;

			m_State = RemoteDirectoryState.ContentsReceived;
		}

		internal void UpdateFromCache()
		{
			// FIXME: Check for cache

			m_SubDirectories = new RemoteDirectory[0];
			m_Files = new RemoteFile[0];
			m_State = RemoteDirectoryState.ContentsUnrequested;
		}
	}

	public enum RemoteDirectoryState
	{
		ContentsUnrequested,
		ContentsRequested,
		ContentsReceived
	}
}
