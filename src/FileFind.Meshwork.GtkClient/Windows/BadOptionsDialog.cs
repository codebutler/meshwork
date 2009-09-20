//
// BadOptionsDialog.cs:
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
	public class BadOptionsDialog : GladeDialog
	{
		[Widget] TreeView badOptionsTree;

		TreeStore badOptionsTreeStore;

		public BadOptionsDialog (Gtk.Window parent, ArrayList badOptions) : base (parent, "BadOptionsDialog")
		{
			badOptionsTreeStore = new TreeStore (typeof (string));
			badOptionsTree.Model = badOptionsTreeStore;
			badOptionsTree.AppendColumn ("Name", new CellRendererText (), "text", 0);

			badOptionsTree.Selection.SelectFunction = new TreeSelectionFunc (SelectFunc);
			
			for (int x = 0; x < badOptions.Count; x++) 
				badOptionsTreeStore.AppendValues (new string[] {badOptions[x].ToString()});
		}
		
		private void on_btnOk_clicked (object sender, EventArgs e)
		{
			base.Dialog.Respond((int)Gtk.ResponseType.Ok);
		}

		private bool SelectFunc (TreeSelection selection, TreeModel model, TreePath path, bool path_currently_selected)
		{
			return false;
		}
	}
}
