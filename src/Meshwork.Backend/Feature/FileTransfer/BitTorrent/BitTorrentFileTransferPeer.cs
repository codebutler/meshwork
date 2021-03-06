//
// BitTorrentFileTransferPeer.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007-2008 FileFind.net (http://filefind.net)
//

using System.Collections.Generic;
using Meshwork.Backend.Core;
using MonoTorrent.Client;

namespace Meshwork.Backend.Feature.FileTransfer.BitTorrent
{
	public class BitTorrentFileTransferPeer : FileTransferPeerBase
	{
		private List<PeerId> peers = new List<PeerId>();
		
		public PeerId Peer {
			get {
				var removeMe = new List<PeerId>();
				PeerId returnMe = null;
				foreach (var p in peers) {
					if (p.IsConnected) {
						if (returnMe != null) {
							LoggingService.LogWarning("!!! Found more than one valid peer!!");
						}
						returnMe = p;
					} else {
						LoggingService.LogDebug("Removing invalid peer !! WOHOO!!");
						removeMe.Add(p);
					}
				}
				foreach (var p in removeMe) {
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
			this.network = network;
			this.node = node;
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
			get
			{
			    if (Peer != null) {
					return Peer.BitField.PercentComplete;
				}
			    return 0;
			}
		}

		public override FileTransferPeerStatus Status {
			get
			{
			    if (Peer == null) {
					// XXX: This could also mean hashing.
					return FileTransferPeerStatus.WaitingForInfo;
				}
			    if (Peer.IsConnected) {
			        return FileTransferPeerStatus.Transfering;
			    }
			    // XXX: It may be possible that this sometimes means 'connecting'
			    return FileTransferPeerStatus.Error;
			}
		}

		public override string StatusDetail {
			get {
				return "";
			}
		}
	}
}
