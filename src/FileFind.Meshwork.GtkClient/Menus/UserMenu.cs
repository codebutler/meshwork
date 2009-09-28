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
		Menu m_Menu;
		
		MenuItem m_ConnectMenuItem;		
		MenuItem m_MessageMenuItem;
		MenuItem m_GetInfoMenuItem;
		MenuItem m_InviteChatMenuRoom;
		MenuItem m_BrowseMenuItem;
		MenuItem m_SendFileMenuItem;
		MenuItem m_TrustMenuItem;
		
		Network network;
		Node selectedNode;

		public UserMenu (Network network, Node node)
		{
			m_Menu = new Menu();
						
			m_MessageMenuItem = new MenuItem("_Message");
			m_MessageMenuItem.Activated += on_m_MessageMenuItem_activate;
			m_Menu.Append(m_MessageMenuItem);
			
			m_GetInfoMenuItem = new MenuItem("View _Info");
			m_GetInfoMenuItem.Activated += on_m_GetInfoMenuItem_activate;
			m_Menu.Append(m_GetInfoMenuItem);
			
			m_InviteChatMenuRoom = new MenuItem("In_vite to Chat...");
			m_InviteChatMenuRoom.Activated += on_m_InviteChatMenuRoom_activate;
			m_Menu.Append(m_InviteChatMenuRoom);
			
			m_Menu.Append(new SeparatorMenuItem());
			
			m_ConnectMenuItem = new MenuItem("C_onnect");
			m_ConnectMenuItem.Activated += on_mnuUsersConnectTo_activate;
			m_Menu.Append(m_ConnectMenuItem);
			
			m_Menu.Append(new SeparatorMenuItem());
			
			m_BrowseMenuItem = new MenuItem("_Browse");
			m_BrowseMenuItem.Activated += on_m_BrowseMenuItem_activate;
			m_Menu.Append(m_BrowseMenuItem);
			
			m_SendFileMenuItem = new MenuItem("Send _File...");
			m_SendFileMenuItem.Activated += on_m_SendFileMenuItem_activate;
			m_Menu.Append(m_SendFileMenuItem);
			
			m_TrustMenuItem = new MenuItem("_Trust");
			m_TrustMenuItem.Activated += on_m_TrustMenuItem_activate;
			m_Menu.Append(m_TrustMenuItem);
			
			m_Menu.ShowAll();
			
			this.selectedNode = node;
			this.network = network;
		}

		public void Popup ()
		{
			Popup(null);
		}
		
		public void Popup (Widget widget)
		{
			// Enable none
			m_ConnectMenuItem.Sensitive = false;
			m_MessageMenuItem.Sensitive = false;
			m_GetInfoMenuItem.Sensitive = false;
			m_InviteChatMenuRoom.Sensitive = false;
			m_BrowseMenuItem.Sensitive = false;
			m_SendFileMenuItem.Sensitive = false;
			
			m_TrustMenuItem.Visible = false;
						
			if (selectedNode != null) {
				if (Core.IsLocalNode(selectedNode)) {
					m_GetInfoMenuItem.Sensitive = true;
					m_BrowseMenuItem.Sensitive = true;
				}
				else {
					if (!network.TrustedNodes.ContainsKey(selectedNode.NodeID)) {
						// Request Public Key
						m_TrustMenuItem.Visible = true;
					} else {
						if (selectedNode.FinishedKeyExchange == true) {
							// Enable all
							m_ConnectMenuItem.Sensitive = true;
							m_MessageMenuItem.Sensitive = true;
							m_GetInfoMenuItem.Sensitive = true;
							m_InviteChatMenuRoom.Sensitive = true;
							m_BrowseMenuItem.Sensitive = true;
							m_SendFileMenuItem.Sensitive = true;
						}
						else { 
							// This just means we are waiting to finish the key exchange 
						}
					}
				}
			}

			m_Menu.Show();
			
			if (widget != null) {
				m_GetInfoMenuItem.Hide();
				
				int windowX, windowY;
				widget.ParentWindow.GetOrigin(out windowX, out windowY);
				MenuPositionFunc positionFunc = delegate (Menu menu, out int x, out int y, out bool push_in) {
					var widgetX = windowX + widget.Allocation.X;
					var widgetY = windowY + widget.Allocation.Y;					
					widgetY += widget.Allocation.Height;
					
					x = widgetX;
					y = widgetY;
					
					push_in = true;
					
				};
				m_Menu.Popup(null, null, positionFunc, 1, 0);
			} else {			
				m_Menu.Popup();
			}
		}

		void on_mnuUsersConnectTo_activate(object o, EventArgs e)
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
				LoggingService.LogError(ex);
				Gui.ShowErrorDialog(ex.Message);
			}
		}

		void on_m_MessageMenuItem_activate(object o, EventArgs e)
		{
			Gui.StartPrivateChat(network, selectedNode);
		}

		void on_m_GetInfoMenuItem_activate(object o, EventArgs e)
		{
			 UserInfoDialog dialog = new UserInfoDialog(Gui.MainWindow.Window, network, selectedNode);
			 dialog.Run();
		}

		void on_m_InviteChatMenuRoom_activate(object o, EventArgs e)
		{
			InviteToChatDialog dialog = new InviteToChatDialog (network, selectedNode);
			dialog.Run ();
		}

		void on_m_BrowseMenuItem_activate (object o, EventArgs e)
		{
			Gui.MainWindow.SelectedPage = UserBrowserPage.Instance;
			if (Core.IsLocalNode(selectedNode)) {
				UserBrowserPage.Instance.NavigateTo(Core.FileSystem.RootDirectory.MyDirectory.FullPath);
			} else {
				UserBrowserPage.Instance.NavigateTo(selectedNode.Directory.FullPath);
			}
		}

		void on_m_SendFileMenuItem_activate(object o, EventArgs e)
		{
			FileSelector dialog = new FileSelector ("Select file to send");
			dialog.Show ();
			if (dialog.Run () == (int)Gtk.ResponseType.Ok) {
				network.SendFile (selectedNode, dialog.Filename);
			}
			dialog.Destroy ();
		}

		void on_m_TrustMenuItem_activate(object o, EventArgs e) {
			network.RequestPublicKey( selectedNode );			
		}
	}
}
