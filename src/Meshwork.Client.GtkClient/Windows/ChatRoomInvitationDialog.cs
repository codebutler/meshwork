//
// ChatRoomInvitationDialog.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using System;
using Glade;
using Gtk;
using Meshwork.Backend.Core;
using Meshwork.Backend.Core.Protocol;

namespace Meshwork.Client.GtkClient.Windows
{
	public class ChatRoomInvitationDialog : GladeWindow
	{
		ChatRoom room;
		[Widget] Widget messageContainer;
		[Widget] Widget passwordInfoBox;
		[Widget] Label descLabel;
		[Widget] Label messageLabel;
		[Widget] Entry passwordEntry;
		[Widget] CheckButton showPasswordCheck;
		[Widget] Button joinButton;
		
		ChatInviteInfo invitation;

		public ChatRoomInvitationDialog (Network network, Node inviteFrom, ChatRoom room, ChatInviteInfo invitation) : base ("ChatRoomInvitationDialog")
		{
			this.room = room;
			this.invitation = invitation;

			descLabel.Markup = string.Format(descLabel.Text, GLib.Markup.EscapeText(inviteFrom.ToString()), GLib.Markup.EscapeText(room.Name));
			
			messageContainer.Visible = !string.IsNullOrEmpty(invitation.Message);
			messageLabel.Text = GLib.Markup.EscapeText(invitation.Message);
			
			passwordInfoBox.Visible = room.HasPassword;
			
			passwordEntry.Text = invitation.Password;
			showPasswordCheck.Visible = !string.IsNullOrEmpty(invitation.Password);
			
			Validate();
		}

		private void joinButton_Clicked (object sender, EventArgs args)
		{
			Window.Destroy();
			Gui.JoinChatRoom(room, passwordEntry.Text);
		}

		private void denyButton_Clicked (object sender, EventArgs args)
		{
			Window.Destroy();
		}
		
		void HandleShowPasswordCheckToggled (object sender, EventArgs args)
		{
			passwordEntry.Visibility = showPasswordCheck.Active;
		}
		
		void HandlePasswordEntryChanged (object sender, EventArgs args)
		{
			Validate();
		}
		
		void Validate ()
		{
			joinButton.Sensitive = !room.HasPassword || (room.HasPassword && room.TestPassword(passwordEntry.Text));
		}
	}
}
