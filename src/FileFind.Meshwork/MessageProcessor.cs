// TODO: Check to see if we arent SendReady-ing in too many places...

//
// MessageProcessor.cs: Processes incoming messages
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2005 FileFind.net (http://filefind.net)
//

using System;
using System.Collections.Generic;
using System.Net;
using FileFind;
using FileFind.Meshwork.Filesystem;
using FileFind.Meshwork.Exceptions;
using FileFind.Meshwork.Protocol;
using FileFind.Meshwork.FileTransfer;
using System.Security.Cryptography;

namespace FileFind.Meshwork
{
	internal class MessageProcessor
	{
		static Dictionary<int,DateTime> SeenSearchRequests = new Dictionary<int,DateTime>();

		Network network;
		internal MessageProcessor (Network network)
		{
			this.network = network;
		}

		internal void ProcessHelloMessage (Node messageFrom, HelloInfo hello)
		{
			messageFrom.NickName = hello.MyNickName;
			network.AppendNetworkState (new NetworkState(hello));
		}

		internal void ProcessPingMessage (Node messageFrom, ulong timestamp)
		{
			network.SendPong(messageFrom, timestamp);
		}
 	
		internal void ProcessRequestAvatarMessage (Node node)
		{
			network.SendAvatar(node);
		}

		internal void ProcessAvatarMessage (Node node, byte[] avatarData)
		{
			network.RaiseReceivedAvatar(node, avatarData);
		}
 
	/*	internal void ProcessPongMessage (Node messageFrom, ulong timestamp)
		{*/
			//TODO: Pinging/ponging is a total mess
		/*	if (timestamp == messageFrom.TimeOfLastPing) {
				messageFrom.PingTime = DateTime.Now.Subtract(messageFrom.TimeOfLastPing).Milliseconds;
				messageFrom.RaisePongReceived(messageFrom.PingTime);
				//messageFrom.TimeOfLastPing = null;
			} else {
				// TODO: Do something?
			}*/
		// }

		/*
		internal void ProcessAuthMessage (LocalNodeConnection connection, Node messageFrom, AuthInfo info)
		{
			ProcessAuthMessage (connection, messageFrom, info, false);
		}
		*/

		internal void ProcessAuthMessage (LocalNodeConnection connection, Node messageFrom, AuthInfo c, bool isReply)
		{
			// Some checks:

			// XXX: Isn't this checked elsewhere?
			if (c.NetworkName != network.NetworkName) {
				InvalidNetworkNameException ex = new InvalidNetworkNameException(c.NetworkName, network.NetworkName);
				network.SendCriticalError (messageFrom, ex);
				throw ex.ToException();
			}

			if (c.ProtocolVersion != Core.ProtocolVersion) {
				VersionMismatchException e = new VersionMismatchException (c.ProtocolVersion.ToString());
				network.SendCriticalError (messageFrom, e);
				throw new VersionMismatchException().ToException();
			}

			// Update node details:
			Node node = network.Nodes[messageFrom.NodeID];
			node.NickName = c.NickName;
			
			if (isReply == false) {
				network.SendAuthReply (connection, messageFrom);
			} else {
				connection.SendReady ();
			}
		}

		internal void ProcessRequestKeyMessage (Node messageFrom)
		{
			LogManager.Current.WriteToLog("{0} requested public key, sending. " + messageFrom.ToString());
			network.SendMyKey(messageFrom);
		}

		internal void ProcessMyKeyMessage (Node node, KeyInfo key)
		{
			bool acceptKey = network.RaiseReceivedKey (node, key);

			if (acceptKey) {
				RSACryptoServiceProvider provider = new RSACryptoServiceProvider ();
				provider.FromXmlString (key.Key);

				TrustedNodeInfo tni = new TrustedNodeInfo();
				tni.NodeID = node.NodeID;
				tni.Identifier = node.NickName;
				tni.EncryptionParameters = provider.ExportParameters(false);

				network.AddTrustedNode(tni);

				Core.Settings.SyncTrustedNodes();
				Core.Settings.SaveSettings();
			}
		}
		
		internal void ProcessRequestInfoMessage (Node messageFrom)
		{
			if (messageFrom.FinishedKeyExchange == false)
				network.SendInfoToTrustedNode(messageFrom);
		}

		internal void ProcessNewSessionKeyMessage (Node messageFrom, byte[] key)
		{
			string keyHash = FileFind.Common.MD5(key);
			
			// This lets us create a brand new session key
			// if someone wants that for whatever reason.
			if (messageFrom.SessionKeyDataHash != String.Empty && keyHash != messageFrom.SessionKeyDataHash) {
				LogManager.Current.WriteToLog("Re-keying with: {0}.", messageFrom.ToString());
				messageFrom.ClearSessionKey();
			}
			
			if (messageFrom.FinishedKeyExchange == false) {
				LogManager.Current.WriteToLog("Received secure channel key from: {0}.", messageFrom.ToString());
			
				messageFrom.SessionKeyDataHash = keyHash;
				messageFrom.DecryptKeyExchange(key);

				if (messageFrom.RemoteHasKey == true) {
					LogManager.Current.WriteToLog("Secure communication channel to {0} now avaliable.", messageFrom.ToString());
					network.SendInfoToTrustedNode(messageFrom);
				} else {
					messageFrom.CreateNewSessionKey();
				}
			} else {
				LogManager.Current.WriteToLog("Received secure communication key from: {0}, but key exchange was already finished!", messageFrom.ToString());
			}
		}

		internal void ProcessMyInfoMessage (Node currentNode, NodeInfo nodeInfo)
		{
			string oldNick = currentNode.NickName;

			currentNode.Bytes         = nodeInfo.Bytes;
			currentNode.ClientName    = nodeInfo.ClientName;
			currentNode.ClientVersion = nodeInfo.ClientVersion;
			currentNode.Email         = nodeInfo.Email;
			currentNode.Files         = nodeInfo.Files;
			//currentNode.HostInfo    = nodeInfo.HostInfo;
			currentNode.NickName      = nodeInfo.NickName;
			currentNode.RealName      = nodeInfo.RealName;
			currentNode.AvatarSize    = nodeInfo.AvatarSize;

			TrustedNodeInfo tNode = network.TrustedNodes[currentNode.NodeID];
			tNode.Identifier = currentNode.NickName;

			tNode.DestinationInfos.Clear();
			tNode.DestinationInfos.AddRange(nodeInfo.DestinationInfos);

			Core.Settings.SyncTrustedNodesAndSave();

			Directory userDirectory = network.Directory.GetSubdirectory(currentNode.NodeID);
			if (userDirectory != null) {
				userDirectory.Delete();
			}

			network.Directory.CreateSubdirectory(currentNode.NodeID, currentNode);
			
			network.RaiseUpdateNodeInfo(oldNick, currentNode);

			network.AppendNetworkState(new NetworkState(nodeInfo));
		}

		internal void ProcessNonCriticalErrorMessage (Node messageFrom, MeshworkException ex)
		{
			if (ex is NotTrustedException) {
				messageFrom.ClearSessionKey();
				messageFrom.RemotelyUntrusted = true;
			} else if (ex is FileTransferException) {
				string id = ((FileTransferException)ex).TransferId;
				foreach (IFileTransfer transfer in Core.FileTransferManager.Transfers) {
					if (transfer.Id == id) {
						((IFileTransferInternal)transfer).ErrorReceived(messageFrom, (FileTransferException)ex);
						break;
					}
				}

			} else {
				network.RaiseReceivedNonCriticalError (messageFrom, ex);
			}
		}

		internal void ProcessSearchRequestMessage (Node messageFrom, SearchRequestInfo searchRequest)
		{
			lock (SeenSearchRequests) {
				if (SeenSearchRequests.ContainsKey(searchRequest.Id)) {
					return; // Ignore. We probably saw this same request from the same person
					        // on multiple networks.
				} else {
					// Store timestamp so we can cleanup the list later.
					// XXX: Cleanup not implemented yet
					SeenSearchRequests[searchRequest.Id] = DateTime.Now;
				}
			}

			SearchResultInfo reply = Core.FileSystem.SearchFiles(searchRequest.Query);
			reply.SearchId = searchRequest.Id;

			if (reply.Files.Length > 0 || reply.Directories.Length > 0) {
				network.SendSearchReply(messageFrom, reply);
			}
		}

		internal void ProcessSearchResultMessage (Node messageFrom, SearchResultInfo result)
		{
			network.RaiseReceivedSearchResult (messageFrom, result);
		}

		internal void ProcessRequestFileMessage (Node messageFrom, RequestFileInfo info)
		{
			string filePath = info.FullPath;
			// Remove network part from path.
			// XXX: This is nasty
			filePath = filePath.Substring(filePath.Split('/')[1].Length + 1);

			File file = File.GetFile(Core.FileSystem, filePath);
			if (file != null && file.NodeID == Core.MyNodeID) {
				Core.FileTransferManager.StartTransfer(network, messageFrom, file);
			} else {
				Console.WriteLine("Invalid file request from: {0}", messageFrom);
				network.SendNonCriticalError(messageFrom, new FileNotFoundException(info.FullPath, info.TransferId));
			}
		}
		
		internal void ProcessJoinChatMessage (Node messageFrom, ChatAction action)
		{
			if (action.RoomName != null && action.RoomName.StartsWith("#")) {
				ChatRoom c;
				if (!network.ChatRooms.ContainsKey(action.RoomName)) {
					c = new ChatRoom(network, action.RoomName);
					c.PasswordTest = action.PasswordTest;
					network.AddChatRoom(c);
				} else {
					c = network.ChatRooms[action.RoomName];
				}

				if (!c.Users.ContainsKey(messageFrom.NodeID)) {
					c.AddUser(messageFrom);
					network.RaiseJoinedChat (messageFrom, c);
				}

			//	Node n = network.Nodes[action.NodeID];
			//	if (n != null) {
			//		if (c.Users[n.NodeID] == null) {
			//			c.Users.Add(n);
			//			network.RaiseJoinedChat (n, c);
			//		}
			//	} else {
			//		
			//	}
			}
		}

		internal void ProcessChatInviteMessage (Node messageFrom, ChatInviteInfo invitation)
		{
			if (network.ChatRooms.ContainsKey(invitation.RoomName)) {
				if (network.ChatRooms[invitation.RoomName].Users.ContainsKey(messageFrom.NodeID)) {
					network.RaiseReceivedChatInvite (messageFrom, invitation);
				} else {
					network.SendNonCriticalError (messageFrom, new MeshworkException("you tried to invite me to a chatroom that you arent in! shame on you!"));
				}
			} else {
				network.SendNonCriticalError (messageFrom, new MeshworkException("you tried to invite me to a chatroom that doesnt exit! shame on you!"));
			}
		}

		internal void ProcessChatMessage (Node messageFrom, ChatMessage message)
		{
			ChatRoom c = network.ChatRooms[message.RoomName];
			

			if (messageFrom != null) {

				if (c == null) {
					c = new ChatRoom (network, message.RoomName);
					network.AddChatRoom(c);
					LogManager.Current.WriteToLog("assuming chat room " + c.Name + " exists and that somebody will be joining it in a moment..");
				}
			
				if (!c.Users.ContainsKey(messageFrom.NodeID)) {
					LogManager.Current.WriteToLog("assuming " + messageFrom.NickName + " is in " + c.Name + "....");
					c.AddUser(messageFrom);

					network.RaiseJoinedChat (messageFrom, c);
				}
				if (c.InRoom == true) {
					string messageText = message.Message;
					if (c.PasswordTest != null) {
						try {
							messageText = Security.Encryption.PasswordDecrypt(c.Password, messageText);
						} catch (Exception) {
							messageText = "<UNABLE TO DECRYPT MESSAGE>";
						}
					}
					network.RaiseChatMessage (c, messageFrom, messageText);
				}
			} else {
				throw new Exception("A chat message was Received from a non existing user! (NodeID: " + messageFrom + ")");
			}

		}


		internal void ProcessPrivateMessage (Node messageFrom, string messageText)
		{
			network.RaisePrivateMessage (messageFrom, messageText);
		}

		/*
		internal void ProcessSendFileMessage (Node messageFrom, SharedFileInfo file)
		{
			throw new NotImplementedException();
			//network.RaiseFileOffered (messageFrom, file);
		}
		*/

		internal void ProcessFileDetailsMessage (Node messageFrom, SharedFileDetails info)
		{
			string filePath = PathUtil.Join("/", PathUtil.Join(network.NetworkID, PathUtil.Join(info.DirPath, info.Name)));

			File file = File.GetFile(Core.FileSystem, filePath);
			if (file == null) {
				Console.WriteLine("Hmm, no file... " + filePath);

				Directory directory = Directory.GetDirectory(Core.FileSystem, info.DirPath);
				if (directory == null) {
					directory = Directory.CreateDirectory(Core.FileSystem, info.DirPath, messageFrom);
				}
				file = directory.CreateFile(info, messageFrom);
			}

			file.InfoHash = info.InfoHash;
			file.PieceLength = info.PieceLength;
			file.Pieces = info.Pieces;
			file.Save();

			// If we have a file transfer that was waiting for
			// piece data, start it up!
			IFileTransfer transfer = Core.FileTransferManager.GetTransfer(file);
			if (transfer != null && transfer.Status == FileTransferStatus.WaitingForInfo) {
				((IFileTransferInternal)transfer).DetailsReceived();
			}
		}

		internal void ProcessConnectionDownMessage (Node messageFrom, ConnectionInfo info)
		{
			INodeConnection c = network.Connections.FindConnection (info.SourceNodeID, info.DestNodeID);
			if (c != null) {
				network.Connections.Remove(c);
				network.RaiseConnectionDown (c);
			} else {
				//LogManager.Current.WriteToLog("ConnectionDown Received from " + messageFrom.IpAddress.ToString() + " for a non existant connection!");
			}
			network.Cleanup();
		}

		internal void ProcessLeaveChatMessage (Node messageFrom, ChatAction action)
		{
			if (action.RoomName != null && action.RoomName.StartsWith("#")) {
				//Node n = network.Nodes[action.NodeID];
				ChatRoom c = network.GetChatRoom(action.RoomName);
				if (c != null) {
					if (c.Users.ContainsKey(messageFrom.NodeID)) {
						c.RemoveUser(messageFrom);
						/*
						if (messageFrom == network.LocalNode) {
							c.InRoom = false;
						}*/

						network.RaiseLeftChat(messageFrom,c);
						
						if (c.Users.Count == 0) {
							network.RemoveChatRoom(c.Name);
						}
					}
				} else {
				}
			} else {
			}
		}

	
		internal void ProcessReadyMessage (LocalNodeConnection connection, Node messageFrom)
		{
			if (connection.ConnectionState != ConnectionState.Ready) {
				connection.NodeRemote.RemotelyUntrusted = false;
				connection.ConnectionState = ConnectionState.Ready;
				connection.RaiseConnectionReady();
				connection.RemoteNodeInfo.LastConnected = DateTime.Now;
				Core.Settings.SyncTrustedNodesAndSave();

				if (connection.ReadySent == false) {
					connection.SendReady();
				}

				if (messageFrom.FinishedKeyExchange == false &&
				    messageFrom.SentKeyExchange == false) {
					messageFrom.CreateNewSessionKey();
				}

				// The network needs to know about me this new connection,
				// any nodes I know about, memos, etc... so say hi to
				// everyone and let them know everything that I know.
				Message message = network.MessageBuilder.CreateHelloMessage ();
				message.To = Network.BroadcastNodeID;
				network.SendBroadcast (message);

			} else {
				// XXX: Do we need this?
				if (connection.ReadySent == false) {
					connection.SendReady();
				}
			}
		}

		internal void ProcessAddMemoMessage (Node messageFrom, MemoInfo memoInfo)
		{
			Memo memo = new Memo (network, memoInfo);

			if (memo.WrittenByNodeID != network.LocalNode.NodeID) {
				if (network.TrustedNodes.ContainsKey(memo.WrittenByNodeID) && memo.Verify () == false) {
					network.RaiseNonCriticalError(new Exception("Ignored a memo with an invalid signature!"));
					return;
				}
				if (network.Memos.ContainsKey(memo.ID)) {
					Memo existingMemo = network.Memos[memo.ID];
					//existingMemo.FileLinks = memo.FileLinks;
					existingMemo.Subject = memo.Subject;
					existingMemo.Text = memo.Text;

					network.RaiseMemoUpdated (memo);
					
				} else {
					//memo.Properties = new PropertiesHashtable();
					network.AddMemo(memo);
				}
			}

		}
		
		internal void ProcessDeleteMemoMessage (Node messageFrom, string memoId)
		{
			if (network.Memos.ContainsKey(memoId)) {
				Memo theMemo = network.Memos[memoId];
				if (messageFrom.NodeID == theMemo.WrittenByNodeID) {
					network.RemoveMemo(theMemo);
				} else {
					network.RaiseNonCriticalError (new Exception("Someone tired to delete someone else's memo!"));
				}
			}
		}

		internal void ProcessRequestDirListingMessage (Node messageFrom, string directoryPath)
		{
			directoryPath = directoryPath.Substring (network.Directory.FullPath.Length); // HACK ewwwwewwwewew
			if (network.TrustedNodes[messageFrom.NodeID].AllowSharedFiles) {
				if (Directory.GetDirectory(Core.FileSystem, directoryPath) != null) {
					network.SendRespondDirListing (messageFrom, directoryPath);
				} else {
					network.SendRoutedMessage(network.MessageBuilder.CreateNonCriticalErrorMessage(messageFrom, new DirectoryNotFoundException(directoryPath)));
				}
			} else {
				network.SendRoutedMessage(network.MessageBuilder.CreateNonCriticalErrorMessage(messageFrom, new MeshworkException("You are not authorized to browse my files.")));
			}

		}
		
		internal void ProcessRespondDirListingMessage (Node messageFrom, SharedDirectoryInfo info)
		{
			string fullPath = PathUtil.Join(network.Directory.FullPath, info.FullPath);

			Directory directory = Directory.GetDirectory(Core.FileSystem, fullPath);
			if (directory == null) {
				directory = Directory.CreateDirectory(Core.FileSystem, fullPath, messageFrom);
			}
			
			directory.ClearDirectories();
			directory.ClearFiles();

			directory.BulkAddFiles(info.Files, messageFrom);
			directory.BulkAddSubdirectories(info.Directories, messageFrom);
			directory.Requested = true;
			
			network.RaiseReceivedDirListing (messageFrom, directory);
		}

		internal void ProcessAckMessage (Node messageFrom, string hash)
		{
			if (network.AckMethods.ContainsKey(hash)) {
				AckMethod m = network.AckMethods[hash];
				m.CallMethod(DateTime.Now);
				network.AckMethods.Remove(hash);
			}
		}
	}
}
