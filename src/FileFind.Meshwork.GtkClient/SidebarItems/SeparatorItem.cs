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
	internal class SeparatorItem : ISidebarItem
	{
		public string Name {
			get {
				return String.Empty;
			}
		}

		public int Count {
			get {
				return -1;
			}
		}

		public Gdk.Pixbuf Icon {
			get {
				return null;
			}
		}

		public Gtk.Widget PageWidget {
			get {
				return null;
			}
		}

		public void Destroy ()
		{
		}
	}
}
