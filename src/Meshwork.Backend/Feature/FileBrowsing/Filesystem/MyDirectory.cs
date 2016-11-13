//
// MyDirectory.cs
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2009 FileFind.net (http://filefind.net)
//

namespace Meshwork.Backend.Feature.FileBrowsing.Filesystem
{
	public class MyDirectory : LocalDirectory
	{
		public MyDirectory (FileSystemProvider fileSystem) : base (fileSystem, 0, 0, "local", null, "/local")
		{
		}
		
		public override int FileCount {
			get {
				return (int)LocalFile.CountByParentId(fileSystem, 0);
			}
		}

		public override int DirectoryCount {
			get {
				return (int)CountByParentId(fileSystem, 0);
			}
		
		}
		
		public override IDirectory Parent => fileSystem.RootDirectory;

	    public new void InvalidateCache ()
		{
			/* Don't need to do anything */
		}
		
		public new void Delete ()
		{
			/* Don't allow this */
		}
	}
}
