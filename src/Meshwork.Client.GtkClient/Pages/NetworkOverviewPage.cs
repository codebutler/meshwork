//
// NetworkOverviewPage.cs: The Network Overview Page
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2005-2008 FileFind.net
// 

using System;
using System.Threading;
using Meshwork.Client.GtkClient.Widgets;
using Meshwork.Client.GtkClient.Windows;
using Gtk;
using Meshwork.Backend.Core;
using Meshwork.Backend.Core.Protocol;
using Meshwork.Backend.Core.Transport;

namespace Meshwork.Client.GtkClient.Pages
{
	public partial class NetworkOverviewPage : HPaned, IPage
	{
		ZoomableNetworkMap map;
		EventBox sidebar;

		public event EventHandler UrgencyHintChanged;

		static NetworkOverviewPage instance;
		public static NetworkOverviewPage Instance {
			get {
				if (instance == null) {
					instance = new NetworkOverviewPage();
				}
				return instance;
			}
		}

		private NetworkOverviewPage ()
		{
			/* Build the UI */

			CreateUserList ();

			/* Create mainbar */

			Widget mapWidget = null;
			try {
				map = new ZoomableNetworkMap ();
				map.SelectedNodeChanged += map_SelectedNodeChanged;
				map.NodeDoubleClicked += map_NodeDoubleClicked;
				mapWidget = map;
			} catch (Exception ex) {
				LoggingService.LogError("Failed to load map !!!", ex);
				mapWidget = new Label("Error loading map.");
			}

			this.Pack1 (mapWidget, true, true);

			/* Create sidebar */
			sidebar = new EventBox();
			sidebar.WidthRequest = 190;

			var sidebarBox = new Gtk.VBox();
			sidebar.Add(sidebarBox);

			var headerAlign = new FadingAlignment();
			sidebarBox.PackStart(headerAlign, false, false, 0);

			var headerLabel = new Gtk.Label();
			headerLabel.Markup = "<b>Users</b>";
			headerLabel.Xalign = 0;
			headerLabel.Ypad = 6;
			headerLabel.Xpad = 6;
			headerAlign.Add(headerLabel);

			sidebarBox.PackStart(AddScrolledWindow(userList), true, true, 0);

			this.Pack2(sidebar, false, true);

			sidebar.ShowAll();

			foreach (Network network in Runtime.Core.Networks) {
				Core_NetworkAdded (network);
			}

		    Runtime.Core.TransportManager.TransportError +=
				(TransportErrorEventHandler)DispatchService.GuiDispatch(
					new TransportErrorEventHandler(Core_TransportError)
				);

		    Runtime.Core.NetworkAdded +=
				(NetworkEventHandler)DispatchService.GuiDispatch(
					new NetworkEventHandler(Core_NetworkAdded)
				);

		    Runtime.Core.NetworkRemoved +=
				(NetworkEventHandler)DispatchService.GuiDispatch(
					new NetworkEventHandler(Core_NetworkRemoved)
				);

		    Runtime.Core.AvatarManager.AvatarsChanged +=
				(EventHandler)DispatchService.GuiDispatch(
					new EventHandler(AvatarManager_AvatarsChanged)
				);

			this.ShowAll();
		}

		public bool UrgencyHint {
			get {
				return false;
			}
		}

