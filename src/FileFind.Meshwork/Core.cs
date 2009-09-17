//
// Core.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006-2008 FileFind.net (http://filefind.net)
//

using System;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using FileFind.Meshwork.Filesystem;
using FileFind.Meshwork.FileTransfer;
using FileFind.Meshwork.FileTransfer.BitTorrent;
using FileFind.Meshwork.Search;
using FileFind.Meshwork.Transport;
using FileFind.Meshwork.Destination;

namespace FileFind.Meshwork
{
	public delegate void MessageInfoEventHandler (MessageInfo info);

	public static class Core
	{
		static List<Network> networks = new List<Network>();
		static ShareBuilder shareBuilder;
		static ShareHasher shareHasher;
		static ShareWatcher shareWatcher;
		static TransportManager transportManager;
		static FileTransferManager fileTransferManager;
		static FileSearchManager fileSearchManager;
		static ArrayList transportListeners = new ArrayList ();
		static FileSystemProvider fileSystem;
		static ISettings settings;
		static bool loaded = false;
		static bool started = false;
		static RSACryptoServiceProvider rsaProvider;
		static string nodeID;
		static IAvatarManager avatarManager;
		static List<PluginInfo> loadedPlugins = new List<PluginInfo>();
		static IPlatform os;
		static DestinationManager destinationManager;
		static List<FailedTransportListener> failedTransportListeners = new List<FailedTransportListener>();

		public static event EventHandler Started;
		public static event EventHandler FinishedLoading;
		public static event MessageInfoEventHandler MessageReceived;
		public static event MessageInfoEventHandler MessageSent;
		public static event NetworkEventHandler NetworkAdded;
		public static event NetworkEventHandler NetworkRemoved;
		
		public static readonly int ProtocolVersion = 243;

		static Core ()
		{
			if (Environment.OSVersion.Platform == PlatformID.Unix) {
				if (Common.OSName == "Linux") {
					Core.OS = new LinuxPlatform();
				} else if (Common.OSName == "Darwin") {
					Core.OS = new OSXPlatform();
				} else {
					throw new Exception(String.Format("Unsupported operating system: {0}", Common.OSName));
				}
			} else {
				Core.OS = new WindowsPlatform();
			}
		}

		public static void Init (ISettings settings)
		{
			if (loaded == true) {
				throw new Exception ("Please only call this method once.");
			}

			if (settings == null) {
				throw new ArgumentNullException("settings");
			}

			Core.Settings = settings;

			rsaProvider = new RSACryptoServiceProvider();
			rsaProvider.ImportParameters (settings.EncryptionParameters);
			nodeID = Common.MD5 (rsaProvider.ToXmlString (false)).ToLower();

			fileSystem = new FileSystemProvider();

			shareBuilder = new ShareBuilder();
			shareBuilder.FinishedIndexing += ShareBuilder_FinishedIndexing;

			shareWatcher = new ShareWatcher();

			shareHasher = new ShareHasher();

			transportManager = new TransportManager ();

			fileTransferManager = new FileTransferManager();

			fileSearchManager = new FileSearchManager();

			destinationManager = new DestinationManager();

			// XXX: Use reflection to load these:
			destinationManager.RegisterSource(new TCPIPv4DestinationSource());
			destinationManager.RegisterSource(new TCPIPv6DestinationSource());

			MonoTorrent.Client.Tracker.TrackerFactory.Register("meshwork", typeof(MeshworkTracker));

			ITransportListener tcpListener = new TcpTransportListener(Core.Settings.TcpListenPort);
			transportListeners.Add (tcpListener);
			
			loaded = true;

			if (FinishedLoading != null) {
				FinishedLoading(null, EventArgs.Empty);
			}
		}

		public static void Start ()
		{
			if (!loaded) {
				throw new InvalidOperationException("Call Init() First!");
			}

			if (started) {
				throw new InvalidOperationException("You already called Start()!");
			}
			
			foreach (NetworkInfo networkInfo in settings.Networks) {
				AddNetwork(networkInfo);
			}
			
			foreach (ITransportListener listener in transportListeners) {
				try {
					listener.StartListening();
				} catch (Exception ex) {
					LogManager.Current.WriteToLog("Listener failed to start: {0}", listener.ToString());
					failedTransportListeners.Add(new FailedTransportListener(listener, ex));
				}
			}

			TimeSpan lastScanAgo = (DateTime.Now - settings.LastShareScan);
			if (Math.Abs(lastScanAgo.TotalHours) >= 1) {
				LogManager.Current.WriteToLog("Rescanning share! Last scan was {0} minutes ago.", Math.Abs(lastScanAgo.TotalMinutes));
				Core.RescanSharedDirectories();
			} else {
				LogManager.Current.WriteToLog("Not rescanning share, last scan was {0} minutes ago.", Math.Abs(lastScanAgo.TotalMinutes));
			}

			shareHasher.Start();
			// XXX: This is blocking ! shareWatcher.Start();

			started = true;

			if (Started != null) {
				Started(null, EventArgs.Empty);
			}
		}

