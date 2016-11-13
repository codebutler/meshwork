//
// Core.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006-2008 FileFind.net (http://filefind.net)
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Meshwork.Backend.Core.Destination;
using Meshwork.Backend.Core.Transport;
using Meshwork.Backend.Feature.FileBrowsing.Filesystem;
using Meshwork.Backend.Feature.FileIndexing;
using Meshwork.Backend.Feature.FileSearch;
using Meshwork.Backend.Feature.FileTransfer;
using Meshwork.Backend.Feature.FileTransfer.BitTorrent;
using Meshwork.Platform;

namespace Meshwork.Backend.Core
{
	public delegate void MessageInfoEventHandler (MessageInfo info);
	
	public delegate string PasswordPromptEventHandler();
	
	public class Core
	{
		private readonly List<Network> networks = new List<Network>();
	    private readonly ShareWatcher shareWatcher;
		private readonly TransportManager transportManager;
	    private readonly ArrayList transportListeners = new ArrayList ();
	    private ISettings settings;
		private bool started = false;
		private readonly RSACryptoServiceProvider rsaProvider;
		private readonly string nodeID;
	    private readonly List<PluginInfo> loadedPlugins = new List<PluginInfo>();
	    private readonly List<FailedTransportListener> failedTransportListeners = new List<FailedTransportListener>();

		public event EventHandler Started;
		public event EventHandler FinishedLoading;
		public event MessageInfoEventHandler MessageReceived;
		public event MessageInfoEventHandler MessageSent;
		public event NetworkEventHandler NetworkAdded;
		public event NetworkEventHandler NetworkRemoved;
		public event EventHandler PasswordPrompt;
		
		public static readonly int ProtocolVersion = 250;

	    // FIXME
//		static Core ()
//		{
//			if (Environment.OSVersion.Platform == PlatformID.Unix) {
//				if (Common.OSName == "Linux") {
//					Core.OS = new LinuxPlatform();
//				} else if (Common.OSName == "Darwin") {
//					Core.OS = new OSXPlatform();
//				} else {
//					throw new Exception(string.Format("Unsupported operating system: {0}", Common.OSName));
//				}
//			} else {
//				Core.OS = new WindowsPlatform();
//			}
//		}
	    public Core(ISettings settings)
		{
			if (settings == null) {
				throw new ArgumentNullException(nameof(settings));
			}

			Settings = settings;
			
			var pidFilePath = Path.Combine(Settings.DataPath, "meshwork.pid");
			if (File.Exists(pidFilePath)) {
				var processId = -1;
				int.TryParse(File.ReadAllText(pidFilePath), out processId);
				try {
					Process.GetProcessById(processId);
					throw new Exception($"Meshwork is already running (PID {processId})!");
				} catch (ArgumentException) {
					File.Delete(pidFilePath);
				}
			}
			File.WriteAllText(pidFilePath, Process.GetCurrentProcess().Id.ToString());
			
			if (settings.KeyEncrypted) {
				PasswordPrompt(null, EventArgs.Empty);
				if (!settings.KeyUnlocked) {
				    throw new Exception();
				}	
			}			
			
			rsaProvider = new RSACryptoServiceProvider();			
			rsaProvider.ImportParameters(settings.EncryptionParameters);
			nodeID = Common.Common.SHA512Str(rsaProvider.ToXmlString(false));

			FileSystem = new FileSystemProvider(this);

			ShareBuilder = new ShareBuilder(this);
			ShareBuilder.FinishedIndexing += ShareBuilder_FinishedIndexing;

			shareWatcher = new ShareWatcher(this);

			ShareHasher = new ShareHasher();

			transportManager = new TransportManager(this);

			FileTransferManager = new FileTransferManager(this);

			FileSearchManager = new FileSearchManager(this);

			DestinationManager = new DestinationManager(this);

			// XXX: Use reflection to load these:
			DestinationManager.RegisterSource(new TCPIPv4DestinationSource(this));
			DestinationManager.RegisterSource(new TCPIPv6DestinationSource(this));

			MonoTorrent.Client.Tracker.TrackerFactory.Register("meshwork", typeof(MeshworkTracker));

			ITransportListener tcpListener = new TcpTransportListener(this, Settings.TcpListenPort);
			transportListeners.Add(tcpListener);
			
			if (FinishedLoading != null) {
				FinishedLoading(null, EventArgs.Empty);
			}
		}

