//
// RootDirectory.cs
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2009 FileFind.net (http://filefind.net)
//

using System;

namespace FileFind.Meshwork.Filesystem
{
	public class RootDirectory : AbstractDirectory
	{
		static RootDirectory s_Instance;
		
		MyDirectory m_MyDirectory;
		
		private RootDirectory ()
		{
			if (s_Instance != null)
				throw new Exception("Only one instance is allowed");
			
			s_Instance = this;
			
			m_MyDirectory = new MyDirectory();
		}
		
		public static RootDirectory Instance {
			get {
				if (s_Instance == null) {
					s_Instance = new RootDirectory();
				}
				return s_Instance;
			}
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
					directories[x] = Core.Networks[x - 1].Directory;
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
				return Core.Networks.Length + 1;
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
