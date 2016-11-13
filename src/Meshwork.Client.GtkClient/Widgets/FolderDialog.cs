//
// Author: John Luke  <jluke@cfl.rr.com>
// License: LGPL
//

using Gtk;

namespace Meshwork.Client.GtkClient.Widgets
{
	public class FolderDialog : FileSelector
	{
		public FolderDialog (string title) : base (title, FileChooserAction.SelectFolder)
		{
			this.SelectMultiple = false;
		}
	}
}