		public void Start () {
			if (started) {
				throw new InvalidOperationException("You already called Start()!");
			}
			
			foreach (var networkInfo in settings.Networks) {
				AddNetwork(networkInfo);
			}
			
			foreach (ITransportListener listener in transportListeners) {
				try {
					listener.StartListening();
				} catch (Exception ex) {
					LoggingService.LogError(string.Format("Listener failed to start: {0}", listener.ToString()), ex);
					failedTransportListeners.Add(new FailedTransportListener(listener, ex));
				}
			}

			ShareHasher.Start();
			RescanSharedDirectories();
			
			// XXX: This is blocking ! shareWatcher.Start();

			started = true;

		    Started?.Invoke(null, EventArgs.Empty);
		}

		public FileSystemProvider FileSystem { get; }

	    public MyDirectory MyDirectory => FileSystem.RootDirectory.MyDirectory;

	    public string MyNodeID {
			get {
				if (nodeID == null) {
					throw new InvalidOperationException();
				}
				return nodeID;
			}
		}

		public FileSearchManager FileSearchManager { get; }

	    public IAvatarManager AvatarManager { get; set; }

	    public DestinationManager DestinationManager { get; }

	    public IPlatform OS { get; set; }

	    internal RSACryptoServiceProvider CryptoProvider => rsaProvider;

	    public ShareHasher ShareHasher { get; }

	    public ShareBuilder ShareBuilder { get; }

	    public bool IsLocalNode (Node node)
		{
			return (node.NodeID == MyNodeID);
		}

		internal Node CreateLocalNode (Network network)
		{
		    var node = new Node(network, MyNodeID)
		    {
		        NickName = Settings.NickName,
		        RealName = Settings.RealName,
		        Email = Settings.Email,
		        Verified = true
		    };

		    // XXX: This is a mess. Perhaps the client should register it's name and version with Core on Init.
			var attrs = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), true);
			if (attrs.Length > 0) {
				var attr = (AssemblyTitleAttribute)attrs[0];
				var asmName = Assembly.GetEntryAssembly().GetName();
				node.ClientName    = attr.Title;
				node.ClientVersion = asmName.Version.ToString();
			} else {
				node.ClientName = "Unknown";
				node.ClientVersion = "Unknown";
			}

