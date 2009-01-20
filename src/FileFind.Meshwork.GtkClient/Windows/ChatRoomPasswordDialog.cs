//
// ChatRoomPasswordDialog.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using Glade;
using Gtk;

namespace FileFind.Meshwork.GtkClient
{
	public class ChatRoomPasswordDialog : GladeDialog
	{
		[Widget] Label infoLabel;
		[Widget] Entry passwordEntry;
		[Widget] Label badPasswordLabel;

		ChatRoom room;

		public ChatRoomPasswordDialog (Window parent, ChatRoom room)
			: base (parent, "ChatRoomPasswordDialog")
		{
			this.room = room;
			infoLabel.Markup = "<b>" + String.Format(infoLabel.Text, room.Name) + "</b>";
		}

		private void on_okbutton_clicked (object sender, EventArgs args)
		{
			if (room.TestPassword(passwordEntry.Text) == false) {
				badPasswordLabel.Visible = true;
				passwordEntry.GrabFocus();
				Dialog.Respond((int)ResponseType.None);
			}
		}

		private void on_passwordEntry_changed (object sender, EventArgs args)
		{
			if (badPasswordLabel.Visible == true) {
				badPasswordLabel.Visible = false;
			}
		}
		 
		public string Password
		{
			get {
				return passwordEntry.Text;
			}
		}
	}
}
