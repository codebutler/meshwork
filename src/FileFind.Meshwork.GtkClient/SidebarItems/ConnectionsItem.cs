//
// TransfersItem.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007 FileFind.net (http://filefind.net)
//

using System;

namespace FileFind.Meshwork.GtkClient
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
				return Core.TransportManager.TransportCount;
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
