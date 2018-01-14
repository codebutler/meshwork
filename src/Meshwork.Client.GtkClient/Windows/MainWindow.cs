//
// MainWindow.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2006 Meshwork Authors
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Meshwork.Client.GtkClient.Pages;
using Meshwork.Client.GtkClient.SidebarItems;
using Meshwork.Client.GtkClient.Widgets;
using Gtk;
using Meshwork.Backend.Core;
using Meshwork.Backend.Feature.FileIndexing;
using Meshwork.Backend.Feature.FileSearch;
using ErrorEventHandler = System.IO.ErrorEventHandler;
using Meshwork.Platform.MacOS;
using Meshwork.Client.GtkClient.Platform.Mac;

namespace Meshwork.Client.GtkClient.Windows
{
	public class MainWindow
	{
		/* UI Widgets */
		MainSidebar sidebar;
		Window    window;
		Toolbar   toolbar;
		Toolbar   statusBar;
		Label statusLabel;
		VBox      mainVBox;
		HPaned    mainPaned;
		AnimatedImage taskStatusIcon;

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
			
			string title = $"{attr.Title} (BETA) {asmName.Version} (Protocol Version: {Core.ProtocolVersion})";
			
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

			if (Runtime.Core.Platform is OSXPlatform) {
				var osxApp = new GtkOSXApplication();

				var menubar = (MenuShell)Runtime.UIManager.GetWidget("/OSXMainWindowMenuBar");
				menubar.Hide();
				osxApp.SetMenuBar(menubar);

				var aboutItem = (MenuItem)Runtime.UIManager.GetWidget("/OSXMainWindowMenuBar/HelpMenu/About");
				osxApp.InsertAppMenuItem(aboutItem, 0);

				var separator = new SeparatorMenuItem();
				osxApp.InsertAppMenuItem(separator, 1);

				var preferencesItem = (MenuItem)Runtime.UIManager.GetWidget("/OSXMainWindowMenuBar/NetworkMenu/Preferences");
				osxApp.InsertAppMenuItem(preferencesItem, 2);

				osxApp.SyncMenubar();
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
			Runtime.Core.ShareBuilder.StartedIndexing  += (EventHandler)DispatchService.GuiDispatch(new EventHandler(sb_StartedIndexing));
			Runtime.Core.ShareBuilder.FinishedIndexing += (EventHandler)DispatchService.GuiDispatch(new EventHandler(sb_FinishedIndexing));
		    Runtime.Core.ShareBuilder.StoppedIndexing  += (EventHandler)DispatchService.GuiDispatch(new EventHandler(sb_StoppedIndexing));
		    Runtime.Core.ShareBuilder.ErrorIndexing    += (ErrorEventHandler)DispatchService.GuiDispatch(new ErrorEventHandler(sb_ErrorIndexing));
		    Runtime.Core.ShareHasher.StartedHashingFile += (ShareHasherTaskEventHandler)DispatchService.GuiDispatch(new ShareHasherTaskEventHandler(sh_StartedFinished));
		    Runtime.Core.ShareHasher.FinishedHashingFile += (ShareHasherTaskEventHandler)DispatchService.GuiDispatch(new ShareHasherTaskEventHandler(sh_StartedFinished));
		    Runtime.Core.ShareHasher.QueueChanged += (EventHandler)DispatchService.GuiDispatch(new EventHandler(sh_QueueChanged));

		    Runtime.Core.FileSearchManager.SearchAdded   += (FileSearchEventHandler)DispatchService.GuiDispatch(new FileSearchEventHandler(FileSearchManager_SearchAdded));
		    Runtime.Core.FileSearchManager.SearchRemoved += (FileSearchEventHandler)DispatchService.GuiDispatch(new FileSearchEventHandler(FileSearchManager_SearchRemoved));

			window.Resize (Gui.Settings.WindowSize.Width, Gui.Settings.WindowSize.Height);
			window.Move (Gui.Settings.WindowPosition.X, Gui.Settings.WindowPosition.Y);

			SelectedPage = NetworkOverviewPage.Instance;
		}