		public static FileSystemProvider FileSystem {
			get {
				return fileSystem;
			}
		}

		public static MyDirectory MyDirectory {
			get {
				return FileSystem.RootDirectory.MyDirectory;
			}
		}

		public static string MyNodeID {
			get {
				if (nodeID == null) {
					throw new InvalidOperationException();
				}
				return nodeID;
			}
		}

		public static FileSearchManager FileSearchManager {
			get {
				return fileSearchManager;
			}
		}

		public static IAvatarManager AvatarManager {
			get {
				return avatarManager;
			}
			set {
				avatarManager = value;
			}
		}

		public static DestinationManager DestinationManager {
			get {
				return destinationManager;
			}
		}

		public static IPlatform OS {
			get {
				return os;
			}
			set {
				os = value;
			}
		}

		public static string PublicKeyBlock {
			get {
				return KeyFunctions.MakePublicKeyBlock(Core.Settings.NickName, 
				                                       null,
				                                       rsaProvider.ToXmlString(false));
			}
		}
	
		internal static RSACryptoServiceProvider CryptoProvider {
			get {
				return rsaProvider;
			}
		}

		public static ShareHasher ShareHasher {
			get {
				return shareHasher;
			}
		}

		public static ShareBuilder ShareBuilder {
			get {
				return shareBuilder;
			}
		}

		public static bool IsLocalNode (Node node)
		{
			return (node.NodeID == Core.MyNodeID);
		}

		internal static Node CreateLocalNode (Network network)
		{
			if (!loaded) {
				throw new InvalidOperationException("You must call Init() first");
			}
			
			Node node = new Node(network, Core.MyNodeID);
			node.NickName     = Core.Settings.NickName;
			node.RealName     = Core.Settings.RealName;
			node.Email        = Core.Settings.Email;
			node.Verified     = true;

			// XXX: This is a mess. Perhaps the client should register it's name and version with Core on Init.
			object[] attrs = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), true);
			if (attrs.Length > 0) {
				AssemblyTitleAttribute attr = (AssemblyTitleAttribute)attrs[0];
				AssemblyName asmName = Assembly.GetEntryAssembly().GetName();
				node.ClientName    = attr.Title;
				node.ClientVersion = asmName.Version.ToString();
			} else {
				node.ClientName = "Unknown";
				node.ClientVersion = "Unknown";
			}

