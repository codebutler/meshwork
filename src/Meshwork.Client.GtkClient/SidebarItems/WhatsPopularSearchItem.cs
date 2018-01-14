//
// WhatsPopularSearch.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2007 Meshwork Authors
//

using Meshwork.Backend.Core;
using Meshwork.Backend.Feature.FileSearch;

namespace Meshwork.Client.GtkClient.SidebarItems
{
	internal class WhatsPopularSearchItem : FileSearchItem
	{
		Gdk.Pixbuf starPixbuf;

		private static WhatsPopularSearchItem instance;
		public static WhatsPopularSearchItem Instance {
			get {
				if (instance == null) {
					instance = new WhatsPopularSearchItem();
					Runtime.Core.FileSearchManager.AddFileSearch(instance.Search);
				}
				return instance;
			}
		}

		public WhatsPopularSearchItem () : base (new WhatsPopularFileSearch(Runtime.Core))
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
			public WhatsPopularFileSearch (Core core) : base(core)
			{
				base.Name = "What's Popular?";
				// XXX: Set query and stuff.
			}
		}
	}
}
