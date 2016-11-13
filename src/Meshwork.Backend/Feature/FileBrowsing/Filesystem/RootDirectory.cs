//
// RootDirectory.cs
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2009 FileFind.net (http://filefind.net)
//

using System;

namespace Meshwork.Backend.Feature.FileBrowsing.Filesystem
{
	public class RootDirectory : AbstractDirectory
	{
	    private readonly Core.Core core;

		MyDirectory m_MyDirectory;

	    internal RootDirectory (Core.Core core)
		{
		    this.core = core;
			m_MyDirectory = new MyDirectory(core.FileSystem);
		}

		public MyDirectory MyDirectory {
			get {
				return m_MyDirectory;
			}
		}
		
		public override string FullPath {
			get {
				return "/"; 
			}
		}

		public override IDirectory[] Directories {
			get {
				IDirectory[] directories = new IDirectory[DirectoryCount];
				directories[0] = MyDirectory;
				for (int x = 1; x < directories.Length; x++) {
					directories[x] = core.Networks[x - 1].Directory;
				}
				return directories;
			}
		}
		
		public override IFile[] Files {
			get {
				return new IFile[0];
			}
		}
		
		public override int FileCount {
			get {
				return 0;
			}
		}
		
		public override int DirectoryCount {
			get {
				return core.Networks.Length + 1;
			}
		}

		public override string Name {
			get {
				return "/";
			}
		}
		
		public override IDirectory Parent {
			get {
				return null;
			}
		}		
	}
}
