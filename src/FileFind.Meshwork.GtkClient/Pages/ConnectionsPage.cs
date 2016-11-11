//
// ConnectionsPage.cs:
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2005-2007 FileFind.net
// 

using System;
using FileFind.Meshwork.GtkClient.Menus;
using Gtk;

namespace FileFind.Meshwork.GtkClient.Pages
{
	public class ConnectionsPage : VBox, IPage
	{
		TreeView   connectionList;
		ListStore  connectionListStore;
		Gdk.Pixbuf incomingPixbuf;
		Gdk.Pixbuf outgoingPixbuf;

		public event EventHandler UrgencyHintChanged;

		static ConnectionsPage instance;
		public static ConnectionsPage Instance {
			get {
				if (instance == null) {
					instance = new ConnectionsPage();
				}
				return instance;
			}
		}

		public void RefreshList ()
		{
			connectionList.QueueDraw();
		}

		private ConnectionsPage ()
		{
			ScrolledWindow swindow = new ScrolledWindow();

			connectionListStore = new ListStore (typeof(ITransport));
			connectionList = new TreeView ();
			connectionList.Model = connectionListStore;
			connectionList.HeadersVisible = true;
			connectionList.ButtonPressEvent += connectionList_ButtonPressEvent;

			incomingPixbuf = Gui.LoadIcon(16, "go-next");
			outgoingPixbuf = Gui.LoadIcon(16, "go-previous");
			
			TreeViewColumn column;
			
			column = connectionList.AppendColumn ("", new CellRendererPixbuf (), new TreeCellDataFunc (ConnectionListIconFunc));
			column.MinWidth = 25;
			
		    column = connectionList.AppendColumn ("Remote Address", new CellRendererText (), new TreeCellDataFunc (ConnectionListAddressFunc));			
			column.Resizable = true;

		    column = connectionList.AppendColumn ("Type", new CellRendererText (), new TreeCellDataFunc (ConnectionListTypeFunc));
			column.Resizable = true;

		    column = connectionList.AppendColumn ("Status", new CellRendererText (), new TreeCellDataFunc (ConnectionListStatusFunc));			
			column.Resizable = true;

			column = connectionList.AppendColumn ("Information", new CellRendererText (), new TreeCellDataFunc (ConnectionListInformationFunc));
			column.Resizable = true;
			
			swindow.Add(connectionList);
			this.PackStart(swindow, true, true, 0);
			swindow.ShowAll();

			Core.TransportManager.NewTransportAdded +=
				(TransportEventHandler)DispatchService.GuiDispatch(
					new TransportEventHandler(OnNewTransportAdded)
				);

			Core.TransportManager.TransportRemoved +=
				(TransportEventHandler)DispatchService.GuiDispatch(
					new TransportEventHandler(OnTransportRemoved)
				);

			Core.TransportManager.TransportError += 
				(TransportErrorEventHandler)DispatchService.GuiDispatch(
				    new TransportErrorEventHandler(CoreTransportManagerTransportError)
				);
		}

		public bool UrgencyHint {
			get {
				return false;
			}
		}

		private void OnNewTransportAdded (ITransport transport)
		{
			try {
				connectionListStore.AppendValues(transport);
				Gui.MainWindow.RefreshCounts();
			} catch (Exception ex) {
				LoggingService.LogError(ex);
				Gui.ShowErrorDialog(ex.ToString(), Gui.MainWindow.Window);
			}
		}

		private void OnTransportRemoved (ITransport removedTransport)
		{
			Gui.MainWindow.RefreshCounts();
			
			Gtk.TreeIter iter;
			connectionListStore.GetIterFirst (out iter);
			if (connectionListStore.IterIsValid(iter)) {
				do {
					ITransport transport = (ITransport) connectionListStore.GetValue (iter, 0);
					if (transport == removedTransport) {
						connectionListStore.Remove (ref iter);
						return;
					} 

				}  while (connectionListStore.IterNext(ref iter));
			}
		}
		
		void CoreTransportManagerTransportError (ITransport transport, Exception ex)
		{
			RefreshList();		
		}

		private void ConnectionListIconFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			ITransport transport = (ITransport) model.GetValue (iter, 0);
			if (transport.Incoming == true)
				(cell as Gtk.CellRendererPixbuf).Pixbuf = incomingPixbuf;
			else
				(cell as Gtk.CellRendererPixbuf).Pixbuf = outgoingPixbuf;
		}

		private void ConnectionListStatusFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			ITransport transport = (ITransport) model.GetValue (iter, 0);
			(cell as CellRendererText).Text = transport.State.ToString ();
			
			SetConnectionListCellBackground ((CellRendererText)cell, transport);
		}
		
		private void ConnectionListAddressFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			ITransport transport = (ITransport) model.GetValue (iter, 0);
			(cell as CellRendererText).Text = string.Format("{0} ({1})", transport.RemoteEndPoint.ToString(), Core.TransportManager.GetFriendlyName(transport.GetType()));
			
			SetConnectionListCellBackground ((CellRendererText)cell, transport);
		}

		private void ConnectionListTypeFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			ITransport transport = (ITransport) model.GetValue (iter, 0);
			(cell as CellRendererText).Text = ConnectionType.GetFriendlyName (transport.ConnectionType);
			
			SetConnectionListCellBackground ((CellRendererText)cell, transport);
		}

		private void ConnectionListInformationFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			ITransport transport = (ITransport) model.GetValue (iter, 0);
			if (transport.Operation is LocalNodeConnection) {
				LocalNodeConnection connection = (LocalNodeConnection)transport.Operation;
				if (connection.NodeRemote != null) {
					(cell as CellRendererText).Text = string.Format ("{0} on {1}, {2}, {3} ms",
					                                                 connection.NodeRemote.NickName,
											 transport.Network.NetworkName,
											 connection.ConnectionState.ToString(),
											 connection.Latency.ToString());
				} else {
					(cell as CellRendererText).Text = string.Empty;
				}
			} else if (transport.Operation is FileTransferOperation) {
				FileTransferOperation operation = (FileTransferOperation)transport.Operation;
				(cell as CellRendererText).Text = string.Format("{0} on {1}", operation.Peer.Node.NickName, operation.Transfer.File.Name);
			} else {
				(cell as CellRendererText).Text = string.Empty;
			}
			
			SetConnectionListCellBackground ((CellRendererText)cell, transport);
		}

		[GLib.ConnectBefore]
		private void connectionList_ButtonPressEvent (object o, Gtk.ButtonPressEventArgs args)
		{
			TreePath path;
			if (connectionList.GetPathAtPos ((int)args.Event.X, (int)args.Event.Y, out path))
				connectionList.Selection.SelectPath (path);
			else
				connectionList.Selection.UnselectAll ();

			if (args.Event.Button == 3) {
				ConnectionMenu menu = new ConnectionMenu (connectionList);
				menu.Popup (GetSelectedConnection ());
			}
		}

		private ITransport GetSelectedConnection()
		{
			TreeIter iter;
			TreeModel model;
			if (connectionList.Selection.GetSelected (out model, out iter) == true) {
				return (ITransport) model.GetValue (iter, 0);
			} else {
				return null;
			}
		}

		private void SetConnectionListCellBackground (CellRendererText cell, ITransport transport)
		{
			if (transport.State == TransportState.Connected)
				cell.Foreground = "darkgreen";
			else if (transport.State == TransportState.Connecting || transport.State == TransportState.Securing)
				cell.Foreground = "gold";
			else 
				cell.Foreground = "red";
		}
	}
}
