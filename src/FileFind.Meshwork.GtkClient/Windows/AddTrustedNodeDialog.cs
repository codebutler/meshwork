//
// winAddTrustedNode.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006-2008 FileFind.net (http://filefind.net)
//

using System;
using System.IO;
using FileFind.Meshwork.GtkClient.Widgets;
using Glade;
using Gtk;

namespace FileFind.Meshwork.GtkClient.Windows
{
	public class AddTrustedNodeDialog : GladeDialog
	{
		[Widget] TextView txtPublicKey;
		
		TrustedNodeInfo tni;
		
		public AddTrustedNodeDialog (Window parent) : base (parent, "AddTrustedNodeDialog")
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
					txtPublicKey.Buffer.Text = File.ReadAllText(f.Filename);
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
			DownloadPublicKeyDialog win = new DownloadPublicKeyDialog(base.Window);
			if (win.Run() == (int)ResponseType.Ok) {
				txtPublicKey.Buffer.Text = win.Result;
			}
		}
		
		private void on_btnAdd_clicked(object sender, EventArgs e)
		{
			try {
				PublicKey result = PublicKey.Parse(txtPublicKey.Buffer.Text);
				if (Common.SHA512Str(result.Key) == Core.MyNodeID)
					throw new Exception("Cannot add your own key!");
				
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
				Gui.ShowMessageDialog(string.Format("Invalid public key: {0}", ex.Message),
				                      base.Dialog, Gtk.MessageType.Error, ButtonsType.Ok);
				base.Dialog.Respond(ResponseType.None);
				return;
			}
		}
	}
}
