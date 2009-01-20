// created on 05/31/2004 at 19:55
using System;
using Gtk;
using Glade;
using System.IO;
using System.Net;
using FileFind.Meshwork.GtkClient;

namespace FileFind.Meshwork.GtkClient
{
	public class winDownloadPublicKey
	{
		public Gtk.Dialog dialog;
		
		public string result = "";

		[Glade.Widget] public Gtk.Entry txtUrl;
		
	 
		public winDownloadPublicKey() {
			Glade.XML myGlade = new Glade.XML (null, "FileFind.Meshwork.GtkClient.meshwork.glade","winDownloadPublicKey",null);
		myGlade.Autoconnect (this);
		dialog = (Gtk.Dialog) myGlade.GetWidget("winDownloadPublicKey");
		}
		public int Show() {
			int result = 0;
			while (true) {
				result = dialog.Run();
				Console.WriteLine(result.ToString());
				if (result != (int)ResponseType.None) 
					break;
			}
			return result;
		}
		
		public void on_btnDownload_clicked(object sender, EventArgs e) {
			if (txtUrl.Text.Trim() != "") {
				try {
					using (WebClient web = new WebClient()) {
						byte[] b = web.DownloadData(txtUrl.Text);
						result = System.Text.Encoding.Default.GetString(b);
						dialog.Respond(ResponseType.Ok);
						dialog.Destroy();
					}
				}
				catch {
					Gui.ShowMessageDialog ("Invalid URL.", dialog, Gtk.MessageType.Error, Gtk.ButtonsType.Ok);
					dialog.Respond(ResponseType.None);
				}
			}
		}
		public void on_btnCancel_clicked(object sender, EventArgs e) {
			result = null;
			dialog.Respond(ResponseType.Cancel);
			dialog.Destroy();
		}		
	}
}
