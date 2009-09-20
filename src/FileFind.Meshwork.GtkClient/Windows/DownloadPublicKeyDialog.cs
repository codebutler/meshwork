// created on 05/31/2004 at 19:55
using System;
using Gtk;
using Glade;
using System.IO;
using System.Net;
using FileFind.Meshwork.GtkClient;

namespace FileFind.Meshwork.GtkClient
{
	public class DownloadPublicKeyDialog : GladeDialog
	{
		public string result = "";

		[Widget] Entry txtUrl;		
	 
		public DownloadPublicKeyDialog (Window parentWindow) : base (parentWindow, "DownloadPublicKeyDialog")
		{
		}
		
		public string Result {
			get {
				return result;
			}
		}
		
		private void downloadButton_Clicked (object sender, EventArgs e)
		{
			if (txtUrl.Text.Trim() != "") {
				try {
					using (WebClient web = new WebClient()) {
						byte[] b = web.DownloadData(txtUrl.Text);
						result = System.Text.Encoding.Default.GetString(b);
						base.Dialog.Respond(ResponseType.Ok);
					}
				}
				catch {
					Gui.ShowMessageDialog ("Invalid URL.", base.Dialog, Gtk.MessageType.Error, Gtk.ButtonsType.Ok);
					base.Dialog.Respond(ResponseType.None);
				}
			}
		}
	}
}
