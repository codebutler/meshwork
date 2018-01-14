//
// ChatMenu: Chat room context menu
//
// Author:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2005-2006 Meshwork Authors
//

using System;
using Meshwork.Client.GtkClient.Pages;
using Meshwork.Client.GtkClient.Windows;
using Glade;
using Gtk;
using Meshwork.Backend.Core;

namespace Meshwork.Client.GtkClient.Menus
{
	public class ChatMenu
	{
		[Widget] MenuItem mnuChatJoinRoom;
		[Widget] SeparatorMenuItem mnuChatJoinRoomSeporator;
		[Widget] MenuItem mnuChatCreateNewChatroom;
		[Widget] Statusbar statusBar;
		Gtk.Menu mnuChat;
		ChatRoom selectedRoom;

		public ChatMenu ()
		{
			Glade.XML xmlMnuChat = new Glade.XML(null, "Meshwork.Client.GtkClient.Resources.Glade.meshwork.glade","mnuChat",null);
			mnuChat = (xmlMnuChat.GetWidget("mnuChat") as Gtk.Menu);
			xmlMnuChat.Autoconnect(this);
		}

		public void Popup (ChatRoom selectedRoom)
		{
			this.selectedRoom = selectedRoom;
			mnuChat.Popup ();
		}

		private void on_mnuChat_show (object o, EventArgs args)
		{
			if (selectedRoom != null) {
				if (selectedRoom.InRoom == true) {
					(mnuChatJoinRoom.Child as Gtk.Label).Markup = "<b>Show " + selectedRoom.Name + "</b>";
				} else {
					(mnuChatJoinRoom.Child as Gtk.Label).Markup = "<b>Join " + selectedRoom.Name + "</b>";
				}
				mnuChatJoinRoom.Visible = true;
				mnuChatJoinRoomSeporator.Visible = true;
			} else {
				mnuChatJoinRoom.Visible = false;
				mnuChatJoinRoomSeporator.Visible = false;
			}
		}

		public void on_mnuChatJoinRoom_activate (object o, EventArgs e)
		{
			if (selectedRoom.InRoom == false) {
				Gui.JoinChatRoom(selectedRoom);
			} else {
				(selectedRoom.Properties["Window"] as ChatRoomSubpage).GrabFocus();
			}
		}

		public void on_mnuChatCreateNewChatroom_activate (object o, EventArgs e)
		{
			JoinChatroomDialog w = new JoinChatroomDialog (Gui.MainWindow.Window);		
			w.Run ();
		}

	}
}
