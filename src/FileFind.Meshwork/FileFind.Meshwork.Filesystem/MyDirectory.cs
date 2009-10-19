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
		public MyDirectory () : base(0, 0, "local", null)
		{
		
		}
		
		public override IDirectory Parent {
			get {
				return Core.FileSystem.RootDirectory;
			}
		}
		
		public new void InvalidateCache ()
		{
			base.InvalidateCache();
		}
	}
}
