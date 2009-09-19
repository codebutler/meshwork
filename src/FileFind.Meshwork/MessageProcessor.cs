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
			LoggingService.LogInfo("MessageProcessor: {0} requested public key, sending.", messageFrom.ToString());
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
				LoggingService.LogInfo("MessageProcessor: Re-keying with: {0}.", messageFrom.ToString());
				messageFrom.ClearSessionKey();
			}
			
			if (messageFrom.FinishedKeyExchange == false) {
				LoggingService.LogInfo("Received secure channel key from: {0}.", messageFrom.ToString());
			
				messageFrom.SessionKeyDataHash = keyHash;
				messageFrom.DecryptKeyExchange(key);

				if (messageFrom.RemoteHasKey == true) {
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
			string filePath = PathUtil.Join("/local", info.FullPath);

			LocalFile file = (LocalFile)Core.FileSystem.GetFile(filePath);
			if (file != null) {
				Core.FileTransferManager.StartTransfer(network, messageFrom, file);
			} else {
				LoggingService.LogWarning("Invalid file request from: {0}", messageFrom);
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
					LoggingService.LogWarning("MessageProcessor: Assuming chat room {0} exists and that somebody will be joining it in a moment...", c.Name);
				}
			
				if (!c.Users.ContainsKey(messageFrom.NodeID)) {
					LoggingService.LogWarning("MessageProcessor: Assuming {0} is in {1}...", messageFrom.NickName, c.Name);
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

		internal void ProcessRequestFileDetails (Node messageFrom, string path)
		{
			string directoryPath = PathUtil.Join(Core.MyDirectory.FullPath, path);
			LocalFile file = (LocalFile)Core.FileSystem.GetFile(directoryPath);
			if (file != null)
				network.SendFileDetails(messageFrom, file);
			else
				network.SendRoutedMessage(network.MessageBuilder.CreateNonCriticalErrorMessage(messageFrom, new FileNotFoundException()));
		}
		
		internal void ProcessFileDetailsMessage (Node messageFrom, SharedFileDetails info)
		{
			string filePath = PathUtil.Join(messageFrom.Directory.FullPath, PathUtil.Join(info.DirPath, info.Name));
			
			RemoteFile file = (RemoteFile)Core.FileSystem.GetFile(filePath);
			if (file != null)
				file.UpdateWithInfo(info);
			
			// FIXME: Update cache!

			// If we have a file transfer that was waiting for
			// piece data, start it up!
			IFileTransfer transfer = Core.FileTransferManager.GetTransfer(file);
			if (transfer != null && transfer.Status == FileTransferStatus.WaitingForInfo) {
				((IFileTransferInternal)transfer).DetailsReceived();
			}
			
			network.RaiseReceivedFileDetails(file);
		}

		internal void ProcessConnectionDownMessage (Node messageFrom, ConnectionInfo info)
		{
			INodeConnection c = network.Connections.FindConnection (info.SourceNodeID, info.DestNodeID);
			if (c != null) {
				network.Connections.Remove(c);
				network.RaiseConnectionDown (c);
			} else {
				LoggingService.LogWarning("MessageProcessor: ConnectionDown received from {0} for a non-existant connection!", messageFrom);
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
					LoggingService.LogWarning("Ignored a memo with an invalid signature!");
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
					LoggingService.LogWarning("Someone tired to delete someone else's memo!");
				}
			}
		}

		internal void ProcessRequestDirListingMessage (Node messageFrom, string requestedPath)
		{
			try {
				string directoryPath = PathUtil.Join(Core.MyDirectory.FullPath, requestedPath);
				
				if (network.TrustedNodes[messageFrom.NodeID].AllowSharedFiles) {
					if (Core.FileSystem.GetDirectory(directoryPath) != null) {
						network.SendRoutedMessage(network.MessageBuilder.CreateRespondDirListingMessage(messageFrom, directoryPath));
					} else {
						network.SendRoutedMessage(network.MessageBuilder.CreateNonCriticalErrorMessage(messageFrom, new DirectoryNotFoundException(requestedPath)));
					}
				} else {
					network.SendRoutedMessage(network.MessageBuilder.CreateNonCriticalErrorMessage(messageFrom, new MeshworkException("You are not authorized to browse my files.")));
				}
			} catch (Exception ex) {
				network.SendRoutedMessage(network.MessageBuilder.CreateNonCriticalErrorMessage(messageFrom, new DirectoryNotFoundException(requestedPath)));
				throw ex;
			}
		}
		
		internal void ProcessRespondDirListingMessage (Node messageFrom, SharedDirectoryInfo info)
		{
			string fullPath = PathUtil.Join(messageFrom.Directory.FullPath, info.FullPath);
			RemoteDirectory directory = Core.FileSystem.GetDirectory(fullPath) as RemoteDirectory;
			if (directory != null) {
				directory.UpdateFromInfo(info);
				network.RaiseReceivedDirListing(messageFrom, directory);
			} else {
				LoggingService.LogWarning("Unwanted directory listing from " + messageFrom.ToString() + " for " + info.FullPath);
			}
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
