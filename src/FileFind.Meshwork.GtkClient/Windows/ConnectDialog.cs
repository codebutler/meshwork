//
// ConnectDialog.cs: The "Connect to a Friend" dialog
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2005-2008 FileFind.net
// 

using System;
using System.Net;
using Gdk;
using Glade;
using Gtk;

namespace FileFind.Meshwork.GtkClient.Windows
{
	public class ConnectDialog : GladeDialog
	{
		Pixbuf notImage;
		Pixbuf localImage;
		ListStore store;
		ListStore networksListStore;

		[Widget] Button connectButton;
		[Widget] ComboBoxEntry ipCombo;
		[Widget] ComboBox networksComboBox;

		public ConnectDialog () : base(Gui.MainWindow.Window, "ConnectDialog")
		{
			store = new ListStore(typeof(object), typeof(string));
			ipCombo.Model = store;
			ipCombo.TextColumn = 1;
			
			CellRendererPixbuf imageCell = new CellRendererPixbuf();
			CellRendererText textCell = new CellRendererText();
			ipCombo.Clear();
			ipCombo.PackStart(imageCell, false);
			ipCombo.PackStart(textCell, true);
			ipCombo.SetCellDataFunc(imageCell, ShowImage);
			ipCombo.SetCellDataFunc(textCell, ShowText);
			ipCombo.WrapWidth = 3;
			ipCombo.Entry.ActivatesDefault = true;
			PopulateAddressCombo();
			
			networksListStore = new ListStore(typeof(object));
			networksListStore.AppendValues(new object());
			foreach (Network network in Core.Networks) {
				networksListStore.AppendValues(network);
			}
			
			networksComboBox.Clear();
			
			CellRendererText networkNameCell = new CellRendererText();
			networksComboBox.PackStart(networkNameCell, false);
			networksComboBox.SetCellDataFunc(networkNameCell, new CellLayoutDataFunc(NetworkTextFunc));
			networksComboBox.Model = networksListStore;
			networksComboBox.Changed += delegate { PopulateAddressCombo(); };
			networksComboBox.Active = Math.Min(networksListStore.IterNChildren(), 1);
			
			notImage = Gui.LoadIcon(16, "dialog-warning");
			localImage = Gui.LoadIcon(16, "stock_channel");
		}

		private void PopulateAddressCombo ()
		{
			store.Clear();
			
			TreeIter iter;
			if (networksComboBox.GetActiveIter(out iter)) {
				Network selectedNetwork = networksListStore.GetValue(iter, 0) as Network;
				
				/*
				if (Core.ZeroconfManager != null) {
					foreach (NearbyNode node in Core.NearbyNodes) {
						if (node.NetworkId == selectedNetwork.NetworkID) {
							if (node.Port != TcpTransport.DefaultPort) {
								store.AppendValues(node, node.Address.ToString() + ":" + node.Port.ToString());
							} else {
								store.AppendValues(node, node.Address.ToString());
							}
						}
					}
				}
				*/

				foreach (TrustedNodeInfo node in selectedNetwork.TrustedNodes.Values) {
					if (node.AllowConnect) {
						if (!IsNearby(node)) {
							IDestination destination = node.FirstConnectableDestination;
							if (destination != null) {
								store.AppendValues(node, destination.ToString());
							}
						}
					}
				}
				
				if (store.IterNChildren() == 0) {
					store.AppendValues(new object(), string.Empty);
				}
				
				ipCombo.Sensitive = true;
			} else {
				ipCombo.Sensitive = false;
				ipCombo.Entry.Text = string.Empty;
			}
		}

		private bool IsNearby (TrustedNodeInfo info)
		{
			/*
			if (Core.ZeroconfManager != null) {
				foreach (NearbyNode nnode in Core.NearbyNodes) {
					if (nnode.NodeId == info.NodeID) {
						return true;
					}
				}
			}
			*/			
			return false;
		}

		private void ShowText (CellLayout cell_layout, CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			object currentRow = tree_model.GetValue(iter, 0);
			string address = (string)tree_model.GetValue(iter, 1);
			
			if (currentRow is TrustedNodeInfo) {
				TrustedNodeInfo node = (currentRow as TrustedNodeInfo);
				((CellRendererText)cell).Markup = string.Format("{0}\n<span size=\"small\">({1})</span>", node.Identifier, address);
				cell.Sensitive = true;
			} else if (currentRow is NearbyNode) {
				NearbyNode node = (currentRow as NearbyNode);
				((CellRendererText)cell).Markup = string.Format("{0}\n<span size=\"small\">({1})</span>", node.NickName, address);
				cell.Sensitive = true;
			} else {
				((CellRendererText)cell).Markup = "<b>There are no friends to connect to.</b>";
				cell.Sensitive = false;
			}
		}

