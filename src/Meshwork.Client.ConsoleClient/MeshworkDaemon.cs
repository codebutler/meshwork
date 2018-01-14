using System;
using Meshwork.Backend.Core;
using Meshwork.Backend.Core.Logging;
using Meshwork.Platform;
using Meshwork.Platform.MacOS;
using IO = System.IO;

namespace Meshwork.Client.Console
{
	public class MeshworkDaemon : ILogger
	{
	    private readonly Settings settings;
	    private readonly Core core;

		public static int Main (string[] args)
		{
			var syntax = "Syntax: meshwork-daemon [--create-settings] settingsfilename.json";
			if (args.Length == 0 || args[0] == "--help") {
				System.Console.WriteLine(syntax);
				return 1;
			} else if (args[0] == "--create-settings") {
				if (args.Length > 1) {
					new SettingsCreator(args[1]);
				} else {
					System.Console.WriteLine(syntax);
				return 1;
				}
			} else if (!IO.File.Exists(args[0])) {
				System.Console.WriteLine(syntax);
				return 1;
			} else {
				new MeshworkDaemon(args[0]);
			}
			return 0;
		}

		public MeshworkDaemon (string fileName)
		{
			LogItem("MESHWORK DAEMON CLIENT 0.1");

			// FIXME Utils.SetProcessName("meshwork-daemon");
            // Stdlib.signal(Signum.SIGINT, handle_signal);
            // Stdlib.signal(Signum.SIGKILL, handle_signal);

			settings = Settings.ReadSettings(fileName);

		    core = new Core(settings, getPlatform());
			core.AvatarManager = new AvatarManager(core);

		    core.NetworkAdded += AddNetworkEvents;
			LoggingService.AddLogger(this);

		    core.Start();
		}

		private void handle_signal (int signal)
		{
			LogItem("Shutting down...");
			core.Stop();
		}

		private string GetKeyDir (Network network)
		{
			return IO.Path.Combine("received_keys", network.NetworkID);
		}

		private string GetKeyFileName (Network network, string nodeID)
		{
	 		return IO.Path.Combine(GetKeyDir(network), nodeID + ".mpk");
		}

		private void LogItem (string text)
		{
			LogItem(text, null);
		}

		private void LogItem (string text, Exception ex)
		{
			if (ex != null) {
				//Syscall.syslog (SyslogLevel.LOG_ERR, String.Format ("{0}: {1}", text, ex.ToString()));
				System.Console.Error.WriteLine(string.Format ("{0}: {1}", text, ex.ToString()));
			} else {
				//Syscall.syslog (SyslogLevel.LOG_INFO, text);
				System.Console.WriteLine(text);
			}
		}

		private void AddNetworkEvents (Network network)
		{
			network.ReceivedKey += network_ReceivedKey;
			network.PrivateMessage += network_PrivateMessage;
			network.ConnectingTo += network_NewConnection;
			network.NewIncomingConnection += network_NewConnection;
			network.UserOnline += network_UserOnline;
		}

		private void network_NewConnection (Network network, LocalNodeConnection connection)
		{
			try {
				connection.ConnectionError += connection_ConnectionError;
			} catch (Exception ex) {
				LoggingService.LogError("Error in network_NewConnection: " + ex);
			}
		}

		private void connection_ConnectionError (LocalNodeConnection connection, Exception ex)
		{
			LoggingService.LogError("Error in connection {0}: {1}", connection.RemoteAddress, ex);
		}

		private void network_UserOnline (Network network, Node node)
		{
			try {
				if (node.GetTrustedNode() == null && (!IO.File.Exists(GetKeyFileName(network, node.NodeID)))) {
					network.RequestPublicKey(node);
				}
			} catch (Exception ex) {
				LoggingService.LogError("Error in network_UserOnline: " + ex);
			}
		}
		
		private bool network_ReceivedKey (Network network, ReceivedKeyEventArgs args)
		{
			try {
				var publicKey = new PublicKey(args.Key.Info, args.Key.Key);
			    var nodeInfo = new TrustedNodeInfo(publicKey);
			
				if (args.Node != null) {
					nodeInfo.Identifier = args.Node.NickName;
				} else {
					nodeInfo.Identifier = nodeInfo.NodeId;
				}
				
				// First person to connect? Put them in charge!
				if (network.TrustedNodes.Count == 0) {
					LogItem(string.Format("[!] WARNING! {0} is now the admin!", publicKey.Nickname));
					if (!settings.AdminIDs.Contains(nodeInfo.NodeId)) {
						settings.AdminIDs.Add(nodeInfo.NodeId);
						settings.SaveSettings();
					}
					return true;
				}
				
				// Don't accept the key now, but keep it around
				var keyDir = GetKeyDir(network);
				var keyFile = GetKeyFileName(network, nodeInfo.NodeId);
				if (IO.File.Exists (keyFile) == false) {
					if (IO.Directory.Exists (keyDir) == false) {
						IO.Directory.CreateDirectory (keyDir);
					}
					IO.File.WriteAllText(keyFile, publicKey.ToArmoredString());
				}
			} catch (Exception ex) {
				LoggingService.LogError("Error in network_ReceivedKey: " + ex);
			}
			return false;
		}

