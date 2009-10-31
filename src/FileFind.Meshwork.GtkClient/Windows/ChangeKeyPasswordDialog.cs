using System;
namespace FileFind.Meshwork.GtkClient.Windows
{
	public class ChangeKeyPasswordDialog : GladeDialog
	{
		[Glade.Widget] Gtk.Entry oldPasswordEntry;
		[Glade.Widget] Gtk.Entry newPasswordEntry;
		[Glade.Widget] Gtk.Entry confirmPasswordEntry;
		[Glade.Widget] Gtk.Button cancelButton;
		
		public ChangeKeyPasswordDialog (Gtk.Window parentWindow) : base (parentWindow, "ChangeKeyPasswordDialog")
		{
			if (!Core.Settings.KeyEncrypted) {
				oldPasswordEntry.Sensitive = false;
			}
			
			if (Core.Settings.FirstRun) {
				cancelButton.Sensitive = false;
				base.Window.Title = "Set Password";
			}
		}
		
		void on_okButton_clicked (object o, EventArgs args)
		{
			if (Core.Settings.KeyEncrypted && !Core.Settings.CheckKeyPassword(oldPasswordEntry.Text)) {
				Gui.ShowErrorDialog("Old password incorrect");
				oldPasswordEntry.GrabFocus();
				return;
			}
			
			if (newPasswordEntry.Text != confirmPasswordEntry.Text) {
				Gui.ShowErrorDialog("New passwords do not match");
				newPasswordEntry.GrabFocus();
				return;
			}
			
			if (newPasswordEntry.Text == String.Empty) {
				var dialog = new Gtk.MessageDialog(base.Window, Gtk.DialogFlags.Modal, Gtk.MessageType.Question, Gtk.ButtonsType.YesNo, "Are you sure you don't want to set a password?");
				dialog.Show();
				int r = dialog.Run();
				dialog.Destroy();
				if (r != (int)Gtk.ResponseType.Yes) {
					return;
				}
			}
			
			Core.Settings.ChangeKeyPassword(newPasswordEntry.Text);
			
			if (!Core.Settings.FirstRun)
				Gui.ShowMessageDialog("Your password has been changed.");
			
			Dialog.Respond(Gtk.ResponseType.Ok);
		}
	}
}
