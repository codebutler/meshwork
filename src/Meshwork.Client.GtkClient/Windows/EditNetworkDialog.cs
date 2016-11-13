//
// EditNetworkDialog.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using Glade;
using Gtk;
using Meshwork.Backend.Core;

namespace Meshwork.Client.GtkClient.Windows
{
	public class EditNetworkDialog : GladeDialog
	{
		[Widget] TreeView trustedNodesList;
		NetworkInfo networkInfo;
		ListStore trustedNodesListStore;

		public EditNetworkDialog (Window parentWindow, NetworkInfo networkInfo) : base (parentWindow, "EditNetworkDialog")
		{
			this.networkInfo = networkInfo;
			Dialog.Title = string.Format ("Editing network: {0}", networkInfo.NetworkName);
	
			trustedNodesList.AppendColumn ("Name", new CellRendererText (), new TreeCellDataFunc (NameFunc));

			trustedNodesListStore = new ListStore (typeof (TrustedNodeInfo));
			foreach (TrustedNodeInfo trustedNode in networkInfo.TrustedNodes.Values) {
				trustedNodesListStore.AppendValues (trustedNode);
			}
			trustedNodesList.Model = trustedNodesListStore;
		}

		private void addTrustedNodeButton_Clicked (object sender, EventArgs args)
		{
			AddTrustedNodeDialog w = new AddTrustedNodeDialog(Dialog);

			int result = w.Run();
			if (result == (int)ResponseType.Ok) { 
				TrustedNodeInfo tni = w.TrustedNodeInfo;
				if (tni != null) {
					foreach (object[] row in trustedNodesListStore) {
						var thisInfo = (TrustedNodeInfo)row[0];
						if (thisInfo.NodeId == tni.NodeId) {
							Gui.ShowErrorDialog("This node already exists!", base.Dialog);
							return;
						}
					}
					
					trustedNodesListStore.AppendValues(tni);
				}
			}
		}
		
		private void removeTrustedNodeButton_Clicked (object sender, EventArgs args)
		{
			TreeIter iter;
			if (trustedNodesList.Selection.GetSelected (out iter) == true) {
			//	TrustedNodeInfo node = (TrustedNodeInfo) trustedNodesListStore.GetValue (iter, 0);
				trustedNodesListStore.Remove (ref iter);
			}
		}

		private void trustedNodesList_RowActivated (object sender, RowActivatedArgs args)
		{
			TreeIter iter;
			if (trustedNodesList.Selection.GetSelected (out iter)) {
				TrustedNodeInfo trustedNode = (TrustedNodeInfo) trustedNodesListStore.GetValue (iter, 0);
				EditFriendDialog editDialog = new EditFriendDialog (Dialog, ref trustedNode);
				editDialog.Run ();
			}
		}

		private void NameFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			TrustedNodeInfo friend = (TrustedNodeInfo) model.GetValue (iter, 0);
			(cell as CellRendererText).Text = friend.Identifier;
		}

		protected override void OnResponded (int responseId)
		{
			if (responseId == (int)ResponseType.Ok) {
				networkInfo.TrustedNodes.Clear();
				foreach (object[] row in trustedNodesListStore) {
					TrustedNodeInfo tni = (TrustedNodeInfo)row[0];
					networkInfo.TrustedNodes.Add(tni.NodeId, tni);
				}
			}
		}
	}
}
