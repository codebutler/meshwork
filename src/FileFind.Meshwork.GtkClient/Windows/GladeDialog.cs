//
// GladeDialog.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using Gtk;
using Glade;

namespace FileFind.Meshwork.GtkClient
{
	public abstract class GladeDialog : GladeWindow
	{
		Dialog dialog;

		public GladeDialog (Window parentWindow, string dialogName) : base (dialogName)
		{
			dialog = (Dialog)base.Window;
			dialog.Modal = true;
			dialog.WindowPosition = WindowPosition.CenterOnParent;
			dialog.TransientFor = parentWindow;
			
			// Dialog button order is reversed on windows
			if (Environment.OSVersion.Platform != PlatformID.Unix) {
				if (dialog.ActionArea != null) {
					HButtonBox box = dialog.ActionArea;
					int count = 0;
					foreach (Widget widget in box.AllChildren) {
						box.ReorderChild (widget, ++count);
					}
				}
			}
		}

		public virtual int Run ()
		{
			dialog.Show ();
			int result = (int)ResponseType.None;
			do {
				result = dialog.Run ();
			} while (result == (int)ResponseType.None);

			OnResponded (result);
			base.Close();
			//dialog.Hide ();
			return result;
		}

		protected Dialog Dialog {
			get {
				return dialog;
			}
		}

		protected virtual void OnResponded (int responseId)
		{
			
		}
	}
}
