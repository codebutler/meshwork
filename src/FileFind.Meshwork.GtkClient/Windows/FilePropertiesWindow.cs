using System;
using Gtk;
using Glade;
using FileFind.Meshwork.Protocol;
using FileFind.Meshwork.Filesystem;

namespace FileFind.Meshwork.GtkClient
{
	public class FilePropertiesWindow : GladeWindow
	{
		// Basic Tab
		[Widget] Label fileNameLabel;
		[Widget] Label fileTypeLabel;
		[Widget] Label fileSizeLabel;
		[Widget] Label fileFullPathLabel;
		[Widget] Label ownerLabel;
		[Widget] Label sha1Label;
		[Widget] Label infoHashLabel;

		// Sources Tab
		[Widget] TreeView sourcesTreeView;
		[Widget] Button   refreshSourcesButton;

		// Pieces Tab
		[Widget] TreeView piecesTreeView;
		[Widget] Button   fetchPiecesButton;

		IFile file;

		ListStore sourcesListStore;
		ListStore piecesListStore;

		FilePropertiesWindow () : base ("FilePropertiesWindow")
		{
			fetchPiecesButton.Clicked += fetchPiecesButton_Clicked;

			sourcesListStore = new ListStore(typeof(Node));
			sourcesTreeView.AppendColumn("NickName", new CellRendererText(), new TreeCellDataFunc(NodeNicknameFunc));

			piecesListStore = new ListStore(typeof(string));
			piecesTreeView.AppendColumn("Hash", new CellRendererText(), new TreeCellDataFunc(PieceFunc));
			
			base.Window.TransientFor = Gui.MainWindow.Window;
			piecesTreeView.GrabFocus();
		}

		public FilePropertiesWindow (IFile file) : this ()
		{
			this.file = file;

			fileNameLabel.Text = file.Name;
			fileTypeLabel.Text = file.Type;
			fileSizeLabel.Text = Common.FormatBytes(file.Size);
			
			if (file is RemoteFile)
				fileFullPathLabel.Text = ((RemoteFile)file).RemoteFullPath;
			else
				fileFullPathLabel.Text = file.FullPath;
			
			sha1Label.Text = file.SHA1;
			infoHashLabel.Text = file.InfoHash;
			
			if (file is RemoteFile) {
				var remoteFile = (RemoteFile)file;
				ownerLabel.Text = String.Format("{0} ({1})", remoteFile.Node.NickName, 
				                                Common.FormatFingerprint(remoteFile.Node.NodeID));
			} else
				ownerLabel.Text = "You";
				
			if (file.Pieces.Length > 0) {
				foreach (string piece in file.Pieces) {
					piecesListStore.AppendValues(piece);
				}
				fetchPiecesButton.Sensitive = false;
			} else {
				if (file is LocalFile) {
					// Must be waiting for hashing to complete.
					// XXX: Hook into ShareHasher's finished event and automatically update UI.
					piecesListStore.AppendValues("Hashing, please wait...");
					fetchPiecesButton.Sensitive = false;
				}
			}

			sourcesTreeView.Model = sourcesListStore;
			piecesTreeView.Model = piecesListStore;

			base.Window.Title = file.Name;
		}
		
		private void NodeNicknameFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			Node node = (Node)model.GetValue(iter, 0);
			(cell as CellRendererText).Text = node.NickName;
		}
	
		private void PieceFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			string piece = (string)model.GetValue(iter, 0);
			(cell as CellRendererText).Text = piece;
		}

		private void closeButton_Clicked (object sender, EventArgs args)
		{
			base.Close();
		}
		
		private void fetchPiecesButton_Clicked (object sender, EventArgs args)
		{
			Core.FileSystem.BeginGetFileDetails(file.FullPath, delegate (IFile aFile) {
				Application.Invoke(delegate {
					piecesListStore.Clear();
					foreach (string piece in aFile.Pieces) {
						piecesListStore.AppendValues(piece);
					}
				});
			});
		}
	}
}
