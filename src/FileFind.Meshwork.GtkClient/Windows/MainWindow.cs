//
// MainWindow.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Gtk;
using FileFind.Meshwork;
using FileFind.Meshwork.Protocol;
using FileFind.Meshwork.Search;
using Banshee.Widgets;

namespace FileFind.Meshwork.GtkClient
{
	public class MainWindow
	{
		/* UI Widgets */
		MainSidebar sidebar;
		Window    window;
		Toolbar   toolbar;
		Statusbar statusBar;
		VBox      mainVBox;
		Image     rebuildingShareImage;
		EventBox  rebuildingShareEventBox;
		HPaned    mainPaned;

		Notebook pageNotebook;

		public MainWindow ()
		{
			ToolItem        spacerItem;
			FileSearchEntry searchEntry;
			ToolItem        searchEntryItem;
			Alignment       searchEntryBox;

			object[] attrs= Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), true);
			AssemblyTitleAttribute attr = (AssemblyTitleAttribute)attrs[0];
			AssemblyName asmName = Assembly.GetExecutingAssembly().GetName();
			
			string title = String.Format ("{0} (BETA) {1} (Protocol Version: {2})",
				                      attr.Title, asmName.Version, 
				                      Core.ProtocolVersion);
			
			// Create the interface
			window = new Window (title);
			window.SetDefaultSize (850, 550);
			window.DeleteEvent += on_win_delete;
			window.ConfigureEvent += on_MainWindow_configure_event;
			window.FocusInEvent += window_FocusInEvent;

			((ToggleAction)Runtime.BuiltinActions["ToggleMainToolbar"]).Active = Gui.Settings.ShowToolbar;
			Runtime.BuiltinActions["ToggleMainToolbar"].Activated += ToggleMainToolbar_Activated;

			((ToggleAction)Runtime.BuiltinActions["ToggleMainStatusbar"]).Active = Gui.Settings.ShowStatusBar;
			Runtime.BuiltinActions["ToggleMainStatusbar"].Activated += ToggleMainStatusbar_Activated;
			window.AddAccelGroup (Runtime.UIManager.AccelGroup);

			mainVBox = new VBox ();
			window.Add (mainVBox);
			mainVBox.Show ();

			if (Common.OSName == "Darwin") {
				MenuBar menubar = (MenuBar) Runtime.UIManager.GetWidget ("/OSXAppMenu");

				Imendio.MacIntegration.Menu.SetMenuBar(menubar);

				MenuItem preferencesItem = (MenuItem) Runtime.UIManager.GetWidget ("/OSXAppMenu/NetworkMenu/Preferences");
				MenuItem aboutItem = (MenuItem) Runtime.UIManager.GetWidget ("/OSXAppMenu/NetworkMenu/About");

				IntPtr group = Imendio.MacIntegration.Menu.AddAppMenuGroup();

				Imendio.MacIntegration.Menu.AddAppMenuItem(group, aboutItem, "About Meshwork");

				group = Imendio.MacIntegration.Menu.AddAppMenuGroup();

				Imendio.MacIntegration.Menu.AddAppMenuItem(group, preferencesItem, "Preferences");

				MenuItem quitItem = (MenuItem) Runtime.UIManager.GetWidget ("/OSXAppMenu/NetworkMenu/Quit");
				Imendio.MacIntegration.Menu.SetQuitMenuItem(quitItem);
			} else {
				MenuBar menubar = (MenuBar) Runtime.UIManager.GetWidget ("/MainWindowMenuBar");
				mainVBox.PackStart (menubar, false, false, 0);
				menubar.Show ();
			}

			toolbar = (Toolbar) Runtime.UIManager.GetWidget ("/MainWindowToolbar");
			toolbar.ToolbarStyle = ToolbarStyle.BothHoriz;
			toolbar.IconSize = IconSize.LargeToolbar;

			spacerItem = new ToolItem();
			spacerItem.Expand = true;
			toolbar.Insert(spacerItem, -1);
			spacerItem.Show();

			searchEntry = new FileSearchEntry();

			searchEntryBox = new Alignment(0.5f, 0.5f, 0, 0);
			searchEntryBox.LeftPadding = 4;
			searchEntryBox.RightPadding = 1;
			searchEntryBox.Add(searchEntry);

			searchEntryItem = new ToolItem();
			searchEntryItem.Add(searchEntryBox);

			toolbar.Insert(searchEntryItem, -1);
			searchEntryItem.ShowAll();

			mainVBox.PackStart (toolbar, false, false, 0);

			mainPaned = new HPaned();
			mainPaned.Mapped += delegate (object sender, EventArgs args) {
				// XXX: Remember the user's last setting instead
				mainPaned.Position = 190;
				
				// Set some colors
				//infoBoxSeparator.ModifyBg(StateType.Normal, GtkHelper.DarkenColor (mainbar.Style.Background(StateType.Normal), 2));
				//infoSwitcherTree.ModifyBase(StateType.Normal, infoSwitcherTree.Style.Base(StateType.Active));
				//infoSwitcherTree.ModifyBase(StateType.Active, infoBoxSeparator.Style.Base(StateType.Selected));
			};
			mainPaned.Show();
			mainVBox.PackStart (mainPaned, true, true, 0);

