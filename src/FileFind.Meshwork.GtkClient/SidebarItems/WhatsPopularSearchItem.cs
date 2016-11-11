//
// WhatsPopularSearch.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007 FileFind.net (http://filefind.net)
//

namespace FileFind.Meshwork.GtkClient.SidebarItems
{
	internal class WhatsPopularSearchItem : FileSearchItem
	{
		Gdk.Pixbuf starPixbuf;

		private static WhatsPopularSearchItem instance;
		public static WhatsPopularSearchItem Instance {
			get {
				if (instance == null) {
					instance = new WhatsPopularSearchItem();
					Core.FileSearchManager.AddFileSearch(instance.Search);
				}
				return instance;
			}
		}

		public WhatsPopularSearchItem () : base (new WhatsPopularFileSearch())
		{
			starPixbuf = Gui.LoadIcon(16, "star1");
		}

		public override Gdk.Pixbuf Icon {
			get {
				return starPixbuf;
			}
		}

		private class WhatsPopularFileSearch : FileSearch
		{
			public WhatsPopularFileSearch ()
			{
				base.Name = "What's Popular?";
				// XXX: Set query and stuff.
			}
		}
	}
}