		private void ShowImage (CellLayout cell_layout, CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			object currentRow = tree_model.GetValue(iter, 0);
			
			if (currentRow != null) {
				if (currentRow is TrustedNodeInfo) {
					var pixbuf = Gui.AvatarManager.GetSmallAvatar(((TrustedNodeInfo)currentRow).NodeID);
					((CellRendererPixbuf)cell).Pixbuf = pixbuf; 
				} else if (currentRow is NearbyNode)
					((CellRendererPixbuf)cell).Pixbuf = localImage;
				else
					((CellRendererPixbuf)cell).Pixbuf = notImage;
			}
		}

		private void on_connectButton_clicked (object sender, EventArgs e)
		{
			try {
				if (ipCombo.Entry.Text.Trim() == string.Empty) {
					Gui.ShowErrorDialog("Invalid address", base.Dialog);
					return;
				}
				
				Network selectedNetwork = null;
				
				TreeIter iter;
				if (networksComboBox.GetActiveIter(out iter)) {
					selectedNetwork = networksListStore.GetValue(iter, 0) as Network;
				} 
				
				if (selectedNetwork == null) {
					Gui.ShowErrorDialog("No network selected.", Dialog);
					return;
				}
				
				int port = TcpTransport.DefaultPort;
				string address = (ipCombo.Child as Gtk.Entry).Text;
				IPAddress ip = IPAddress.Any;
				
				if (!IPAddress.TryParse(address, out ip)) {
					if (address.StartsWith("[")) {
						port = Convert.ToInt32(address.Substring(address.LastIndexOf(":") + 1));
						address = address.Substring(1, address.LastIndexOf("]") - 1);
					} else if (address.IndexOf(":") > -1 && address.IndexOf(":") == address.LastIndexOf(":")) {
						port = Convert.ToInt32(address.Substring(address.LastIndexOf(":") + 1));
						address = address.Substring(0, address.LastIndexOf(":"));
					}
					
					if (!IPAddress.TryParse(address, out ip)) {		
						connectButton.Sensitive = false;
						base.Dialog.GdkWindow.Cursor = new Cursor(CursorType.Watch);
						System.Threading.ThreadPool.QueueUserWorkItem(delegate {
							try {
								ip = System.Net.Dns.GetHostAddresses(address)[0];									
							} catch (Exception) {
								ip = null;
							}
							Application.Invoke(delegate {
								connectButton.Sensitive = true;
								base.Dialog.GdkWindow.Cursor = new Cursor(CursorType.LeftPtr);	
								Connect(selectedNetwork, address, ip, port);
							});
						});
						
						return;						
					}
				}
				
				Connect(selectedNetwork, address, ip, port);
			
			} catch (Exception ex) {
				LoggingService.LogError(ex);
				Gui.ShowErrorDialog(ex.Message, base.Dialog);
			}
		}
		
		private void Connect (Network network, string address, IPAddress ip, int port)
		{
			try {
				if (!base.Dialog.Visible)
					return;
				
				if (ip == null) {
					Gui.ShowErrorDialog("Unable to resolve hostname.", Dialog);
					return;
				}
				
				ITransport transport = new TcpTransport(ip, port, ConnectionType.NodeConnection);
				network.ConnectTo(transport);
				
				if (Gui.Settings.RecentConnections.IndexOf(address) != -1)
					Gui.Settings.RecentConnections.Remove(address);
				
				Gui.Settings.RecentConnections.Insert(0, address);
				Gui.Settings.SaveSettings();
				
				Dialog.Respond((int)ResponseType.Ok);
			} catch (Exception ex) {
				LoggingService.LogError(ex);
				Gui.ShowErrorDialog(ex.Message, base.Dialog);
			}
		}

		private void NetworkTextFunc (CellLayout layout, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			Network network = (model.GetValue(iter, 0) as Network);
			if (network != null) {
				(cell as CellRendererText).Text = network.NetworkName;
				(cell as CellRendererText).Sensitive = true;
			} else {
				(cell as CellRendererText).Text = "(Select a network)";
				(cell as CellRendererText).Sensitive = false;
			}
		}
	}
}
