using System;
using FileFind.Meshwork;

namespace FileFind.Meshwork.FileTransfer
{
	///<summary>
	///Possible transfer peer statuses, all received remotely from peer, or
	///infered from what's going on.
	///</summary>
	public enum FileTransferPeerStatus
	{
		///<summary>
		///We are trying to connect to peer, or peer is trying to
		///connect to us.
		///</summary>
		Connecting,

		///<summary>
		///Peer is hasing file. The StatusDetail field will have the
		///percent.
		Hashing,

		///<summary>
		///Peer is waiting for us to send them information.
		WaitingForInfo,

		///<summary>
		///Peer has this transfer queued. The StatusDetail field will
		///have place this transfer's place in line.
		///</summary>
		Queued,

		///<summary>
		///Transfer is going. We set this automatically when we start
		///sending/receving data.
		///</summary>
		Transfering,
		
		///<summary>
		///Peer has paused transfer.
		///</summary>
		Paused,
	
		///<summary>
		///Something went wrong.
		///</summary>
		Error
	}
}
