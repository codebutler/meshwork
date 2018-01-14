//
// UserBrowserItem.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2007 Meshwork Authors
//

using System;
using Meshwork.Client.GtkClient.Pages;

namespace Meshwork.Client.GtkClient.SidebarItems
{
	internal class UserBrowserItem : ISidebarItem
	{
		Gdk.Pixbuf icon;

		public UserBrowserItem ()
		{
			icon = Gui.LoadIcon(24, "folder");
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
