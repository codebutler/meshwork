//
// AcceptKeyDialog.cs:
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
	public class AcceptKeyDialog : GladeDialog
	{		
		[Widget] Label nicknameLabel;
		[Widget] Label nodeIdLabel;
		[Widget] TextView keyTextView;
		[Widget] EventBox eventbox10;
		[Widget] Label denyKeyButtonLabel;
		[Widget] Label connectionTitleLabel;
		[Widget] Label connectionLabel;
		
		int secondsLeft = 20;
		
		public AcceptKeyDialog (Network network, ReceivedKeyEventArgs args) : base (null, "AcceptKeyDialog")
		{
			var publicKey = new PublicKey(args.Key.Info, args.Key.Key);
			keyTextView.Buffer.Text = publicKey.ToArmoredString();
			string nodeID = Common.Utils.SHA512Str(publicKey.Key);
			nodeIdLabel.Text = nodeID;

			if (args.Node != null)  {
				if (nodeID.ToUpper() != args.Node.NodeID.ToUpper()) {
					throw new Exception ("The key recieved does not match this user!");
				}
				nicknameLabel.Text = args.Node.NickName;
			} else if (args.Connection != null) {
				var conn = args.Connection;
				
				connectionLabel.Text = string.Format("{0} ({1})", conn.RemoteAddress, conn.Incoming ? "Incoming" : "Outgoing");
				nicknameLabel.Text = args.Key.Info;
				
				connectionLabel.Show();
				connectionTitleLabel.Show();
			} else {
				nicknameLabel.Text = args.Key.Info;
			}
			
			denyKeyButtonLabel.Text = string.Format ("Deny Key ({0})", secondsLeft);

		}
		
		public override int Run ()
		{
			base.Dialog.Resize(1,1);			
			eventbox10.ModifyBg (Gtk.StateType.Normal, keyTextView.Style.Base (Gtk.StateType.Normal));

			GLib.Timeout.Add (1000, new GLib.TimeoutHandler (IncreaseDenyCountdown));

			return base.Run();
		}
		
		private bool IncreaseDenyCountdown ()
		{
			denyKeyButtonLabel.Text = string.Format ("Deny Key ({0})", secondsLeft);
			secondsLeft --;

			if (secondsLeft == 0) {
				base.Dialog.Respond ((int)Gtk.ResponseType.Cancel);
				return false;
			} else {
				return true;
			}
		}

		private void on_btnOK_clicked (object o, EventArgs e)
		{
			if (Gui.ShowMessageDialog ("Are you absolutley sure you want to add this node to your trusted nodes list using this key?", base.Dialog, Gtk.MessageType.Question, ButtonsType.YesNo) == (int)ResponseType.Yes) {
				base.Dialog.Respond ((int)Gtk.ResponseType.Ok);			
			} else {
				Gui.ShowMessageDialog ("No key was added.", base.Dialog);
				base.Dialog.Respond ((int)Gtk.ResponseType.Cancel);
			}
		}

		private void on_btnCancel_clicked (object o, EventArgs e)
		{
			base.Dialog.Respond ((int)Gtk.ResponseType.Cancel);
		}
	}
}
