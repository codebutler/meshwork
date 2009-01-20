//
// MapMenu.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;

namespace FileFind.Meshwork.GtkClient
{
	public class MapMenu
	{
		Gtk.Menu mnuMap;
		
		public MapMenu ()
		{
			Glade.XML xmlMnuMap = new Glade.XML(null, "FileFind.Meshwork.GtkClient.meshwork.glade","mnuMap",null); 
			mnuMap = (xmlMnuMap.GetWidget("mnuMap") as Gtk.Menu);
			xmlMnuMap.Autoconnect(this);	
		}

		public void Popup ()
		{
			mnuMap.Popup ();
		}

			
		public void on_mnuNewConnection_activate (object o, EventArgs e) {
			ConnectDialog connectWindow = new ConnectDialog();
			connectWindow.Run ();
		}

		public void on_mnuNewMemo_activate (object o, EventArgs e)
		{
			EditMemoDialog w = new EditMemoDialog ();
			if (w.Run() != (int)Gtk.ResponseType.Cancel) {
				MemosPage.Instance.UpdateMemoList();
			}
		}

		public void on_mnuJoinChat_activate (object o, EventArgs e)
		{
			JoinChatroomDialog dialog = new JoinChatroomDialog ();		
			dialog.Run ();
		}

		public void on_mnuBrowseFiles_activate (object o, EventArgs e)
		{
			//XXX:Gui.BrowserWindow.Show();
			throw new NotImplementedException();
		}
	}
}
