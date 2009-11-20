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
		
		string m_FullPath;

		RemoteDirectory[] m_SubDirectories = new RemoteDirectory[0];
		RemoteFile[]      m_Files          = new RemoteFile[0];

		RemoteDirectoryState m_State = RemoteDirectoryState.ContentsUnrequested;
		
		internal RemoteDirectory (string fullPath)
		{
			m_FullPath = fullPath;
		}
		
		public virtual Node Node {
			get { 
				if (m_Node == null)
					m_Node = PathUtil.GetNode(m_FullPath);
				return m_Node; 
			}
		}
		
		public Network Network {
			get { return this.Node.Network; }
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
				return PathUtil.GetBaseName(m_FullPath);
			}
		}
		
		public override string FullPath {
			get {
				return m_FullPath;
			}
		}
		
		public override IDirectory Parent {
			get {
				if (m_Parent == null)
					m_Parent = Core.FileSystem.GetDirectory(PathUtil.GetParentPath(this.FullPath));
				return m_Parent;
			}
		}

		internal void UpdateFromInfo (SharedDirectoryInfo info)
		{
			var newDirectories = new RemoteDirectory[info.Directories.Length];
			for (int x = 0; x < info.Directories.Length; x++)
			{
				newDirectories[x] = Core.FileSystem.GetOrCreateRemoteDirectory(PathUtil.Join(m_FullPath, info.Directories[x]));
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
	}

	public enum RemoteDirectoryState
	{
		ContentsUnrequested,
		ContentsRequested,
		ContentsReceived
	}
}
