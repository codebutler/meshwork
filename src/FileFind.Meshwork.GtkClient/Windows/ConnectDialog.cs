//
// ConnectDialog.cs: The "Connect to a Friend" dialog
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2005-2008 FileFind.net
// 

using System;
using System.Text.RegularExpressions;
using System.Net;
using Gtk;
using GtkSharp;
using Gdk;
using Glade;
using FileFind.Meshwork;
using FileFind.Meshwork.Transport;
using FileFind.Meshwork.Destination;

namespace FileFind.Meshwork.GtkClient
{
	public class ConnectDialog : GladeDialog
	{
		Pixbuf userImage;
		Pixbuf notImage;
		Pixbuf localImage;
		ListStore store;
		ListStore networksListStore;

		[Widget] Button btnConnect;
		[Widget] ComboBoxEntry ipCombo;
		[Widget] ComboBox networksComboBox;
		
		public ConnectDialog () : base (Gui.MainWindow.Window, "ConnectDialog")
		{
			store = new ListStore (typeof(object), typeof(string));
			ipCombo.Model = store;
			ipCombo.TextColumn = 1;
			
			CellRendererPixbuf imageCell = new CellRendererPixbuf ();
			CellRendererText textCell = new CellRendererText();
			ipCombo.Clear ();
			ipCombo.PackStart (imageCell, false);
			ipCombo.PackStart (textCell, true);
			ipCombo.SetCellDataFunc (imageCell, ShowImage);
			ipCombo.SetCellDataFunc (textCell, ShowText);
			ipCombo.WrapWidth = 3;
			ipCombo.Entry.ActivatesDefault = true;
			PopulateAddressCombo();

			networksListStore = new ListStore (typeof (object));
			networksListStore.AppendValues (new object());
			foreach (Network network in Core.Networks) {
				networksListStore.AppendValues (network);
			}
			
			networksComboBox.Clear ();
			
			CellRendererText networkNameCell = new CellRendererText ();
			networksComboBox.PackStart (networkNameCell, false);
			networksComboBox.SetCellDataFunc (networkNameCell, new CellLayoutDataFunc (NetworkTextFunc));
			networksComboBox.Model = networksListStore;
			networksComboBox.Changed += delegate {
				PopulateAddressCombo();
			};
			networksComboBox.Active = Math.Min(networksListStore.IterNChildren(), 1);

			userImage = Gui.LoadIcon (16, "stock_person");
			notImage = Gui.LoadIcon (16, "stock_not");
			localImage = Gui.LoadIcon (16, "stock_channel");
		}
	
		private void PopulateAddressCombo ()
		{
			store.Clear ();

			TreeIter iter;
			if (networksComboBox.GetActiveIter (out iter)) {
				Network selectedNetwork = networksListStore.GetValue (iter, 0) as Network;

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
						if (!IsNearby (node)) {
							IDestination destination = node.FirstConnectableDestination;
							if (destination != null) {
								store.AppendValues(node, node.FirstConnectableDestination.ToString());
							}
						}
					}
				}

				if (store.IterNChildren() == 0) {
					store.AppendValues (new object(), String.Empty);
				}

				ipCombo.Sensitive = true;
			} else {
				ipCombo.Sensitive = false;
				ipCombo.Entry.Text = String.Empty;
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
	
		private void ShowText (CellLayout cell_layout, CellRenderer cell,
	                              TreeModel tree_model, TreeIter iter)
		{
		     object currentRow = tree_model.GetValue (iter, 0);
			string address = (string)tree_model.GetValue(iter, 1);

		      if (currentRow is TrustedNodeInfo) {
				TrustedNodeInfo node = (currentRow as TrustedNodeInfo);
				((CellRendererText)cell).Markup = String.Format ("{0}\n<span size=\"small\">({1})</span>",
				                                               node.Identifier, address);
				cell.Sensitive = true;
		       } else if (currentRow is NearbyNode) {
				NearbyNode node = (currentRow as NearbyNode);
				((CellRendererText)cell).Markup = String.Format ("{0}\n<span size=\"small\">({1})</span>",
				                                                node.NickName,
										address);
				cell.Sensitive = true;
		       } else {
				((CellRendererText)cell).Markup = "<b>There are no friends to connect to.</b>";
				cell.Sensitive = false;
		       }
	       } 
	       
	       private void ShowImage (CellLayout cell_layout, CellRenderer cell, TreeModel tree_model, TreeIter iter)
	       {
		       object currentRow = tree_model.GetValue (iter, 0);

		       if (currentRow != null) {
				if (currentRow is TrustedNodeInfo)
					((CellRendererPixbuf)cell).Pixbuf = userImage;
				else if (currentRow is NearbyNode)
					((CellRendererPixbuf)cell).Pixbuf = localImage;
				else
					((CellRendererPixbuf)cell).Pixbuf = notImage;
		       }
	       }
		
		private void on_connectButton_clicked (object sender, EventArgs e)
		{
			try {
				int port = TcpTransport.DefaultPort;
				string address = (ipCombo.Child as Gtk.Entry).Text;
				IPAddress ip = IPAddress.Any;

				if (!IPAddress.TryParse (address, out ip)) {
				
					// XXX: Support IPv6 w/ port syntax: "[address]:port"
					
					if (address.IndexOf (":") > -1) {
						port = Convert.ToInt32 (address.Substring (address.LastIndexOf (":") + 1));
						address = address.Substring (0, address.LastIndexOf (":"));
						LoggingService.LogDebug(port + " " + address);
					}
					
					if (!IPAddress.TryParse (address, out ip)) {
						try {
							ip = System.Net.Dns.GetHostAddresses(address)[0];
						} catch (Exception) {
							Gui.ShowMessageDialog ("Unable to resolve hostname.", Dialog);
							return;
						}
					}
				}

				try {
					TreeIter iter;
					if (networksComboBox.GetActiveIter (out iter)) {
						Network selectedNetwork = networksListStore.GetValue (iter, 0) as Network;
						if (selectedNetwork != null) {
							ITransport transport = new TcpTransport (ip, port, ConnectionType.NodeConnection);
							selectedNetwork.ConnectTo (transport);
						} else {
							Gui.ShowMessageDialog ("No network selected.", Dialog);
							return;
						}
					} else {
						Gui.ShowMessageDialog ("No network selected.", Dialog);
						return;
					}
				}
				catch (Exception ex) {
					LoggingService.LogError(ex);
					Gui.ShowMessageDialog (ex.ToString(), Dialog);
					return;
				}
			
				if (Gui.Settings.RecentConnections.IndexOf(address) != -1)
					Gui.Settings.RecentConnections.Remove(address);
		
				Gui.Settings.RecentConnections.Insert(0, address);
				Gui.Settings.SaveSettings();
				
				Dialog.Respond ((int)ResponseType.Ok);
			} catch (Exception ex) {
				Gui.ShowMessageDialog (ex.Message);
			}
		}
		
		private void NetworkTextFunc (CellLayout layout, CellRenderer cell, TreeModel model, TreeIter iter)
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
	}
}
