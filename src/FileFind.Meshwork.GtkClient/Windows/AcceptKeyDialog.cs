//
// AcceptKeyDialog.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.Security.Cryptography;
using Gtk;
using Glade;
using System.IO;
using System.Net;
using FileFind;
using FileFind.Meshwork;
using FileFind.Meshwork.Collections;
using FileFind.Meshwork.Exceptions;
using FileFind.Serialization;
using FileFind.Meshwork.GtkClient;
using FileFind.Meshwork.Protocol;

namespace FileFind.Meshwork.GtkClient
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
		
		RSACryptoServiceProvider provider;
		
		public AcceptKeyDialog (Network network, ReceivedKeyEventArgs args) : base (null, "AcceptKeyDialog")
		{
			provider = new RSACryptoServiceProvider ();
			provider.FromXmlString(args.Key.Key);
			keyTextView.Buffer.Text = KeyFunctions.MakePublicKeyBlock (nicknameLabel.Text, null, provider.ToXmlString (false));
			string nodeID = FileFind.Common.MD5(provider.ToXmlString (false));
			nodeIdLabel.Text = nodeID;

			if (args.Node != null)  {
				if (nodeID.ToUpper() != args.Node.NodeID.ToUpper()) {
					throw new Exception ("The key recieved does not match this user!");
				}
				nicknameLabel.Text = args.Node.NickName;
			} else if (args.Connection != null) {
				var conn = args.Connection;
				
				connectionLabel.Text = String.Format("{0} ({1})", conn.RemoteAddress, conn.Incoming ? "Incoming" : "Outgoing");
				nicknameLabel.Text = args.Key.Info;
				
				connectionLabel.Show();
				connectionTitleLabel.Show();
			} else {
				nicknameLabel.Text = args.Key.Info;
			}
			
			denyKeyButtonLabel.Text = String.Format ("Deny Key ({0})", secondsLeft);

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
			denyKeyButtonLabel.Text = String.Format ("Deny Key ({0})", secondsLeft);
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