			// Create page notebook
			pageNotebook = new Notebook();
			pageNotebook.ShowTabs = false;
			pageNotebook.ShowBorder = false;
			mainPaned.Pack2(pageNotebook, true, true);
			pageNotebook.ShowAll();

			// Create sidebar
			sidebar = new MainSidebar();
			sidebar.ItemAdded += sidebar_ItemAdded;
			sidebar.SelectedItemChanged += sidebar_SelectedItemChanged;
			sidebar.AddBuiltinItems();

			mainPaned.Pack1(sidebar, false, false);
			sidebar.ShowAll();

			CreateStatusbar ();

			// Apply "view" settings
			toolbar.Visible = Gui.Settings.ShowToolbar;
			statusBar.Visible = Gui.Settings.ShowStatusBar;

			// Hook up Core events
			Core.ShareBuilder.StartedIndexing  += (EventHandler)DispatchService.GuiDispatch(new EventHandler(sb_StartedIndexing));
			Core.ShareBuilder.FinishedIndexing += (EventHandler)DispatchService.GuiDispatch(new EventHandler(sb_FinishedIndexing));
			Core.ShareBuilder.StoppedIndexing  += (EventHandler)DispatchService.GuiDispatch(new EventHandler(sb_StoppedIndexing));
			Core.ShareBuilder.ErrorIndexing    += (ErrorEventHandler)DispatchService.GuiDispatch(new ErrorEventHandler(sb_ErrorIndexing));
			Core.ShareHasher.QueueFinished     += (EventHandler)DispatchService.GuiDispatch(new EventHandler(sh_QueueFinished));

			Core.FileSearchManager.SearchAdded   += (FileSearchEventHandler)DispatchService.GuiDispatch(new FileSearchEventHandler(FileSearchManager_SearchAdded));
			Core.FileSearchManager.SearchRemoved += (FileSearchEventHandler)DispatchService.GuiDispatch(new FileSearchEventHandler(FileSearchManager_SearchRemoved));

			window.Resize (Gui.Settings.WindowSize.Width, Gui.Settings.WindowSize.Height);
			window.Move (Gui.Settings.WindowPosition.X, Gui.Settings.WindowPosition.Y);

