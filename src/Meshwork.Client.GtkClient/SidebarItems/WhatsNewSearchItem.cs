//
// WhatsNewSearchItem.cs:
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
	internal class WhatsNewSearchItem : FileSearchItem
	{
		Gdk.Pixbuf starPixbuf;

		private static WhatsNewSearchItem instance;
		public static WhatsNewSearchItem Instance {
			get {
				if (instance == null) {
					instance = new WhatsNewSearchItem();
					Runtime.Core.FileSearchManager.AddFileSearch(instance.Search);
				}
				return instance;
			}
		}

		private WhatsNewSearchItem () : base (new WhatsNewFileSearch(Runtime.Core))
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
			public WhatsNewFileSearch (Core core) : base(core)
			{
				base.Name = "What's New?";
				// XXX: Set query and stuff.
			}
		}
	}
}
