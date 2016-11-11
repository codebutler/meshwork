using Meshwork.Backend.Feature.FileBrowsing.Filesystem;

namespace Meshwork.Backend.Feature.FileTransfer
{
	internal interface IFileTransferProvider
	{
		IFileTransfer CreateFileTransfer(IFile file);

		int GlobalUploadSpeedLimit {
			get;
			set;
		}

		int GlobalDownloadSpeedLimit {
			get;
			set;
		}
	}
}
