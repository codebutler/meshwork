//
// FileTransferManager.cs: Keeps track of ongoing file transfers
//
// Author:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2007 Meshwork Authors
//

using System;
using System.Collections.Generic;
using System.IO;
using Meshwork.Backend.Core;
using Meshwork.Backend.Core.Transport;
using Meshwork.Backend.Feature.FileBrowsing.Filesystem;
using Meshwork.Backend.Feature.FileTransfer.BitTorrent;

namespace Meshwork.Backend.Feature.FileTransfer
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

		internal FileTransferManager (Core.Core core)
		{
			// XXX: Hard-coded for now, may change later!
			provider = new BitTorrentFileTransferProvider(core);
		}

		// Starts a new file transfer, or adds a new peer if one
		// already exists.
		internal IFileTransfer StartTransfer(Network network, Node node, IFile file)
		{
			if (node.NodeID == network.Core.MyNodeID) {
				throw new ArgumentException("You cannot start a file transfer with yourself.");
			}
			
			// Don't download files if it already exists in the completed downloads directory.
			// If the remote file is different, but has the same filename, it'll globber your copy.
			if (!(file is LocalFile)) {
				if (File.Exists(Path.Combine(network.Core.Settings.CompletedDownloadDir, file.Name))) {
					throw new Exception("A file by that name already exists in your download directory.");
				}
			}

			var transfer = GetTransfer(file);
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
			if (infoHash == null || infoHash == string.Empty) {
				throw new ArgumentNullException("infoHash");
			}

			foreach (var transfer in transfers) {
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

		internal IFileTransfer GetTransfer(IFile file)
		{
			return GetTransfer(file.FullPath);
		}

		internal void NewIncomingConnection(ITransport transport)
		{
			var c = new TorrentConnection(transport);
			((BitTorrentFileTransferProvider)provider).AddConnection(c);
		}

		internal IFileTransferProvider Provider {
			get {
				return provider;
			}
		}
	
		private void RaiseNewTransfer(IFileTransfer transfer)
		{
			LoggingService.LogInfo("Transfer added: {0}", transfer.File.Name);
			
			if (NewFileTransfer != null)
				NewFileTransfer(transfer);
		}

		private void RaiseTransferRemoved(IFileTransfer transfer)
		{
			LoggingService.LogInfo("Transfer removed: {0}", transfer.File.Name);

			if (FileTransferRemoved != null)
				FileTransferRemoved(transfer);
		}
	}
}
