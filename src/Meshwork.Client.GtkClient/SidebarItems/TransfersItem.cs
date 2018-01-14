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
				return Runtime.Core.FileTransferManager.Transfers.Count;
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
