//
// UserMenu.cs: User context menu
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2005-2008 FileFind.net (http://filefind.net)
//

using Gtk;
using Glade;
using System;
using FileFind.Meshwork.Transport;
using FileFind.Meshwork.Destination;

namespace FileFind.Meshwork.GtkClient
{
	public class UserMenu
	{
		[Widget] MenuItem mnuUsersConnectTo;
		[Widget] MenuItem mnuUsersMessageUser;
		[Widget] MenuItem mnuUsersGetInfo;
		[Widget] MenuItem mnuUsersInviteChat;
		[Widget] MenuItem mnuUsersBrowseFiles;
		[Widget] MenuItem mnuUsersSendFile;
		[Widget] MenuItem mnuUsersRequestPublicKey;
		Menu mnuUsers;
		Network network;
		Node selectedNode;

		public UserMenu ()
		{
			Glade.XML xmlMnuUsers = new Glade.XML(null, "FileFind.Meshwork.GtkClient.meshwork.glade", "mnuUsers", null);
			mnuUsers = (xmlMnuUsers.GetWidget("mnuUsers") as Gtk.Menu);
			xmlMnuUsers.Autoconnect(this);
		}

		public void Popup (Network network, Node selectedNode)
		{
			if (selectedNode != null) {
				this.selectedNode = selectedNode;
				this.network = network;
			}

			// Enable none
			mnuUsersConnectTo.Sensitive = false;
			mnuUsersMessageUser.Sensitive = false;
			mnuUsersGetInfo.Sensitive = false;
			mnuUsersInviteChat.Sensitive = false;
			mnuUsersBrowseFiles.Sensitive = false;
			mnuUsersSendFile.Sensitive = false;
			
			mnuUsersRequestPublicKey.Visible = false;
			
			if (selectedNode != null) {
				if (Core.IsLocalNode(selectedNode)) {
					mnuUsersGetInfo.Sensitive = true;
					mnuUsersBrowseFiles.Sensitive = true;
				}
				else {
					if (!network.TrustedNodes.ContainsKey(selectedNode.NodeID)) {
						// Request Public Key
						mnuUsersRequestPublicKey.Visible = true;
					} else {
						if (selectedNode.FinishedKeyExchange == true) {
							// Enable all
							mnuUsersConnectTo.Sensitive = true;
							mnuUsersMessageUser.Sensitive = true;
							mnuUsersGetInfo.Sensitive = true;
							mnuUsersInviteChat.Sensitive = true;
							mnuUsersBrowseFiles.Sensitive = true;
							mnuUsersSendFile.Sensitive = true;
						}
						else { 
							// This just means we are waiting to finish the key exchange 
						}
					}
				}
			}

			mnuUsers.Show();
			mnuUsers.Popup ();
		}


		public void on_mnuUsersConnectTo_activate(object o, EventArgs e)
		{
			try {
				IDestination destination = selectedNode.FirstConnectableDestination;
				if (destination != null) {
					ITransport transport = destination.CreateTransport(ConnectionType.NodeConnection);
					network.ConnectTo(transport);
				} else {
					Gui.ShowErrorDialog("You cannot connect to this user.");
				}
			} catch (Exception ex) {
				Console.WriteLine (ex);
				Gui.ShowErrorDialog(ex.Message);
			}
		}

		public void on_mnuUsersMessageUser_activate(object o, EventArgs e)
		{
			Gui.StartPrivateChat(network, selectedNode);
		}

		public void on_mnuUsersGetInfo_activate(object o, EventArgs e)
		{
			 UserInfoDialog dialog = new UserInfoDialog(Gui.MainWindow.Window, network, selectedNode);
			 dialog.Run();
		}

		public void on_mnuUsersInviteChat_activate(object o, EventArgs e)
		{
			InviteToChatDialog dialog = new InviteToChatDialog (network, selectedNode);
			dialog.Run ();
		}

		public void on_mnuUsersBrowseFiles_activate(object o, EventArgs e)
		{
			Gui.MainWindow.SelectedPage = UserBrowserPage.Instance;
			if (Core.IsLocalNode(selectedNode)) {
				UserBrowserPage.Instance.NavigateTo("/" + selectedNode.NodeID + "/");
			} else {
				UserBrowserPage.Instance.NavigateTo("/" + network.NetworkID + "/" + selectedNode.NodeID + "/");
			}
		}

		public void on_mnuUsersSendFile_activate(object o, EventArgs e)
		{
			FileSelector dialog = new FileSelector ("Select file to send");
			dialog.Show ();
			if (dialog.Run () == (int)Gtk.ResponseType.Ok) {
				network.SendFile (selectedNode, dialog.Filename);
			}
			dialog.Destroy ();
		}

		public void on_mnuUsersRequestPublicKey_activate(object o, EventArgs e) {
			network.RequestPublicKey( selectedNode );			
		}

		public void on_mnuUsers_show (object o, EventArgs args)
		{
		}
	}
}
