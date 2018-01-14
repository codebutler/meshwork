//
// ISidebarItem.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2007 Meshwork Authors
//

namespace Meshwork.Client.GtkClient.SidebarItems
{
	internal class SeparatorItem : ISidebarItem
	{
		public string Name {
			get {
				return string.Empty;
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
