//
// SelectAvatarDialog.cs: Avatar selection dialog
// 
// Authors:
// 	Eric Butler <eric@extermeboredom.net>
//
// Copyright (C) 2005 FileFind.net
// 

using Gtk;
using Glade;
using System;
using System.IO;
using System.Threading;
using System.Net;

namespace FileFind.Meshwork.GtkClient
{
	public class SelectAvatarDialog : Dialog
	{
		[Widget] ProgressBar	loadingProgressBar;
		[Widget] ScrolledWindow	iconViewScrolledWindow;
		[Widget] Button		okButton;

		IconView avatarIconView;
		ListStore store;
		
		Thread gravatarThread;

		bool gravatarFinished = false;
		
		public SelectAvatarDialog (Window parent) : base ()
		{
			XML glade = new XML (null, "FileFind.Meshwork.GtkClient.meshwork.glade", 
						"SelectAvatarDialog", null);

			this.Remove (this.Child);

			glade.Autoconnect (this);

			Window window = (Window) glade ["SelectAvatarDialog"];
			this.Title = window.Title;
			Widget child = window.Child;
			child.Reparent (this);
			window.Destroy ();
			child.Show ();

			store = new ListStore (typeof (Gdk.Pixbuf), 
						typeof (string));
			
			avatarIconView = new IconView (store);
			avatarIconView.PixbufColumn = 0;
			avatarIconView.ItemActivated += avatarIconView_ItemActivated;
			avatarIconView.SelectionChanged += avatarIconView_SelectionChanged;
			avatarIconView.DragDataReceived += avatarIconView_DragDataReceived;
			avatarIconView.ButtonPressEvent += avatarIconView_ButtonPressEvent;
			
			Gtk.Drag.DestSet (avatarIconView, DestDefaults.All, 
					new TargetEntry [] { 
						new TargetEntry ("STRING", 
								0, (uint) 0)
					},
					Gdk.DragAction.Copy);

			iconViewScrolledWindow.Add (avatarIconView);

			avatarIconView.Show ();

			
			Resize (570, 400);

			GLib.Timeout.Add (50, new GLib.TimeoutHandler (PulseProgressBar));
			
			// Load images
			gravatarThread = new Thread (new ThreadStart (LoadImages));
			gravatarThread.Start ();

		}

		private bool PulseProgressBar ()
		{
			loadingProgressBar.Pulse ();
			return !gravatarFinished;
		}

		private void TimeoutGravatar ()
		{
			Thread.Sleep (2000);
			gravatarThread.Abort ();
			Application.Invoke (delegate { GotGravatarImage (null); });
		}

		private void LoadImages ()
		{

			string directoryName = "/usr/share/pixmaps/meshwork/avatars";

			if (Directory.Exists (directoryName) == true) {
				foreach (FileInfo file in new DirectoryInfo (directoryName).GetFiles()) {
					Gdk.Pixbuf pixbuf = new Gdk.Pixbuf (file.FullName);

					if (pixbuf.Width > 80 | pixbuf.Height > 80)
						pixbuf = pixbuf.ScaleSimple (80, 80, Gdk.InterpType.Hyper);

					Gtk.Application.Invoke (delegate {
							TreeIter iter = store.Insert (0);
							store.SetValue (iter, 0, pixbuf);
							store.SetValue (iter, 1, file.FullName);
							});
				}
			}

				
			// Load gravatar... timeout after a few seconds
			Thread timeoutThread = new Thread (new ThreadStart (TimeoutGravatar));
			timeoutThread.Start ();

			string hash = "b653c99616e34ee4f834da53b109ce01";
			
			WebClient client = new WebClient ();

			try {
				byte[] data = client.DownloadData (String.Format ("http://www.gravatar.com/avatar.php?gravatar_id={0}", hash));
				Application.Invoke (delegate { GotGravatarImage (data); });
			} catch (Exception ex) {
				Console.WriteLine (ex);
				Application.Invoke (delegate { GotGravatarImage (null); });
			}
		}
		 
