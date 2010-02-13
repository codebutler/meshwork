//
// MyDirectory.cs
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2009 FileFind.net (http://filefind.net)
//

using System;

namespace FileFind.Meshwork.Filesystem
{
	public class MyDirectory : LocalDirectory
	{
		public MyDirectory () : base (0, 0, "local", null, "/local")
		{
		}
		
		public override int FileCount {
			get {
				return (int)LocalFile.CountByParentId(0);
			}
		}

		public override int DirectoryCount {
			get {
				return (int)LocalDirectory.CountByParentId(0);
			}
		
		}
		
		public override IDirectory Parent {
			get {
				return Core.FileSystem.RootDirectory;
			}
		}
		
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
