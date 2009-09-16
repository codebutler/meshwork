//
// AbstractFile.cs
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2009 FileFind.net (http://filefind.net)
//

using System;

namespace FileFind.Meshwork.Filesystem
{
	public abstract class AbstractFile : IFile
	{
		public abstract string InfoHash {
			get;
			internal set;
		}
		
		public abstract string[] Pieces {
			get;
			internal set;
		}
		
		public abstract int PieceLength {
			get;
			internal set;
		}
		
		public abstract string FullPath {
			get;
		}
		
		public abstract long Size {
			get;
		}		

		public abstract string Name {
			get;
		}
		
		public abstract string Type {
			get;
		}
		
		public abstract IDirectory Parent {
			get;
		}
		
		public abstract void Reload();	
	}
}
