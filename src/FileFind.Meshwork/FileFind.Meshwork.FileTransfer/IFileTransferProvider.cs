using System;
using FileFind.Meshwork.Filesystem;

namespace FileFind.Meshwork.FileTransfer
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
