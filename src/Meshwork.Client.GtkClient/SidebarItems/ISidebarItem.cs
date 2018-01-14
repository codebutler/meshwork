//
// ISidebarItem.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2007 Meshwork Authors
//

using Gdk;
using Gtk;

namespace Meshwork.Client.GtkClient.SidebarItems
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
