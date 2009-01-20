//
// IFileTransfer.cs: 
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using System;
using FileFind.Meshwork.Filesystem;
using FileFind.Meshwork.Exceptions;
using FileFind.Meshwork.Transport;

namespace FileFind.Meshwork.FileTransfer
{
	public interface IFileTransfer
	{
		event FileTransferPeerEventHandler PeerAdded;
		event FileTransferPeerEventHandler PeerRemoved;
		event FileTransferErrorEventHandler Error;

		/// <summary>
		/// Unique ID of the transfer
		/// </summary>
		string Id {
			get;
		}

		/// <summary>
		/// Returns TransferDirection.Downloading if we don't have the
		/// complete file yet, TransferDirection.Uploading if we do.
		/// Either way, we could be uploading data since this is using
		/// BitTorrent.
		///
		/// Note that when a download completes, the direction
		/// automatically switches to TransferDirection.Uploading.
		/// </summary>
		FileTransferDirection Direction {
			get;
		}

		/// <summary>
		/// Local status of the transfer.
		/// </summary>
		FileTransferStatus Status {
			get;
		}

		/// <summary>
		/// Extra status information (error message, etc.)
		/// </summary>
		string StatusDetail {
			get;
		}



		/// <summary>
		/// A list of everyone else participating in this transfer.
		/// </summary>
		IFileTransferPeer[] Peers {
			get;
		}

		/// <summary>
		/// The file being transfered.
		/// </summary>
		File File {
			get;
		}

		/// <summary>
		/// Progress at the current state.
		/// </summary>
		double Progress {
			get;
		}
		
		/// <summary>
		/// Total speed we're downloading at.
		/// </summary>
		ulong TotalDownloadSpeed {
			get;
		}
		
		
		/// <summary>
		/// Total speed we're uploading at.
		/// </summary>
		ulong TotalUploadSpeed {
			get;
		}

		/// <summary>
		/// Number of bytes downloaded.
		/// </summary>
		ulong BytesDownloaded {
			get;
		}

		/// <summary>
		/// Number of bytes uploaded.
		/// </summary>
		ulong BytesUploaded {
			get;
		}

		/// <summary>
		/// Cancel this transfer.
		/// </summary>
		void Cancel();

		/// <summary>
		/// Pause this transfer. No data will be sent or requested until
		/// Resume() is called. This will send a TransferStatusUpdated
		/// message to all peers informing them that the transfer has
		/// been paused. 
		/// </summary>
		void Pause();

		/// <summary>
		/// Resume a paused transfer
		/// </summary>
		void Resume();

		void Start();

		void AddPeer(Network network, Node node);

		int UploadSpeedLimit {
			get;
			set;
		}

		int DownloadSpeedLimit {
			get;
			set;
		}
	}

	internal interface IFileTransferInternal
	{
		void DetailsReceived ();
		void ErrorReceived (Node node, FileTransferException ex);
	}

	public class FileTransferOperation : IMeshworkOperation
	{
		ITransport        transport;
		IFileTransfer     transfer;
		IFileTransferPeer peer;

		internal FileTransferOperation (ITransport transport, IFileTransfer transfer, IFileTransferPeer peer)
		{
			this.transfer = transfer;
			this.peer = peer;
		}

		public IFileTransfer Transfer {
			get {
				return transfer;
			}
		}

		public IFileTransferPeer Peer {
			get {
				return peer;
			}
		}

		public ITransport Transport {
			get {
				return transport;
			}
		}
	}
}
