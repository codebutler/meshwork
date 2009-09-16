//
// FileTransferBase.cs: 
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using System;
using System.Collections.Generic;
using FileFind.Meshwork.Filesystem;
using FileFind.Meshwork.Exceptions;

namespace FileFind.Meshwork.FileTransfer
{
	public abstract class FileTransferBase : IFileTransfer, IFileTransferInternal
	{
		protected IFile file;
		protected string id;
		protected string statusDetail;
		protected List<IFileTransferPeer> peers = new List<IFileTransferPeer>();

		public event FileTransferPeerEventHandler PeerAdded;

		public event FileTransferPeerEventHandler PeerRemoved;

		public event FileTransferErrorEventHandler Error;


		public abstract FileTransferDirection Direction {
			get;
		}

		public abstract FileTransferStatus Status {
			get;
		}

		public abstract double Progress {
			get;
		}

		public string StatusDetail {
			get {
				return statusDetail;
			}
		}

		public string Id {
			get {
				return id;
			}
		}

		public IFileTransferPeer[] Peers {
			get {
				return peers.ToArray();
			}
		}
	
		public IFile File {
			get {
				return file;
			}
		}

		public abstract ulong TotalDownloadSpeed {
			get;
		}

		public abstract ulong TotalUploadSpeed {
			get;
		}

		public abstract ulong BytesDownloaded {
			get;
		}

		public abstract ulong BytesUploaded {
			get;
		}

		public abstract void Start();

		public abstract void Cancel();

		public abstract void Pause();

		public abstract void Resume();

		public abstract void AddPeer(Network network, Node node);

		public abstract void DetailsReceived();

		public abstract void ErrorReceived (Node node, FileTransferException ex);

		public abstract int UploadSpeedLimit {
			get;
			set;
		}

		public abstract int DownloadSpeedLimit {
			get;
			set;
		}
	}
}
