//
// ChildWindow.cs:
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
	public delegate void UrgentChangedEventHandler (ChildWindow window, bool urgent);

	public abstract class ChildWindow
	{
		XML glade;
		Widget child;
		string title;

		
		protected event EventHandler FocusInEvent;
		protected event EventHandler Closed;
		
		public event UrgentChangedEventHandler UrgentChanged;
		public event EventHandler TitleChanged;
		bool urgent;

		public ChildWindow (string gladeName)
		{
			try {
				glade = new XML (null, "FileFind.Meshwork.GtkClient.meshwork.glade", gladeName, null);
				glade.Autoconnect (this);
				
				Window window = (Window) glade[gladeName];
				title = window.Title;
				
				if (Gui.ChildWindowManager.Mode == WindowMode.Single) {
					child = new HBox ();
					window.Child.Reparent (child);
					Gui.ChildWindowManager.AddWindow (this);
					window.Destroy ();
				} else {
					if (this is NetworkOverviewWindow) {	
						child = new HBox ();
						window.Child.Reparent (child);
						Gui.ChildWindowManager.AddWindow (this);
						window.Destroy ();
					} else {
						child = window;
						window.FocusInEvent += window_FocusInEvent;
						window.DeleteEvent += window_DeleteEvent;
					}
					//Gui.ChildWindowManager.AddWindow (this);
				}
				
			} catch (Exception ex) {
				Console.WriteLine (ex);
				throw ex;
			}
		}

		private void window_DeleteEvent (object o, DeleteEventArgs args)
		{
			args.RetVal = true;
			Close ();
		}

		private void window_FocusInEvent (object o, EventArgs args)
		{
			if (FocusInEvent != null)
				FocusInEvent (this, null);
		}

		protected Gdk.Pixbuf Icon {
			set {
				if (child is Gtk.Window)
					(child as Gtk.Window).Icon = value;
				else
					throw new NotImplementedException ();
			}
		}

		protected bool IsActive
		{
			get {
				if (child is Gtk.Window)
					return (child as Gtk.Window).IsActive;
				else
					return Gui.ChildWindowManager.ActiveWindow == this;
			}
		}
		
		public Widget Child
		{
			get 
			{
				return child;
			}
		}

		protected Gtk.Window ParentWindow {
			get {
				return (Gtk.Window) child.Toplevel;
			/*
				if (child is Gtk.Window) {
					return child;
				} else {
					return null;
				}*/
			}
		}

		public bool Urgent {
			get {
				return urgent;
			}
		}
		
		public void GrabFocus ()
		{
			if (child is Gtk.Window) {
				child.GrabFocus ();
				(child as Window).Present();
			} else
				if (FocusInEvent != null)
					FocusInEvent (this, null);
		}
		
		public string Title 
		{
			/*protected */ set {
				title = value;

				if (child is Gtk.Window)
					(child as Gtk.Window).Title = value;

				if (TitleChanged != null)
					TitleChanged (this, null);
			}
			get {
				return title;
			}
		}

		public void SetUrgent ()
		{
			if (child is Gtk.Window)
				Gui.SetWindowUrgencyHint ((Gtk.Window)child, true);

			urgent = true;
			if (UrgentChanged != null)
				UrgentChanged (this, true);
		}
		
		public void SetNotUrgent ()
		{
			if (child is Gtk.Window)
				Gui.SetWindowUrgencyHint ((Gtk.Window)child, false);

			urgent = false;
			if (UrgentChanged != null)
				UrgentChanged (this, false);
		}
	
		public virtual void Close ()
		{
			Gui.ChildWindowManager.RemoveWindow (this);
			child.Destroy ();
			if (Closed != null)
				Closed (this, null);
		}

		public virtual void Show ()
		{
			child.Show ();
			if (child is Gtk.Window) (child as Gtk.Window).Present ();
			Gui.ChildWindowManager.SelectWindow (this);
		}

		public virtual void Hide ()
		{
			child.Hide ();
		}
	}
}
