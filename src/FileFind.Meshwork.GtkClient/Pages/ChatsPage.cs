//
// ChatsPage.cs:
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006-2008 FileFind.net
// 

using Gtk;
using System;
using System.Collections.Generic;
using FileFind.Meshwork;

namespace FileFind.Meshwork.GtkClient 
{
	public class ChatsPage : VBox, IPage
	{
		Notebook notebook;
		TreeView chatList;
		NetworkGroupedTreeStore<ChatRoom> chatTreeStore;
		Dictionary<Widget, ChatSubpageBase> tabLabelPages;

		bool urgencyHint = false;
		public event EventHandler UrgencyHintChanged;

		static ChatsPage instance;
		public static ChatsPage Instance {
			get {
				if (instance == null) {
					instance = new ChatsPage();
				}
				return instance;
			}
		}
	
		private ChatsPage ()
		{
			base.FocusGrabbed += base_FocusGrabbed;

			tabLabelPages = new Dictionary<Widget, ChatSubpageBase>();
			
			notebook = new Notebook();
			notebook.TabPos = PositionType.Bottom;
			notebook.SwitchPage += notebook_SwitchPage;
			notebook.PageReordered += notebook_PageReordered;

			ScrolledWindow swindow = new ScrolledWindow();
			swindow.HscrollbarPolicy = PolicyType.Automatic;
			swindow.VscrollbarPolicy = PolicyType.Automatic;
			chatList = new TreeView ();
			swindow.Add(chatList);

			chatTreeStore = new NetworkGroupedTreeStore<ChatRoom>(chatList);
			chatList.Model = chatTreeStore;

			TreeViewColumn column;

			column = chatList.AppendColumn ("Room Name", new CellRendererText (),
					new TreeCellDataFunc (NameDataFunc));
			column.Expand = true;
			column.Sizing = TreeViewColumnSizing.Autosize;

			column = chatList.AppendColumn ("Users", new CellRendererText (),
					new TreeCellDataFunc (RoomUsersDataFunc));
			column.Sizing = TreeViewColumnSizing.Autosize;
		
			chatList.RowActivated += chatList_RowActivated;
			chatList.ButtonPressEvent += chatList_ButtonPressEvent;

			notebook.AppendPage(swindow, new Label("Chatroom List"));

			this.PackStart(notebook, true, true, 0);
			notebook.ShowAll();

			foreach (Network network in Core.Networks) {
				Core_NetworkAdded (network);
			}

			Core.NetworkAdded +=
				(NetworkEventHandler)DispatchService.GuiDispatch(
					new NetworkEventHandler(Core_NetworkAdded)
				);
		}

		public bool UrgencyHint {
			get {
				return urgencyHint;
			}
		}

		public int ChatCount {
			get {
				return notebook.NPages -1;
			}
		}

		public void AddPrivateChatSubpage (PrivateChatSubpage page)
		{
			Widget labelWidget = CreateTabLabel(page.Node.NickName);
			tabLabelPages[labelWidget] = page;
			AppendPage(page, labelWidget);
		}

		private void base_FocusGrabbed (object o, EventArgs args)
		{
			notebook.CurrentPageWidget.GrabFocus();
		}

		private void Core_NetworkAdded (Network network)
		{
			network.JoinedChat += (JoinPartChatEventHandler) DispatchService.GuiDispatch (new JoinPartChatEventHandler (network_JoinedChat));
			network.LeftChat += (JoinPartChatEventHandler) DispatchService.GuiDispatch (new JoinPartChatEventHandler (network_LeftChat));
		}

		private void NameDataFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			object item = model.GetValue (iter, 0);
			if (item is Network) {
				(cell as CellRendererText).Markup = "<b>" + (item as Network).NetworkName + "</b>";
			} else {
				(cell as CellRendererText).Text = (item as ChatRoom).Name;
			}
		}
	
