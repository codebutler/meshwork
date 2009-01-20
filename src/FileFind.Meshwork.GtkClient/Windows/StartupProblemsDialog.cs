//
// StartupProblemsDialog.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using System;
using Gtk;
using Glade;
using FileFind.Meshwork.Transport;

namespace FileFind.Meshwork.GtkClient
{
	public class StartupProblemsDialog  : GladeDialog
	{
		[Widget] TreeView tree;

		ListStore store;

		public StartupProblemsDialog () : base (Gui.MainWindow.Window, "StartupProblemsDialog")
		{
 			store = new ListStore(typeof(string), typeof(string));

			tree.AppendColumn("Object", new CellRendererText(), "text", 0);
			tree.AppendColumn("Error", new CellRendererText(), "text", 1);
			tree.Model = store;
			
			foreach (FailedTransportListener failedListenerInfo in Core.FailedTransportListeners) {
				store.AppendValues(failedListenerInfo.Listener.ToString(), failedListenerInfo.Error.Message);
			}
		}
	}
}
