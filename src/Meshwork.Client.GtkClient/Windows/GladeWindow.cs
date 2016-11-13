//
// GladeWindow.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.Reflection;
using Glade;
using Gtk;

namespace Meshwork.Client.GtkClient.Windows
{
	public delegate void UrgentChangedEventHandler (GladeWindow window, bool urgent);

	public abstract class GladeWindow
	{
		protected event EventHandler Closed;
		XML    xml;
		Window window;

		public GladeWindow (string windowName) : this (null, "Meshwork.Client.GtkClient.meshwork.glade", windowName)
		{
		}

		public GladeWindow (Assembly assembly, string resourceName, string windowName)
		{
			xml = new XML (assembly, resourceName, windowName, null);
			xml.Autoconnect (this);
			window = (Window)xml[windowName];
			window.DeleteEvent += window_DeleteEvent;
		}

		public virtual void Show ()
		{
			window.Show();
			window.Present();
		}

		public virtual void Close ()
		{
			window.Hide();
			if (Closed != null) {
				Closed(this, EventArgs.Empty);
			}
		}

		public virtual bool ToggleVisible ()
		{
			return (window.Visible = !window.Visible);
		}

		public bool IsVisible {
			get {
				return window.Visible;
			}
		}
		
		public Window Window {
			get {
				return window;
			}
		}		
		
		protected void SetUrgent ()
		{
			Gui.SetWindowUrgencyHint (window, true);
		}
				
		protected void SetNotUrgent ()
		{
			Gui.SetWindowUrgencyHint (window, false);
		}
		
		protected Gdk.Pixbuf Icon {
			get {
				return window.Icon;
			}
			set {
				window.Icon = value;
			}
		}

		protected Widget GetWidget (string name)
		{
			return xml.GetWidget(name);
		}
		
		protected virtual void window_DeleteEvent (object sender, DeleteEventArgs args)
		{
			args.RetVal = true;
			Close();
		}
	}
}
