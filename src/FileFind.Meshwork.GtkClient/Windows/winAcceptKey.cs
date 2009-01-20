//
// winAcceptKey.cs:
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
	public class AcceptKeyDialog
	{
		private Gtk.Dialog dialog;
		
		[Widget] Label nicknameLabel;
		[Widget] Label nodeIdLabel;
		[Widget] TextView keyTextView;
		[Widget] EventBox eventbox10;
		[Widget] Label denyKeyButtonLabel;

		private Network network;
		private Node node;
		private KeyInfo key;
		private string nodeID;
		int secondsLeft = 20;
		
		RSACryptoServiceProvider provider;
		
		public AcceptKeyDialog (Network network, Node node, KeyInfo key)
		{
			Glade.XML winXml = new Glade.XML (null, "FileFind.Meshwork.GtkClient.meshwork.glade", "AcceptKeyDialog", null);
			winXml.Autoconnect (this);
			dialog = (Gtk.Dialog) winXml.GetWidget ("AcceptKeyDialog");

			this.network = network;
			this.node = node;
			this.key = key;

			provider = new RSACryptoServiceProvider ();
			provider.FromXmlString (key.Key);
			keyTextView.Buffer.Text = KeyFunctions.MakePublicKeyBlock (nicknameLabel.Text, null, provider.ToXmlString (false));
			nodeID = FileFind.Common.MD5 (provider.ToXmlString (false));
			nodeIdLabel.Text = nodeID;

			if (node != null)  {
				if (nodeID.ToUpper() != node.NodeID.ToUpper()) {
					throw new Exception ("The key recieved does not match this user!");
				}
				nicknameLabel.Text = node.NickName;
			} else {
				nicknameLabel.Text = key.Info;
			}
			
			denyKeyButtonLabel.Text = String.Format ("Deny Key ({0})", secondsLeft);

		}
		
		public int Show ()
		{
			dialog.ShowAll ();
			dialog.Resize (1,1);
			eventbox10.ModifyBg (Gtk.StateType.Normal, keyTextView.Style.Base (Gtk.StateType.Normal));

			GLib.Timeout.Add (1000, new GLib.TimeoutHandler (IncreaseDenyCountdown));

			int result = 0;
			while (true) {
				result = dialog.Run();
				if (result != (int)ResponseType.None)
					break;
			}
			dialog.Hide ();

			return result;
		}
		
		private bool IncreaseDenyCountdown ()
		{
			denyKeyButtonLabel.Text = String.Format ("Deny Key ({0})", secondsLeft);
			secondsLeft --;

			if (secondsLeft == 0) {
				dialog.Respond ((int)Gtk.ResponseType.Cancel);
				return false;
			} else {
				return true;
			}
		}

		private void on_btnOK_clicked (object o, EventArgs e)
		{
			if (Gui.ShowMessageDialog ("Are you absolutley sure you want to add this node to your trusted nodes list using this key?", dialog, Gtk.MessageType.Question, ButtonsType.YesNo) == (int)ResponseType.Yes) {
				dialog.Respond ((int)Gtk.ResponseType.Ok);			
			} else {
				Gui.ShowMessageDialog ("No key was added.", dialog);
				dialog.Respond ((int)Gtk.ResponseType.Cancel);
			}
		}

		private void on_btnCancel_clicked (object o, EventArgs e)
		{
			dialog.Respond ((int)Gtk.ResponseType.Cancel);
		}
		 
		
	}
}