		private void Core_NetworkAdded (Network network)
		{
			foreach (LocalNodeConnection connection in network.LocalConnections) {
				AddConnectionEventHandlers(connection);
			}

			// Add Event Handlers
			network.ReceivedKey              += network_ReceivedKey;
			network.NewIncomingConnection    += (NetworkLocalNodeConnectionEventHandler)DispatchService.GuiDispatch(new NetworkLocalNodeConnectionEventHandler(network_NewIncomingConnection));
			network.ConnectingTo             += (NetworkLocalNodeConnectionEventHandler)DispatchService.GuiDispatch(new NetworkLocalNodeConnectionEventHandler(network_ConnectingTo));
			network.UserOnline               += (NodeOnlineOfflineEventHandler)DispatchService.GuiDispatch(new NodeOnlineOfflineEventHandler(network_UserOnline));
			network.UserOffline              += (NodeOnlineOfflineEventHandler)DispatchService.GuiDispatch(new NodeOnlineOfflineEventHandler(network_UserOffline));
			network.UpdateNodeInfo           += (UpdateNodeInfoEventHandler)DispatchService.GuiDispatch(new UpdateNodeInfoEventHandler(network_UpdateNodeInfo));
			network.ConnectionUp             += (ConnectionUpDownEventHandler)DispatchService.GuiDispatch(new ConnectionUpDownEventHandler(network_ConnectionUp));
			network.ConnectionDown           += (ConnectionUpDownEventHandler)DispatchService.GuiDispatch(new ConnectionUpDownEventHandler(network_ConnectionDown));
			//network.FileOffered            += (FileOfferedEventHandler)DispatchService.GuiDispatch(new FileOfferedEventHandler(network_FileOffered));
			network.ReceivedChatInvite       += (ReceivedChatInviteEventHandler)DispatchService.GuiDispatch(new ReceivedChatInviteEventHandler(network_ReceivedChatInvite));
			network.ChatMessage              += (ChatMessageEventHandler)DispatchService.GuiDispatch(new ChatMessageEventHandler(network_ChatMessage));
			network.PrivateMessage           += (PrivateMessageEventHandler)DispatchService.GuiDispatch(new PrivateMessageEventHandler(network_PrivateMessage));
			network.ReceivedNonCriticalError += (ReceivedNonCriticalErrorEventHandler)DispatchService.GuiDispatch(new ReceivedNonCriticalErrorEventHandler(network_ReceivedNonCriticalError));
			network.ReceivedCriticalError    += (ReceivedCriticalErrorEventHandler)DispatchService.GuiDispatch(new ReceivedCriticalErrorEventHandler(network_ReceivedCriticalError));

			TreeIter iter = userListStore.AppendValues (network);
			foreach (Node node in network.Nodes.Values) {
				userListStore.AppendValues (iter, node);
			}
			userList.ExpandRow (userListStore.GetPath(iter), false);
		}

		private void Core_NetworkRemoved (Network network)
		{
			TreeIter iter;

			if (userListStore.GetIterFirst(out iter)) {
				do {
					Network thisNetwork = (Network)userListStore.GetValue(iter, 0);
					if (thisNetwork == network) {
						userListStore.Remove(ref iter);
						break;
					}
				} while (userListStore.IterNext(ref iter));
			}
		}

		private void Core_TransportError (ITransport transport, Exception ex)
		{
			// FIXME: Do anything here?
		}

		private void AvatarManager_AvatarsChanged (object sender, EventArgs args)
		{
			RefreshUserList();
			map.QueueDraw();
		}

		private void base_FocusInEvent (object o, EventArgs args)
		{
			if (map != null) {
				map.GrabFocus ();
			}
		}
		
		private Gtk.Widget AddScrolledWindow (Gtk.Widget widget)
		{
			Gtk.ScrolledWindow window = new Gtk.ScrolledWindow();
			window.Add (widget);
			return window;
		}

		public void RefreshUserList ()
		{
			userList.QueueDraw();
			userList.ColumnsAutosize();
		}

		private void mnuFileQuit_Activate(object sender, EventArgs e)
		{
			Runtime.QuitMeshwork();
		}

		private void ConnectionInfoChanged (LocalNodeConnection connection)
		{
			UpdateConnectionList();
		}

		private void network_ConnectingTo (Network network, LocalNodeConnection connection)
		{
			AddConnectionEventHandlers(connection);
		}

		private void ConnectionReady (LocalNodeConnection sender)
		{
			UpdateConnectionList();
		}

		private void network_ConnectionUp(INodeConnection c)
		{
			try {
				UpdateConnectionList();
			} catch (Exception ex) {
				LoggingService.LogError(ex);
				Gui.ShowErrorDialog (ex.ToString(), Gui.MainWindow.Window);
			}
		}

