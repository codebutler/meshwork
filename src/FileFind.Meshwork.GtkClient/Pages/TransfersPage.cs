//
// TransfersPage:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006-2007 FileFind.net (http://filefind.net)
//

using System;
using FileFind.Meshwork.GtkClient.Menus;
using Gdk;
using Gtk;

namespace FileFind.Meshwork.GtkClient.Pages
{
	public class TransfersPage : VBox, IPage
	{
		Pixbuf downloadImage;
		Pixbuf uploadImage;

		Gtk.TreeView transferList;
		Gtk.ListStore transferListStore;

		public event EventHandler UrgencyHintChanged;

		static TransfersPage instance;
		public static TransfersPage Instance {
			get {
				if (instance == null) {
					instance = new TransfersPage();
				}
				return instance;
			}
		}

		private TransfersPage ()
		{
			ScrolledWindow swindow = new ScrolledWindow();

			transferListStore = new ListStore(typeof(IFileTransfer));
			transferList = new TreeView ();
			transferList.Model = transferListStore;
			
			swindow.Add(transferList);
			this.PackStart(swindow, true, true, 0);
			swindow.ShowAll();
			
			TreeViewColumn column = transferList.AppendColumn ("", new CellRendererPixbuf(), new TreeCellDataFunc (TransferIconFunc));
			column.MinWidth = 25;

			column = transferList.AppendColumn("Name", new CellRendererText(), new TreeCellDataFunc(TransferNameFunc));
			column.Expand = true;
			column.Resizable = true;

			column = transferList.AppendColumn("Progress", new CellRendererProgress(), new TreeCellDataFunc(TransferProgressFunc));
			column.Resizable = true;
			column.MinWidth = 100;
					
			column = transferList.AppendColumn("Up Speed", new CellRendererText(), new TreeCellDataFunc(TransferUpSpeedFunc));
			column.Resizable = true;
			
			column = transferList.AppendColumn("Down Speed", new CellRendererText(), new TreeCellDataFunc(TransferDownSpeedFunc));
			column.Resizable = true;
			
			column = transferList.AppendColumn("Status", new CellRendererText(), new TreeCellDataFunc(TransferStatusFunc));
			column.Resizable = true;
			column.MinWidth = 150;
			
			transferList.RowActivated += OnTransferListRowActivated;
			transferList.ButtonPressEvent +=  OnTransferListButtonPressEvent;

			downloadImage = Gui.LoadIcon(16, "go-down");
			uploadImage = Gui.LoadIcon(16, "go-up");

			GLib.Timeout.Add (500, new GLib.TimeoutHandler (RefreshTransferList));

			/*
			Core.NetworkAdded +=
				(NetworkEventHandler)DispatchService.GuiDispatch(
					new NetworkEventHandler(Core_NetworkAdded)
				);
				*/

			Core.FileTransferManager.NewFileTransfer +=
				(FileTransferEventHandler)DispatchService.GuiDispatch(
					new FileTransferEventHandler(manager_NewFileTransfer)
				);

			Core.FileTransferManager.FileTransferRemoved +=
				(FileTransferEventHandler)DispatchService.GuiDispatch(
					new FileTransferEventHandler(manager_FileTransferRemoved)
				);
		}

		public bool UrgencyHint {
			get {
				return false;
			}
		}

		private void TransferNameFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
     		{
			IFileTransfer transfer = (IFileTransfer) model.GetValue (iter, 0);
			((CellRendererText)cell).Text = transfer.File.Name;
		}

		private void TransferUpSpeedFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
     		{
			IFileTransfer transfer = (IFileTransfer) model.GetValue (iter, 0);
			if (transfer.Status == FileTransferStatus.Transfering) {
				((CellRendererText)cell).Text = FileFind.Common.FormatBytes(transfer.TotalUploadSpeed) + "/s";
			} else {
				((CellRendererText)cell).Text = string.Empty;
			}
		}

		private void TransferDownSpeedFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
     		{
			IFileTransfer transfer = (IFileTransfer) model.GetValue (iter, 0);
			if (transfer.Status == FileTransferStatus.Transfering) {
				((CellRendererText)cell).Text = FileFind.Common.FormatBytes(transfer.TotalDownloadSpeed) + "/s";
			} else {
				((CellRendererText)cell).Text = string.Empty;
			}
		}

		private void TransferIconFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
     		{
			IFileTransfer transfer = (IFileTransfer) model.GetValue (iter, 0);

			if (transfer.Direction == FileTransferDirection.Download)
				((CellRendererPixbuf)cell).Pixbuf = downloadImage;
			else
				((CellRendererPixbuf)cell).Pixbuf = uploadImage;
		}

