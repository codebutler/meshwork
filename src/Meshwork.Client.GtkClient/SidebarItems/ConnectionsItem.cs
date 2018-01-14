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
	internal class ConnectionsItem : ISidebarItem
	{
		Gdk.Pixbuf icon;

		public ConnectionsItem ()
		{
			icon = Gui.LoadIcon(16, "network-transmit-receive");
		}

		public string Name {
			get {
				return "Connections";
			}
		}

		public int Count {
			get {
				return Runtime.Core.TransportManager.TransportCount;
			}
		}

		public Gdk.Pixbuf Icon {
			get {
				return icon;
			}
		}

		public Gtk.Widget PageWidget {
			get {
				return ConnectionsPage.Instance;
			}
		}

		public void Destroy ()
		{
			throw new InvalidOperationException("This should never be destroyed.");
		}
	}
}
