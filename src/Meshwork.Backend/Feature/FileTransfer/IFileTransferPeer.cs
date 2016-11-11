using Meshwork.Backend.Core;

namespace Meshwork.Backend.Feature.FileTransfer
{
	public interface IFileTransferPeer
	{
		///<summary>The Network that Node is part of</summary>
		Network Network {
			get;
		}

		///<summary>The remote Node</summary>
		Node Node {
			get;
		}

		///<summary>
		///Speed at which we are download data from this peer.
		///</summary>
		ulong DownloadSpeed { 
			get;
		}

		///<summary>
		///Speed at which we are uploading data to this peer.
		///</summary>
		ulong UploadSpeed {
			get;
		}

		///<summary>
		///Percent of the file this peer has.
		///</summary>
		double Progress {
			get;
		}

		///<summary>Status of the peer.</summary>
		FileTransferPeerStatus Status {
			get;
		}

		///<summary>
		///Extra status information, see FileTransferPeerStatus 
		///documentation for details.
		///</summary>
		string StatusDetail {
			get;
		}
	}
}