		private void RoomUsersDataFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			object item = model.GetValue (iter, 0);
			if (item is ChatRoom) {
				ChatRoom room = (ChatRoom) item;
				if (room.Users.Count == 1)
					(cell as CellRendererText).Text = room.Users.Count.ToString () + " user";
				else
					(cell as CellRendererText).Text = room.Users.Count.ToString () + " users";
			} else {
				(cell as CellRendererText).Text = "";
			}
		}

		private ChatRoom GetSelectedChatRoom ()
		{
			TreeIter iter;
			TreeModel model;
			if (chatList.Selection.GetSelected (out model, out iter) == true) {
				return (model.GetValue (iter, 0) as ChatRoom);
			} else {
				return null;
			}
		}

		[GLib.ConnectBefore]
		private void chatList_RowActivated (object o, RowActivatedArgs args)
		{
			TreeIter iter;
			if (chatTreeStore.GetIter (out iter, args.Path) == true) {
				object item = chatTreeStore.GetValue (iter, 0);
				if (item is ChatRoom) {
					ChatRoom room = (ChatRoom)item;
					Gui.JoinChatRoom(room);
				}
			}
		}

		[GLib.ConnectBefore]
		private void chatList_ButtonPressEvent (object o, ButtonPressEventArgs args)
		{
			TreePath path;
			if (chatList.GetPathAtPos ((int)args.Event.X, (int)args.Event.Y, out path))
				chatList.Selection.SelectPath (path);
			else
				chatList.Selection.UnselectAll ();

			if (args.Event.Button == 3) {
				ChatMenu menu = new ChatMenu();
				menu.Popup(GetSelectedChatRoom());
			}
		}

		private void network_JoinedChat (Network network, ChatEventArgs args)
		{
			try {
				LogManager.Current.WriteToLog (args.Node.NickName + " has joined " + args.Room.Name);

				if (chatTreeStore.ContainsItem (network, args.Room) == false) {
					TreeIter iter = chatTreeStore.AddItem (network, args.Room);
					if (args.Node.IsMe) {
						chatList.Selection.SelectIter (iter);
						chatList.GrabFocus();
					}
				}
				
				if (args.Room.InRoom == true) {
					if (args.Room.Properties.ContainsKey("Window") == false) {
						Widget labelWidget = CreateTabLabel(args.Room.Name);
						

						ChatRoomSubpage w = new ChatRoomSubpage(args.Room);
						AppendPage(w, labelWidget);
						args.Room.Properties.Add("Window", w);
						w.GrabFocus();

						tabLabelPages[labelWidget] = w;
					} else {
						(args.Room.Properties["Window"] as ChatRoomSubpage).AddUser (args.Node);
					}
				}

				chatList.QueueDraw ();
				Gui.MainWindow.RefreshCounts();

			} catch (Exception ex) {
				LogManager.Current.WriteToLog (ex);
			}
		}
		
		private void network_LeftChat (Network network, ChatEventArgs args) 
		{
			LogManager.Current.WriteToLog (args.Node.NickName + " has left " + args.Room.Name);
		    
			if (args.Room.InRoom == true) {
				(args.Room.Properties["Window"] as ChatRoomSubpage).RemoveUser (args.Node);
			}

			if (args.Room.Users.Count == 0) {
				chatTreeStore.RemoveItem (network, args.Room);
			} else {
				chatList.QueueDraw ();
			}

			Gui.MainWindow.RefreshCounts();
		}

		private void notebook_SwitchPage(object o, SwitchPageArgs args)
		{
			notebook.CurrentPageWidget.GrabFocus();

			for (int x = 1; x < notebook.NPages; x++) {
				ChatSubpageBase page = (ChatSubpageBase)notebook.GetNthPage(x);
				page.IsActive = (page == notebook.CurrentPageWidget);
			}
		}

		private void notebook_PageReordered(object o, PageReorderedArgs args)
		{
			if (args.P1 == 0) {
				notebook.ReorderChild(args.P0, 1);
			}
		}

		private void chatSubPage_UrgencyHintChanged (object o, EventArgs args)
		{
			urgencyHint = false;
			for (int x = 1; x < notebook.NPages; x++) {
				ChatSubpageBase page = (ChatSubpageBase)notebook.GetNthPage(x);
				Label label = (Label)((Container)notebook.GetTabLabel(page)).Children[0];
				if (page.UrgencyHint == true) {
					urgencyHint = true;
					label.Markup = "<b>" + label.Text + "</b>";
				} else {
					label.Text = label.Text;
				}
			}

			if (UrgencyHintChanged != null) {
				UrgencyHintChanged(this, EventArgs.Empty);
			}
		}

		private void chatSubPage_FocusGrabbed (object o, EventArgs args)
		{
			if (notebook.CurrentPage != notebook.PageNum((Gtk.Widget)o)) {
				notebook.CurrentPage = notebook.PageNum((Gtk.Widget)o);
			}
		}

		private void chatSubPage_Destroyed (object o, EventArgs args)
		{
			Gui.MainWindow.RefreshCounts();
		}

		private void closeButton_Clicked (object o, EventArgs args)
		{
			ChatSubpageBase page = tabLabelPages[((Button)o).Parent];
			page.Close();
		}

		private void AppendPage (ChatSubpageBase w, Widget labelWidget)
		{
			notebook.AppendPage(w, labelWidget);
			notebook.SetTabReorderable(w, true);
			w.FocusGrabbed       += chatSubPage_FocusGrabbed;
			w.UrgencyHintChanged += chatSubPage_UrgencyHintChanged;
			w.Destroyed          += chatSubPage_Destroyed;
			w.Show();
		}

		private Widget CreateTabLabel (string text)
		{
			Button closeButton = new Button(new Image(Gui.LoadIcon(12, "stock-close")));
			closeButton.SetSizeRequest(17,17);
			closeButton.FocusOnClick = false;
			closeButton.CanFocus = false;
			closeButton.Relief = ReliefStyle.None;
			closeButton.Clicked += closeButton_Clicked;

			HBox labelWidget = new HBox();
			labelWidget.PackStart(new Label(text), true, true, 0);
			labelWidget.PackStart(closeButton, true, true, 0);
			labelWidget.CanFocus = false;

			labelWidget.ShowAll();
			return labelWidget;
		}
	}
}
