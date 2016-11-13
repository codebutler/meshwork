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
using System.IO;
using Meshwork.Backend.Core;
using Meshwork.Backend.Core.Transport;
using Meshwork.Backend.Feature.FileBrowsing.Filesystem;
using MonoTorrent.BEncoding;
using MonoTorrent.Client;
using MonoTorrent.Common;

namespace Meshwork.Backend.Feature.FileTransfer.BitTorrent
{
	internal class BitTorrentFileTransfer : FileTransferBase
	{
	    private readonly Core.Core core;

	    TorrentManager manager;
		double hashingPercent;
		bool isCanceled;
		bool startCalled;
		int maxUploadSpeed;
		int maxDownloadSpeed;
		
		public BitTorrentFileTransfer(Core.Core core, IFile file)
		{
		    this.core = core;
		    this.file = file;
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
				
				if (isCanceled) {
					return FileTransferStatus.Canceled;
				}
				
				if (manager == null) {
					if (file.Pieces.Length == 0)
					{
					    if (!(file is LocalFile)) {
							return FileTransferStatus.WaitingForInfo;
						}
					    return FileTransferStatus.Hashing;
					}
				    // File was updated, but DetailsReceived() not yet called
				    return FileTransferStatus.WaitingForInfo;
				}
			    switch (manager.State) {
			        case TorrentState.Paused:
			            return FileTransferStatus.Paused;
						
			        case TorrentState.Hashing:
			            return FileTransferStatus.Hashing;
						
			        case TorrentState.Stopped:
			            if (manager.Progress == 100) {
			                if (Direction == FileTransferDirection.Download) {
			                    return FileTransferStatus.Completed;
			                }
			                // XXX: For uploads, this isnt always right.
			                // Need to check that other peer got the entire file.
			                return FileTransferStatus.Completed;
			            }
			            if (!isCanceled) {
			                // XXX: I think this might happen for just a breif moment while
			                // we're going from Transferring -> Canceled.
			                LoggingService.LogWarning("This shouldn't happen ever, right? " + manager.Progress);
			            }
							
			            return FileTransferStatus.Canceled;

			        /*
						case TorrentState.Queued:
							return FileTransferStatus.Queued;
						*/
						
			        case TorrentState.Seeding:
			        case TorrentState.Downloading:
			            if (peers.Count > 0)
			            {
			                if (manager.OpenConnections == 0) {
			                    return FileTransferStatus.Connecting;
			                }
			                return FileTransferStatus.Transfering;
			            }
			            return FileTransferStatus.NoPeers;

			        default:
			            // XXX:
			            LoggingService.LogWarning("Add a case for this: " + manager.State);
			            return FileTransferStatus.WaitingForInfo;
			    }
			}
		}

		public override double Progress {
			get
			{
			    if (manager != null)
			    {
			        if (manager.State == TorrentState.Hashing) {
						return hashingPercent;
					}
			        if (Direction == FileTransferDirection.Upload)
			        {
			            if (Peers.Length == 1) {
			                return Peers[0].Progress;
			            }
			            if (Peers.Length == 0) {
			                return -1;
			            }
			            double averageProgress = 0;
			            foreach (var peer in Peers) {
			                averageProgress += peer.Progress; 
			            };
			            averageProgress = averageProgress / Peers.Length;
			            return averageProgress;
			        }
			        return manager.Progress;
			    }
			    return -1;
			}
		}

		internal TorrentManager Manager {
			get {
				return manager;
			}
		}

		public override void ErrorReceived (Node node, FileTransferError error)
		{
			LoggingService.LogError("Received File Transfer Error: {0}", error.Message);
			statusDetail = error.Message;
			Cancel();
		}

