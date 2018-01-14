//
// TransfersItem.cs:
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
