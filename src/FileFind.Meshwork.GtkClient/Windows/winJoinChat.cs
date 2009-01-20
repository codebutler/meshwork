//
// winJoinChat.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using FileFind.Meshwork;
using System;
using Gtk;
using Glade;
using FileFind.Meshwork.GtkClient;

namespace FileFind.Meshwork.GtkClient
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
			foreach (Network network in Core.Networks) {
				networksListStore.AppendValues (network);
			}

			roomListStore = new ListStore (typeof(string));

			CellRendererText textCell = new CellRendererText ();
			networkCombo.PackStart (textCell, true);
			networkCombo.SetCellDataFunc (textCell, new CellLayoutDataFunc (networkComboBox_TextFunc));
			networkCombo.Model = networksListStore;
			networkCombo.Active = 0;

			roomNameCombo.Entry.Changed += roomNameCombo_Entry_Changed;
			roomNameCombo.Entry.ActivatesDefault = true;
			roomNameCombo.Entry.Text = "#";

			textCell = new CellRendererText ();
			roomNameCombo.PackStart (textCell, true);
			roomNameCombo.AddAttribute (textCell, "text", 0);
			roomNameCombo.Model = roomListStore;
			roomNameCombo.TextColumn = 0;

			base.Dialog.Shown += delegate {
				roomNameCombo.Entry.SelectRegion (1,1);
			};

			EnableDisableOkButton();
		}
		
		private void roomNameCombo_Entry_Changed (object sender, EventArgs args)
		{
			// Enable/disable password box 

			Network selectedNetwork = GetSelectedNetwork();
			if (selectedNetwork != null) {
				ChatRoom room = selectedNetwork.GetChatRoom(roomNameCombo.Entry.Text);
				if (room != null && room.HasPassword) {
					passwordCheck.Active = true;
					passwordCheck.Sensitive = false;
				} else {
					passwordCheck.Sensitive = true;
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
				Gui.ShowMessageDialog ("No network selected.",
				                       Dialog,
						       Gtk.MessageType.Error,
						       Gtk.ButtonsType.Ok);
				return;
			}

			try {
				string roomName = roomNameCombo.Entry.Text.Trim();
				ChatRoom room = selectedNetwork.GetChatRoom(roomName);
				if (room == null || room.InRoom == false) {
					if (passwordCheck.Active == true) { 
						selectedNetwork.JoinChat(roomName, passwordEntry.Text);
					}
					else {
						selectedNetwork.JoinChat(roomName);
					}
				} else {
					ChatRoomSubpage window = (ChatRoomSubpage)room.Properties["Window"];
					window.GrabFocus();
				}
			} catch (Exception ex) {
				Console.Error.WriteLine(ex);
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
				foreach (ChatRoom room in selectedNetwork.ChatRooms.Values) {
					roomNameCombo.AppendText (room.Name);
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
	}
}