		public override void Start()
		{
			isCanceled = false;
			startCalled = true;

			// UPLOAD: Do we need to hash the file?
			if (file is LocalFile) {
				if (file.Pieces.Length == 0) {
					// Yep!
				    core.ShareHasher.HashFile((LocalFile)file, HashCallback);
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
				foreach (BitTorrentFileTransferPeer peer in peers) {
					if (peer.Node.NodeID != core.MyNodeID) {
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
				// Start the transfer				
				DetailsReceived();
			} catch (Exception ex) {
				LoggingService.LogError("Error in callback:", ex);
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

			var provider =
				(BitTorrentFileTransferProvider)core.FileTransferManager.Provider;

			var torrent = CreateTorrent(file);
			
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
			manager.PeerConnected += manager_PeerConnected;
			manager.PeerDisconnected += manager_PeerDisconnected;

			#if RIDICULOUS_DEBUG_OUTPUT
			LoggingService.LogDebug("Engine ID: {0}", provider.Engine.PeerId);
			#endif

			manager.Start();

			if (file is LocalFile) {
				foreach (BitTorrentFileTransferPeer peer in peers) {
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

			var peer = new BitTorrentFileTransferPeer(network, node);
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
			get
			{
			    if (manager != null) {
					return (ulong)manager.Monitor.DownloadSpeed;
				}
			    return 0;
			}
		}

		public override ulong TotalUploadSpeed {
			get
			{
			    if (manager != null) {
					return (ulong)manager.Monitor.UploadSpeed;
				}
			    return 0;
			}
		}

		public override ulong BytesDownloaded {
			get
			{
			    if (manager != null) {
					return (ulong) ((manager.Progress * 0.01) * file.Size);
					
					//XXX: Perhaps add a separate property
					//that's bytes downloaded in "this session"
					//return (ulong)manager.Monitor.DataBytesDownloaded;
				}
			    return 0;
			}
		}

		public override ulong BytesUploaded {
			get
			{
			    if (manager != null) {
					return (ulong)manager.Monitor.DataBytesUploaded;
				}
			    return 0;
			}
		}

		
		public override int UploadSpeedLimit {
			get
			{
			    if (manager != null) {
					return manager.Settings.MaxUploadSpeed;
				}
			    return maxUploadSpeed;
			}
			set {
				maxUploadSpeed = value;
				if (manager != null) {
					manager.Settings.MaxUploadSpeed = maxUploadSpeed;
				}
			}
		}

		public override int DownloadSpeedLimit {
			get
			{
			    if (manager != null) {
					return manager.Settings.MaxDownloadSpeed;
				}
			    return maxDownloadSpeed;
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
			var infoDict = new BEncodedDictionary();
			infoDict[new BEncodedString("piece length")] = new BEncodedNumber(file.PieceLength);
			infoDict[new BEncodedString("pieces")] = new BEncodedString(Common.Utils.StringToBytes(string.Join("", file.Pieces)));
			infoDict[new BEncodedString("length")] = new BEncodedNumber(file.Size);
			infoDict[new BEncodedString("name")] = new BEncodedString(file.Name);

			var dict = new BEncodedDictionary();
			dict[new BEncodedString("info")] = infoDict;

			var announceTier = new BEncodedList();
			announceTier.Add(new BEncodedString($"meshwork://transfers/{file.InfoHash}"));
			var announceList = new BEncodedList();
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
				LoggingService.LogDebug("PEER CONNECTED: {0} {1}", args.PeerID.Uri, args.PeerID.GetHashCode());
			
				// XXX: This check can probably be removed.
				if (args.TorrentManager != manager) {
					throw new Exception("PeerConnected for wrong manager. This should NEVER happen.");
				}
				
				// Now, match the peer to the internal BittorrentFileTransferPeer.
				lock (peers) {
					foreach (BitTorrentFileTransferPeer peer in peers) {
						var nodeID = args.PeerID.Uri.AbsolutePath;
						if (nodeID == peer.Node.NodeID) {
							var transport = ((TorrentConnection)args.PeerID.Connection).Transport;
							transport.Operation = new FileTransferOperation(transport, this, peer);

							peer.AddPeerId(args.PeerID);
							return;
						}
					}
				}

				// If we got here, then we were not expecting this peer.
				throw new Exception("Unexpected peer!!!! - " + args.PeerID.Uri);
			} catch (Exception ex) {
				LoggingService.LogError("Error in manager_PeerConnected.", ex);
				args.PeerID.CloseConnection();
			}
		}

		private void manager_PeerDisconnected (object sender, PeerConnectionEventArgs args)
		{
			try {
				LoggingService.LogDebug("Peer Disconnected: {0}", args.PeerID.Uri);

				// Find the matching peer
				var found = false;

				var nodeID = args.PeerID.Uri.AbsolutePath;
				lock (peers) {
					foreach (BitTorrentFileTransferPeer peer in peers) {
						if (nodeID == peer.Node.NodeID) {
							peers.Remove(peer);
							found = true;
							break;
						}
					}
				}
				if (!found) {
					// This should never hapen.
					LoggingService.LogWarning("PeerDisconnected: Unknown peer!");
				}

				if (peers.Count == 0) {
					if (manager.Progress != 100) {
						// Transfer didn't finish, cancel!
						LoggingService.LogWarning("No more peers - canceling torrent!");
						Cancel();
					} else {	
						// Transfer was complete (or an upload), just stop normally.
						manager.Stop();
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError("Error in manager_PeerDisconnected:", ex);
				Cancel();
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
					hashingPercent = ((args.PieceIndex / (double)manager.Torrent.Pieces.Count) * 100);
				}

				#if RIDICULOUS_DEBUG_OUTPUT
				LoggingService.LogDebug("Piece Hashed!");
				#endif
			} catch (Exception ex) {
				LoggingService.LogError("Error in manager_PieceHashed.", ex);
				Cancel();
			}
		}

		private void manager_TorrentStateChanged(object sender, TorrentStateChangedEventArgs args)
		{
			try {
				LoggingService.LogDebug("State: {0}", args.NewState);
				LoggingService.LogDebug("Progress: {0:0.00}", manager.Progress);

				if (args.NewState == TorrentState.Downloading || (args.NewState == TorrentState.Seeding && args.OldState != TorrentState.Downloading)) {
					// XXX: Only have the requesting end connect for now,
					// so we dont end up with redundant conncetions in each direction.
					// We need a solution for this that can handle reverse connections.
					if (!(File is LocalFile)) {
						LoggingService.LogDebug("Torrent is ready! connecting to peers!");
						lock (peers) {
							var didConnect = false;
							foreach (var p in peers) {
								var peer = (BitTorrentFileTransferPeer)p;
								if (ConnectToPeer(peer))
									didConnect = true;
							}
							if (!didConnect) {
								statusDetail = "Unable to connect to any peers";
								Cancel();
							}
						}
					} else {
						LoggingService.LogDebug("Torrent is ready! Waiting for connections from peers!");
					}
				}

				if (args.NewState == TorrentState.Seeding && manager.Progress == 100) {

					if (Direction == FileTransferDirection.Download) {
						if (core.Settings.IncompleteDownloadDir != core.Settings.CompletedDownloadDir) {
							// Ensure torrent is stopped before attempting to move file, to avoid access violation.
							manager.Stop();
							
							System.IO.File.Move(Path.Combine(core.Settings.IncompleteDownloadDir, file.Name),
							             Path.Combine(core.Settings.CompletedDownloadDir, file.Name));
						}
					}


					foreach (BitTorrentFileTransferPeer peer in peers) {
						if (peer.Peer == null || !peer.Peer.IsSeeder) {
							return;
						}
					}
					
					if (manager == null || manager.Progress != 100) {
						// If we got here, then everyone is a seeder.
						// No need to keep the transfer active.
						Cancel();
					} else {
						// Success! Ensure torrent is stopped.
						manager.Stop();
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError("Error in manager_TorrentStateChanged.", ex);
				Cancel();
			}
		}

		private bool ConnectToPeer (BitTorrentFileTransferPeer peer)
		{
			var destination = peer.Node.FirstConnectableDestination;
			if (destination != null) {
				var transport = destination.CreateTransport(ConnectionType.TransferConnection);
				LoggingService.LogDebug("New outgoing connection");
				peer.Network.ConnectTo(transport, OutgoingPeerTransportConnected);
				return true;
			}
		    // FIXME: Mark peer as bad!
		    LoggingService.LogError("Transfer can't connect to peer {0} - no destinations available!", peer.Node);
		    return false;
		}
		
		private void OutgoingPeerTransportConnected (ITransport t)
		{
			try {	
				((BitTorrentFileTransferProvider)core.FileTransferManager.Provider).Listener.AddConnection(new TorrentConnection(t), manager);
			} catch (Exception ex) {
				// XXX: Better error handling here! Stop the torrent! Kill connections! Wreak havoc!
				LoggingService.LogError(ex);
			}
		}
	}
}
