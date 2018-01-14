//
// TransfersMenu.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2006-2008 Meshwork Authors
//

using System;
using System.Collections.Generic;
using Meshwork.Client.GtkClient.Windows;
using Gtk;
using Meshwork.Backend.Feature.FileTransfer;

namespace Meshwork.Client.GtkClient.Menus
{
	public class TransfersMenu 
	{
		Gtk.Menu menu;
		
		Gtk.TreeView  transfersList;
		IFileTransfer transfer;
		
		[Glade.Widget]
		Gtk.MenuItem mnuShowTransferDetails;
		
		[Glade.Widget]
		Gtk.MenuItem mnuPauseTransfer;
		
		[Glade.Widget]
		Gtk.MenuItem mnuResumeTransfer;
		
		[Glade.Widget]
		Gtk.MenuItem mnuCancelTransfer;
		
		[Glade.Widget]
		Gtk.MenuItem mnuCancelAndRemoveTransfer;
		
		[Glade.Widget]
		Gtk.MenuItem mnuClearFinishedFailedTransfers;
		
		
		public TransfersMenu(TreeView transfersList, IFileTransfer transfer)
		{
			Glade.XML glade = new Glade.XML(null, "Meshwork.Client.GtkClient.Resources.Glade.meshwork.glade", "TransfersMenu", null);
			glade.Autoconnect(this);
			this.menu = (Gtk.Menu) glade.GetWidget("TransfersMenu");
			
			this.transfersList = transfersList;
			this.transfer = transfer;
			
			if (transfer != null) {
				mnuCancelAndRemoveTransfer.Visible = true;
				mnuShowTransferDetails.Sensitive = true;
				mnuClearFinishedFailedTransfers.Sensitive = true;
				if (transfer.Status == FileTransferStatus.Paused) {
					mnuPauseTransfer.Visible = false;
					mnuResumeTransfer.Visible = true;
					mnuResumeTransfer.Sensitive = true;
					mnuCancelTransfer.Sensitive = true;
				} else if (transfer.Status == FileTransferStatus.Canceled || transfer.Status == FileTransferStatus.Completed) {
					mnuPauseTransfer.Sensitive = false;
					mnuResumeTransfer.Visible = false;
					mnuCancelTransfer.Sensitive = false;
				}
			} else {
				mnuCancelAndRemoveTransfer.Visible = false;
				mnuShowTransferDetails.Sensitive = false;
				mnuPauseTransfer.Sensitive = false;
				mnuResumeTransfer.Visible = false;
				mnuCancelTransfer.Sensitive = false;
			}
		}
		
		public void Popup() 
		{
			menu.Popup();
		}
		
		public void on_mnuShowTransferDetails_activate (object o, EventArgs args) 
		{
			FileTransferWindow window = new FileTransferWindow(transfer);
			window.Show();
		}
		
		public void on_mnuPauseTransfer_activate (object o, EventArgs args) 
		{
			try {
				transfer.Pause();
			} catch (Exception ex) {
				Gui.ShowErrorDialog (ex.ToString ());
			}
		}
		
		public void on_mnuResumeTransfer_activate (object o, EventArgs args)
		{
			try {
				transfer.Resume();
			} catch (Exception ex) {
				Gui.ShowErrorDialog(ex.ToString ());
			}
		}
		
		public void on_mnuCancelTransfer_activate (object o, EventArgs args)
		{
			try {
				transfer.Cancel();
			} catch (Exception ex) {
				Gui.ShowErrorDialog (ex.ToString ());
			}
		}
		
		public void on_mnuCancelAndRemoveTransfer_activate (object o, EventArgs args) 
		{
		    Runtime.Core.FileTransferManager.RemoveTransfer(transfer);
		}

		public void on_mnuClearFinishedFailedTransfers_activate (object o, EventArgs args) 
		{
			try {
				List<IFileTransfer> toRemove = new List<IFileTransfer>();
				foreach (IFileTransfer transfer in Runtime.Core.FileTransferManager.Transfers) {
					if (transfer.Status == FileTransferStatus.Canceled || transfer.Status == FileTransferStatus.Completed) {
						toRemove.Add(transfer);
					}
				}

				toRemove.ForEach(delegate (IFileTransfer transfer) { Runtime.Core.FileTransferManager.RemoveTransfer(transfer); });

			} catch (Exception ex) {
				Gui.ShowErrorDialog (ex.ToString ());
			}
		}
		
	}
}