		private void TransferProgressFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
     		{
			IFileTransfer transfer = (IFileTransfer)model.GetValue(iter, 0);

			CellRendererProgress progressCell = (CellRendererProgress)cell;

			if (transfer.Progress >= 0) {
				progressCell.Value = Convert.ToInt32(transfer.Progress);
				progressCell.Visible = true;
			} else {
				progressCell.Value = 0;
				progressCell.Visible = false;
			}
			progressCell.Text = string.Format("{0}%", progressCell.Value);
		}

		private void TransferStatusFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
     		{
			IFileTransfer transfer = (IFileTransfer)model.GetValue (iter, 0);
			CellRendererText textCell = (CellRendererText)cell;
			
			if (!string.IsNullOrEmpty(transfer.StatusDetail)) {
				textCell.Text = string.Format("{0} - {1}", transfer.Status, transfer.StatusDetail);
			} else {
				textCell.Text = transfer.Status.ToString();
			}

			string color = null;

			switch (transfer.Status) {
				//case FileTransferStatus.Failed:
				case FileTransferStatus.Canceled:
					color = "red";
					break;
				case FileTransferStatus.Transfering:
					color = "black";
					break;
				
				case FileTransferStatus.Completed:
					color = "darkgreen";
					break;
			
				case FileTransferStatus.AllPeersBusy:
				case FileTransferStatus.Hashing:
				case FileTransferStatus.Paused:
				case FileTransferStatus.Queued:
					color =  "gold";
					break;
			}
			
			textCell.Foreground = color;		
		}

		private bool RefreshTransferList ()
		{
			transferList.QueueDraw ();
			return true;
		}

		private void OnTransferListRowActivated(object o, Gtk.RowActivatedArgs e)
		{
			// TODO
		}
		
		[GLib.ConnectBefore]
		public void OnTransferListButtonPressEvent (object o, Gtk.ButtonPressEventArgs args)
		{
			TreePath path;
			if (transferList.GetPathAtPos ((int)args.Event.X, (int)args.Event.Y, out path)) {
				transferList.Selection.SelectPath (path);
			} else {
				transferList.Selection.UnselectAll ();
			}

			if (args.Event.Button == 3) {
				TransfersMenu transfersMenu = new TransfersMenu (transferList, GetSelectedTransfer());
				transfersMenu.Popup();
			}
		}

		private IFileTransfer GetSelectedTransfer ()
		{
			Gtk.TreeIter iter;
			Gtk.TreeModel model;
			if (transferList.Selection.GetSelected (out model, out iter) == true) {
				return (IFileTransfer) model.GetValue (iter, 0);
			} else {
				return null;
			}
		}

		private void manager_NewFileTransfer(IFileTransfer transfer)
		{
			try {
				// Add transfer to list
				transferListStore.AppendValues(transfer);

				// Watch a few other events
				transfer.PeerAdded += (FileTransferPeerEventHandler)DispatchService.GuiDispatch(
					new FileTransferPeerEventHandler(transfer_PeerAdded)
				);

				transfer.Error += (FileTransferErrorEventHandler)DispatchService.GuiDispatch(
					new FileTransferErrorEventHandler(transfer_Error)
				);

				Gui.MainWindow.RefreshCounts();

			} catch (Exception ex) {
				LoggingService.LogError(ex);
				Gui.ShowErrorDialog(ex.ToString(), Gui.MainWindow.Window);
			}
		}

		private void manager_FileTransferRemoved(IFileTransfer transfer)
		{
			try {
				// Remove transfer from list
				Gtk.TreeIter iter;
				transferListStore.GetIterFirst(out iter);
				if (transferListStore.IterIsValid(iter)) {
					do {
						IFileTransfer currentItem = (IFileTransfer)transferListStore.GetValue (iter, 0);
						if (currentItem == transfer) {
							transferListStore.Remove (ref iter);
							return;
						}
					}  while (transferListStore.IterNext(ref iter));
				}

				Gui.MainWindow.RefreshCounts();

			} catch (Exception ex) {
				LoggingService.LogError(ex);
				Gui.ShowErrorDialog(ex.ToString(), Gui.MainWindow.Window);
			}
		}

		// FIXME: Nothing calls this!
		private void transfer_PeerAdded(IFileTransfer transfer, IFileTransferPeer peer)
		{
			LoggingService.LogDebug("New Transfer Peer ({0}): {1}", transfer.File.Name, peer.Node);
		}

		// FIXME: Nothing calls this!
		private void transfer_Error(IFileTransfer transfer, Exception ex)
		{
			LoggingService.LogError(string.Format("Transfer error ({0})", transfer.File.Name), ex);
		}
	}
}
