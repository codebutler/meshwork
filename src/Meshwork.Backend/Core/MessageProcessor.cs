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
using System.Text;
using Meshwork.Backend.Core.Protocol;
using Meshwork.Backend.Feature.FileBrowsing.Filesystem;
using Meshwork.Backend.Feature.FileTransfer;

namespace Meshwork.Backend.Core
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
				var error = new InvalidNetworkNameError(c.NetworkName, network.NetworkName);
				network.SendCriticalError (messageFrom, error);
				throw error.ToException();
			}

			if (c.ProtocolVersion != Core.ProtocolVersion) {
				var e = new VersionMismatchError (c.ProtocolVersion.ToString());
				network.SendCriticalError (messageFrom, e);
				throw new VersionMismatchError().ToException();
			}

			// Update node details:
			var node = network.Nodes[messageFrom.NodeID];
			node.NickName = c.NickName;
			
			if (isReply == false) {
				connection.ConnectionState = ConnectionState.Authenticating;
				network.SendAuthReply (connection, messageFrom);
			} else {
				connection.SendReady ();
			}
		}

		internal void ProcessRequestKeyMessage (Node messageFrom)
		{
			LoggingService.LogInfo("MessageProcessor: {0} requested public key, sending.", messageFrom.ToString());
			network.SendMyKey(messageFrom);
		}

		internal void ProcessMyKeyMessage (Node node, KeyInfo key)
		{
			var acceptKey = network.RaiseReceivedKey (node, key);
			if (acceptKey) {
			    network.AddTrustedNode(new TrustedNodeInfo(node.NickName, node.NodeID, key.Key));
				network.Core.Settings.SyncNetworkInfoAndSave(network.Core);
			}
		}
		
		internal void ProcessRequestInfoMessage (Node messageFrom)
		{
			if (messageFrom.FinishedKeyExchange == false)
				network.SendInfoToTrustedNode(messageFrom);
		}

		internal void ProcessNewSessionKeyMessage (Node messageFrom, byte[] key)
		{
			var keyHash = Common.Utils.SHA512Str(key);
			
			// This lets us create a brand new session key
			// if someone wants that for whatever reason.
			if (messageFrom.SessionKeyDataHash != string.Empty && keyHash != messageFrom.SessionKeyDataHash) {
				LoggingService.LogInfo("MessageProcessor: Re-keying with: {0}.", messageFrom.ToString());
				messageFrom.ClearSessionKey();
			}
			
			if (messageFrom.FinishedKeyExchange == false) {
				LoggingService.LogInfo("Received secure channel key from: {0}.", messageFrom.ToString());
			
				messageFrom.SessionKeyDataHash = keyHash;
				messageFrom.DecryptKeyExchange(key);

				if (messageFrom.RemoteHasKey) {
					LoggingService.LogInfo("Secure communication channel to {0} now avaliable.", messageFrom.ToString());
					network.SendInfoToTrustedNode(messageFrom);
				} else {
					messageFrom.CreateNewSessionKey();
				}
			} else {
				LoggingService.LogWarning("Received secure communication key from: {0}, but key exchange was already finished!", messageFrom.ToString());
			}
		}

		internal void ProcessMyInfoMessage (Node currentNode, NodeInfo nodeInfo)
		{
			var oldNick = currentNode.NickName;

			currentNode.Bytes         = nodeInfo.Bytes;
			currentNode.ClientName    = nodeInfo.ClientName;
			currentNode.ClientVersion = nodeInfo.ClientVersion;
			currentNode.Email         = nodeInfo.Email;
			currentNode.Files         = nodeInfo.Files;
			//currentNode.HostInfo    = nodeInfo.HostInfo;
			currentNode.NickName      = nodeInfo.NickName;
			currentNode.RealName      = nodeInfo.RealName;
			currentNode.AvatarSize    = nodeInfo.AvatarSize;

			var tNode = network.TrustedNodes[currentNode.NodeID];
		    tNode.Update(nodeInfo);

			network.Core.Settings.SyncNetworkInfoAndSave(network.Core);

			/*
			IDirectory userDirectory = network.Directory.GetSubdirectory(currentNode.NodeID);
			if (userDirectory != null) {
				userDirectory.Delete();
			}

			network.Directory.CreateSubdirectory(currentNode.NodeID, currentNode);
			*/
			
			network.RaiseUpdateNodeInfo(oldNick, currentNode);

			network.AppendNetworkState(new NetworkState(nodeInfo));
		}

		internal void ProcessNonCriticalErrorMessage (Node messageFrom, MeshworkError error)
		{
			if (error is NotTrustedError) {
				messageFrom.ClearSessionKey();
				messageFrom.RemotelyUntrusted = true;
			} else if (error is FileTransferError) {
				var id = ((FileTransferError)error).TransferId;
				foreach (var transfer in network.Core.FileTransferManager.Transfers) {
					if (transfer.Id == id) {
						((IFileTransferInternal)transfer).ErrorReceived(messageFrom, (FileTransferError)error);
						break;
					}
				}

			} else {
				network.RaiseReceivedNonCriticalError (messageFrom, error);
			}
		}

		internal void ProcessSearchRequestMessage (Node messageFrom, SearchRequestInfo searchRequest)
		{
			lock (SeenSearchRequests) {
				if (SeenSearchRequests.ContainsKey(searchRequest.Id)) {
					return; // Ignore. We probably saw this same request from the same person
					        // on multiple networks.
				}
			    // Store timestamp so we can cleanup the list later.
			    // XXX: Cleanup not implemented yet
			    SeenSearchRequests[searchRequest.Id] = DateTime.Now;
			}

			var reply = network.Core.FileSystem.SearchFiles(searchRequest.Query);
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
			var filePath = PathUtil.Join("/local", info.FullPath);

			var file = (LocalFile) network.Core.FileSystem.GetFile(filePath);
			if (file != null) {
				network.Core.FileTransferManager.StartTransfer(network, messageFrom, file);
			} else {
				LoggingService.LogWarning("Invalid file request from: {0}", messageFrom);
				network.SendNonCriticalError(messageFrom, new FileNotFoundError(info.FullPath, info.TransferId));
			}
		}
		
		internal void ProcessJoinChatMessage (Node messageFrom, ChatAction action)
		{
			if (action.RoomName != null && action.RoomName.StartsWith("#")) {
				ChatRoom c;
				if (!network.HasChatRoom(action.RoomId)) {
					c = new ChatRoom(network, action.RoomId, action.RoomName);
					network.AddChatRoom(c);
				} else {
					c = network.GetChatRoom(action.RoomId);
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
			if (network.HasChatRoom(invitation.RoomId)) {
				var room = network.GetChatRoom(invitation.RoomId);
				if (room.Users.ContainsKey(messageFrom.NodeID)) {
					network.RaiseReceivedChatInvite (messageFrom, invitation);
				} else {
					network.SendNonCriticalError (messageFrom, new MeshworkError("you tried to invite me to a chatroom that you arent in! shame on you!"));
				}
			} else {
				network.SendNonCriticalError (messageFrom, new MeshworkError("you tried to invite me to a chatroom that doesnt exit! shame on you!"));
			}
		}

		internal void ProcessChatMessage (Node messageFrom, ChatMessage message)
		{
			var c = network.GetChatRoom(message.RoomId);
			
			if (messageFrom != null) {
				
				if (c == null) {
					c = new ChatRoom (network, message.RoomId, message.RoomName);
					network.AddChatRoom(c);
					LoggingService.LogWarning("MessageProcessor: Assuming chat room {0} exists and that somebody will be joining it in a moment...", c.Name);
				}
			
				if (!c.Users.ContainsKey(messageFrom.NodeID)) {
					LoggingService.LogWarning("MessageProcessor: Assuming {0} is in {1}...", messageFrom.NickName, c.Name);
					c.AddUser(messageFrom);

					network.RaiseJoinedChat (messageFrom, c);
				}
				if (c.InRoom) {
					var messageText = message.Message;
					if (c.HasPassword) {
						try {
							var saltBytes = Encoding.UTF8.GetBytes(c.Id);
							messageText = Encryption.PasswordDecrypt(c.Password, messageText, saltBytes);
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

		internal void ProcessRequestFileDetails (Node messageFrom, string path)
		{
			var directoryPath = PathUtil.Join(network.Core.MyDirectory.FullPath, path);
			var file = (LocalFile)network.Core.FileSystem.GetFile(directoryPath);
			if (file != null)
				network.SendFileDetails(messageFrom, file);
			else
				network.SendRoutedMessage(network.MessageBuilder.CreateNonCriticalErrorMessage(messageFrom, new FileNotFoundError(path)));
		}

		internal void ProcessConnectionDownMessage (Node messageFrom, ConnectionInfo info)
		{
			var c = network.FindConnection(info.SourceNodeID, info.DestNodeID);
			if (c != null) {
				network.RemoveConnection(c);
			} else {
				LoggingService.LogWarning("MessageProcessor: ConnectionDown received from {0} for a non-existant connection!", messageFrom);
			}
			network.Cleanup();
		}

		internal void ProcessLeaveChatMessage (Node messageFrom, ChatAction action)
		{
			if (action.RoomName == null || !action.RoomName.StartsWith("#"))
				return;
			
			var room = network.GetChatRoom(action.RoomId);
			if (room != null) {
				if (room.Users.ContainsKey(messageFrom.NodeID)) {
					room.RemoveUser(messageFrom);
					network.RaiseLeftChat(messageFrom, room);					
					if (room.Users.Count == 0) {
						network.RemoveChatRoom(room);
					}
				}
			} else {				
				LoggingService.LogWarning("Received LeaveChat message for unknown room {0}", action.RoomName);
			}
		}

	
		internal void ProcessReadyMessage (LocalNodeConnection connection, Node messageFrom)
		{
			if (connection.ConnectionState != ConnectionState.Ready) {
				connection.NodeRemote.RemotelyUntrusted = false;
				connection.ConnectionState = ConnectionState.Ready;
				connection.RaiseConnectionReady();
				connection.RemoteNodeInfo.LastConnected = DateTime.Now;
			    network.Core.Settings.SyncNetworkInfoAndSave(network.Core);

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
				var message = network.MessageBuilder.CreateHelloMessage ();
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
			var memo = new Memo (network, memoInfo);

			if (!network.Core.IsLocalNode(memo.Node)) {
				if (network.TrustedNodes.ContainsKey(memo.Node.NodeID) && memo.Verify() == false) {
					LoggingService.LogWarning("Ignored a memo with an invalid signature!");
					return;
				}
				network.AddOrUpdateMemo(memo);
			}

		}
		
		internal void ProcessDeleteMemoMessage (Node messageFrom, string memoId)
		{
			if (network.HasMemo(memoId)) {
				var theMemo = network.GetMemo(memoId);
				if (messageFrom == theMemo.Node) {
					network.RemoveMemo(theMemo);
				} else {
					LoggingService.LogWarning("Someone tired to delete someone else's memo!");
				}
			}
		}

		internal void ProcessRequestDirListingMessage (Node messageFrom, string requestedPath)
		{
			try {
				var directoryPath = PathUtil.Join(network.Core.MyDirectory.FullPath, requestedPath);
				
				if (network.TrustedNodes[messageFrom.NodeID].AllowSharedFiles) {
					var directory = network.Core.FileSystem.GetLocalDirectory(directoryPath);
					if (directory != null) {
						network.SendRoutedMessage(network.MessageBuilder.CreateRespondDirListingMessage(messageFrom, directory));
					} else {
						network.SendRoutedMessage(network.MessageBuilder.CreateNonCriticalErrorMessage(messageFrom, new DirectoryNotFoundError(requestedPath)));
					}
				} else {
					network.SendRoutedMessage(network.MessageBuilder.CreateNonCriticalErrorMessage(messageFrom, new MeshworkError("You are not authorized to browse my files.")));
				}
			} catch (Exception ex) {
				network.SendRoutedMessage(network.MessageBuilder.CreateNonCriticalErrorMessage(messageFrom, new DirectoryNotFoundError(requestedPath)));
				throw ex;
			}
		}

		internal void ProcessAckMessage (Node messageFrom, string hash)
		{
			if (network.AckMethods.ContainsKey(hash)) {
				var m = network.AckMethods[hash];
				m.CallMethod(DateTime.Now);
				network.AckMethods.Remove(hash);
			}
		}
	}
}
