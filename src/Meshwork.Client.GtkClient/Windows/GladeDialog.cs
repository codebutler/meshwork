//
// GladeDialog.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2006 Meshwork Authors
//

using System;
using Gtk;

namespace Meshwork.Client.GtkClient.Windows
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
			base.Show();
			
			int result = (int)ResponseType.None;
			do {
				result = dialog.Run ();
			} while (result == (int)ResponseType.None);

			OnResponded (result);
			
			base.Close();
			
			return result;
		}
		
		public override void Show ()
		{
			this.Run();
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