			node.OperatingSystem = Core.OS.VersionInfo;
			return node;
		}

		public static PluginInfo[] Plugins {
			get {
				return loadedPlugins.ToArray();
			}
		}

		public static void Stop ()
		{
			if (!loaded) return;

			shareBuilder.Stop();

			shareHasher.Stop();
			shareWatcher.Stop();

			foreach (ITransportListener listener in transportListeners) {
				listener.StopListening ();
			}

			foreach (ITransport transport in TransportManager.Transports) {
				transport.Disconnect();
			}
		}

		public static void LoadPlugin (string fileName)
		{
			try {
				if (fileName == null) {
					throw new ArgumentNullException ("fileName");
				}

				PluginInfo info = new PluginInfo (fileName);

				foreach (PluginInfo cInfo in loadedPlugins) {
					if (cInfo.Type == info.Type) {
						throw new Exception ("Plugin already loaded.");
					}
				}

				info.CreateInstance();
				loadedPlugins.Add(info);
			} catch (Exception ex) {
				Console.Error.WriteLine (ex);
			}
		}

		public static void UnloadPlugin (PluginInfo info)
		{
			if (info == null) {
				throw new ArgumentNullException ("info");
			}

			info.DestroyInstance();
			loadedPlugins.Remove (info);
		}

		private static void ShareBuilder_FinishedIndexing (object sender, EventArgs args)
		{
			try {
				Core.FileSystem.InvalidateCache();

				Core.Settings.LastShareScan = DateTime.Now;

				foreach (Network network in networks) {
					network.SendInfoToTrustedNodes();
				}

			} catch (Exception ex) {
				Console.Error.WriteLine(ex);
			}
		}

		private static void AddNetwork (NetworkInfo networkInfo)
		{
			foreach (Network thisNetwork in networks) {
				if (thisNetwork.NetworkID == networkInfo.NetworkID) {
					throw new Exception("That network has already been added.");
				}
			}

			Network network = Network.FromNetworkInfo(networkInfo);

			networks.Add(network);

			/*
			if (!fileSystem.RootDirectory.HasSubdirectory(network.NetworkID)) {
				Directory directory = fileSystem.RootDirectory.CreateSubdirectory(network.NetworkID);
				directory.Requested = true;
			}
			*/
			
			if (NetworkAdded != null) {
				NetworkAdded (network);
			}
			network.Start ();
		}

		private static void RemoveNetwork (Network network)
		{
			network.Stop();

			networks.Remove(network);

			if (NetworkRemoved != null) {
				NetworkRemoved(network);
			}
		}
		
		public static Network GetNetwork (string networkID)
		{
			foreach (Network network in networks) {
				if (network.NetworkID == networkID) {
					return network;
				}
			}
			return null;
		}

		internal static void ConnectTo (ITransport transport, TransportCallback connectCallback)
		{
			if (transport == null) {
				throw new ArgumentNullException("transport");
			}

			if (transport.Network == null) {
				throw new ArgumentNullException("transport.Network");
			}

			if (transport.ConnectionType == ConnectionType.NodeConnection) {
				// XXX: This doesn't belong here. Have LocalNodeConnection set this up
				// and call me with the proper callback.
				LocalNodeConnection connection = new LocalNodeConnection(transport);
				transport.Network.Connections.Add ((INodeConnection)connection);
				transport.Operation = connection;
				transport.Network.RaiseConnectingTo (connection);

				transportManager.Add (transport, delegate (ITransport bleh) { 
					connection.Start (); 

					if (connectCallback != null) {
						connectCallback(transport);
					}
				});
			} else {
				transportManager.Add (transport, connectCallback);
			}
		}

		public static int CountTransports (ulong connectionType)
		{
			int result = 0;
			foreach (ITransport transport in transportManager.Transports) {
				if (transport.ConnectionType == connectionType) {
					result++;
				}
			}
			return result;
		}

		public static TransportManager TransportManager {
			get {
				return transportManager;
			}
		}

		public static FileTransferManager FileTransferManager {
			get {
				return fileTransferManager;
			}
		}

		public static Network[] Networks {
			get {
				return networks.ToArray ();
			}
		}

		public static void RescanSharedDirectories ()
		{
			if (shareBuilder.Going == true) {
				Console.WriteLine ("Starting scan over!!");
				shareBuilder.Stop();
			}

			shareBuilder.Start();
		}
		
		public static void RaiseMessageSent (MessageInfo info)
		{
			if (MessageReceived != null) {
				MessageSent(info);
			}
		}

		public static void RaiseMessageReceived (MessageInfo info)
		{
			if (MessageReceived != null) {
				MessageReceived(info);
			}
		}

		public static FailedTransportListener[] FailedTransportListeners {
			get {
				return failedTransportListeners.ToArray();
			}
		}

		public static ISettings Settings {
			get {
				return settings;
			}
			set {
				if (value == null) {
					throw new ArgumentNullException("value");
				}

				settings = value;

				if (started) {
					// Update/remove networks.
					foreach (Network network in Networks) {
						string oldNick = network.LocalNode.NickName;
						network.LocalNode.NickName = settings.NickName;
						network.LocalNode.RealName = settings.RealName;
						network.LocalNode.Email = settings.Email;

						foreach (NetworkInfo networkInfo in settings.Networks) {
							if (networkInfo.NetworkID == network.NetworkID) {
								network.UpdateTrustedNodes(networkInfo.TrustedNodes);
								goto found;
							}
						}

						// Actually, this network was removed!
						Core.RemoveNetwork(network);
						continue;

						found:
						
						network.SendInfoToTrustedNodes();
						network.RaiseUpdateNodeInfo(oldNick, network.LocalNode);

						network.AutoconnectManager.ConnectionCount = settings.AutoConnectCount;
					}

					// Add new networks
					foreach (NetworkInfo networkInfo in settings.Networks) {
						if (GetNetwork(networkInfo.NetworkID) == null) {
							AddNetwork(networkInfo);
						}
					}
				
					// Update file transfer options
					if (settings.EnableGlobalDownloadSpeedLimit) {
						FileTransferManager.Provider.GlobalDownloadSpeedLimit = settings.GlobalDownloadSpeedLimit * 1024;
					} else {
						FileTransferManager.Provider.GlobalDownloadSpeedLimit = 0;
					}

					if (settings.EnableGlobalUploadSpeedLimit) {
						FileTransferManager.Provider.GlobalUploadSpeedLimit = settings.GlobalUploadSpeedLimit * 1024;
					} else {
						FileTransferManager.Provider.GlobalUploadSpeedLimit = 0;
					}

					// Update listeners
					foreach (ITransportListener listener in transportListeners) {
						if (listener is TcpTransportListener) {
							((TcpTransportListener)listener).Port = settings.TcpListenPort;
						}
					}

					RescanSharedDirectories ();
				}
				
				if (settings.Plugins != null) {
					foreach (string fileName in settings.Plugins) {
						LoadPlugin (fileName);
					}
				}

				if (Core.DestinationManager != null) {
					Core.DestinationManager.SyncFromSettings();
				}
			}
		}
	}
}
