//
// MessageBuilder.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using FileFind;
using FileFind.Meshwork;
using FileFind.Meshwork.Collections;
using FileFind.Meshwork.Exceptions;
using FileFind.Meshwork.Filesystem;
using FileFind.Meshwork.Protocol;
using FileFind.Meshwork.FileTransfer;
using FileFind.Meshwork.Errors;

namespace FileFind.Meshwork
{
	internal class MessageBuilder
	{
		private Network network;

		public MessageBuilder(Network n) {
			network = n;
		}

		public Message CreateCriticalErrorMessage(Node MessageTo, MeshworkError error) {
			Message m = new Message(network, MessageType.CriticalError);
			m.To = MessageTo.NodeID;
			m.Content = error;
			return m;
		}

		public Message CreateHelloMessage ()
		{
			Message message = new Message (network, MessageType.Hello);
			HelloInfo hello = new HelloInfo ();
			
			List<ConnectionInfo> connections = new List<ConnectionInfo>();
			List<ChatRoomInfo> rooms = new List<ChatRoomInfo>();
			List<MemoInfo> memos = new List<MemoInfo>();

			foreach (INodeConnection con in network.Connections) {
				if (con.ConnectionState == ConnectionState.Ready | con.ConnectionState == ConnectionState.Remote) {
					ConnectionInfo n = new ConnectionInfo();
					Node ConnectionSourceNode = con.NodeLocal;
					Node ConnectionDestNode = con.NodeRemote;
					n.SourceNodeID = ConnectionSourceNode.NodeID;
					n.SourceNodeNickname = ConnectionSourceNode.NickName;
					n.DestNodeID = ConnectionDestNode.NodeID;
					n.DestNodeNickname = ConnectionDestNode.NickName;
					connections.Add(n);
				}
			}

			foreach (ChatRoom currentRoom in network.ChatRooms) {
				ChatRoomInfo tmpRoom = new ChatRoomInfo();
				tmpRoom.Id = currentRoom.Id;
				tmpRoom.Name = currentRoom.Name;
				tmpRoom.Users = new string[currentRoom.Users.Count];
				int x = 0;
				foreach (Node node in currentRoom.Users.Values) {
					tmpRoom.Users[x] = node.NodeID;
					x ++;
				}
				rooms.Add(tmpRoom);
			}

			foreach (Memo currentMemo in network.Memos) {
				MemoInfo info = new MemoInfo(currentMemo);
				memos.Add(info);
			}

			hello.KnownConnections = connections.ToArray();
			hello.KnownChatRooms = rooms.ToArray();
			hello.KnownMemos = memos.ToArray();
			hello.MyNickName = network.LocalNode.NickName;
			
			message.Content = hello;
			return message;
		}

		public Message CreateAuthMessage(INodeConnection connection, TrustedNodeInfo messageTo)
		{
			Message p = new Message(network, MessageType.Auth);
			p.To = messageTo.NodeID;
			AuthInfo c = new AuthInfo();
			c.ProtocolVersion = Core.ProtocolVersion;
			c.NetworkName = network.NetworkName;
			c.NickName = network.LocalNode.NickName;
			p.Content = c;
			return p;
		}

		public Message CreateAuthReplyMessage(INodeConnection connection, TrustedNodeInfo messageTo)
		{
			Message p = new Message(network, MessageType.AuthReply);
			p.To = messageTo.NodeID;
			AuthInfo c = new AuthInfo();
			c.ProtocolVersion = Core.ProtocolVersion;
			c.NetworkName = network.NetworkName;
			c.NickName = network.LocalNode.NickName;
			c.NetworkName = network.NetworkName;
			p.Content = c;
			return p;
		}

		public Message CreateReadyMessage(Node MessageTo)
		{
			Message p = new Message(network, MessageType.Ready);
			p.To = MessageTo.NodeID;
	
			// TODO: Put anything here?
			p.Content = "READY!";
			return p;
		}

