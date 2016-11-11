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
	internal class MemosItem : ISidebarItem
	{
		Gdk.Pixbuf icon;

		public MemosItem ()
		{
			icon = Gui.LoadIcon(16, "mail_generic");
		}

		public string Name {
			get {
				return "Memos";
			}
		}

		public int Count {
			get {
				return MemosPage.Instance.MemoCount;
			}
		}

		public Gdk.Pixbuf Icon {
			get {
				return icon;
			}
		}

		public Gtk.Widget PageWidget {
			get {
				return MemosPage.Instance;
			}
		}

		public void Destroy ()
		{
			throw new InvalidOperationException("This should never be destroyed.");
		}
	}
}
