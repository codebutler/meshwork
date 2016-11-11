namespace Meshwork.Backend.Feature.FileTransfer
{
	///<summary>
	///Possible transfer statuses, all set locally
	///</summary>
	public enum FileTransferStatus
	{
		///<summary>No peers available</summary>
		NoPeers,

		///<summary>Waiting for file info</summary>
		WaitingForInfo,

		///<summary>We have info, connecting or waiting for connection now.</summary>
		Connecting,
		///</summary>

		///<summary>
		///We have peers, but they are all busy (hashing or paused)
		///</summary>
		AllPeersBusy,

		///<summary>Transfer is queued locally.</summary>
		Queued,

		///<summary>
		///We are hashing the file, used only when uploading.
		///</summary>
		Hashing,

		///<summary>Transfer is going.</summary>
		Transfering,

		///<summary>User has paused transfer</summary>
		Paused,

		/*
		///<summary>
		///Something went horribly wrong, check the Error field for
		///details.
		///</summary>
		Failed,
		*/

		///<summary>User canceled transfer</summary>
		Canceled,

		///<summary>Transfer completed successfully.</summary>
		Completed
	}
}
