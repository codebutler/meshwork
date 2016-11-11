//
// TransfersItem.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007 FileFind.net (http://filefind.net)
//

using System;
using FileFind.Meshwork.GtkClient.Pages;

namespace FileFind.Meshwork.GtkClient.SidebarItems
{
	internal class StatusItem : ISidebarItem
	{
		Gdk.Pixbuf icon;

		public StatusItem ()
		{
			icon = Gui.LoadIcon(16, "text-x-generic");
		}

		public string Name {
			get {
				return "Status Log";
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
				return StatusLogPage.Instance;
			}
		}

		public void Destroy ()
		{
			throw new InvalidOperationException("This should never be destroyed.");
		}
	}
}