		private void CreateStatusbar ()
		{
			statusBar = new Toolbar();
			statusBar.ShowArrow = false;
			statusBar.ToolbarStyle = ToolbarStyle.BothHoriz;
			statusBar.ExposeEvent +=  StatusBarExposeEvent;
			
			statusLabel = new Label();
			statusLabel.Xalign = 0;
			statusLabel.Xpad = 6;
			
			ToolItem statusLabelItem = new ToolItem();
			Alignment statusAlign = new Alignment(0.5f, 0.5f, 1.0f, 1.0f);
			statusLabelItem.Add(statusLabel);
			statusLabelItem.Expand = true;			
			statusBar.Insert(statusLabelItem, -1);
			statusLabelItem.ShowAll();
			
			taskStatusIcon = new AnimatedImage();
			taskStatusIcon.Pixbuf = Gui.LoadIcon(22, "process-working");
			taskStatusIcon.FrameHeight = 22;
			taskStatusIcon.FrameWidth = 22;
			taskStatusIcon.Load();
			
			EventBox taskStatusIconBox = new EventBox();
			taskStatusIconBox.MotionNotifyEvent += delegate {
				UpdateTaskStatusIcon();
			};
			taskStatusIconBox.ButtonReleaseEvent += delegate {
				IndexingStatusWindow.Instance.Show();
			};
			taskStatusIconBox.SizeAllocated += delegate (object o, SizeAllocatedArgs args) {
				statusAlign.LeftPadding = (uint)args.Allocation.Width;
			};
			taskStatusIconBox.SetSizeRequest(22, 22);
			taskStatusIconBox.Add(taskStatusIcon);
			taskStatusIconBox.Show();
			
			ToolItem taskStatusIconItem = new ToolItem();
			taskStatusIconItem.Add(taskStatusIconBox);
			statusBar.Insert(taskStatusIconItem, -1);
			taskStatusIconItem.Show();
			
			mainVBox.PackStart(statusBar, false, false, 0);
			
			UpdateTaskStatusIcon();
			UpdateStatusText();
		}

		void StatusBarExposeEvent (object o, ExposeEventArgs args)
		{
			Toolbar toolbar = (Toolbar)o;
			window.Style.ApplyDefaultBackground(toolbar.GdkWindow, true, window.State, 
				                                  args.Event.Area, toolbar.Allocation.X, toolbar.Allocation.Y, 
				                                  toolbar.Allocation.Width, toolbar.Allocation.Height);

				foreach (Widget child in toolbar.Children) {               
					toolbar.PropagateExpose (child, args.Event);
				}
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

		public void UpdateStatusText ()
		{
			string text = "";

			int totalConnections = 0;
			long totalBytes      = 0;
			long totalFiles      = 0;
			int totalNodes       = 0;

			List<string> countedNodes = new List<string>();
			foreach (Network network in Runtime.Core.Networks) {
				totalConnections += network.ReadyLocalConnections.Length;
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
					text = $"You are connected to {totalConnections} friends. ";
				else
					text = "You are connected to 1 friend. ";

				if (totalNodes > Runtime.Core.Networks.Length) {
					text +=
					    $"There are a total of {totalNodes} people and {Common.Utils.FormatNumber(totalFiles)} files ({Common.Utils.FormatBytes(totalBytes)}) avaliable.";
				}
			}

			statusLabel.Text = text;
		}

		private void sb_ErrorIndexing (object sender, ErrorEventArgs errorEventArgs)
		{
			// FIXME: Do something here.
			UpdateTaskStatusIcon();
		}

		private void sb_StoppedIndexing (object sender, EventArgs args)
		{
			UpdateTaskStatusIcon();
		}

		private void sb_FinishedIndexing (object sender, EventArgs args)
		{
			UpdateStatusText();
			NetworkOverviewPage.Instance.RefreshUserList();
			UpdateTaskStatusIcon();
		}

		private void sb_StartedIndexing (object sender, EventArgs args)
		{
			UpdateTaskStatusIcon();
		}

		private void sh_QueueChanged (object sender, EventArgs args)
		{
			UpdateTaskStatusIcon();
		}
		
		private void sh_StartedFinished (ShareHasherTask task)
		{
			UpdateTaskStatusIcon();
		}

		void UpdateTaskStatusIcon ()
		{
			if (Runtime.Core.ShareBuilder.Going || Runtime.Core.ShareHasher.Going) {
				if (Runtime.Core.ShareHasher.Going) {
					taskStatusIcon.TooltipMarkup =
					    $"<b>Updating Shared Files</b>\nFiles To Hash: {Common.Utils.FormatNumber(Runtime.Core.ShareHasher.FilesRemaining)}";
				} else {
					taskStatusIcon.TooltipMarkup = "<b>Updating Shared Files</b>";
				}
				taskStatusIcon.Show();
			} else {
				taskStatusIcon.Hide();	
			}			
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
