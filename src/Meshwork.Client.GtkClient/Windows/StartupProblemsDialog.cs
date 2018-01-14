//
// StartupProblemsDialog.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2008 Meshwork Authors
//

using Glade;
using Gtk;
using Meshwork.Backend.Core.Transport;

namespace Meshwork.Client.GtkClient.Windows
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
			
			foreach (FailedTransportListener failedListenerInfo in Runtime.Core.FailedTransportListeners) {
				store.AppendValues(failedListenerInfo.Listener.ToString(), failedListenerInfo.Error.Message);
			}
		}
	}
}
