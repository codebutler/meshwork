//
// ISidebarItem.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007 FileFind.net (http://filefind.net)
//

using System;
using Gtk;
using Gdk;

namespace FileFind.Meshwork.GtkClient
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
