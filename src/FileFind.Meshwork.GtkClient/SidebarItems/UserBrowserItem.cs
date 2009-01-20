//
// UserBrowserItem.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007 FileFind.net (http://filefind.net)
//

using System;

namespace FileFind.Meshwork.GtkClient
{
	internal class UserBrowserItem : ISidebarItem
	{
		Gdk.Pixbuf icon;

		public UserBrowserItem ()
		{
			icon = Gui.LoadIcon(24, "stock_person");
		}

		public string Name {
			get {
				return "File Browser";
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
				return UserBrowserPage.Instance;
			}
		}

		public void Destroy ()
		{
			throw new InvalidOperationException("This should never be destroyed.");
		}
	}
}
