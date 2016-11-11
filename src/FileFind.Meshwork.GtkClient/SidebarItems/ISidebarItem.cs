//
// ISidebarItem.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007 FileFind.net (http://filefind.net)
//

using Gdk;
using Gtk;

namespace FileFind.Meshwork.GtkClient.SidebarItems
{
	public interface ISidebarItem
	{
		string Name {
			get;
		}

		int Count {
			get;
		}

		Pixbuf Icon {
			get;
		}

		Widget PageWidget {
			get;
		}

		void Destroy();
	}
}
