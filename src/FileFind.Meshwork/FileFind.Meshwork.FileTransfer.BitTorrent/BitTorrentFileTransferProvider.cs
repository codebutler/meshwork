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
using MonoTorrent.Common;
using MonoTorrent.Client;
using FileFind.Meshwork.FileTransfer;
using FileFind.Meshwork.Filesystem;

namespace FileFind.Meshwork.FileTransfer.BitTorrent
{
	internal class BitTorrentFileTransferProvider : IFileTransferProvider
	{
		ClientEngine 	engine;
		TorrentSettings torrentDefaults;

		MeshworkPeerConnectionListener listener;

		public BitTorrentFileTransferProvider ()
		{
			MonoTorrent.Client.Logger.AddListener(new System.Diagnostics.ConsoleTraceListener());

			string downloadPath = Core.Settings.IncompleteDownloadDir;
			EngineSettings settings = new EngineSettings (downloadPath, 1);
			
			torrentDefaults = new TorrentSettings (4, 60, 0, 0);
			torrentDefaults.FastResumeEnabled = false;
			
			listener = new MeshworkPeerConnectionListener ();
			engine = new ClientEngine(settings, listener);

			Core.FinishedLoading += delegate {
				Core.FileTransferManager.FileTransferRemoved += Core_FileTransferRemoved;
			};
			
			#if RIDICULOUS_DEBUG_OUTPUT
			engine.ConnectionManager.PeerMessageTransferred += delegate (object sender, PeerMessageEventArgs e) {
				Console.BackgroundColor = ConsoleColor.White;
				Console.ForegroundColor = ConsoleColor.Black;
				Console.WriteLine("{0}: {1}", e.Direction, e.Message.GetType().Name);
				Console.ResetColor();
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
			BitTorrentFileTransfer transfer = new BitTorrentFileTransfer(file);
			return transfer;
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
			string localPath = (file is LocalFile) ? System.IO.Path.GetDirectoryName(((LocalFile)file).LocalPath) : engine.Settings.SavePath;
			Console.WriteLine("Local path: {0}", localPath);
			TorrentManager manager = new TorrentManager(torrent,
			                             localPath,
			                             torrentDefaults);
			engine.Register(manager);
			Console.WriteLine("{0}: Registered Manager with engine", Environment.TickCount);
			return manager;
		}

		internal ClientEngine Engine {
			get {
				return engine;
			}
		}
		
		public void AddConnection (TorrentConnection connection)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("Incoming connection: {0}", connection.IsIncoming ? "Incoming" : "Outgoing");
			Console.ResetColor();
			listener.AddConnection(connection, null);
		}

		private void Core_FileTransferRemoved (IFileTransfer transfer)
		{
			if (transfer is BitTorrentFileTransfer) {
				TorrentManager manager = ((BitTorrentFileTransfer)transfer).Manager;
				if (manager != null) {
					Console.WriteLine("Removing torrent from engine!");
					engine.Unregister(manager);
				}
			}
		}
	}
}
