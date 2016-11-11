//
// NewSearchItem.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007-2008 FileFind.net (http://filefind.net)
//

using System;
using FileFind.Meshwork.GtkClient.Pages;

namespace FileFind.Meshwork.GtkClient.SidebarItems
{
	internal class NewSearchItem : ISidebarItem
	{
		Gdk.Pixbuf icon;

		public NewSearchItem ()
		{
			icon = Gui.LoadIcon(24, "system-search");
		}

		public string Name {
			get {
				return "New Search";
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
				return NewSearchPage.Instance;
			}
		}

		public void Destroy ()
		{
			throw new InvalidOperationException("This should never be destroyed.");
		}
	}

}
