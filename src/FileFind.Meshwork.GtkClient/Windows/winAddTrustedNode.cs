//
// winAddTrustedNode.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006-2008 FileFind.net (http://filefind.net)
//

using System;
using Gtk;
using Glade;
using System.IO;
using System.Security.Cryptography;

using FileFind;
using FileFind.Meshwork;
using FileFind.Meshwork.Collections;
using FileFind.Meshwork.Exceptions;
using FileFind.Serialization;
using FileFind.Meshwork.GtkClient;

namespace FileFind.Meshwork.GtkClient
{
	public class winAddTrustedNode : GladeDialog
	{
		[Widget] TextView txtPublicKey;
		
		TrustedNodeInfo tni;
		
		public winAddTrustedNode (Window parent) : base (parent, "winAddTrustedNode")
		{
		
		}

		public TrustedNodeInfo TrustedNodeInfo {
			get {
				return tni;
			}
		}

		private void on_btnImportPublicKey_clicked(object sender, EventArgs e)
		{
			FileSelector f = new FileSelector("Select Public Key");
			try {
				if (f.Run() == (int)ResponseType.Ok) {
					txtPublicKey.Buffer.Text = FileFind.Common.ReadAllText(f.Filename);
				}
				f.Destroy();
			}
			catch (Exception) {
				f.Destroy();
				Gui.ShowMessageDialog ("The selected file is not valid.",base.Dialog, Gtk.MessageType.Error,ButtonsType.Ok);
			}
					
		}

		private void on_btnDownloadPublicKey_clicked(object sender, EventArgs e)
		{
			winDownloadPublicKey win = new winDownloadPublicKey();
			if (win.Show() == (int)ResponseType.Ok) {
				Console.WriteLine("Setting");
				txtPublicKey.Buffer.Text = win.result;
			}
		}
		
		private void on_btnAdd_clicked(object sender, EventArgs e)
		{
			try {
				PublicKey result = KeyFunctions.ParsePublicKeyBlock(txtPublicKey.Buffer.Text);
				
				tni = new TrustedNodeInfo(result);
			
				EditFriendDialog w = new EditFriendDialog (base.Dialog, ref tni);
				int editResult = w.Run ();

				if (editResult == (int)ResponseType.Ok)  {
					base.Dialog.Respond(ResponseType.Ok);
				} else {
					base.Dialog.Respond(ResponseType.Cancel);
				}
				base.Dialog.Destroy();
			}
			catch (Exception ex) {
				Gui.ShowMessageDialog ("Invalid public key \n\n" + ex.ToString(),base.Dialog, Gtk.MessageType.Error,ButtonsType.Ok);
				base.Dialog.Respond(ResponseType.None);
				return;
			}
		}
	}
}
