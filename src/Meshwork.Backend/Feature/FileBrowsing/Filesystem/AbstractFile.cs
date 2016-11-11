//
// AbstractFile.cs
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2009 FileFind.net (http://filefind.net)
//

using System.Collections.Generic;

namespace Meshwork.Backend.Feature.FileBrowsing.Filesystem
{
	public abstract class AbstractFile : IFile
	{
		public abstract string InfoHash {
			get;
		}
		
		public abstract string SHA1 {
			get;
		}
		
		public abstract string[] Pieces {
			get;
		}
		
		public abstract int PieceLength {
			get;
		}
		
		public virtual string FullPath {
			get { return PathUtil.Join(Parent.FullPath, Name); }
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
		
		public abstract Dictionary<string, string> Metadata {
			get;
		}
		
		public abstract IDirectory Parent {
			get;
		}
	}
}
