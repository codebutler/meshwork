//
// WhatsNewSearchItem.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007 FileFind.net (http://filefind.net)
//

using System;
using FileFind.Meshwork.Search;

namespace FileFind.Meshwork.GtkClient
{
	internal class WhatsNewSearchItem : FileSearchItem
	{
		Gdk.Pixbuf starPixbuf;

		private static WhatsNewSearchItem instance;
		public static WhatsNewSearchItem Instance {
			get {
				if (instance == null) {
					instance = new WhatsNewSearchItem();
					Core.FileSearchManager.AddFileSearch(instance.Search);
				}
				return instance;
			}
		}

		private WhatsNewSearchItem () : base (new WhatsNewFileSearch())
		{
			starPixbuf = Gui.LoadIcon(16, "star1");
		}

		public override Gdk.Pixbuf Icon {
			get {
				return starPixbuf;
			}
		}

		private class WhatsNewFileSearch : FileSearch
		{
			public WhatsNewFileSearch  ()
			{
				base.Name = "What's New?";
				// XXX: Set query and stuff.
			}
		}
	}
}
