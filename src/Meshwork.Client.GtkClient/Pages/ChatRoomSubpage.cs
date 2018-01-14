//
// ChatRoomSubpage.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2006 Meshwork Authors
//

using System;
using System.Globalization;
using Gdk;
using GLib;
using Gtk;
using Meshwork.Backend.Core;
using Meshwork.Client.GtkClient.Menus;

namespace Meshwork.Client.GtkClient.Pages
{
	public class ChatRoomSubpage : ChatSubpageBase
	{
		ChatRoom  thisRoom;
		Network   network;
		ListStore userListStore;

		public ChatRoomSubpage (ChatRoom room)
		{
			thisRoom = room;
			network = room.Network;

			userListStore = new ListStore (typeof (Node));
			userList.Model = userListStore;
			
			var iconCell = new CellRendererPixbuf();
			var textCell = new CellRendererText();
			
			var column = new TreeViewColumn();
			column.PackStart(iconCell, false);
			column.SetCellDataFunc(iconCell, UserListIconFunc);
			
			column.PackStart(textCell, true);
			column.SetCellDataFunc(textCell, UserListTextFunc);
			
			userList.AppendColumn(column);

			userList.HeadersVisible = false;
			userList.RowActivated += on_userList_RowActivated;
			userList.ButtonReleaseEvent +=  on_userList_button_release_event;

			foreach (Node n in room.Users.Values) {
				userListStore.AppendValues (n);
			}
			
			AddToChat (null, string.Format ("You have joined {0}.", thisRoom.Name));

			if (room.HasPassword) {
				AddToChat (null, "This chatroom is password-protected. Other users on the network who do not have the password are not able to evesdrop on the conversation.\n");
			} else {
				AddToChat (null, "This chatroom is not password-protected. Other users on the network are able to evesdrop on the conversation, regardless of if they appear to be in the room or not.\n");
			}

			SendMessage += base_SendMessage;
		}

		public void AddUser (Node node)
		{
			userListStore.AppendValues (node);
			AddToChat (null, node + " has joined " + thisRoom.Name + ".");
		}
	    
		public void RemoveUser (Node node)
		{
			TreeIter iter;

			if (userListStore.GetIterFirst (out iter)) {
				do {
					Node currentNode = (Node) userListStore.GetValue (iter, 0);
					if (currentNode == node) {
						userListStore.Remove (ref iter);
						AddToChat (null, node + " has left " + thisRoom.Name + ".");
						return;
					}	
				} while (userListStore.IterNext (ref iter));
			}
		}

		public override void Close ()
		{
			network.LeaveChat(thisRoom);
			thisRoom.Properties.Remove("Window");
			base.Close();
		}

		private void on_userList_RowActivated (object sender, RowActivatedArgs e)
		{
			Gui.StartPrivateChat(network, GetSelectedNode());
		}

		[ConnectBefore]
		private void on_userList_button_release_event (object o, ButtonReleaseEventArgs args)
		{
			if (args.Event.Button == 3) {
				UserMenu menu = new UserMenu(network, GetSelectedNode());
				menu.Popup();
			}
		}

		private void UserListIconFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			Node node = (Node)model.GetValue (iter, 0);
			Pixbuf avatar = Gui.AvatarManager.GetSmallAvatar(node);
			(cell as CellRendererPixbuf).Pixbuf = avatar;
		}

		private void UserListTextFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			Node node = (Node)model.GetValue (iter, 0);
	
			string smallText = "";

			NumberFormatInfo nfi = new CultureInfo ("en-US", false ).NumberFormat;
			nfi.NumberDecimalDigits = 0;       
				      
			if (node == network.LocalNode | node.FinishedKeyExchange)
				if (node.Files > 0)
					smallText =
					        $"\n<span foreground=\"#666666\" size=\"small\">{node.Files.ToString("N", nfi)} Files ({Common.Utils.FormatBytes(node.Bytes)})</span>";
				else
					smallText = "\n<span foreground=\"#666666\" size=\"small\">No shared files</span>";

			else if (!network.TrustedNodes.ContainsKey(node.NodeID))
				smallText = "\n<span foreground=\"#666666\" size=\"small\">Untrusted Node</span>";
			else if (node.RemotelyUntrusted)
				smallText = "\n<span foreground=\"#666666\" size=\"small\">Remotely Untrusted</span>";
			else
				smallText = "\n<span foreground=\"#666666\" size=\"small\">Creating encrypted session...</span>";

			(cell as CellRendererText).Markup =  node + smallText;
			
		}

		private Node GetSelectedNode()
		{
			TreeIter iter;
			TreeModel model;
			if (userList.Selection.GetSelected (out model, out iter)) {
				return (Node) model.GetValue (iter, 0);
			}
		    return null;
		}

		private void base_SendMessage (object sender, EventArgs args)
		{
			AddToChat(network.LocalNode, inputTextView.Buffer.Text);
			network.SendChatMessage(thisRoom, inputTextView.Buffer.Text);
		}
	}
}