		public Message CreatePingMessage(Node MessageTo, ulong timestamp) {
			Message p = new Message(network, MessageType.Ping);
			p.To = MessageTo.NodeID;
			p.Content = timestamp;
			return p;
		}

		public Message CreatePongMessage(Node messageTo, ulong timestamp) {
			Message p = new Message(network, MessageType.Pong);
			p.To = messageTo.NodeID;
			p.Content = timestamp;
			return p;
		}

		public Message CreateRequestAvatarMessage (Node messageTo)
		{
			Message message = new Message(network, MessageType.RequestAvatar);
			message.To = messageTo.NodeID;
			message.Content = "plzkthx";
			return message;
		}

		public Message CreateAvatarMessage (Node messageTo, byte[] avatarData)
		{
			Message message = new Message(network, MessageType.Avatar);
			message.To = messageTo.NodeID;
			message.Content = avatarData;
			return message;
		}

		public Message CreateAddMemoMessage (Memo memo)
		{
			MemoInfo memoInfo = new MemoInfo (memo);

			Message message = new Message (network, MessageType.AddMemo);
			message.Content = memoInfo;
			return message;
		}

		public Message CreateDelMemoMessage(Memo theMemo) {
			Message theMessage = new Message(network, MessageType.DeleteMemo);
			theMessage.Content = theMemo.ID;
			return theMessage;
		}

		public Message CreateJoinChatMessage (ChatRoom room)
		{
			Message p = new Message(network, MessageType.JoinChat);
			ChatAction c = new ChatAction();
			c.RoomId = room.Id;
			c.RoomName = room.Name;
			p.Content = c;
			return p;
		}

		public Message CreateLeaveChatMessage (ChatRoom room) 
		{
			Message p = new Message(network, MessageType.LeaveChat);
			ChatAction c = new ChatAction();
			c.RoomId = room.Id;
			c.RoomName = room.Name;
			p.Content = c;
			return p;
		}

		public Message CreateConnectionDownMessage(string ConnectionSourceNodeID, string ConnectionDestNodeID) {
			Message p = new Message(network, MessageType.ConnectionDown);
			ConnectionInfo c = new ConnectionInfo();
			c.SourceNodeID = ConnectionSourceNodeID;
			c.DestNodeID = ConnectionDestNodeID;
			p.Content = c;
			return p;
		}

		public Message CreateConnectionDownMessage(Node ConnectionSourceNode, Node ConnectionDestNode) {
			return CreateConnectionDownMessage(ConnectionSourceNode.NodeID, ConnectionDestNode.NodeID);
		}

		public Message CreateNewSessionKeyMessage(Node sessionWith, byte[] keyExchangeBytes) {
			Message p = new Message(network, MessageType.NewSessionKey);
			p.To = sessionWith.NodeID;
			p.Content = keyExchangeBytes;
			return p;
		}

		public Message CreateNonCriticalErrorMessage(Node To, MeshworkError error) {
			return CreateNonCriticalErrorMessage(To.NodeID, error);
		}

		public Message CreateNonCriticalErrorMessage(string To, MeshworkError error) {
			Message p = new Message(network, MessageType.NonCriticalError);
			p.To = To;
			p.Content = error;
			return p;
		}

		public Message CreateRequestKeyMessage(Node messageto) {
			Message m = new Message(network, MessageType.RequestKey);
			m.To = messageto.NodeID;
			m.Content = "MUST...GET...KEY!!!";
			return m;
		}

		public Message CreateRequestInfoMessage(Node MessageTo) {
			Message m = new Message(network, MessageType.RequestInfo);
			m.To = MessageTo.NodeID;
			m.Content = "GIMME GIMME GIMME!";
			return m;
		}

		public Message CreateMyKeyMessage (Node messageTo)
		{
			Message m = new Message(network, MessageType.MyKey);

			if (messageTo != null) {
				m.To = messageTo.NodeID;
			}

			KeyInfo info = new KeyInfo ();
			info.Key = Core.CryptoProvider.ToXmlString (false);
			info.Info = Core.Settings.NickName;

			m.Content = info;

			return m;
		}

