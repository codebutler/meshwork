//
// BitTorrentFileTransfer.cs: IFileTransfer implementation using MonoTorrent
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007 FileFind.net (http://filefind.net)
//


//#define RIDICULOUS_DEBUG_OUTPUT

using System;
using IO=System.IO;
using FileFind.Meshwork.FileTransfer;
using FileFind.Meshwork.Filesystem;
using MonoTorrent.Common;
using MonoTorrent.Client;
using MonoTorrent.BEncoding;
using FileFind.Meshwork.Transport;
using FileFind.Meshwork.Destination;
using FileFind.Meshwork.Exceptions;

namespace FileFind.Meshwork.FileTransfer.BitTorrent
{
	internal class BitTorrentFileTransfer : FileTransferBase
	{
		TorrentManager manager;
		double hashingPercent = 0;
		bool isCanceled = false;
		bool startCalled = false;
		//string transferId;
		int maxUploadSpeed = 0;
		int maxDownloadSpeed = 0;
		
		public BitTorrentFileTransfer(IFile file)
		{
			this.file = file;
			base.id = Common.MD5(new Random().Next().ToString());
		}

		public override FileTransferDirection Direction {
			get {
				//return manager != null && manager.Complete ? FileTransferDirection.Upload : FileTransferDirection.Download;
				return (file is LocalFile) ? FileTransferDirection.Upload : FileTransferDirection.Download;
			}
		}

		public override FileTransferStatus Status {
			get {
				#if RIDICULOUS_DEBUG_OUTPUT
				if (manager != null)
					LoggingService.LogDebug("Transfer Internal Status -- Canceled: " + isCanceled + " StartCalled: " + startCalled + " State: " + manager.State + " Progress: " + manager.Progress);
				else
					LoggingService.LogDebug("Transfer Internal Status -- Canceled: " + isCanceled + " StartCalled: " + startCalled);
				#endif

				if (!startCalled) {
					return FileTransferStatus.Queued;
				}

				if (manager == null) {
					if (isCanceled) {
						return FileTransferStatus.Canceled;
					}

					if (file.Pieces.Length == 0) {
						if (!(file is LocalFile)) {
							return FileTransferStatus.WaitingForInfo;
						} else {
							return FileTransferStatus.Hashing;
						}
					} else {
						return FileTransferStatus.Connecting;
					}
				} else {
					switch (manager.State) {
						case TorrentState.Paused:
							return FileTransferStatus.Paused;
						
						case TorrentState.Hashing:
							return FileTransferStatus.Hashing;
						
						case TorrentState.Stopped:
							if (manager.Progress == 100) {
								if (Direction == FileTransferDirection.Download) {
									return FileTransferStatus.Completed;
								} else {
									// XXX: For uploads, this isnt always right.
									// Need to check that other peer got the entire file.
									return FileTransferStatus.Completed;
								}
							} else {
								if (!isCanceled) {
									// XXX: I think this might happen for just a breif moment while
									// we're going from Transferring -> Canceled.
									LoggingService.LogWarning("This shouldn't happen ever, right? " + manager.Progress);
								}
							
								return FileTransferStatus.Canceled;
							}
						
						/*
						case TorrentState.Queued:
							return FileTransferStatus.Queued;
						*/
						
						case TorrentState.Seeding:
						case TorrentState.Downloading:
							if (peers.Count > 0) {
								if (manager.OpenConnections == 0) {
									return FileTransferStatus.Connecting;
								} else {
									return FileTransferStatus.Transfering;
								}
							} else {
								return FileTransferStatus.NoPeers;
							}

						default:
							// XXX:
							LoggingService.LogWarning("Add a case for this: " + manager.State);
							return FileTransferStatus.WaitingForInfo;
					}
				}
			}
		}

		public override double Progress {
			get {
				if (manager != null) {
					if (manager.State == TorrentState.Hashing) {
						return hashingPercent;
					} else {
						if (Direction == FileTransferDirection.Upload) {
							if (this.Peers.Length == 1) {
								return this.Peers[0].Progress;
							} else if (this.Peers.Length == 0) {
								return -1;
							} else {
								double averageProgress = 0;
								foreach (IFileTransferPeer peer in this.Peers) {
									averageProgress += peer.Progress; 
								};
								averageProgress = averageProgress / this.Peers.Length;
								return averageProgress;
							}
						} else {
							return manager.Progress;
						}
					}
				} else {
					return -1;
				}
			}
		}

		internal TorrentManager Manager {
			get {
				return manager;
			}
		}

		public override void ErrorReceived (Node node, FileTransferException ex)
		{
			LoggingService.LogError("Received File Transfer Error: {0}", ex.Message);
			base.statusDetail = ex.Message;
			Cancel();
		}

		public override void Start()
		{
			isCanceled = false;
			startCalled = true;

			file.Reload();

			// UPLOAD: Do we need to hash the file?
			if (file is LocalFile) {
				if (file.Pieces.Length == 0) {
					// Yep!
					Core.ShareHasher.BeginHashFile((LocalFile)file, HashCallback, this);
				} else {
					// Nope, we're good! Just start!
					DetailsReceived();
				}
			
			// DOWNLOAD: Request file!
			} else {
				// Tell the other side that we want this file.
				// They will response with a FileDetails message, and DetailsReceived will be called.
				
				// XXX: If we already have the file pieces, we still need to send this message,
				// but the response (FileDetails) doesn't need to include pieces.
				foreach (BitTorrentFileTransferPeer peer in this.peers) {
					if (peer.Node.NodeID != Core.MyNodeID) {
						peer.Network.SendRoutedMessage(peer.Network.MessageBuilder.CreateRequestFileMessage(peer.Node, this));
					}
				}

				if (file.Pieces.Length > 0) {
					DetailsReceived();
				}
			}
		}

		private void HashCallback (IAsyncResult result)
		{
			try {
				Core.ShareHasher.EndHashFile(result);

				// Start the transfer
				DetailsReceived();

			} catch (Exception ex) {
				LoggingService.LogError("Error in callback:", ex.ToString());
			}
		}

		public override void DetailsReceived ()
		{
			if (isCanceled) {
				return;
			}

			// Restart transfer. 
			if (manager != null) {
				manager.Start();
				return;
			}

			file.Reload();

			if (file.Pieces.Length == 0) {
				throw new InvalidOperationException("No pieces");
			}	

			LoggingService.LogDebug("{0}: Calling Start:\n{1}", Environment.TickCount, Environment.StackTrace);
			
			if (file.Pieces.Length == 0) {
				throw new InvalidOperationException("No pieces");
			}

			if (string.IsNullOrEmpty(file.InfoHash)) {
				throw new InvalidOperationException("No info hash");
			}

			BitTorrentFileTransferProvider provider =
				(BitTorrentFileTransferProvider)Core.FileTransferManager.Provider;

			Torrent torrent = BitTorrentFileTransfer.CreateTorrent(file);
			
			#if RIDICULOUS_DEBUG_OUTPUT
			// Dump the hashes to the screen
			for (int i=0; i < torrent.Pieces.Count; i++)
				LoggingService.LogDebug(string.Format("{0}) {1}", i, BitConverter.ToString(torrent.Pieces.ReadHash(i))));
			#endif
			
			manager = provider.CreateTorrentManager(torrent, file);
			manager.Settings.MaxUploadSpeed = maxUploadSpeed;
			manager.Settings.MaxDownloadSpeed = maxDownloadSpeed;
			manager.PeersFound += manager_PeersFound;
			manager.PieceHashed += manager_PieceHashed;
			manager.TorrentStateChanged += manager_TorrentStateChanged;
			manager.PeerConnected += new EventHandler<PeerConnectionEventArgs>(manager_PeerConnected);
			manager.PeerDisconnected += new EventHandler<PeerConnectionEventArgs>(manager_PeerDisconnected);

			#if RIDICULOUS_DEBUG_OUTPUT
			LoggingService.LogDebug("Engine ID: {0}", provider.Engine.PeerId);
			#endif

			manager.Start();

			if (file is LocalFile) {
				foreach (BitTorrentFileTransferPeer peer in this.peers) {
					peer.Network.SendFileDetails(peer.Node, (LocalFile)file);
				}
			}
		}

		public override void Cancel()
		{
			// Torrent has been started
			if (manager != null) {
				// Don't try to stop twice.
				if (manager.State != TorrentState.Stopped) {
					manager.Stop();
				}
			
			// Torrent has not been started, may be hashing.
			} else {
				/* XXX:
				if (hashingThread != null) {
					hashingThread.Abort();
				}
				*/
			}
			
			LoggingService.LogDebug("Transfer Cancel() {0}", Environment.StackTrace);

			isCanceled = true;
		}

		public override void Pause ()
		{
			if (manager != null) {
				manager.Pause();
			} else {
				throw new InvalidOperationException("Transfer has not been started.");
			}
		}

		public override void Resume ()
		{
			// To resume a paused torrent, just hit start
			if (manager != null) {
				manager.Start();
			} else {
				throw new InvalidOperationException("Transfer has not been started.");
			}
		}

		public override void AddPeer (Network network, Node node)
		{
			// Don't allow adding the same node (regardless of network)
			// more than once.
			foreach (BitTorrentFileTransferPeer p in peers) {
				if (p.Node.NodeID == node.NodeID) {
					throw new Exception("This node is already a peer.");
				}
			}

			BitTorrentFileTransferPeer peer = new BitTorrentFileTransferPeer(network, node);
			peers.Add(peer);
			
			if ((manager != null) && Direction == FileTransferDirection.Upload && file.Pieces.Length > 0) {
				peer.Network.SendFileDetails(node, (LocalFile)file);
			}

			if (manager == null || manager.State == TorrentState.Stopped) {
				return;
			}
			
			ConnectToPeer(peer);
		}
	
		public override ulong TotalDownloadSpeed {
			get {
				if (manager != null) {
					return (ulong)manager.Monitor.DownloadSpeed;
				} else {
					return 0;
				}
			}
		}

		public override ulong TotalUploadSpeed {
			get {
				if (manager != null) {
					return (ulong)manager.Monitor.UploadSpeed;
				} else {
					return 0;
				}
			}
		}

		public override ulong BytesDownloaded {
			get {
				if (manager != null) {
					return (ulong) ((manager.Progress * 0.01) * file.Size);
					
					//XXX: Perhaps add a separate property
					//that's bytes downloaded in "this session"
					//return (ulong)manager.Monitor.DataBytesDownloaded;
				} else {
					return 0;
				}
			}
		}

		public override ulong BytesUploaded {
			get {
				if (manager != null) {
					return (ulong)manager.Monitor.DataBytesUploaded;
				} else {
					return 0;
				}
			}
		}

		
		public override int UploadSpeedLimit {
			get {
				if (manager != null) {
					return manager.Settings.MaxUploadSpeed;
				} else {
					return maxUploadSpeed;
				}
			}
			set {
				maxUploadSpeed = value;
				if (manager != null) {
					manager.Settings.MaxUploadSpeed = maxUploadSpeed;
				}
			}
		}

		public override int DownloadSpeedLimit {
			get {
				if (manager != null) {
					return manager.Settings.MaxDownloadSpeed;
				} else {
					return maxDownloadSpeed;
				}
			}
			set {
				maxDownloadSpeed = value;
				if (manager != null) {
					manager.Settings.MaxDownloadSpeed = maxDownloadSpeed;
				}
			}
		}

		private static BEncodedDictionary GetTorrentData (IFile file)
		{
			BEncodedDictionary infoDict = new BEncodedDictionary();
			infoDict[new BEncodedString("piece length")] = new BEncodedNumber(file.PieceLength);
			infoDict[new BEncodedString("pieces")] = new BEncodedString(Common.StringToBytes(String.Join("", file.Pieces)));
			infoDict[new BEncodedString("length")] = new BEncodedNumber(file.Size);
			infoDict[new BEncodedString("name")] = new BEncodedString(file.Name);

			BEncodedDictionary dict = new BEncodedDictionary();
			dict[new BEncodedString("info")] = infoDict;

			BEncodedList announceTier = new BEncodedList();
			announceTier.Add(new BEncodedString(String.Format("meshwork://transfers/{0}", file.InfoHash)));
			BEncodedList announceList = new BEncodedList();
			announceList.Add(announceTier);
			dict[new BEncodedString("announce-list")] = announceList;
			
			return dict;
		}

		private static Torrent CreateTorrent (IFile file)
		{
			return Torrent.Load(GetTorrentData(file));
		}

		private void manager_PeerConnected (object sender, PeerConnectionEventArgs args)
		{
			try {
				LoggingService.LogDebug("PEER CONNECTED: {0} {1}", args.PeerID.Location, args.PeerID.GetHashCode());
			
				// XXX: This check can probably be removed.
				if (args.TorrentManager != this.manager) {
					throw new Exception("PeerConnected for wrong manager. This should NEVER happen.");
				}
				
				// Now, match the peer to the internal BittorrentFileTransferPeer.
				lock (this.peers) {
					foreach (BitTorrentFileTransferPeer peer in this.peers) {
						string nodeID = args.PeerID.Location.Host;
						if (nodeID == peer.Node.NodeID) {
							ITransport transport = ((TorrentConnection)args.PeerID.Connection).Transport;
							transport.Operation = new FileTransferOperation(transport, this, peer);

							peer.AddPeerId(args.PeerID);
							return;
						}
					}
				}

				// If we got here, then we were not expecting this peer.
				throw new Exception("Unexpected peer!!!! - " + args.PeerID.Location);
			} catch (Exception ex) {
				LoggingService.LogError("Error in manager_PeerConnected.", ex);
				args.PeerID.CloseConnection();
			}
		}

		private void manager_PeerDisconnected (object sender, PeerConnectionEventArgs args)
		{
			try {
				LoggingService.LogDebug("Disconnected: {0}", args.PeerID.Location);

				// Find the matching peer
				bool found = false;

				string nodeID = args.PeerID.Location.Host;
				lock (this.peers) {
					foreach (BitTorrentFileTransferPeer peer in this.peers) {
						if (nodeID == peer.Node.NodeID) {
							this.peers.Remove(peer);
							found = true;
							break;
						}
					}
				}
				if (!found) {
					// This should never hapen.
					LoggingService.LogWarning("PeerDisconnected: Unknown peer!");
				}

				// No more peers, stop the torrent!
				if (base.peers.Count == 0) {
					LoggingService.LogWarning("No more peers - canceling torrent!");
					this.Cancel();
				}
			} catch (Exception ex) {
				LoggingService.LogError("Error in manager_PeerDisconnected:", ex);
				this.Cancel();
			}
		}
		
		private void manager_PeersFound(object sender, PeersAddedEventArgs args)
		{
			LoggingService.LogDebug("Peers Found!");
		}
		
		private void manager_PieceHashed(object sender, PieceHashedEventArgs args)
		{	
			try {
				if (manager.State == TorrentState.Hashing) {
					hashingPercent = (((double)args.PieceIndex / (double)manager.Torrent.Pieces.Count) * 100);
				}

				#if RIDICULOUS_DEBUG_OUTPUT
				LoggingService.LogDebug("Piece Hashed!");
				#endif
			} catch (Exception ex) {
				LoggingService.LogError("Error in manager_PieceHashed.", ex);
				this.Cancel();
			}
		}

		private void manager_TorrentStateChanged(object sender, TorrentStateChangedEventArgs args)
		{
			try {
				LoggingService.LogDebug("State: {0}", args.NewState);
				LoggingService.LogDebug("Progress: {0:0.00}", this.manager.Progress);

				if (args.NewState == TorrentState.Downloading || (args.NewState == TorrentState.Seeding && args.OldState != TorrentState.Downloading)) {
					// XXX: Only have the requesting end connect for now,
					// so we dont end up with redundant conncetions in each direction.
					// We need a solution for this that can handle reverse connections.
					if (!(File is LocalFile)) {
						LoggingService.LogDebug("Torrent is ready! connecting to peers!");
						lock (this.peers) {
							bool didConnect = false;
							foreach (IFileTransferPeer p in this.peers) {
								BitTorrentFileTransferPeer peer = (BitTorrentFileTransferPeer)p;
								if (ConnectToPeer(peer))
									didConnect = true;
							}
							if (!didConnect) {
								base.statusDetail = "Unable to connect to any peers";
								this.Cancel();
							}
						}
					} else {
						LoggingService.LogDebug("Torrent is ready! Waiting for connections from peers!");
					}
				}

				if (args.NewState == TorrentState.Seeding && this.manager.Progress == 100) {

					if (Direction == FileTransferDirection.Download) {
						if (Core.Settings.IncompleteDownloadDir != Core.Settings.CompletedDownloadDir) {
							IO.File.Move(IO.Path.Combine(Core.Settings.IncompleteDownloadDir, file.Name), IO.Path.Combine(Core.Settings.CompletedDownloadDir, file.Name));
						}
					}


					foreach (BitTorrentFileTransferPeer peer in base.peers) {
						if (peer.Peer == null || !peer.Peer.IsSeeder) {
							return;
						}
					}
					// If we got here, then everyone is a seeder.
					// No need to keep the transfer active.
					this.Cancel();
				}
			} catch (Exception ex) {
				LoggingService.LogError("Error in manager_TorrentStateChanged.", ex);
				this.Cancel();
			}
		}

		private bool ConnectToPeer (BitTorrentFileTransferPeer peer)
		{
			IDestination destination = peer.Node.FirstConnectableDestination;
			if (destination != null) {
				ITransport transport = destination.CreateTransport(ConnectionType.TransferConnection);
				LoggingService.LogDebug("New outgoing connection");
				peer.Network.ConnectTo(transport, OutgoingPeerTransportConnected);
				return true;
			} else {
				// FIXME: Mark peer as bad!
				LoggingService.LogError("Transfer can't connect to peer {0} - no destinations available!", peer.Node);
				return false;
			}
		}
		
		private void OutgoingPeerTransportConnected (ITransport t)
		{
			try {	
				((BitTorrentFileTransferProvider)Core.FileTransferManager.Provider).Listener.AddConnection(new TorrentConnection(t), this.manager);
			} catch (Exception ex) {
				// XXX: Better error handling here! Stop the torrent! Kill connections! Wreak havoc!
				LoggingService.LogError(ex);
			}
		}
	}
}
