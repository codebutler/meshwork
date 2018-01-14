//
// UserBrowserPage.cs: 
//
// Author:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2007 Meshwork Authors
//

using System;
using System.Collections.Generic;
using Meshwork.Client.GtkClient.Widgets;
using Meshwork.Client.GtkClient.Windows;
using Gtk;
using Meshwork.Backend.Core;
using Meshwork.Backend.Feature.FileBrowsing.Filesystem;

namespace Meshwork.Client.GtkClient.Pages
{
	public class UserBrowserPage : VBox, IPage
	{
		TreeView filesList;

		string currentPath;
		IDirectory currentDirectory;
		
		bool navigating = false;
		string navigatingTo = "";
			 
		Gdk.Pixbuf stockDirectoryPixbuf;
		Gdk.Pixbuf stockFilePixbuf;
		Gdk.Pixbuf networkIcon;
		
		NavigationBar navigationBar;

		ListStore filesListStore;

		Dictionary<string, string> selectedRows = new Dictionary<string, string>();

		Alignment waitingBoxAlignment;
		Label waitLabel;
		ProgressBar waitProgressBar;

		Menu resultPopupMenu;

		public event EventHandler UrgencyHintChanged;

		static UserBrowserPage instance;
		public static UserBrowserPage Instance {
			get {
				if (instance == null) {
					instance = new UserBrowserPage();
				}
				return instance;
			}
		}

		public UserBrowserPage () 
		{
			// Create the files tree
			filesList = new TreeView();
			filesList.RowActivated += on_filesList_row_activated;
			filesList.ButtonPressEvent += filesList_ButtonPressEvent;

			// Create the navigation bar
			Alignment navigationBarAlignment = new Alignment(0, 0, 1, 1);
			navigationBarAlignment.TopPadding = 3;
			navigationBarAlignment.BottomPadding = 3;
			navigationBar = new NavigationBar ();
			navigationBar.PathButtonClicked += on_navigationBar_PathButtonClicked;
			navigationBarAlignment.Add(navigationBar);
			base.PackStart(navigationBarAlignment, false, false, 0);
			navigationBarAlignment.ShowAll ();
			
			// Load some images
			stockDirectoryPixbuf = Gui.LoadIcon(16, "folder");
			stockFilePixbuf      = Gui.LoadIcon(16, "text-x-generic");
			networkIcon          = Gui.LoadIcon(16, "stock_internet");
	
			// Set up the file list 
			filesList.Selection.Changed += filesList_Selection_Changed;
			filesList.Selection.Mode = SelectionMode.Browse;
			/*
			filesList.Selection.Mode = SelectionMode.Multiple;
			filesList.RubberBanding = true;
			*/

			filesListStore = new ListStore (typeof(IDirectoryItem));
			filesList.Model = filesListStore;
		 
			TreeViewColumn column;

			// Add Name column
			column = new TreeViewColumn ();
			column.Title = "Name";
			column.Resizable = true; 

			Gtk.CellRendererPixbuf fileListRowIcon = new Gtk.CellRendererPixbuf();
			column.PackStart (fileListRowIcon, false);
			column.SetCellDataFunc (fileListRowIcon, new TreeCellDataFunc(FileNameIconFunc));

			Gtk.CellRendererText fileListRowText = new Gtk.CellRendererText();
			column.PackStart (fileListRowText, true);
			column.SetCellDataFunc (fileListRowText, new TreeCellDataFunc(FileNameTextFunc));

			filesList.AppendColumn (column);

			// Add Size Column
			column = filesList.AppendColumn ("Size", new CellRendererText(), new TreeCellDataFunc (FileSizeFunc));
		       	column.Resizable = true; 
		
			// Add Type Column
			column = filesList.AppendColumn ("Type", new CellRendererText(), new TreeCellDataFunc (FileTypeFunc));
			column.Resizable = true; 

			// Add InfoHash Column
			column = filesList.AppendColumn ("Info Hash", new CellRendererText(), new TreeCellDataFunc (FileInfoHashFunc));
			column.Resizable = true; 

			base.PackStart(Gui.AddScrolledWindow(filesList), true, true, 0);
			filesList.ShowAll();
		
			waitingBoxAlignment = new Alignment(0.5f, 0.5f, 0, 0);
			VBox waitingBox = new VBox();
			waitProgressBar = new ProgressBar();
			waitLabel = new Label();
			waitingBox.PackStart(waitProgressBar, false, false, 0);
			waitingBox.PackStart(waitLabel, false, false, 0);
			waitingBoxAlignment.Add(waitingBox);
			this.PackStart(waitingBoxAlignment, true, true, 0);

		    Runtime.Core.NetworkAdded += Core_NetworkAdded;
			foreach (Network network in Runtime.Core.Networks) {
				Core_NetworkAdded (network);
			}

			resultPopupMenu = new Menu();
			
			ImageMenuItem item = new ImageMenuItem("Download");
			item.Image = new Image(Gui.LoadIcon(16, "go-down"));
			item.Activated += on_mnuFileDownload_activate;
			resultPopupMenu.Append(item);

			item = new ImageMenuItem(Gtk.Stock.Properties, null);
			item.Activated += filePropertiesMenuItem_Activated;
			resultPopupMenu.Append(item);

			NavigateTo ("/");
		}

