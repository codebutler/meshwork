//
// winInviteUser.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using Gtk;
using Glade;
using System.IO;
using System.Net;

namespace FileFind.Meshwork.GtkClient
{
	public class InviteToChatDialog : GladeDialog
	{
		Network network;

		[Widget] Button   okButton;
		[Widget] ComboBox userComboBox;
		[Widget] ComboBox roomComboBox;
		[Widget] TextView messageTextView;
		[Widget] Widget   passwordedBox;
		[Widget] CheckButton includePasswordCheck;
		
		ListStore userStore;
		ListStore roomStore;

		public InviteToChatDialog (Network network, Node node) : this (network, node, null)
		{
			if (node == null) {
				throw new ArgumentNullException ("node");
			}
		}

		public InviteToChatDialog (Network network, ChatRoom room) : this (network, null, room)
		{
			if (room == null) {
				throw new ArgumentNullException ("room");
			}
		}

		private InviteToChatDialog (Network network, Node node, ChatRoom room) : base (null, "InviteToChatDialog")
		{
			if (network == null) {
				throw new ArgumentNullException ("network");
			}

			this.network = network;

			CellRendererPixbuf imageCell = new CellRendererPixbuf ();
			CellRendererText textCell = new CellRendererText ();
			
			imageCell.Pixbuf = Gui.LoadIcon (16, "stock_person");

			userStore = new ListStore (typeof (Node));
			userComboBox.PackStart (imageCell, false);
			userComboBox.PackStart (textCell, true);
			userComboBox.SetCellDataFunc (textCell, UserComboTextFunc);
			userComboBox.Changed += delegate {
				EnableDisableOkButton();
			};

			imageCell = new CellRendererPixbuf ();
			textCell = new CellRendererText ();
			
			roomStore = new ListStore (typeof (object));
			roomComboBox.PackStart (textCell, true);
			roomComboBox.PackStart (imageCell, false);
			roomComboBox.SetCellDataFunc (imageCell, RoomComboImageFunc);
			roomComboBox.SetCellDataFunc (textCell, RoomComboTextFunc);
			roomComboBox.Changed += delegate {
				EnableDisableOkButton();
				HideShowPasswordBox();
			};

			if (node == null) {
				foreach (Node currentNode in network.Nodes.Values)
					userStore.AppendValues (currentNode);
				userComboBox.Model = userStore;
			} else {
				userStore.AppendValues (node);
				userComboBox.Model = userStore;
				userComboBox.Active = 0;
				userComboBox.Sensitive = false;
			}

			if (room == null) {
				int count = 0;
				foreach (ChatRoom currentRoom in network.ChatRooms) {
					if (currentRoom.InRoom) {
						if (count == 0)
							roomStore.AppendValues(new SelectRoom());
						
						roomStore.AppendValues(currentRoom);
						count ++;
					}
				}
				if (count == 0) {
					roomStore.AppendValues(new NotInAnyRooms());
					roomComboBox.Sensitive = false;
				}
				roomComboBox.Model = roomStore;	
				roomComboBox.Active = 0;
			} else {
				roomStore.AppendValues(room);
				roomComboBox.Model = roomStore;	
				roomComboBox.Active = 0;
				roomComboBox.Sensitive = false;
			}

		}
		
		private Node SelectedNode {
			get {
				TreeIter iter;
				if (userComboBox.GetActiveIter (out iter)) {
					return (Node) userStore.GetValue (iter, 0);
				} else {
					return null;
				}
			}
		}

		private ChatRoom SelectedChatRoom {
			get {
				TreeIter iter;
				if (roomComboBox.GetActiveIter (out iter)) {
					object obj = roomStore.GetValue (iter, 0);
					if (obj is ChatRoom) {
						return (ChatRoom)obj;
					}
				}
				return null;
			}
		}

		protected override void OnResponded (int responseId)
		{
			if (responseId == (int)ResponseType.Ok) {
				string password = null;
				if (SelectedChatRoom.HasPassword && includePasswordCheck.Active) {
					password = SelectedChatRoom.Password;
				}			
				network.SendChatInvitation(SelectedNode, SelectedChatRoom, messageTextView.Buffer.Text, password);
			}
		}
	
		private void UserComboTextFunc (CellLayout layout, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			Node node = (Node) model.GetValue (iter, 0);
			(cell as CellRendererText).Text = node.ToString ();
		}
		
		private void RoomComboImageFunc (CellLayout layout, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			var pixbufCell = (CellRendererPixbuf)cell;
			var obj = model.GetValue (iter, 0);
			if (obj is ChatRoom && ((ChatRoom)obj).HasPassword) {
				pixbufCell.Pixbuf = Gui.LoadIcon(22, "dialog-password");
			} else {
				 pixbufCell.Pixbuf = null;
			}
		}
		
		private void RoomComboTextFunc (CellLayout layout, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			object obj = model.GetValue (iter, 0);
			if (obj is ChatRoom) {
				(cell as CellRendererText).Text = ((ChatRoom)obj).Name;
				(cell as CellRendererText).Sensitive = true;
			} else if (obj is NotInAnyRooms) {
				(cell as CellRendererText).Text = "(You are not in any chat rooms)";
				(cell as CellRendererText).Sensitive = false;
			} else if (obj is SelectRoom) {
				(cell as CellRendererText).Text = "(Select chat room)";
				(cell as CellRendererText).Sensitive = false;
			} else {
				(cell as CellRendererText).Text = String.Empty;
				(cell as CellRendererText).Sensitive = false;
			}
		}

		private void EnableDisableOkButton ()
		{
			okButton.Sensitive = (SelectedNode != null) && (SelectedChatRoom != null);
		}

		private void HideShowPasswordBox ()
		{
			 passwordedBox.Visible = (SelectedChatRoom != null && SelectedChatRoom.HasPassword);
		}

		private class SelectRoom { } 
		private class NotInAnyRooms { } 
	}
}
