//
// FileTransferWindow.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2006-2008 Meshwork Authors
//

using Gtk;
using Glade;
using System;
using Meshwork.Backend.Feature.FileTransfer;

namespace Meshwork.Client.GtkClient.Windows
{
	public class FileTransferWindow : GladeWindow
	{
		[Widget] Label fileNameLabel;
		[Widget] Label fileSizeLabel;
		[Widget] Label timeElapsedLabel;
		[Widget] Label timeRemainingLabel;
		[Widget] Label downloadedLabel;
		[Widget] Label uploadedLabel;
		[Widget] Label downloadSpeedLabel;
		[Widget] Label uploadSpeedLabel;

		[Widget] ProgressBar progressBar;
		
		[Widget] Button cancelButton;
		
		[Widget] ToggleButton pauseButton;

		[Widget] TreeView peersTreeView;
		
		IFileTransfer transfer;

		ListStore peerListStore;

		bool keepGoing = true;

		public FileTransferWindow (IFileTransfer transfer) : base("FileTransferWindow")
		{
			this.transfer = transfer;

			fileNameLabel.Text = transfer.File.Name;
			fileSizeLabel.Text = Common.Utils.FormatBytes(transfer.File.Size);

			peersTreeView.AppendColumn("Node", new CellRendererText(), new TreeCellDataFunc(peersTreeView_NodeFunc));
			peersTreeView.AppendColumn("Download Speed", new CellRendererText(), new TreeCellDataFunc(peersTreeView_DownloadSpeedFunc));
			peersTreeView.AppendColumn("Upload Speed", new CellRendererText(), new TreeCellDataFunc(peersTreeView_UploadSpeedFunc));
			peersTreeView.AppendColumn("Progress", new CellRendererProgress(), new TreeCellDataFunc(peersTreeView_ProgressFunc));
			peersTreeView.AppendColumn("Status", new CellRendererText(), new TreeCellDataFunc(peersTreeView_StatusFunc));

			peerListStore = new ListStore(typeof(IFileTransferPeer));
			peersTreeView.Model = peerListStore;

			base.Closed += base_Closed;

			if (transfer.Status == FileTransferStatus.Paused) {
				pauseButton.Active = true;
			}

			pauseButton.Toggled += pauseButton_Toggled;
		}
		
		public override void Show() 
		{
			base.Show();

			UpdateWindow();
			GLib.Timeout.Add(60, new GLib.TimeoutHandler(UpdateWindow));
		}
		
		private bool UpdateWindow()
		{
			downloadedLabel.Text    = Common.Utils.FormatBytes(transfer.BytesDownloaded);
			downloadSpeedLabel.Text = $"{Common.Utils.FormatBytes(transfer.TotalDownloadSpeed)}/s";
			uploadedLabel.Text      = Common.Utils.FormatBytes(transfer.BytesUploaded);
			uploadSpeedLabel.Text   = $"{Common.Utils.FormatBytes(transfer.TotalUploadSpeed)}/s";

			string progress = $"{Math.Round(transfer.Progress, 2)}%";
			if (transfer.Progress < 0) {
				progressBar.Fraction = 0;
				progressBar.Text = $"({transfer.Status.ToString()}...)";
			} else { 
				double fraction = Math.Round(transfer.Progress * 0.01, 2);
				progressBar.Fraction = fraction;
				if (transfer.Status != FileTransferStatus.Transfering) {
					progressBar.Text = $"{transfer.Status} - {progress}";
				} else {
					progressBar.Text = progress;
				}
			}
			
			base.Window.Title = $"{transfer.Direction.ToString()} {transfer.File.Name} - {progress}";
			
			peerListStore.Clear();
			foreach (IFileTransferPeer peer in transfer.Peers) {
				peerListStore.AppendValues(peer);
			}
			
			if (transfer.Status == FileTransferStatus.Completed || transfer.Status == FileTransferStatus.Canceled) {
				cancelButton.Sensitive = false;
				pauseButton.Sensitive = false;
				//keepGoing = false;
			} 

			return keepGoing;
		}
		
		private void base_Closed (object sender, EventArgs args)
		{
			keepGoing = false;
		}
		
		private void on_cancelButton_clicked(object o, EventArgs e)
		{
			transfer.Cancel();
		}

		private void showFilePropertiesButton_clicked (object sender, EventArgs args)
		{
			FilePropertiesWindow win = new FilePropertiesWindow(transfer.File);
			win.Show();
		}

		private void peersTreeView_NodeFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			IFileTransferPeer peer = (IFileTransferPeer)model.GetValue(iter, 0);
			(cell as CellRendererText).Text = peer.Node.ToString();
		}

		private void peersTreeView_DownloadSpeedFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			IFileTransferPeer peer = (IFileTransferPeer)model.GetValue(iter, 0);
			(cell as CellRendererText).Text = $"{Common.Utils.FormatBytes(peer.DownloadSpeed)}/s";
		}

		private void peersTreeView_UploadSpeedFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			IFileTransferPeer peer = (IFileTransferPeer)model.GetValue(iter, 0);
			(cell as CellRendererText).Text = $"{Common.Utils.FormatBytes(peer.UploadSpeed)}/s";
		}

		private void peersTreeView_StatusFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			IFileTransferPeer peer = (IFileTransferPeer)model.GetValue(iter, 0);
			(cell as CellRendererText).Text = $"{peer.Status} {peer.StatusDetail}";
		}

		private void peersTreeView_ProgressFunc(TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			IFileTransferPeer peer = (IFileTransferPeer)model.GetValue(iter, 0);
			CellRendererProgress progressCell = (CellRendererProgress)cell;

			if (peer.Progress > int.MinValue && peer.Progress < int.MaxValue) {
				progressCell.Value = Convert.ToInt32(peer.Progress);
				progressCell.Text = $"{progressCell.Value}%";
			} else {
				progressCell.Value = 0;
				progressCell.Text = "Unknown";
			}
		}

		private void pauseButton_Toggled (object sender, EventArgs args)
		{
			if (pauseButton.Active == false) {
				transfer.Resume();
			} else {
				transfer.Pause();
			}
		}
	}
}
