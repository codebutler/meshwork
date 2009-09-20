//
// SplashWindow.cs:
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
	public class SplashWindow : GladeWindow
	{
		[Widget] Image splashImage;
		
		public SplashWindow() : base ("SplashWindow")
		{
			splashImage.Pixbuf = new Gdk.Pixbuf(null, "FileFind.Meshwork.GtkClient.meshwork.png");
		}
		
		public new void Show()
		{
			base.Show();
			
			base.Window.QueueDraw();

			while (Application.EventsPending())
				Application.RunIteration();
		}
	}
}
