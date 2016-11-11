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
	internal class TransfersItem : ISidebarItem
	{
		Gdk.Pixbuf icon;

		public TransfersItem ()
		{
			icon = Gui.LoadIcon(16, "go-down");
		}

		public string Name {
			get {
				return "File Transfers";
			}
		}

		public int Count {
			get {
				return Core.FileTransferManager.Transfers.Count;
			}
		}

		public Gdk.Pixbuf Icon {
			get {
				return icon;
			}
		}

		public Gtk.Widget PageWidget {
			get {
				return TransfersPage.Instance;
			}
		}

		public void Destroy ()
		{
			throw new InvalidOperationException("This should never be destroyed.");
		}
	}
}
