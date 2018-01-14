﻿//
// IFileTransfer.cs: 
//
// Author:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2008 Meshwork Authors
//

using Meshwork.Backend.Core;
using Meshwork.Backend.Core.Transport;
using Meshwork.Backend.Feature.FileBrowsing.Filesystem;

namespace Meshwork.Backend.Feature.FileTransfer
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
		IFile File {
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
		void ErrorReceived (Node node, FileTransferError error);
	}

	public class FileTransferOperation : IMeshworkOperation
	{
		ITransport        transport;
		IFileTransfer     transfer;
		IFileTransferPeer peer;

		internal FileTransferOperation (ITransport transport, IFileTransfer transfer, IFileTransferPeer peer)
		{
			this.transport = transport;
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