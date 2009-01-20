//
// FileTransferManager.cs: Keeps track of ongoing file transfers
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007 FileFind.net (http://filefind.net)
//

using System;
using System.Net;
using System.Collections.Generic;
using FileFind.Meshwork.Filesystem;
using FileFind.Meshwork.Transport;
using IO = System.IO;

namespace FileFind.Meshwork.FileTransfer
{
	public delegate void FileTransferEventHandler (IFileTransfer transfer);
	public delegate void FileTransferErrorEventHandler (IFileTransfer transfer, Exception ex);
	public delegate void FileTransferPeerEventHandler (IFileTransfer transfer, IFileTransferPeer peer);

	public class FileTransferManager 
	{
		public event FileTransferEventHandler NewFileTransfer;
		public event FileTransferEventHandler FileTransferRemoved;

		IFileTransferProvider provider;

		List<IFileTransfer> transfers = new List<IFileTransfer>();

		internal FileTransferManager ()
		{
			// XXX: Hard-coded for now, may change later!
			provider = new FileFind.Meshwork.FileTransfer.BitTorrent.BitTorrentFileTransferProvider();
		}

		// Starts a new file transfer, or adds a new peer if one
		// already exists.
		internal IFileTransfer StartTransfer(Network network, Node node, File file)
		{
			if (node.NodeID == Core.MyNodeID) {
				throw new ArgumentException("You cannot start a file transfer with yourself.");
			}
			
			// Don't download files if it already exists in the completed downloads directory.
			// If the remote file is different, but has the same filename, it'll globber your copy.
			if (file.NodeID != Core.MyNodeID) {
				if (IO.File.Exists(IO.Path.Combine(Core.Settings.CompletedDownloadDir, file.Name))) {
					throw new Exception("A file by that name already exists in your download directory.");
				}
			}

			IFileTransfer transfer = GetTransfer(file);
			if (transfer == null) {
				transfer = provider.CreateFileTransfer(file);
				transfers.Add(transfer);
				RaiseNewTransfer(transfer);
			}
			
			transfer.AddPeer(network, node);
			transfer.Start();

			return transfer;
		}

		public void RemoveTransfer (IFileTransfer transfer)
		{
			if (!transfers.Contains(transfer)) {
				throw new ArgumentException("Unknown transfer");
			}
			
			transfer.Cancel();

			transfers.Remove(transfer);

			RaiseTransferRemoved(transfer);
		}

		public IList<IFileTransfer> Transfers {
			get {
				return transfers.AsReadOnly();
			}
		}

		internal IFileTransfer GetTransferFromInfoHash(string infoHash)
		{
			if (infoHash == null || infoHash == String.Empty) {
				throw new ArgumentNullException("infoHash");
			}

			foreach (IFileTransfer transfer in transfers) {
				if (transfer.File.InfoHash == infoHash) {
					return transfer;
				}
			}
			return null;
		}

		internal IFileTransfer GetTransfer(string filePath)
		{
			return transfers.Find(delegate (IFileTransfer t) {
				return t.File.FullPath == filePath;
			});
		}

		internal IFileTransfer GetTransfer(File file)
		{
			return transfers.Find (delegate (IFileTransfer t) { return t.File.Equals(file); });
		}

		internal void NewIncomingConnection(ITransport transport)
		{
			BitTorrent.TorrentConnection c = new BitTorrent.TorrentConnection(transport);
			((BitTorrent.BitTorrentFileTransferProvider)provider).AddConnection(c);
		}

		internal IFileTransferProvider Provider {
			get {
				return provider;
			}
		}
	
		private void RaiseNewTransfer(IFileTransfer transfer)
		{
			if (NewFileTransfer != null)
				NewFileTransfer(transfer);
		}

		private void RaiseTransferRemoved(IFileTransfer transfer)
		{
			if (FileTransferRemoved != null)
				FileTransferRemoved(transfer);
		}
	}
}
