//
// BitTorrentFileTransferPeer.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007-2008 FileFind.net (http://filefind.net)
//

using System;
using System.Collections.Generic;
using FileFind.Meshwork.FileTransfer;
using MonoTorrent.Client;

namespace FileFind.Meshwork.FileTransfer.BitTorrent
{
	public class BitTorrentFileTransferPeer : FileTransferPeerBase
	{
		private List<PeerId> peers = new List<PeerId>();
		
		public PeerId Peer {
			get {
				List<PeerId> removeMe = new List<PeerId>();
				PeerId returnMe = null;
				foreach (PeerId p in peers) {
					if (p.IsValid) {
						if (returnMe != null) {
							Console.Error.WriteLine("!!! Found more than one valid peer!!");
						}
						returnMe = p;
					} else {
						Console.WriteLine("Removing invalid peer !! WOHOO!!");
						removeMe.Add(p);
					}
				}
				foreach (PeerId p in removeMe) {
					peers.Remove(p);
				}

				return returnMe;
			}
		}

		public void AddPeerId (PeerId p)
		{
			peers.Add(p);
		}
		
		public BitTorrentFileTransferPeer (Network network, Node node)
		{
			base.network = network;
			base.node = node;
		}

		public override ulong DownloadSpeed {
			get {
				return Peer == null ? 0 : (ulong)(Peer.Monitor.DownloadSpeed);
			}
		}

		public override ulong UploadSpeed {
			get {
				return Peer == null ? 0 : (ulong)(Peer.Monitor.UploadSpeed);
			}
		}

		public override double Progress {
			get {
				if (Peer != null) {
					return Peer.Bitfield.PercentComplete;
				} else {
					return 0;
				}
			}
		}

		public override FileTransferPeerStatus Status {
			get {
				if (Peer == null) {
					// XXX: This could also mean hashing.
					return FileTransferPeerStatus.WaitingForInfo;
				} else {
					if (Peer.IsValid) {
						return FileTransferPeerStatus.Transfering;
					} else {
						// XXX: It may be possible that this sometimes means 'connecting'
						return FileTransferPeerStatus.Error;
					}
				}
			}
		}

		public override string StatusDetail {
			get {
				return "";
			}
		}
	}
}