		private void GotGravatarImage (byte[] data)
		{
			if (data != null) {
				Gdk.Pixbuf pixbuf = new Gdk.Pixbuf (data);

				TreeIter iter = store.Insert (0);
				store.SetValue (iter, 0, pixbuf);
				store.SetValue (iter, 1, "GRAVATAR");
			} 
			loadingProgressBar.Visible = false;
			gravatarFinished = true;
		}
		
		public Gdk.Pixbuf SelectedPixbuf
		{
			get
			{
				if (avatarIconView.SelectedItems.Length > 0) {
					Gtk.TreeIter iter;
					if (store.GetIter (out iter, avatarIconView.SelectedItems[0]))
						return (Gdk.Pixbuf) store.GetValue (iter, 0);
					else
						return null;
				} else {
					return null;
				}
			}
		}
		
		private void browseButton_clicked (object o, EventArgs args)
		{
			FileSelector selector = new FileSelector ("Select Image");
			selector.Show ();
			int result = selector.Run ();
			if (result == (int)Gtk.ResponseType.Ok) {
				try {
					AddFile (selector.Filename);
				} catch (Exception ex) {
					selector.Hide ();
					Gui.ShowMessageDialog (ex.Message);
					return;
				}
			}
			selector.Hide ();

		}

		private void AddFile (string fileName)
		{
			Gdk.Pixbuf pixbuf = new Gdk.Pixbuf (fileName);

			if (pixbuf.Width > 80 | pixbuf.Height > 80)
				pixbuf = pixbuf.ScaleSimple (80, 80, Gdk.InterpType.Hyper);
			TreeIter iter = store.Append ();
			store.SetValue (iter, 0, pixbuf);
			store.SetValue (iter, 1, fileName);
		}
		
		private void okButton_clicked (object o, EventArgs args)
		{
			Respond ((int)ResponseType.Ok);
		}

		private void cancelButton_clicked (object o, EventArgs args)
		{
			Respond ((int)ResponseType.Cancel);
		}

		private void avatarIconView_ItemActivated (object o, ItemActivatedArgs args)
		{
			okButton_clicked (null, null);
		}

		private void avatarIconView_SelectionChanged (object o, EventArgs args)
		{
			okButton.Sensitive = (avatarIconView.SelectedItems.Length > 0);
		}

		private void avatarIconView_DragDataReceived (object sender, DragDataReceivedArgs args)
		{
			if (args.SelectionData.Length >=0 && args.SelectionData.Format == 8) {
				try {
					string fileName = new Uri (args.SelectionData.Text.Trim ()).LocalPath;
					if (File.Exists (fileName)) {
						AddFile (fileName);
					} 
				} catch (Exception ex) {
					Gui.ShowMessageDialog (ex.Message);
				}
			}
			Gtk.Drag.Finish (args.Context, false, false, args.Time);
		}

		private void avatarIconView_ButtonPressEvent (object o, ButtonPressEventArgs args)
		{
			if (args.Event.Button == 3) {

				TreePath path = avatarIconView.GetPathAtPos ((int)args.Event.X, (int)args.Event.Y);
				if (path != null)
					avatarIconView.SelectPath (path);
				else
					avatarIconView.UnselectAll ();
				
				Gtk.Menu menu = new Gtk.Menu ();
				ImageMenuItem removeItem = new ImageMenuItem (Gtk.Stock.Remove, null);

				if (avatarIconView.SelectedItems.Length > 0) {
					removeItem.Activated += delegate { 
						TreeIter iter;
						store.GetIter (out iter, path);
						store.Remove (ref iter);
					};
			
				} else {
					removeItem.Sensitive = false;
				}
				menu.Append (removeItem);
				menu.ShowAll ();
				menu.Popup ();
			}
		}
	}
}
