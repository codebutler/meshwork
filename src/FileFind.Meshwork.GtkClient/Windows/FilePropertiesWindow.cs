using System;
using Gtk;
using Glade;
using FileFind.Meshwork.Filesystem;

namespace FileFind.Meshwork.GtkClient
{
	public class FilePropertiesWindow : GladeWindow
	{
		// Basic Tab
		[Widget] Label fileNameLabel;
		[Widget] Label fileTypeLabel;
		[Widget] Label fileFullPathLabel;
		[Widget] Label ownerLabel;
		[Widget] Label infoHashLabel;

		// Sources Tab
		[Widget] TreeView sourcesTreeView;
		[Widget] Button   refreshSourcesButton;

		// Pieces Tab
		[Widget] TreeView piecesTreeView;
		[Widget] Button   fetchPiecesButton;

		File file;

		ListStore sourcesListStore;
		ListStore piecesListStore;

		public FilePropertiesWindow (File file) : base ("FilePropertiesWindow")
		{
			this.file = file;

			fileNameLabel.Text     = file.Name;
			fileTypeLabel.Text     = file.Type;
			fileFullPathLabel.Text = file.FullPath;
			infoHashLabel.Text     = file.InfoHash;
			
			//XXX:
			ownerLabel.Text        = file.NodeID;

			sourcesListStore = new ListStore(typeof(Node));
			sourcesTreeView.AppendColumn("NickName", new CellRendererText(), new TreeCellDataFunc(NodeNicknameFunc));

			piecesListStore = new ListStore(typeof(string));
			piecesTreeView.AppendColumn("Hash", new CellRendererText(), new TreeCellDataFunc(PieceFunc));
				
			if (file.Pieces.Length > 0) {
				foreach (string piece in file.Pieces) {
					piecesListStore.AppendValues(piece);
				}
				fetchPiecesButton.Sensitive = false;
			} else {
				if (file.NodeID == Core.MyNodeID) {
					// Must be waiting for hashing to complete.
					// XXX: Hook into ShareHasher's finished event and automatically update UI.
					piecesListStore.AppendValues("Hashing, please wait...");
					fetchPiecesButton.Sensitive = false;
				} else {
					// Use can click fetch button.
				}
			}

			sourcesTreeView.Model = sourcesListStore;
			piecesTreeView.Model = piecesListStore;

			base.Window.TransientFor = Gui.MainWindow.Window;
			base.Window.Title = file.Name;

			piecesTreeView.GrabFocus();
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
	}
}
