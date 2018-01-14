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
	internal class ChatsItem : ISidebarItem
	{
		Gdk.Pixbuf icon;

		public ChatsItem ()
		{
			icon = Gui.LoadIcon(16, "internet-group-chat");
		}

		public string Name {
			get {
				return "Chats";
			}
		}

		public int Count {
			get {
				return ChatsPage.Instance.ChatCount;
			}
		}

		public Gdk.Pixbuf Icon {
			get {
				return icon;
			}
		}

		public Gtk.Widget PageWidget {
			get {
				return ChatsPage.Instance;
			}
		}

		public void Destroy ()
		{
			throw new InvalidOperationException("This should never be destroyed.");
		}
	}
}
