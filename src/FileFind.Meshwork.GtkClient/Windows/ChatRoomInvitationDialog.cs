//
// ChatRoomInvitationDialog.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using System;
using Gtk;
using Glade;
using FileFind.Meshwork;
using FileFind.Meshwork.Protocol;

namespace FileFind.Meshwork.GtkClient
{
	public class ChatRoomInvitationDialog : GladeWindow
	{
		ChatRoom room;
		[Widget] Expander passwordExpander;
		[Widget] Label descLabel;
		[Widget] Label passwordLabel;
		ChatInviteInfo invitation;

		public ChatRoomInvitationDialog (Network network, Node inviteFrom, ChatRoom room, ChatInviteInfo invitation) : base ("ChatRoomInvitationDialog")
		{
			this.room = room;
			this.invitation = invitation;

			descLabel.Markup = String.Format(descLabel.Text, inviteFrom.ToString(), room.Name, invitation.Message);
			passwordExpander.Visible = !String.IsNullOrEmpty(invitation.Password);
			passwordLabel.Markup = String.Format(passwordLabel.Text, invitation.Password);
		}

		private void joinButton_Clicked (object sender, EventArgs args)
		{
			Window.Destroy();
			Gui.JoinChatRoom(room, invitation.Password);
		}

		private void denyButton_Clicked (object sender, EventArgs args)
		{
			Window.Destroy();
		}
	}
}
