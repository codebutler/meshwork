//
// winEditTrustedNode.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using Glade;
using Gtk;

namespace FileFind.Meshwork.GtkClient.Windows
{
	public class EditFriendDialog : GladeDialog
	{
		[Widget] Label nameLabel;
		[Widget] Label nodeIdLabel;
		[Widget] CheckButton chkAllowProfile;
		[Widget] CheckButton chkAllowNetworkInfo;
		[Widget] CheckButton chkAllowSharedFiles;
		[Widget] CheckButton chkAllowConnect;
		[Widget] TreeView connectionsTreeView;
		[Widget] TextView keyTextView;

		TrustedNodeInfo tni;
		
		public EditFriendDialog (Window parentWindow, ref TrustedNodeInfo tni) : base (parentWindow, "EditFriendDialog")
		{
			if (tni.Identifier != "") {
				nameLabel.Markup = "<b>" + tni.Identifier + "</b>";
			} else {
				nameLabel.Markup = "<b>[Unknown Nickname]</b>";
				tni.Identifier = tni.NodeID;
			}

			nodeIdLabel.Markup = "<span font=\"monospace\">" + Common.FormatFingerprint(tni.NodeID, 8) + "</span>";
			
			chkAllowProfile.Active = tni.AllowProfile;
			chkAllowNetworkInfo.Active = tni.AllowNetworkInfo;
			chkAllowSharedFiles.Active = tni.AllowSharedFiles;
			
			chkAllowConnect.Active = tni.AllowConnect;

			TreeViewColumn column;
			column = connectionsTreeView.AppendColumn("Protocol", new CellRendererText(), "text", 0);
			column = connectionsTreeView.AppendColumn("Address Details", new CellRendererText(), "text", 1);
			column.Expand = true;
			connectionsTreeView.AppendColumn("Supported", new CellRendererText(), "text", 2);
			connectionsTreeView.AppendColumn("Connectable", new CellRendererText(), "text", 3);

			ListStore addressListStore = new ListStore(typeof(string), typeof(string), typeof(string), typeof(string));
			foreach (IDestination destination in tni.Destinations) {
				addressListStore.AppendValues(destination.FriendlyTypeName, destination.ToString(), "True", destination.CanConnect.ToString());
			}
			foreach (DestinationInfo info in tni.DestinationInfos) {
				if (!info.Supported) {
					addressListStore.AppendValues(info.FriendlyName, string.Join(", ", info.Data), "False", "False");
				}
			}
			connectionsTreeView.Model = addressListStore;
			
			keyTextView.Buffer.Text = tni.PublicKey.ToArmoredString();
				
			this.tni = tni;
		}
		
		private void OnOkButtonClicked (object o, EventArgs e)
		{
			tni.AllowProfile = chkAllowProfile.Active;
			tni.AllowNetworkInfo = chkAllowNetworkInfo.Active;
			tni.AllowSharedFiles = chkAllowSharedFiles.Active;
			
			tni.AllowConnect = chkAllowConnect.Active;
		
			Dialog.Respond(ResponseType.Ok);
			Dialog.Destroy();
		}

		public void on_btnCancel_clicked (object o, EventArgs e)
		{
			Dialog.Respond (Gtk.ResponseType.Cancel);
			Dialog.Hide ();
		}

		public void on_winEditTrustedNode_close (object o, EventArgs e)
		{
			Dialog.Respond ((int)Gtk.ResponseType.Cancel);
			Dialog.Hide ();
		}
	}
}