		private void network_ConnectionDown(INodeConnection c)
		{
			try {
				UpdateConnectionList();
			} catch (Exception ex) {
				LoggingService.LogError(ex);
				Gui.ShowErrorDialog (ex.ToString(), Gui.MainWindow.Window);
			}
		}

		/*
		private void network_FileOffered (Network network, FileOfferedEventArgs args)
		{
			try {
				LogManager.Current.WriteToLog (args.From.NickName + " offers to send you " + args.File.FileName);

				MessageDialog dialog = new MessageDialog (null, 
						DialogFlags.Modal, 
						Gtk.MessageType.Question, 
						ButtonsType.YesNo, 
						"{0} would like to send you the following file:\n\n{1}\n\nDo you want to accept it?", 
						args.From.ToString(), 
						args.File.FileName);

				dialog.Show ();

				if (dialog.Run() == (int)Gtk.ResponseType.Yes) {
					//network.DownloadFile (args.From, args.File.FileFullPath, args.File.File.Size);
				}

				dialog.Destroy ();
			} catch (Exception ex) {
				LoggingService.LogError(ex);
				Gui.ShowErrorDialog (ex.ToString(), Gui.MainWindow.Window);
			}
		}
		*/

		private void network_ReceivedChatInvite (Network network, Node inviteFrom, ChatRoom room, ChatInviteInfo invitation)
		{
			try {
				ChatRoomInvitationDialog dialog = new ChatRoomInvitationDialog (network, inviteFrom, room, invitation);
				dialog.Show ();
			} catch (Exception ex) {
				LoggingService.LogError(ex);
				Gui.ShowErrorDialog (ex.ToString(), Gui.MainWindow.Window);
			}
		}

		private void network_PrivateMessage(Network network, Node messageFrom, string messageText)
		{
			try {
				PrivateChatSubpage page = Gui.GetPrivateMessageWindow(messageFrom);
				if (page == null) {
					page = Gui.StartPrivateChat(network, messageFrom, false);
				}
				page.AddToChat(messageFrom, messageText);
			} catch (Exception ex) {
				LoggingService.LogError(ex);
				Gui.ShowErrorDialog (ex.ToString(), Gui.MainWindow.Window);
			}
		}

		private void network_ChatMessage(ChatRoom room, Node node, string text)
		{
			try {
				if (room.InRoom == true) {
					(room.Properties["Window"] as ChatRoomSubpage).AddToChat (node, text);
				}
			} catch (Exception ex) {
				LoggingService.LogError(ex);
				Gui.ShowErrorDialog (ex.ToString(), Gui.MainWindow.Window);
			}
		}		

		private bool network_ReceivedKey (Network network, ReceivedKeyEventArgs args)
		{
			try {
				bool accept = false;
				AutoResetEvent receiveKeyWait = new AutoResetEvent (false);
				
				Application.Invoke(delegate {
					AskAcceptKey(network, args, receiveKeyWait, ref accept);
				});
				
				receiveKeyWait.WaitOne ();
				return accept;
				
			} catch (Exception ex) {
				LoggingService.LogError(ex);
				return false;
			}
		}

		private void AskAcceptKey (Network network, ReceivedKeyEventArgs args, 
		                           AutoResetEvent receiveKeyWait, 
		                           ref bool acceptKey)
		{
			try {
				AcceptKeyDialog dialog = new AcceptKeyDialog(network, args);
				int response = dialog.Run();
				
				acceptKey = (response == (int)ResponseType.Ok);
				
				receiveKeyWait.Set ();
				
			} catch (Exception ex) {
				LoggingService.LogError(ex);
				Gui.ShowErrorDialog (ex.ToString(), Gui.MainWindow.Window);
			}
		}
			