			node.OperatingSystem = OS.VersionInfo;
			return node;
		}

		public PluginInfo[] Plugins => loadedPlugins.ToArray();

	    public void Stop ()
		{
			ShareBuilder.Stop();

			ShareHasher.Stop();
			shareWatcher.Stop();

			foreach (ITransportListener listener in transportListeners) {
				listener.StopListening ();
			}

			foreach (var transport in TransportManager.Transports) {
				transport.Disconnect();
			}
		}

		public void LoadPlugin (string fileName)
		{
			try {
				if (fileName == null) {
					throw new ArgumentNullException (nameof(fileName));
				}

				var info = new PluginInfo (fileName);

				foreach (var cInfo in loadedPlugins) {
					if (cInfo.Type == info.Type) {
						throw new Exception ("Plugin already loaded.");
					}
				}

				info.CreateInstance();
				loadedPlugins.Add(info);
			} catch (Exception ex) {
				LoggingService.LogError("Failed to load plugin", ex);
			}
		}

		public void UnloadPlugin (PluginInfo info)
		{
			if (info == null) {
				throw new ArgumentNullException (nameof(info));
			}

			info.DestroyInstance();
			loadedPlugins.Remove (info);
		}

		private void ShareBuilder_FinishedIndexing (object sender, EventArgs args)
		{
			try {
				FileSystem.InvalidateCache();

				foreach (var network in networks) {
					network.SendInfoToTrustedNodes();
				}

			} catch (Exception ex) {
				LoggingService.LogError(ex);
			}
		}

		private void AddNetwork (NetworkInfo networkInfo)
		{
			foreach (var thisNetwork in networks) {
				if (thisNetwork.NetworkID == networkInfo.NetworkID) {
					throw new Exception("That network has already been added.");
				}
			}

			var network = Network.FromNetworkInfo(this, networkInfo);

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

		private void RemoveNetwork (Network network)
		{
			network.Stop();

			networks.Remove(network);

			if (NetworkRemoved != null) {
				NetworkRemoved(network);
			}
		}
		
		public Network GetNetwork (string networkID)
		{
			foreach (var network in networks) {
				if (network.NetworkID == networkID) {
					return network;
				}
			}
			return null;
		}

		internal void ConnectTo (ITransport transport, TransportCallback connectCallback)
		{
			if (transport == null) {
				throw new ArgumentNullException(nameof(transport));
			}

			if (transport.Network == null) {
				throw new ArgumentNullException("transport.Network");
			}

			if (transport.ConnectionType == ConnectionType.NodeConnection) {
				// XXX: This doesn't belong here. Have LocalNodeConnection set this up
				// and call me with the proper callback.
				var connection = new LocalNodeConnection(transport);
				transport.Operation = connection;
				transport.Network.AddConnection(connection);
				transportManager.Add(transport, delegate (ITransport bleh) { 
					connection.Start();
					if (connectCallback != null) {
						connectCallback(transport);
					}
				});
			} else {
				transportManager.Add (transport, connectCallback);
			}
		}

		public int CountTransports (ulong connectionType)
		{
		    return transportManager.Transports.Count(transport => transport.ConnectionType == connectionType);
		}

		public TransportManager TransportManager => transportManager;

	    public FileTransferManager FileTransferManager { get; }

	    public Network[] Networks => networks.ToArray ();

	    public void RescanSharedDirectories ()
		{
			if (ShareBuilder.Going == true) {
				LoggingService.LogDebug("Starting scan over!!");
				ShareBuilder.Stop();
			}

			ShareBuilder.Start();
		}
		
		public void RaiseMessageSent (MessageInfo info)
		{
		    MessageSent?.Invoke(info);
		}

		public void RaiseMessageReceived (MessageInfo info)
		{
		    MessageReceived?.Invoke(info);
		}

		public FailedTransportListener[] FailedTransportListeners => failedTransportListeners.ToArray();

	    public ISettings Settings {
			get {
				return settings;
			}
			set {
				if (value == null) {
					throw new ArgumentNullException(nameof(value));
				}
				
				if (settings != null) 
					throw new InvalidOperationException("Settings already set!");

				settings = value;				
				
				ReloadSettings();
			}
		}
		
		public void ReloadSettings ()
		{				
			if (settings.Plugins != null) {
				foreach (var fileName in settings.Plugins) {
					LoadPlugin (fileName);
				}
			}

		    DestinationManager?.SyncFromSettings();

		    // Update listeners
			foreach (ITransportListener listener in transportListeners)
			{
			    var transportListener = listener as TcpTransportListener;
			    if (transportListener != null) {
					transportListener.Port = settings.TcpListenPort;
				}
			}
			
			if (!started)
				return;

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
			
			// Update/remove networks.
			foreach (var network in Networks) {
				var oldNick = network.LocalNode.NickName;
				network.LocalNode.NickName = settings.NickName;
				network.LocalNode.RealName = settings.RealName;
				network.LocalNode.Email = settings.Email;

				var foundNetwork = false;
				
				foreach (var networkInfo in settings.Networks) {
					if (networkInfo.NetworkID == network.NetworkID) {
						network.UpdateTrustedNodes(networkInfo.TrustedNodes);
						foundNetwork = true;
						break;
					}
				}

				if (!foundNetwork) {
					// Actually, this network was removed!
					RemoveNetwork(network);
				} else {
					network.SendInfoToTrustedNodes();
					network.RaiseUpdateNodeInfo(oldNick, network.LocalNode);
					network.AutoconnectManager.ConnectionCount = settings.AutoConnectCount;
				}
			}

			// Add new networks
			foreach (var networkInfo in settings.Networks) {
				if (GetNetwork(networkInfo.NetworkID) == null) {
					AddNetwork(networkInfo);
				}
			}

			RescanSharedDirectories();
		}

	    public bool HasExternalIPv6 {
	        get {
	            foreach (var destination in DestinationManager.Destinations) {
	                if (destination is IPv6Destination && ((IPv6Destination)destination).IsExternal) {
	                    return true;
	                }
	            }
	            return false;
	        }
	    }
	}
}