		public Message CreateMyInfoMessage(Node MessageTo)
		{
			Message p = new Message(network, MessageType.MyInfo);
			p.To = MessageTo.NodeID;
			TrustedNodeInfo t = network.TrustedNodes[MessageTo.NodeID];

			NodeInfo nodeInfo = new NodeInfo();
			
			nodeInfo.NodeID = network.LocalNode.NodeID;
			nodeInfo.NickName = network.LocalNode.NickName;
			
			nodeInfo.AvatarSize = network.LocalNode.AvatarSize;
				
			if (MessageTo.IsConnectedLocally == true || t.AllowNetworkInfo == true) {
				nodeInfo.DestinationInfos = Core.DestinationManager.DestinationInfos;
			}
			if (t.AllowProfile == true) {
				nodeInfo.RealName = network.LocalNode.RealName;
				nodeInfo.Email = network.LocalNode.Email;
			}
			nodeInfo.ClientName = network.LocalNode.ClientName;
			nodeInfo.ClientVersion = network.LocalNode.ClientVersion;
			if (t.AllowSharedFiles == true) {
				nodeInfo.Bytes = network.LocalNode.Bytes;
				nodeInfo.Files = network.LocalNode.Files;
			}
	
			List<ConnectionInfo> connections = new List<ConnectionInfo>();
			List<ChatRoomInfo> rooms = new List<ChatRoomInfo>();
			List<MemoInfo> memos = new List<MemoInfo>();

			foreach (INodeConnection con in network.Connections) {
				if (con.NodeLocal != MessageTo & con.NodeRemote != MessageTo) {
					if (con.ConnectionState == ConnectionState.Ready | con.ConnectionState == ConnectionState.Remote) {
						ConnectionInfo n = new ConnectionInfo();
						Node ConnectionSourceNode = con.NodeLocal;
						Node ConnectionDestNode = con.NodeRemote;
						n.SourceNodeID = ConnectionSourceNode.NodeID;
						n.SourceNodeNickname = ConnectionSourceNode.NickName;
						n.DestNodeID = ConnectionDestNode.NodeID;
						n.DestNodeNickname = ConnectionDestNode.NickName;
						connections.Add (n);
					}
				}
			}

			foreach (ChatRoom currentRoom in network.ChatRooms) {
				ChatRoomInfo roomInfo = new ChatRoomInfo();
				roomInfo.Id = currentRoom.Id;
				roomInfo.Name = currentRoom.Name;
				roomInfo.Users = new string[currentRoom.Users.Count];
				int x = 0;
				foreach (Node node in currentRoom.Users.Values) {
					roomInfo.Users[x] = node.NodeID;
					x ++;
				}
				rooms.Add(roomInfo);
			}

			foreach (Memo currentMemo in network.Memos) {
				if (Core.IsLocalNode(currentMemo.Node)) {
					MemoInfo info = new MemoInfo(currentMemo);
					memos.Add(info);
				}
			}

			nodeInfo.KnownConnections = connections.ToArray();
			nodeInfo.KnownChatRooms = rooms.ToArray();
			nodeInfo.KnownMemos = memos.ToArray();

			p.Content = nodeInfo;
			return p;
		}

		public Message CreateChatMessageMessage(ChatRoom room, string messageText) {
			Message p = new Message(network, MessageType.ChatroomMessage);
			ChatMessage c = new ChatMessage();
			c.RoomId = room.Id;
			c.RoomName = room.Name;
			c.Message = messageText;
	//		p.To = "";
			p.Content = c;
			return p;
		}

		public Message CreateMessageMessage(Node MessageTo, string MessageText) {
			Message p = new Message(network, MessageType.PrivateMessage);
			p.To = MessageTo.NodeID;
			p.Content = MessageText;
			return p;
		}

		public Message CreateRequestDirectoryMessage (Node messageTo, string requestedDirPath)
		{
			Message p = new Message(network, MessageType.RequestDirListing);
			p.To = messageTo.NodeID;
			p.Content = requestedDirPath;
			return p;
		}