		private void network_ReceivedNonCriticalError(Network network, Node from, MeshworkError error)
		{
			try {
				UpdateConnectionList();
			} catch (Exception ex) {
				LoggingService.LogError(ex);
				Gui.ShowErrorDialog (ex.ToString(), Gui.MainWindow.Window);
			}
		}

		private void network_ReceivedCriticalError(INodeConnection ErrorFrom, MeshworkError error)
		{
			try {
				UpdateConnectionList();
			} catch (Exception ex) {
				LoggingService.LogError(ex);
				Gui.ShowErrorDialog (ex.ToString(), Gui.MainWindow.Window);
			}
		}

		private void network_NewIncomingConnection (Network network, LocalNodeConnection c)
		{
			try {
				AddConnectionEventHandlers(c);
			} catch (Exception ex) {
				LoggingService.LogError(ex);
				Gui.ShowErrorDialog (ex.ToString(), Gui.MainWindow.Window);
			}
		}

		private void UpdateConnectionList()
		{
			ConnectionsPage.Instance.RefreshList();			
			Gui.MainWindow.UpdateStatusText();
		}	

		private void AddConnectionEventHandlers(LocalNodeConnection c)
		{
			c.ConnectionConnected   += (LocalNodeConnectionEventHandler)DispatchService.GuiDispatch(new LocalNodeConnectionEventHandler(ConnectionConnected));
			c.ConnectionClosed      += (LocalNodeConnectionEventHandler)DispatchService.GuiDispatch(new LocalNodeConnectionEventHandler(ConnectionClosed));
			c.PongReceived          += (LocalNodeConnectionEventHandler)DispatchService.GuiDispatch(new LocalNodeConnectionEventHandler(Connection_PongReceived));
			c.ConnectionReady       += (LocalNodeConnectionEventHandler)DispatchService.GuiDispatch(new LocalNodeConnectionEventHandler(ConnectionReady));
			c.ConnectionInfoChanged += (LocalNodeConnectionEventHandler)DispatchService.GuiDispatch(new LocalNodeConnectionEventHandler(ConnectionInfoChanged));
			c.ConnectionError       += (LocalNodeConnectionErrorEventHandler)DispatchService.GuiDispatch(new LocalNodeConnectionErrorEventHandler(LocalConnectionError));
		}

		private void ConnectionConnected(LocalNodeConnection s)
		{
			try {
				UpdateConnectionList ();
			} catch (Exception ex) {
				LoggingService.LogError(ex);
				Gui.ShowErrorDialog (ex.ToString(), Gui.MainWindow.Window);
			}
		}

		private void ConnectionClosed(LocalNodeConnection c)
		{
			try {
				UpdateConnectionList ();
			} catch (Exception ex) {
				LoggingService.LogError(ex);
				Gui.ShowErrorDialog (ex.ToString(), Gui.MainWindow.Window);
			}
		}

		private void Connection_PongReceived(LocalNodeConnection c)
		{
			try {
				UpdateConnectionList();
			} catch (Exception ex) {
				LoggingService.LogError(ex);
				Gui.ShowErrorDialog (ex.ToString(), Gui.MainWindow.Window);
			}
		}

		private void LocalConnectionError(LocalNodeConnection c, Exception error)
		{
			try {
				// TODO: !!!!
				//(c.Properties ["ListItem"] as ConnectionListItem).ErrorText = ex.Message;

				UpdateConnectionList ();
			} catch (Exception ex) {
				LoggingService.LogError(ex);
				Gui.ShowErrorDialog (ex.ToString(), Gui.MainWindow.Window);
			}
		}

		private void map_NodeDoubleClicked (Node selectedNode)
		{
			Gui.StartPrivateChat(selectedNode.Network, selectedNode);
		}

		private void map_SelectedNodeChanged (Node selectedNode)
		{
			SelectNode (selectedNode);
		}

		private void map_MapError(Exception ex)
		{
			LoggingService.LogError("Map Error", ex);
		}

		public bool UserListVisible {
			get {
				return sidebar.Visible;
			}
			set {
				sidebar.Visible = value;
			}
		}
	}
}
