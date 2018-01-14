//
// winJoinChat.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2006 Meshwork Authors
//

using System;
using Meshwork.Client.GtkClient.Pages;
using Glade;
using Gtk;
using Meshwork.Backend.Core;

namespace Meshwork.Client.GtkClient.Windows
{
	public class JoinChatroomDialog : GladeDialog
	{
		[Widget] ComboBox 		networkCombo;
		[Widget] ComboBoxEntry 	roomNameCombo;
		[Widget] Entry 			passwordEntry;
		[Widget] CheckButton 	passwordCheck;
		[Widget] Button 		joinButton;

		ListStore networksListStore;
		ListStore roomListStore;

		public JoinChatroomDialog () : this (null)
		{
		}

		public JoinChatroomDialog (Window parent, ChatRoom room) : this (parent)
		{
			roomNameCombo.Entry.Text = room.Name;
			roomNameCombo.Sensitive = false;
		}

		public JoinChatroomDialog (Window parent) : base (parent, "JoinChatroomDialog")
		{	
			passwordEntry.Text = "";

			networksListStore = new ListStore (typeof (object));
			networksListStore.AppendValues (new object());
			foreach (Network network in Runtime.Core.Networks) {
				networksListStore.AppendValues (network);
			}

			roomListStore = new ListStore (typeof(string), typeof(ChatRoom));

			CellRendererText textCell = new CellRendererText ();
			networkCombo.PackStart (textCell, true);
			networkCombo.SetCellDataFunc (textCell, new CellLayoutDataFunc (networkComboBox_TextFunc));
			networkCombo.Model = networksListStore;

			roomNameCombo.Entry.Changed += roomNameCombo_Entry_Changed;
			roomNameCombo.Entry.ActivatesDefault = true;
			roomNameCombo.Entry.Text = "#";

			var imageCell = new CellRendererPixbuf ();
			roomNameCombo.PackEnd(imageCell, false);			
			roomNameCombo.SetCellDataFunc(imageCell, RoomComboImageFunc);
			
			roomNameCombo.Model = roomListStore;
			roomNameCombo.TextColumn = 0;
			
			if (networksListStore.IterNChildren() > 0) {
				networkCombo.Active = 1;
				roomNameCombo.Entry.GrabFocus();
				roomNameCombo.Entry.SelectRegion(1,1);
			} else {
				networkCombo.Active = 0;
			}

			base.Dialog.Shown += delegate {
				roomNameCombo.Entry.SelectRegion (1,1);
			};

			EnableDisableOkButton();
		}
		
		private void roomNameCombo_Entry_Changed (object sender, EventArgs args)
		{
			Network selectedNetwork = GetSelectedNetwork();
			if (selectedNetwork != null) {
				foreach (ChatRoom room in selectedNetwork.ChatRooms) {
					if (room.Name == roomNameCombo.Entry.Text) {
						passwordCheck.Active = room.HasPassword;
						break;
					}
				}
			}
			
			EnableDisableOkButton();
		}

		private void on_cancelButton_clicked(object sender, EventArgs e)
		{
			Dialog.Respond (ResponseType.Cancel);
		}
		
		private void joinButton_Clicked (object sender, EventArgs e)
		{
			Network selectedNetwork = GetSelectedNetwork();
			if (selectedNetwork == null) {
				Gui.ShowMessageDialog("No network selected.", Dialog, Gtk.MessageType.Error, Gtk.ButtonsType.Ok);
				return;
			}

			try {
				string roomName = roomNameCombo.Entry.Text.Trim();
				string password = passwordEntry.Text;
				
				string roomId = (passwordCheck.Active) ? Common.Utils.SHA512Str(roomName + password) : Common.Utils.SHA512Str(roomName);
				
				ChatRoom room = selectedNetwork.GetChatRoom(roomId);
				if (room != null && room.InRoom) {
					// Already in here!
					ChatRoomSubpage window = (ChatRoomSubpage)room.Properties["Window"];
					window.GrabFocus();					
				} else {
					if (passwordCheck.Active == true) { 
						selectedNetwork.JoinOrCreateChat(roomName, passwordEntry.Text);
					}
					else {
						selectedNetwork.JoinOrCreateChat(roomName, null);
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError(ex);
				Gui.ShowMessageDialog (ex.Message, Dialog);
				Dialog.Respond (ResponseType.None);
				return;
			}		 
			Dialog.Respond (ResponseType.Ok);
		}
		
		private void on_passwordCheck_toggled(object o, EventArgs e)
		{
			passwordEntry.Sensitive = passwordCheck.Active;
			EnableDisableOkButton();
		}	
		
		private void networkComboBox_TextFunc (CellLayout layout, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			Network network = (model.GetValue (iter, 0) as Network);
			if (network != null) {
				(cell as CellRendererText).Text = network.NetworkName;
				(cell as CellRendererText).Sensitive = true;
			} else {
				(cell as CellRendererText).Text = "(Select a network)";
				(cell as CellRendererText).Sensitive = false;
			}
		}

		private void networkCombo_Changed (object o, EventArgs args)
		{
			roomListStore.Clear();

			Network selectedNetwork = GetSelectedNetwork();
			if (selectedNetwork != null) {
				foreach (ChatRoom room in selectedNetwork.ChatRooms) {
					((ListStore)roomNameCombo.Model).AppendValues(room.Name, room);
				}
			}

			EnableDisableOkButton();

			roomNameCombo.Sensitive = (selectedNetwork != null);
			passwordCheck.Sensitive = (selectedNetwork != null);
		}

		private void passwordEntry_Changed (object sender, EventArgs args)
		{
			EnableDisableOkButton();
		}

		private Network GetSelectedNetwork ()
		{
			TreeIter iter;
			if (networkCombo.GetActiveIter (out iter)) {
				return (networksListStore.GetValue (iter, 0) as Network);
			} 
			return null;
		}

		private void EnableDisableOkButton ()
		{
			joinButton.Sensitive = (GetSelectedNetwork() != null &&
			                        roomNameCombo.Entry.Text.Length > 1 &&
						roomNameCombo.Entry.Text.StartsWith("#") == true && 
						((passwordCheck.Active & passwordEntry.Text.Length > 0) ||
						passwordCheck.Active == false));

		}
			
		private void RoomComboImageFunc (CellLayout layout, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			var pixbufCell = (CellRendererPixbuf)cell;
			var room = model.GetValue(iter, 1) as ChatRoom;
			if (room != null && room.HasPassword)
				pixbufCell.Pixbuf = Gui.LoadIcon(22, "dialog-password");
			else
				pixbufCell.Pixbuf = null;
		}
	}
}
