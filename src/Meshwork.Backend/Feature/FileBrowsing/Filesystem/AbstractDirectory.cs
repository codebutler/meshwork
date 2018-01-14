//
// AbstractDirectory.cs
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2009 Meshwork Authors
//

namespace Meshwork.Backend.Feature.FileBrowsing.Filesystem
{
	public abstract class AbstractDirectory : IDirectory
	{
		public IFile GetFile (string name)
		{
			foreach (var file in Files) {
				if (file.Name == name) {
					return file;
				}
			}
			return null;
		}

		public bool HasFile (string name)
		{
			foreach (var file in Files) {
				if (file.Name == name) {
					return true;
				}
			}
			return false;
		}			

		public IDirectory GetSubdirectory (string name)
		{
			foreach (var subdir in Directories) {
				if (subdir.Name == name) {
					return subdir;
				}
			}
			return null;
		}

		public abstract string Name {
			get;
		}
		
		public virtual long Size {
			get {
				return (FileCount + DirectoryCount);
			}
		}
				
		public virtual string Type {
			get {
				return "Directory";
			}
		}
		
		public abstract IDirectory Parent {
			get;
		}
		
		public virtual string FullPath {
			get { return PathUtil.Join(Parent.FullPath, Name); }
		}

		public abstract IDirectory[] Directories {
			get;
		}
		
		public abstract IFile[] Files {
			get;
		}
		
		public abstract int FileCount {
			get;
		}
		
		public abstract int DirectoryCount {
			get;
		}
	}
}
