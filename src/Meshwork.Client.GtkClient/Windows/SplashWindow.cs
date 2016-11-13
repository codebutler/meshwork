//
// SplashWindow.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using Glade;
using Gtk;

namespace Meshwork.Client.GtkClient.Windows
{
	public class SplashWindow : GladeWindow
	{
		[Widget] Image splashImage;
		
		public SplashWindow() : base ("SplashWindow")
		{
			splashImage.Pixbuf = new Gdk.Pixbuf(null, "Meshwork.Client.GtkClient.meshwork.png");
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
