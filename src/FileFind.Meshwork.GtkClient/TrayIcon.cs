//
// TrayIcon.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// Copyright (C) 2006 FileFind.net
//

using System;
using Gtk;
using Gdk;
using Glade;

namespace FileFind.Meshwork.GtkClient
{
	public class TrayIcon
	{
		Menu          trayMenu;
		StatusIcon    statusIcon = null;

		public TrayIcon ()
		{
			Pixbuf pixbuf = new Pixbuf(null, "FileFind.Meshwork.GtkClient.tray_icon.png");
			statusIcon = new StatusIcon(pixbuf);
			statusIcon.Visible = true;
			
			trayMenu = (Menu) Runtime.UIManager.GetWidget ("/TrayPopupMenu");

			statusIcon.PopupMenu += statusIcon_PopupMenu;
			statusIcon.Activate += statusIcon_Activate;
		}

		private void statusIcon_Activate (object o, EventArgs e)
		{
			Gui.MainWindow.ToggleVisible ();
		}

		private void statusIcon_PopupMenu (object o, PopupMenuArgs args)
		{
			trayMenu.Show ();
			trayMenu.Popup ();
		}
	}
}
