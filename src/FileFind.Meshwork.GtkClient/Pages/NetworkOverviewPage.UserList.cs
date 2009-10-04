//
// NetworkOverviewPage.UserList.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.Globalization;
using Gtk;
using FileFind.Meshwork;

namespace FileFind.Meshwork.GtkClient
{
	public partial class NetworkOverviewPage
	{
		TreeView userList;
		TreeStore userListStore;
		
		private void CreateUserList ()
		{
			userList = new TreeView();
			userListStore = new TreeStore (typeof (object));
			userList.Model = userListStore;
			userList.RowActivated += OnUserListRowActivated;
			userList.ButtonPressEvent += userList_ButtonPressEvent;
			userList.HeadersVisible = false;
			
			TreeViewColumn column = new TreeViewColumn();
						
			CellRendererPixbuf imageCell = new CellRendererPixbuf ();
			column.PackStart (imageCell, false);
			column.SetCellDataFunc (imageCell, new TreeCellDataFunc (UserListIconFunc));

			CellRendererText textCell = new CellRendererText ();
			column.PackStart (textCell, true);
			
			column.SetCellDataFunc (textCell, new TreeCellDataFunc (UserListTextFunc));
						
			userList.AppendColumn(column);

			Runtime.BuiltinActions["ToggleMainUsers"].Activated += ToggleMainUsers_Activated;
		}

		private void ToggleMainUsers_Activated (object sender, EventArgs args)
		{
			sidebar.Visible = ((ToggleAction)sender).Active;
		}
		
		private void UserListIconFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			object item = model.GetValue (iter, 0);

			if (item is Node) {
				Node node = (Node)item;

				AvatarManager avatarManager = (AvatarManager)Core.AvatarManager;
				Gdk.Pixbuf avatar = avatarManager.GetSmallAvatar(node);
				(cell as CellRendererPixbuf).Pixbuf = avatar;
				(cell as CellRendererPixbuf).Visible = true;
			} else {
				//Network network = (Network)item;
				//XXX: Show some sort of icon
				(cell as CellRendererPixbuf).Visible = false;
			}
		}

		private void UserListTextFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			object item = model.GetValue (iter, 0);

