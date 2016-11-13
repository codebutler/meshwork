//
// BitTorrentFileTransferProvider.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007-2008 FileFind.net (http://filefind.net)
//

//#define RIDICULOUS_DEBUG_OUTPUT

using System;
using System.Diagnostics;
using System.IO;
using Meshwork.Backend.Core;
using Meshwork.Backend.Feature.FileBrowsing.Filesystem;
using MonoTorrent.Client;
using MonoTorrent.Common;

namespace Meshwork.Backend.Feature.FileTransfer.BitTorrent
{
	internal class BitTorrentFileTransferProvider : IFileTransferProvider
	{
	    private readonly Core.Core core;

	    ClientEngine 	engine;
		TorrentSettings torrentDefaults;

		MeshworkPeerConnectionListener listener;

		public BitTorrentFileTransferProvider (Core.Core core)
		{
		    this.core = core;

		    Logger.AddListener(new ConsoleTraceListener());

			var downloadPath = core.Settings.IncompleteDownloadDir;
			var settings = new EngineSettings (downloadPath, 1);
			
			torrentDefaults = new TorrentSettings (4, 60, 0, 0);
			
			listener = new MeshworkPeerConnectionListener (core);
			engine = new ClientEngine(settings, listener);

			core.FinishedLoading += delegate {
				core.FileTransferManager.FileTransferRemoved += Core_FileTransferRemoved;
			};
			
			#if RIDICULOUS_DEBUG_OUTPUT
			engine.ConnectionManager.PeerMessageTransferred += delegate (object sender, PeerMessageEventArgs e) {
				LoggingService.LogDebug("{0}: {1}", e.Direction, e.Message.GetType().Name);
			};
			#endif
		}

		public MeshworkPeerConnectionListener Listener {
			get {
				return listener;
			}
		}

		public IFileTransfer CreateFileTransfer(IFile file)
		{
		    return new BitTorrentFileTransfer(core, file);
		}

		public int GlobalUploadSpeedLimit {
			get {
				return engine.Settings.GlobalMaxUploadSpeed;
			}
			set {
				engine.Settings.GlobalMaxUploadSpeed = value;
			}
		}

		public int GlobalDownloadSpeedLimit {
			get {
				return engine.Settings.GlobalMaxDownloadSpeed;
			}
			set {
				engine.Settings.GlobalMaxDownloadSpeed = value;
			}
		}

		internal TorrentManager CreateTorrentManager(Torrent torrent, IFile file)
		{
			var localPath = (file is LocalFile) ? Path.GetDirectoryName(((LocalFile)file).LocalPath) : engine.Settings.SavePath;
			LoggingService.LogDebug("Local path: {0}", localPath);
			var manager = new TorrentManager(torrent,
			                             localPath,
			                             torrentDefaults);
			engine.Register(manager);
			LoggingService.LogDebug("{0}: Registered Manager with engine", Environment.TickCount);
			return manager;
		}

		internal ClientEngine Engine {
			get {
				return engine;
			}
		}
		
		public void AddConnection (TorrentConnection connection)
		{
			LoggingService.LogDebug("Incoming connection: {0}", connection.IsIncoming ? "Incoming" : "Outgoing");
			listener.AddConnection(connection, null);
		}

		private void Core_FileTransferRemoved (IFileTransfer transfer)
		{
			if (transfer is BitTorrentFileTransfer) {
				var manager = ((BitTorrentFileTransfer)transfer).Manager;
				if (manager != null) {
					LoggingService.LogDebug("Removing torrent from engine!");
					engine.Unregister(manager);
				}
			}
		}
	}
}
