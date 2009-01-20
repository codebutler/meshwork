//
// NewSearchItem.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007-2008 FileFind.net (http://filefind.net)
//

using System;

namespace FileFind.Meshwork.GtkClient
{
	internal class NewSearchItem : ISidebarItem
	{
		Gdk.Pixbuf icon;

		public NewSearchItem ()
		{
			icon = Gui.LoadIcon(24, Gtk.Stock.Find);
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
