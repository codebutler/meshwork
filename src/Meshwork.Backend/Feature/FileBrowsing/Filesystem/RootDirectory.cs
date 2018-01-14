//
// RootDirectory.cs
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2009 Meshwork Authors
//

namespace Meshwork.Backend.Feature.FileBrowsing.Filesystem
{
	public class RootDirectory : AbstractDirectory
	{
	    private readonly Core.Core core; // FIXME: Remove this
	    private readonly FileSystemProvider fileSystem;

		private readonly MyDirectory m_MyDirectory;

	    internal RootDirectory (Core.Core core, FileSystemProvider fileSystem)
		{
		    this.core = core;
			m_MyDirectory = new MyDirectory(fileSystem);
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
				var directories = new IDirectory[DirectoryCount];
				directories[0] = MyDirectory;
				for (var x = 1; x < directories.Length; x++) {
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
