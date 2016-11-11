using System;

namespace FileFind.Meshwork.GtkClient.Windows
{
	public class UnlockKeyDialog : GladeDialog
	{
		[Glade.Widget] Gtk.Entry passwordEntry;
		
		public UnlockKeyDialog (Gtk.Window parentWindow) : base (parentWindow, "UnlockKeyDialog")
		{			
		}
		
		void HandleOkButtonClicked (object o, EventArgs args)
		{
			if (Core.Settings.UnlockKey(passwordEntry.Text)) {
				Dialog.Respond(Gtk.ResponseType.Ok);
			} else {
				Gui.ShowErrorDialog("Incorrect Password");
			}			
			passwordEntry.Text = string.Empty;
		}
	}
}
