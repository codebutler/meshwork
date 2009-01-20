//
// ISidebarItem.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007 FileFind.net (http://filefind.net)
//

using System;

namespace FileFind.Meshwork.GtkClient
{
	internal class NetworkOverviewItem : ISidebarItem
	{
		Gdk.Pixbuf icon;

		public NetworkOverviewItem ()
		{
			icon = Gui.LoadIcon(24, "stock_internet");
		}

		public string Name {
			get {
				return "Network Overview";
			}
		}

		public int Count {
			get {
				return -1;
			}
		}

		public Gdk.Pixbuf Icon {
			get {
				return icon;
			}
		}

		public Gtk.Widget PageWidget {
			get {
				return NetworkOverviewPage.Instance;
			}
		}

		public void Destroy ()
		{
			throw new InvalidOperationException("This should never be destroyed.");
		}
	}
}
