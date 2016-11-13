//
// RemoteDirectory.cs
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2009 FileFind.net (http://filefind.net)
//

using System;
using Meshwork.Backend.Core;
using Meshwork.Backend.Core.Protocol;
using Meshwork.Common;

namespace Meshwork.Backend.Feature.FileBrowsing.Filesystem
{
	public class RemoteDirectory : AbstractDirectory, IRemoteDirectoryItem
	{
	    private readonly Core.Core core;

		IDirectory m_Parent;
		Node m_Node;
		
		string m_FullPath;

		RemoteDirectory[] m_SubDirectories = new RemoteDirectory[0];
		RemoteFile[]      m_Files          = new RemoteFile[0];

		RemoteDirectoryState m_State = RemoteDirectoryState.ContentsUnrequested;
		
		internal RemoteDirectory (Core.Core core, string fullPath)
		{
		    this.core = core;
			m_FullPath = fullPath;
		}
		
		public virtual Node Node {
			get { 
				if (m_Node == null)
					m_Node = PathUtil.GetNode(core, m_FullPath);
				return m_Node; 
			}
		}
		
		public Network Network {
			get { return this.Node.Network; }
		}

		public string RemoteFullPath {
			get {
				return "/" + string.Join("/", this.FullPath.Split('/').Slice(3));
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
					m_Parent = core.FileSystem.GetDirectory(PathUtil.GetParentPath(this.FullPath));
				return m_Parent;
			}
		}

		internal void UpdateFromInfo (SharedDirectoryInfo info)
		{
			var newDirectories = new RemoteDirectory[info.Directories.Length];
			for (int x = 0; x < info.Directories.Length; x++) {
				RemoteDirectory dir = (RemoteDirectory) GetSubdirectory(info.Directories[x]);
				if (dir == null)
					dir = new RemoteDirectory(core, PathUtil.Join(m_FullPath, info.Directories[x]));
				newDirectories[x] = dir;
			}

			var newFiles = new RemoteFile[info.Files.Length];
			for (int x = 0; x < info.Files.Length; x++) {
				RemoteFile file = (RemoteFile) GetFile(info.Files[x].Name);
				if (file == null)
					file = new RemoteFile(this, info.Files[x]);
				else
					file.UpdateFromInfo(info.Files[x]);
				newFiles[x] = file;
			}
			
			m_SubDirectories = newDirectories;
			m_Files          = newFiles;
			
			m_State = RemoteDirectoryState.ContentsReceived;
		}
		
		internal RemoteDirectory CreateSubdirectory (string name)
		{
			var dir = new RemoteDirectory(core, PathUtil.Join(m_FullPath, name));
			
			var newDirectories = new RemoteDirectory[m_SubDirectories.Length + 1];
			Array.Copy(m_SubDirectories, newDirectories, m_SubDirectories.Length);
			newDirectories[newDirectories.Length - 1] = dir;
			
			m_SubDirectories = newDirectories;
			
			return dir;
		}
		
		internal RemoteFile CreateFile (SharedFileListing listing)
		{
			var file = new RemoteFile(this, listing);
			
			var newFiles = new RemoteFile[m_Files.Length + 1];
			Array.Copy(m_Files, newFiles, m_Files.Length);
			newFiles[newFiles.Length - 1] = file;
			
			m_Files = newFiles;
			
			return file;
		}
	}

	public enum RemoteDirectoryState
	{
		ContentsUnrequested,
		ContentsRequested,
		ContentsReceived
	}
}
