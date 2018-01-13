//
// MemoMenu.cs: Memo context menu
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2005 FileFind.net (http://filefind.net)
//

using System;
using Meshwork.Client.GtkClient.Windows;
using Glade;
using Gtk;
using Meshwork.Backend.Core;

namespace Meshwork.Client.GtkClient.Menus
{
	public class MemoMenu
	{
		[Widget] MenuItem mnuViewMemo;
		[Widget] MenuItem mnuEditMemo;
		[Widget] MenuItem mnuDeleteMemo;
		[Widget] MenuItem mnuPostMemo;
		Menu mnuMemos;
		Memo selectedMemo;

		public MemoMenu ()
		{
			Glade.XML xmlMnuMemos = new Glade.XML(null, "Meshwork.Client.GtkClient.Resources.Glade.meshwork.glade","mnuMemos",null);
			mnuMemos = (xmlMnuMemos.GetWidget("mnuMemos") as Gtk.Menu);
			xmlMnuMemos.Autoconnect(this);
		}
		
		public void Popup (Memo selectedMemo)
		{
			this.selectedMemo = selectedMemo;
			mnuMemos.Popup ();
		}
		
			
		public void on_mnuEditMemo_activate(object o, EventArgs e)
		{
			EditMemoDialog w = new EditMemoDialog (null, selectedMemo);
			if (w.Run() != (int)ResponseType.Cancel) {
				// XXX: UpdateMemoList();
			 }
		}
		
		public void on_mnuDeleteMemo_activate (object o, EventArgs e)
		{
			selectedMemo.Network.DeleteMemo (selectedMemo);
		}

		public void on_mnuPostMemo_activate (object o, EventArgs e)
		{
			EditMemoDialog editMemoDialog = new EditMemoDialog(Gui.MainWindow.Window);
			if (editMemoDialog.Run() != (int)ResponseType.Cancel) {
				// XXX: UpdateMemoList();
			}
		}

		
		public void on_mnuViewMemo_activate(object o, EventArgs e)
		{
			MemoWindow w = new MemoWindow(selectedMemo);
			w.Show();
		}
		
		public void on_mnuMemos_show(object o, EventArgs e)
		{	
			if (selectedMemo != null) {
				mnuViewMemo.Sensitive = true;			
				if (Runtime.Core.IsLocalNode(selectedMemo.Node)) {
					mnuEditMemo.Sensitive = true;	
					mnuDeleteMemo.Sensitive = true;
				} else {	
					mnuEditMemo.Sensitive = false;	
					mnuDeleteMemo.Sensitive = false;
				}
			} else {
				mnuViewMemo.Sensitive = false;
				mnuEditMemo.Sensitive = false;	
				mnuDeleteMemo.Sensitive = false;
			}
		}
	}
}
