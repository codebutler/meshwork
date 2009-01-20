using System;
using FileFind.Meshwork;

namespace FileFind.Meshwork.FileTransfer
{
	public abstract class FileTransferPeerBase : IFileTransferPeer
	{
		protected Network network;
		protected Node node;
		
		public Network Network {
			get {
				return network;
			}
		}

		public Node Node {
			get {
				return node;
			}
		}

		public abstract ulong UploadSpeed {
			get;
		}

		public abstract ulong DownloadSpeed {
			get;
		}

		public abstract FileTransferPeerStatus Status {
			get;
		}

		public abstract string StatusDetail {
			get;
		}

		public abstract double Progress {
			get;
		}
	}
}