		public bool UrgencyHint {
			get {
				return false;
			}
		}

		private void Core_NetworkAdded (Network network)
		{
			network.ReceivedNonCriticalError += (ReceivedNonCriticalErrorEventHandler)DispatchService.GuiDispatch(new ReceivedNonCriticalErrorEventHandler(network_ReceivedNonCriticalError));
			
			network.UserOnline += network_UserOnlineOffline;
			network.UserOffline += network_UserOnlineOffline;
			
			if (currentDirectory == Runtime.Core.FileSystem.RootDirectory) {
				Application.Invoke(delegate {
					Refresh();
				});
			}
		}
		
		private void network_UserOnlineOffline (Network network, Node node)
		{
			if (currentDirectory is NetworkDirectory && ((NetworkDirectory)currentDirectory).Network == network) {
				Application.Invoke(delegate {
					Refresh();
				});
			}			
		}

		private void network_ReceivedNonCriticalError (Network network, Node from, MeshworkError error)
		{
			if (error is DirectoryNotFoundError)
			{
				string errorPath = ((DirectoryNotFoundError)error).DirPath;
				errorPath = errorPath.Substring(1);

				// FIXME: errorPath doesn't have network part, navigatingTo does!!
				if (true)
				//if (errorPath == navigatingTo)
				{
					Gui.ShowErrorDialog("Directory not found");

					StopNavigating();

					// FIXME: Maybe something should reset the state on the directory object
				}
			}
		}
		
		private void FileNameIconFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			IDirectoryItem item = (IDirectoryItem) model.GetValue (iter, 0);

			if (item is IDirectory) {
				if (item is NetworkDirectory) {
					(cell as CellRendererPixbuf).Pixbuf = networkIcon;
				} else if (item is NodeDirectory || item is MyDirectory) {
					var nodeId = (item is NodeDirectory) ? ((NodeDirectory)item).Node.NodeID : Runtime.Core.MyNodeID;
					var avatar = Gui.AvatarManager.GetMiniAvatar(nodeId);
					(cell as CellRendererPixbuf).Pixbuf = avatar;
				} else {
					(cell as CellRendererPixbuf).Pixbuf = stockDirectoryPixbuf;
				}
			} else {
				/*
				 //ALREADY DOWNLOADING
				foreach (ITransferItem transfer in network.Transfers) {
					if (transfer.RemotePath == item.FullPath) {
						(cell as CellRendererPixbuf).Pixbuf = ;
						return;
					}
				}*/
				(cell as CellRendererPixbuf).Pixbuf = stockFilePixbuf;
			}
		}
		
		private void FileNameTextFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			IDirectoryItem item = (IDirectoryItem)model.GetValue (iter, 0);
			
