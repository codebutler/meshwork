using Mono.Unix.Native;
using System;
using System.ServiceProcess;
using System.Diagnostics;
using System.Security.Cryptography;
using IO = System.IO;
using FileFind.Meshwork.Logging;

namespace FileFind.Meshwork.DaemonClient
{
	public class MeshworkDaemon : ILogger
	{
		Settings settings;

		public static int Main (string[] args)
		{
			string syntax = "Syntax: meshwork-daemon [--create-settings] settingsfilename.xml";
			if (args.Length == 0 || args[0] == "--help") {
				Console.WriteLine(syntax);
				return 1;
			} else if (args[0] == "--create-settings") {
				if (args.Length > 1) {
					new SettingsCreator(args[1]);
				} else {
					Console.WriteLine(syntax);
				return 1;
				}
			} else if (!IO.File.Exists(args[0])) {
				Console.WriteLine(syntax);
				return 1;
			} else {
				new MeshworkDaemon(args[0]);
			}
			return 0;
		}

		public MeshworkDaemon (string fileName)
		{
			LogItem("MESHWORK DAEMON CLIENT 0.1");

			FileFind.Common.SetProcessName("meshwork-daemon");

			Stdlib.signal(Signum.SIGINT, handle_signal);
			Stdlib.signal(Signum.SIGKILL, handle_signal);
			
			settings = Settings.ReadSettings(fileName);
			Core.Init (settings);
			Core.AvatarManager = new AvatarManager();
			
			Core.NetworkAdded += AddNetworkEvents;
			LoggingService.AddLogger(this);

			Core.Start();
		}

		private void handle_signal (int signal)
		{
			LogItem("Shutting down...");
			Core.Stop();
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
				Console.Error.WriteLine(String.Format ("{0}: {1}", text, ex.ToString()));
			} else {
				//Syscall.syslog (SyslogLevel.LOG_INFO, text);
				Console.WriteLine(text);
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
				TrustedNodeInfo nodeInfo = new TrustedNodeInfo(publicKey);
			
				if (args.Node != null) {
					nodeInfo.Identifier = args.Node.NickName;
				} else {
					nodeInfo.Identifier = nodeInfo.NodeID;
				}
				
				// First person to connect? Put them in charge!
				if (network.TrustedNodes.Count == 0) {
					LogItem(String.Format("[!] WARNING! {0} is now the admin!", publicKey.Nickname));
					if (!settings.AdminIDs.Contains(nodeInfo.NodeID)) {
						settings.AdminIDs.Add(nodeInfo.NodeID);
						settings.SaveSettings();
					}
					return true;
				}
				
				// Don't accept the key now, but keep it around
				string keyDir = GetKeyDir(network);
				string keyFile = GetKeyFileName(network, nodeInfo.NodeID);
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
					string result = ProcessCommand(network, messageText.Split(' '));
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
			foreach (NetworkInfo n in settings.Networks) {
				if (n.NetworkID == network.NetworkID) {
					networkInfo = n;
					break;
				}
			}
			if (networkInfo == null) {
				throw new Exception("EGADS!!");
			}

			string result = String.Empty;
			switch (args[0]) {
				case "key":
					if (args.Length > 1) {
						switch (args[1]) {
							case "list":
								result += "TRUSTED KEYS\n";
								foreach (TrustedNodeInfo n in network.TrustedNodes.Values) {
									result += "  " + n.NodeID + "\n";
								}
								result += "\nUNTRUSTED KEYS\n";
								foreach (IO.DirectoryInfo dir in new IO.DirectoryInfo("received_keys").GetDirectories()) {
									if (dir.Name == network.NetworkID) {
										foreach (IO.FileInfo file in dir.GetFiles()) {
											result += "  " + file.Name + "\n";
										}
									}
								}
								break;
							case "trust":
								if (args.Length > 2) {
									string id = args[2];
									string keyDir = IO.Path.Combine("received_keys", network.NetworkID); 
									string keyFile = IO.Path.Combine(keyDir, id + ".mpk");

									if (IO.File.Exists(keyFile)) {
										PublicKey key = PublicKey.Parse(IO.File.ReadAllText(keyFile));
										TrustedNodeInfo nodeInfo = new TrustedNodeInfo(key);
										
										networkInfo.TrustedNodes.Add(nodeInfo.NodeID, nodeInfo);
										
										settings.SaveSettings();
										Core.Settings = settings;
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
									string nodeid = args[2];
									Node node = network.Nodes[nodeid];
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
								foreach (string id in settings.AdminIDs) {
									TrustedNodeInfo info = networkInfo.TrustedNodes[id];
									result += info.Identifier + " (" + id + ")\n";
								}

								break;

							case "add":
								if (args.Length > 2) {
									string id = args[2];
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
									string id = args[2];
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
	}
}