		public Message CreateRespondDirListingMessage (Node messageTo, string dirPath)
		{
			LocalDirectory directory = (LocalDirectory)Core.FileSystem.GetDirectory(dirPath);
			
			SharedDirectoryInfo info = new SharedDirectoryInfo(directory);
			
			List<SharedFileListing> files = new List<SharedFileListing>();
			foreach (IFile file in directory.Files) {
				SharedFileListing fileInfo = new SharedFileListing((LocalFile)file);
				files.Add(fileInfo);
			}
			info.Files = files.ToArray();

			string[] directories = new string[directory.Directories.Length];
			for (int x = 0; x < directory.Directories.Length; x++) {
				IDirectory subDirectory = directory.Directories[x];
				directories[x] = subDirectory.Name;
			}
			info.Directories = directories;

			Message message = new Message (network, MessageType.RespondDirListing);
			message.To = messageTo.NodeID;
			message.Content = info;
			return message;
		}

		public Message CreateRequestFileMessage(Node node, IFileTransfer transfer)
		{
			var remoteFile = (RemoteFile)transfer.File;
 			Message p = new Message(network, MessageType.RequestFile);
 			p.To = node.NodeID;
			p.Content = new RequestFileInfo(remoteFile.RemoteFullPath, transfer.Id);
 			return p;
		}

		public Message CreateChatInviteMessage (Node messageTo, ChatRoom room, string message, string password)
		{
			Message p = new Message(network, MessageType.ChatInvite);
			p.To = messageTo.NodeID;
			ChatInviteInfo c = new ChatInviteInfo();
			c.RoomId = room.Id;
			c.RoomName = room.Name;
			c.Message = message;
			c.Password = password;
			p.Content = c;
			return p;
		}

		public Message CreateAckMessage(string MessageID, Node MessageTo) {
			Message p = new Message(network, MessageType.Ack);
			p.To = MessageTo.NodeID;
			p.Content = MessageID;
			return p;
		}

		public Message CreateSearchRequestMessage(int searchRequestId, string searchString, int page)
		{
			Message p = new Message(network, MessageType.SearchRequest);
			SearchRequestInfo c = new SearchRequestInfo(searchRequestId, searchString, page);
			p.Content = c;
			return p;
		}

		public Message CreateFileDetailsMessage (Node sendTo, LocalFile file)
		{
			Message message = new Message(network, MessageType.FileDetails);
			message.To = sendTo.NodeID;
			message.Content = new SharedFileListing(file);
			return message;
		}

		public Message CreateSendFileMessage(Node sendTo, string filePath, long fileSize)
		{
			throw new NotImplementedException();
			/*
			Message m = new Message(network, MessageType.SendFile);
			m.To = SendTo.NodeID;
			SharedFileInfo c = new SharedFileInfo();
			c.FileName = FilePath.Substring(FilePath.LastIndexOf("/") + 1);
			c.FileSize = FileSize;
			c.FileFullPath = FilePath;
			m.Content = c);
			return m;
			*/
		}

		public Message CreateSendFileMessage(Node SendTo, IFile theFile)
		{
			return CreateSendFileMessage(SendTo, theFile.FullPath, theFile.Size);
		}

		public Message CreateSearchReplyMessage(Node To, SearchResultInfo result)
		{
			Message p = new Message(network, MessageType.SearchResult);
			p.To = To.NodeID;
			p.Content = result;
			return p;
		}

		public Message CreateTransportConnectMessage (Node to, string connectionId)
		{
			Message msg = new Message (network, MessageType.TransportConnect);
			msg.To = to.NodeID;
			msg.Content = connectionId;
			return msg;
		}

		public Message CreateTransportDataMessage (Node to, string connectionId, byte[] data)
		{
			Message msg = new Message (network, MessageType.TransportConnect);
			msg.To = to.NodeID;
			msg.Content = new TransportDataInfo(connectionId, data);
			return msg;

		}
	}
}