			if (item is IDirectory) {
				if (item is MyDirectory) {
					(cell as CellRendererText).Text = "My Shared Files";
				} else if (item is NodeDirectory) {
					Node node = ((NodeDirectory)item).Node;
					if (node != null) {
						(cell as CellRendererText).Text = node.NickName;
					} else {
						(cell as CellRendererText).Text = "<error>";
					}
				} else if (item is NetworkDirectory) {
					Network network = ((NetworkDirectory)item).Network;
					if (network != null) {
						(cell as CellRendererText).Text = network.NetworkName;
					} else {
						(cell as CellRendererText).Text = "AAHHH";
					}
				} else {
					(cell as CellRendererText).Text = item.Name;
				}
			} else {
				(cell as CellRendererText).Text = item.Name;
			}
		}

		private void FileSizeFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			IDirectoryItem item = (IDirectoryItem) model.GetValue (iter, 0);
			//if (item is IDirectory & item.Parent.Parent == Core.FileSystem.RootDirectory)
			//	(cell as CellRendererText).Text = network.Nodes [item.Name].GetAmountSharedString ();
			//else
			if (item is IDirectory) {
				if (item is LocalDirectory || (item is RemoteDirectory && ((RemoteDirectory)item).State == RemoteDirectoryState.ContentsReceived)) {
					(cell as CellRendererText).Text = string.Format("{0} items", item.Size.ToString());
				} else {
					(cell as CellRendererText).Text = string.Empty;
				}
			} else if (item is IFile) {
				(cell as CellRendererText).Text = Common.Utils.FormatBytes(item.Size);
			}
		}

		private void FileTypeFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			IDirectoryItem item = (IDirectoryItem) model.GetValue (iter, 0);
			(cell as CellRendererText).Text = item.Type;
		}

		private void FileInfoHashFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			IDirectoryItem item = (IDirectoryItem) model.GetValue (iter, 0);
			if (item is IFile) {
				(cell as CellRendererText).Text = ((IFile)item).InfoHash;
			} else {
				(cell as CellRendererText).Text = string.Empty;
			}
		}

		private void base_FocusInEvent (object o, EventArgs args)
		{
			filesList.GrabFocus ();
		}
		
		public void on_mnuFileDownload_activate (object o, EventArgs e)
		{
			try {
				TreeIter iter;
				if (filesList.Selection.GetSelected (out iter) == true) {
					IDirectoryItem thisItem = (IDirectoryItem)filesListStore.GetValue (iter, 0);
					DownloadItem(thisItem);
				} else {
					Gui.ShowMessageDialog ("Nothing selected");
				}
			} catch (Exception ex) {
				LoggingService.LogError(ex);
				Gui.ShowErrorDialog(ex.Message);
			}
		}
		
		public void on_mnuViewRefresh_activate (object o, EventArgs e)
		{
			Refresh ();
		}
		
		private void filesList_Selection_Changed (object o, EventArgs args)
		{
			if (filesList.Selection.GetSelectedRows().Length > 0) {
				TreePath path = filesList.Selection.GetSelectedRows()[0];
	
				if (!selectedRows.ContainsKey(currentPath))
					selectedRows.Add(currentPath, path.ToString());
				else
					selectedRows[currentPath] = path.ToString();
			}
		}
		
		private void on_filesList_row_activated (object o, RowActivatedArgs e) 
		{
			try {
				TreeIter iter;
				if (filesListStore.GetIter (out iter, e.Path) == true) {
				
					IDirectoryItem thisItem = (IDirectoryItem)filesListStore.GetValue (iter, 0);
					
					if (thisItem is IDirectory) { 
						/*
						if (selectedRows [currentPath] == null) 
							selectedRows.Add (currentPath, e.Path.ToString ());
						else
							selectedRows [currentPath] = e.Path.ToString ();
						*/

						NavigateTo(PathUtil.Join(currentPath, thisItem.Name));
					} else {
						DownloadItem(thisItem);
					}	
				}
			} catch (Exception ex) {
				LoggingService.LogError(ex);
				Gui.ShowErrorDialog(ex.Message);
			}
		}

		[GLib.ConnectBefore]
		private void filesList_ButtonPressEvent (object sender, ButtonPressEventArgs args)
		{
			TreePath path;

			if (filesList.GetPathAtPos ((int)args.Event.X, (int)args.Event.Y, out path)) {
				filesList.Selection.SelectPath (path);
			} else {
				filesList.Selection.UnselectAll ();
			}

			IDirectoryItem item = GetSelectedItem();

			if (args.Event.Button == 3) {
				if (item is IFile) {
					resultPopupMenu.ShowAll();
					resultPopupMenu.Popup();
				}
			}
		}

		private IDirectoryItem GetSelectedItem ()
		{
			TreeIter iter;
			if (filesList.Selection.GetSelected(out iter)) {
				return (IDirectoryItem)filesListStore.GetValue (iter, 0);
			} else {
				return null;
			}
		}
		
		public void Refresh() {
			NavigateTo(currentPath);
		}
		
		public void NavigateUp() {
			if (currentDirectory.Parent != null)
				NavigateTo(currentDirectory.Parent.FullPath);
		}
		
		public void NavigateTo (string path)
		{
			if (string.IsNullOrEmpty (path)) {
				throw new ArgumentNullException("path");
			}
			
			try {				
				if (selectedRows.ContainsKey(navigatingTo))
						selectedRows.Remove(navigatingTo);
						
				navigatingTo = path;
				navigating = true;
				
				waitLabel.Text = "Loading Directory...";
				filesList.Parent.Visible = false;
				waitingBoxAlignment.ShowAll();
				GLib.Timeout.Add (50, new GLib.TimeoutHandler (PulseProgressBar));
				
			    Runtime.Core.FileSystem.BeginGetDirectory(path, delegate (IDirectory directory) {
					Application.Invoke(delegate {
						GotDirectory(path, directory);
					});
				});
				
			} catch (Exception ex) {
				LoggingService.LogError(ex.ToString());
				Gui.ShowErrorDialog(ex.Message);
			}
		}
		
		void GotDirectory (string path, IDirectory directory)
		{
			try {		
				if (directory != null) {
					currentDirectory = directory;
					currentPath = directory.FullPath;
					
					navigationBar.SetLocation(currentPath);
					
					filesListStore.Clear();

					foreach (IDirectory currentSubDirectory in directory.Directories) 
						filesListStore.AppendValues(currentSubDirectory);
					
					foreach (IFile currentFile in directory.Files)
						filesListStore.AppendValues(currentFile);
				
					if (selectedRows.ContainsKey(path)) {
						filesList.Selection.Changed -= filesList_Selection_Changed;
						
						TreePath treePath = new TreePath(selectedRows[path].ToString());
						filesList.Selection.SelectPath(treePath);
						filesList.ScrollToCell(treePath, null, true, 0.5f, 0);
						filesList.Selection.Changed += filesList_Selection_Changed;
					}
				} else {
					Gui.ShowErrorDialog(string.Format("Directory not found: {0}.", path));
				}
				
				StopNavigating();
				filesList.QueueDraw();
				filesList.GrabFocus();
			} catch (Exception ex) {
				LoggingService.LogError(ex.ToString());
				Gui.ShowErrorDialog(ex.Message);
			}
		}
		
		private bool PulseProgressBar ()
		{
			waitProgressBar.Pulse();
			return waitingBoxAlignment.Visible;
		}

		private void on_navigationBar_PathButtonClicked (string path)
		{
			NavigateTo(path);
		}

		private void filePropertiesMenuItem_Activated (object sender, EventArgs args)
		{
			IDirectoryItem item = GetSelectedItem();
			if (item is IFile) {
				FilePropertiesWindow win = new FilePropertiesWindow((IFile)item);
				win.Show();
			}
		}

		private void DownloadItem (IDirectoryItem item)
		{
			if (item is ILocalDirectoryItem) {
				throw new Exception ("You cannot download files from yourself.");
			}

			if (item is RemoteFile) {
				RemoteFile remoteFile = (RemoteFile)item;
				Network network = remoteFile.Network;
				network.DownloadFile(remoteFile.Node, remoteFile);
			} else {
				throw new Exception("Downloading directories is not currently supported.");
			}
		}

		private void StopNavigating()
		{
			navigating = false;
			filesList.Parent.Visible = true;
			waitingBoxAlignment.Visible = false;
			// FIXME: Remove timeout
		}
	}
}
