//
// winSplash.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using Gtk;
using Glade;
using System.IO;
using System.Net;

namespace FileFind.Meshwork.GtkClient
{
	public class SplashWindow
	{
		Gtk.Window window;
		
		[Widget] ProgressBar progressBar;

		bool pulse = true;

		public SplashWindow()
		{
			Glade.XML winXml = new Glade.XML (null, "FileFind.Meshwork.GtkClient.meshwork.glade","winSplash",null);
			winXml.Autoconnect (this);
			window = (Gtk.Window) winXml.GetWidget("winSplash");
			(winXml["splashImage"] as Image).Pixbuf = new Gdk.Pixbuf (null, "FileFind.Meshwork.GtkClient.meshwork.png");

			ProgressPulse ();
			GLib.Timeout.Add (50, new GLib.TimeoutHandler (ProgressPulse));
		}
		
		public void Show()
		{
			window.Show();
			window.QueueDraw();

			while (Application.EventsPending())
				Application.RunIteration();
		
		}
		
		private bool ProgressPulse ()
		{
			if (pulse == true && window != null) {
						progressBar.Pulse ();
				return pulse;
			} else {
				pulse = false;
				return false;
			}
		}
		
		public void Hide()
		{
			window.Hide();
		}

		public void Destroy ()
		{	
			Hide ();
			pulse = false;
			//window.Destroy();
			window = null;
		}
	}
}
