//
// winBadOptions.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using Gtk;
using Glade;
using System.Collections;

namespace FileFind.Meshwork.GtkClient
{
	public class BadOptionsDialog
	{
		[Widget] TreeView badOptionsTree;

		TreeStore badOptionsTreeStore;

		Dialog dialog; 
		 
		public BadOptionsDialog (Gtk.Window parent, ArrayList badOptions)
		{
			Glade.XML winXml = new Glade.XML (null, "FileFind.Meshwork.GtkClient.meshwork.glade","BadOptionsDialog",null);
			winXml.Autoconnect (this);
			dialog = (Dialog) winXml.GetWidget ("BadOptionsDialog");
			dialog.TransientFor = parent;
		
			badOptionsTreeStore = new TreeStore (typeof (string));
			badOptionsTree.Model = badOptionsTreeStore;
			badOptionsTree.AppendColumn ("Name", new CellRendererText (), "text", 0);

			badOptionsTree.Selection.SelectFunction = new TreeSelectionFunc (SelectFunc);
			
			for (int x = 0; x < badOptions.Count; x++) 
				badOptionsTreeStore.AppendValues (new string[] {badOptions[x].ToString()});
		}
		
		public void Show ()
		{
			dialog.Show ();
			dialog.Run ();
		}

		private void on_btnOk_clicked (object sender, EventArgs e)
		{
			dialog.Hide ();
		//	dialog.Destroy ();
		}

		private bool SelectFunc (TreeSelection selection, TreeModel model, TreePath path, bool path_currently_selected)
		{
			return false;
		}
	}
}