			SelectedPage = NetworkOverviewPage.Instance;
		}

		private void CreateStatusbar ()
		{
			statusBar = new Statusbar ();
			mainVBox.PackStart (statusBar, false, false, 0);

			// Create the 'Rescanning shared files' widget
			rebuildingShareEventBox = new Gtk.EventBox ();
			Gtk.HBox rebuildingShareBox = new Gtk.HBox ();
			rebuildingShareImage = new Gtk.Image (Gui.LoadIcon(16, "stock_refresh"));
			rebuildingShareBox.PackStart (rebuildingShareImage, false, false, 0);
			Gtk.Label rebuildingShareLabel = new Gtk.Label ();
			rebuildingShareLabel.Markup = "<span size=\"small\">Rescanning shared files...</span>";
			rebuildingShareBox.PackStart (rebuildingShareLabel, true, true, 0);
			rebuildingShareEventBox.Add (rebuildingShareBox);
			statusBar.PackEnd (rebuildingShareEventBox, false, false, 0);

			Tooltips rebuildingShareTooltips = new Tooltips();

			rebuildingShareEventBox.MotionNotifyEvent += delegate {
				string text = null;
				if (Core.ShareHasher.Going) {
					text = String.Format("Scanning: {0}\nHashing: True ({1} remianing)", Core.ShareBuilder.Going.ToString(), Common.FormatNumber(Core.ShareHasher.FilesRemaining));
				} else {
					text = String.Format("Scanning: {0}\nHashing: False", Core.ShareBuilder.Going.ToString());
				}

				rebuildingShareTooltips.SetTip(rebuildingShareEventBox, text, String.Empty);
			};
		}

		public void Show ()
		{
			window.Show ();
			((ToggleAction) Runtime.BuiltinActions ["ToggleMainWindow"]).Active = IsVisible;
		}

		public void Iconify ()
		{
			window.Iconify ();
		}

		public ISidebarItem SelectedItem {
			get {
				return sidebar.SelectedItem;
			}
		}

		public IPage SelectedPage {
			set {
				sidebar.SelectPage(value);
			}
			get {
				return (IPage)sidebar.SelectedItem.PageWidget;
			}
		}
		
		private void ToggleMainToolbar_Activated (object sender, EventArgs args)
		{
			toolbar.Visible = ((ToggleAction)sender).Active;
			Gui.Settings.ShowToolbar = toolbar.Visible;
		}

		private void ToggleMainStatusbar_Activated (object sender, EventArgs args)
		{
			statusBar.Visible = ((ToggleAction)sender).Active;
			Gui.Settings.ShowStatusBar = statusBar.Visible;
		}

		private void sidebar_ItemAdded (MainSidebar sidebar, ISidebarItem newItem)
		{
			if (newItem.PageWidget != null) {
				/* int pageNum = */ pageNotebook.AppendPage(newItem.PageWidget, null);
				newItem.PageWidget.Show();
			}
		}

		private void sidebar_SelectedItemChanged (MainSidebar sidebar, ISidebarItem selectedItem)
		{
			if (selectedItem != null && selectedItem.PageWidget != null) {
				int pageNum = pageNotebook.PageNum(selectedItem.PageWidget);
				pageNotebook.CurrentPage = pageNum;
				pageNotebook.CurrentPageWidget.GrabFocus();
			}
		}

		public void UpdateStatusText()
		{
			statusBar.Pop(0);
			
			string text = "";

			int totalConnections = 0;
			long totalBytes      = 0;
			long totalFiles      = 0;
			int totalNodes       = 0;

			List<string> countedNodes = new List<string>();
			foreach (Network network in Core.Networks) {
				totalConnections += network.GetLocalConnections().Length;
				foreach (Node node in network.Nodes.Values) {
					if (node.IsMe || node.FinishedKeyExchange) {
						if (!countedNodes.Contains(node.NodeID)) {
							totalNodes ++;
							totalBytes += node.Bytes;
							totalFiles += node.Files;
							countedNodes.Add(node.NodeID);
						}
					}
				}
			}
			
			if (totalConnections == 0) {
				text = "You are not connected to anybody.";
			} else {
				if (totalConnections > 1)
					text = String.Format ("You are connected to {0} friends. ", totalConnections);
				else
					text = "You are connected to 1 friend. ";

				if (totalNodes > Core.Networks.Length) {
					text += String.Format ("There are a total of {0} people and {1} files ({2}) avaliable.", 
					                       totalNodes,
							       FileFind.Common.FormatNumber(totalFiles),
							       FileFind.Common.FormatBytes(totalBytes));
				}
			}

			statusBar.Push(0, text);
		}

		private void sb_ErrorIndexing (object sender, Exception ex)
		{
			// FIXME: Do something here.
		}

		private void sb_StoppedIndexing (object sender, EventArgs args)
		{
			rebuildingShareEventBox.Visible = false;
		}

		private void sb_FinishedIndexing (object sender, EventArgs args)
		{
			UpdateStatusText ();
			NetworkOverviewPage.Instance.RefreshUserList ();
			
			if (!Core.ShareHasher.Going) {
				rebuildingShareEventBox.Visible = false;
			}
		}

		private void sb_StartedIndexing (object sender, EventArgs args)
		{
			rebuildingShareEventBox.ShowAll();
		}

		private void sh_QueueFinished (object sender, EventArgs args)
		{
			rebuildingShareEventBox.Visible = false;
		}

		public bool IsVisible {
			get {
				return window.Visible;
			}
		}
		
		public bool ToggleVisible ()
		{
			if (window.Visible == true)
				window.Visible = false;
			else
				window.Visible = true;

			((ToggleAction) Runtime.BuiltinActions ["ToggleMainWindow"]).Active = IsVisible;

			return window.Visible;
		}

		public Window Window {
			get {
				return window;
			}
		}

		public void on_win_delete (object o, DeleteEventArgs e)
		{
			/*
			e.RetVal = true;
			ToggleVisible ();

			Runtime.NotifyService.NotifyTray ("Meshwork is still running!", 
					"Even though the main window is no longer visible, Meshwork is still running.\n\n" +
					"To restore the main window, click the icon that this notice is pointing to.\n\n" +
					"<a href=\"#\">Click here</a> to never show this notice again.", 
					null);

			*/

			e.RetVal = true;
			Runtime.QuitMeshwork();
		}

		private void window_FocusInEvent (object o, EventArgs args)
		{
			window.UrgencyHint = false;
			sidebar.ClearActiveUrgency();
		}

		public void RefreshCounts ()
		{
			sidebar.RefreshCounts();
		}
		
		[GLib.ConnectBefore()]
		private void on_MainWindow_configure_event(object o, ConfigureEventArgs e)
		{
			Gui.Settings.WindowSize = new System.Drawing.Size(e.Event.Width, e.Event.Height);
			Gui.Settings.WindowPosition = new System.Drawing.Point(e.Event.X, e.Event.Y);
		}

		private void FileSearchManager_SearchAdded (FileSearch search)
		{
			sidebar.AddNewSearch(search);
		}

		private void FileSearchManager_SearchRemoved (FileSearch search)
		{
			sidebar.RemoveSearch(search);
		}
	}
}