			if (item is Node) {
				Node node = (Node)item;
		
				string smallText = "";

				NumberFormatInfo nfi = new CultureInfo ("en-US", false ).NumberFormat;
				nfi.NumberDecimalDigits = 0;

				Gdk.Color color;

				if (userList.Selection.IterIsSelected (iter) == true)
					color = userList.Style.Text (Gtk.StateType.Selected);
				else
					color = userList.Style.Dark (Gtk.StateType.Normal);

				string darkColor = String.Format ("#{0}{1}{2}", (color.Red / 256).ToString ("X"), (color.Green / 256).ToString("X"), (color.Blue / 256).ToString("X"));

				if (node == node.Network.LocalNode | node.FinishedKeyExchange == true)
					if (node.Files > 0)
						smallText = "\n<span foreground=\"" + darkColor + "\" size=\"small\">" + node.Files.ToString("N",nfi) + " Files (" + FileFind.Common.FormatBytes(node.Bytes) + ")</span>";
					else
						smallText = "\n<span foreground=\"" + darkColor + "\" size=\"small\">No shared files</span>";

				else if (!node.Network.TrustedNodes.ContainsKey(node.NodeID))
					smallText = "\n<span foreground=\"" + darkColor + "\" size=\"small\">Untrusted Node</span>";
				else if (node.RemotelyUntrusted == true)
					smallText = "\n<span foreground=\"" + darkColor + "\" size=\"small\">Remotely Untrusted</span>";
				else
					smallText = "\n<span foreground=\"" + darkColor + "\" size=\"small\">Creating encrypted session...</span>";

				(cell as CellRendererText).Markup =  node.ToString() + smallText;
			} else {
				Network network = (Network)item;
				(cell as CellRendererText).Markup = "<b>" + network.NetworkName + "</b>";
			}
		}
		
		public Node GetSelectedNode()
		{
			TreeIter iter;
			TreeModel model;
			if (userList.Selection.GetSelected (out model, out iter) == true) {
				object item = model.GetValue (iter, 0);
				if (item is Node) {
					return (Node)item;
				} 
			}
			return null;
		}

		private void SelectNode (Node node)
		{
			if (node != null) {
				TreeIter topIter;
				TreeIter nodeIter;
				userListStore.GetIterFirst (out topIter);
				if (userListStore.IterIsValid (topIter)) {
					do {
						if (userListStore.IterChildren (out nodeIter, topIter)) {
							do {
								Node currentNode = (Node) userListStore.GetValue (nodeIter, 0);
								if (currentNode == node) {
									userList.Selection.SelectIter (nodeIter);
									return;
								}
							} while (userListStore.IterNext (ref nodeIter));
						} 
					} while (userListStore.IterNext (ref topIter));
				}
			} else {
				userList.Selection.UnselectAll ();
			}
		}

		private void network_UserOnline(Network network, Node node)
		{
			userListStore.AppendValues (IterForNetwork (node.Network), node);
			
			Gui.MainWindow.UpdateStatusText();
			
			if (Gui.GetPrivateMessageWindow (node) != null) {
				Gui.GetPrivateMessageWindow (node).SetUserOnline ();
			}
		}

		private void network_UserOffline(Network network, Node n)
		{
			TreeIter networkIter;
			TreeIter nodeIter;

			if (userListStore.GetIterFirst (out networkIter)) {
				do {
					if (userListStore.IterChildren (out nodeIter, networkIter)) {
						do {
							Node currentNode = (Node) userListStore.GetValue (nodeIter, 0);
							if (currentNode == n) {
								userListStore.Remove (ref nodeIter);
								break;
							}
						} while (userListStore.IterNext (ref nodeIter));
					}
				} while (userListStore.IterNext (ref networkIter));
			}
			
			Gui.MainWindow.UpdateStatusText ();

			if (Gui.GetPrivateMessageWindow (n) != null) {
				Gui.GetPrivateMessageWindow (n).SetUserOffline ();
			}
		}

		private void network_UpdateNodeInfo (Network network, string oldNick, Node node)
		{
			try {
				if (oldNick != node.NickName) {
					if (Gui.GetPrivateMessageWindow(node) != null) {
						Gui.GetPrivateMessageWindow (node).UserInfoChanged (oldNick);
					}
				}

				RefreshUserList();
				Gui.MainWindow.UpdateStatusText ();
			} catch (Exception ex) {
				LoggingService.LogError(ex);
				Gui.ShowErrorDialog (ex.ToString(), Gui.MainWindow.Window);
			}
		}

		private TreeIter IterForNetwork (Network network)
		{
			TreeIter iter;
			if (userListStore.GetIterFirst (out iter)) {
				do {
					Network thisNetwork = (Network)userListStore.GetValue (iter, 0);
					if (thisNetwork == network) {
						return iter;
					}
				} while (userListStore.IterNext (ref iter));
			} 
			return TreeIter.Zero;
		}

		private void OnUserListRowActivated (object sender, Gtk.RowActivatedArgs e)
		{
			Node node = GetSelectedNode();
			if (node != null) {
				Gui.StartPrivateChat(node.Network, node);
			}
		}

		[GLib.ConnectBefore]
		private void userList_ButtonPressEvent (object o, Gtk.ButtonPressEventArgs args)
		{
			TreePath path;
			if (userList.GetPathAtPos ((int)args.Event.X, (int)args.Event.Y, out path)) {
				userList.Selection.SelectPath (path);
			} else {
				userList.Selection.UnselectAll ();
			}

			var node = GetSelectedNode();
			
			if (map != null) {
				map.SelectNode(node);
			}
						
			if (args.Event.Button == 3) {
				if (node != null) {
					UserMenu menu = new UserMenu(node.Network, node);
					menu.Popup();
				}
			}
		}
	}
}