		private void network_PrivateMessage (Network network, Node messageFrom, string messageText)
		{
			try {
				if (settings.AdminIDs.Contains(messageFrom.NodeID)) {
					var result = ProcessCommand(network, messageText.Split(' '));
					messageFrom.Network.SendPrivateMessage (messageFrom, result);
				} else {
					messageFrom.Network.SendPrivateMessage (messageFrom, "Access Denied");
				}
			} catch (Exception ex) {
				LoggingService.LogError("Error in network_PrivateMessage: " + ex);
			}
		}

		private string ProcessCommand (Network network, string[] args)
		{
			NetworkInfo networkInfo = null;
			foreach (var n in settings.Networks) {
				if (n.NetworkId == network.NetworkID) {
					networkInfo = n;
					break;
				}
			}
			if (networkInfo == null) {
				throw new Exception("EGADS!!");
			}

			var result = string.Empty;
			switch (args[0]) {
				case "key":
					if (args.Length > 1) {
						switch (args[1]) {
							case "list":
								result += "TRUSTED KEYS\n";
								foreach (var n in network.TrustedNodes.Values) {
									result += "  " + n.NodeId + "\n";
								}
								result += "\nUNTRUSTED KEYS\n";
								foreach (var dir in new IO.DirectoryInfo("received_keys").GetDirectories()) {
									if (dir.Name == network.NetworkID) {
										foreach (var file in dir.GetFiles()) {
											result += "  " + file.Name + "\n";
										}
									}
								}
								break;
							case "trust":
								if (args.Length > 2) {
									var id = args[2];
									var keyDir = IO.Path.Combine("received_keys", network.NetworkID); 
									var keyFile = IO.Path.Combine(keyDir, id + ".mpk");

									if (IO.File.Exists(keyFile)) {
										var key = PublicKey.Parse(IO.File.ReadAllText(keyFile));
										var nodeInfo = new TrustedNodeInfo(key);
										
										networkInfo.TrustedNodes.Add(nodeInfo.NodeId, nodeInfo);
										
										settings.SaveSettings();
										core.Settings = settings;
										IO.File.Delete (keyFile);
										result += "Key added!";
									} else {
										result += "File not found.";
									}
								} else {
									result += "Argument(s) expected: keyid";
								}
								break;
							case "request":
								if (args.Length > 2) {
									var nodeid = args[2];
									var node = network.Nodes[nodeid];
									if (node != null) {
										network.RequestPublicKey(node);
										result += "Key requested!";
									} else {
										result += "Node not found!";
									}
									break;
								} else {
									result += "Argument(s) expected: nodeid";
								}
								break;
							default:
								result += "key list - show all keys\n";
								result += "key trust <id> - trust a key\n";
								result += "key request <id> - request a user's key\n";
								break;
						}
					} else {
						result += "Argument expected: key command.";
					}
					break;
				case "admin":
					if (args.Length > 1) {
						switch (args[1]) {
							case "list":
								result += "\nThere are " + settings.AdminIDs.Count + " admins:\n";
								foreach (var id in settings.AdminIDs) {
									var info = networkInfo.TrustedNodes[id];
									result += info.Identifier + " (" + id + ")\n";
								}

								break;

							case "add":
								if (args.Length > 2) {
									var id = args[2];
									if (!settings.AdminIDs.Contains(id)) {
										if (networkInfo.TrustedNodes[id] != null) {
											settings.AdminIDs.Add(id);
											settings.SaveSettings();
										} else {
											result += "Cannot add untrusted node as admin!";
										}
									} else {
										result += "Already an admin!";
									}
								} else {
									result += "Argument expected: nodeid";
								}
								break;
							case "remove":
								if (args.Length > 2) {
									var id = args[2];
									if (settings.AdminIDs.Count == 1) {
										result += "Cannot remove last admin.";
									} else {
										if (settings.AdminIDs.Contains(id)) {
											settings.AdminIDs.Remove(id);
											settings.SaveSettings();
										} else {
											result += "That ID is not an admin.";
										}
									}
								} else {
									result += "Argument expected: nodeid";
								}
								break;
							default:
								result += "admin list - show all admins\n";
								result += "admin add <nodeid> - add new admin\n";
								result += "admin remove <nodeid> - remove existing admin\n";
								break;
						}
					} else {
						result += "Argument expected: admin command.";
					}

					break;
				default:
					result += "Unknown command";
					break;
			}
			return result + "\n";
		}

		#region ILogger implementation
		public void Log (LogLevel level, string message)
		{
			LogItem(level.ToString() + ": " + message);
		}

		public EnabledLoggingLevel EnabledLevel {
			get {
				return EnabledLoggingLevel.All;
			}
		}

		public string Name {
			get {
				return "Console";
			}
		}
		#endregion

	    static IPlatform getPlatform()
	    {
	        // FIXME
//            if (Environment.OSVersion.Platform == PlatformID.Unix) {
//				if (Utils.OSName Utils.OSName == "Linux") {
//					return new LinuxPlatform();
//				} else if (Utils.OSName == "Darwin") {
					return new OSXPlatform();
//				} else {
//					throw new Exception(string.Format("Unsupported operating system: {0}", Utils.OSName));
//				}
//			} else {
//				Core.OS = new WindowsPlatform();
//			}
	    }
	}
}
