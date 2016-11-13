//
// MessageBuilder.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.Collections.Generic;
using System.Linq;
using Meshwork.Backend.Core.Protocol;
using Meshwork.Backend.Feature.FileBrowsing.Filesystem;
using Meshwork.Backend.Feature.FileTransfer;

namespace Meshwork.Backend.Core
{
	internal class MessageBuilder
	{
		private Network network;

		public MessageBuilder(Network n) {
			network = n;
		}

		public Message CreateCriticalErrorMessage(Node MessageTo, MeshworkError error) {
			var m = new Message(network, MessageType.CriticalError);
			m.To = MessageTo.NodeID;
			m.Content = error;
			return m;
		}

		public Message CreateHelloMessage ()
		{
			var message = new Message (network, MessageType.Hello);
			var hello = new HelloInfo ();
			
			var connections = new List<ConnectionInfo>();
			var rooms = new List<ChatRoomInfo>();
			var memos = new List<MemoInfo>();

			foreach (var con in network.Connections) {
				if (con.ConnectionState == ConnectionState.Ready | con.ConnectionState == ConnectionState.Remote) {
					var n = new ConnectionInfo();
					var ConnectionSourceNode = con.NodeLocal;
					var ConnectionDestNode = con.NodeRemote;
					n.SourceNodeID = ConnectionSourceNode.NodeID;
					n.SourceNodeNickname = ConnectionSourceNode.NickName;
					n.DestNodeID = ConnectionDestNode.NodeID;
					n.DestNodeNickname = ConnectionDestNode.NickName;
					connections.Add(n);
				}
			}

			foreach (var currentRoom in network.ChatRooms) {
				var tmpRoom = new ChatRoomInfo();
				tmpRoom.Id = currentRoom.Id;
				tmpRoom.Name = currentRoom.Name;
				tmpRoom.Users = new string[currentRoom.Users.Count];
				var x = 0;
				foreach (var node in currentRoom.Users.Values) {
					tmpRoom.Users[x] = node.NodeID;
					x ++;
				}
				rooms.Add(tmpRoom);
			}

			foreach (var currentMemo in network.Memos) {
				var info = new MemoInfo(currentMemo);
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
			var p = new Message(network, MessageType.Auth);
			p.To = messageTo.NodeId;
			var c = new AuthInfo();
			c.ProtocolVersion = Core.ProtocolVersion;
			c.NetworkName = network.NetworkName;
			c.NickName = network.LocalNode.NickName;
			p.Content = c;
			return p;
		}

		public Message CreateAuthReplyMessage(INodeConnection connection, TrustedNodeInfo messageTo)
		{
			var p = new Message(network, MessageType.AuthReply);
			p.To = messageTo.NodeId;
			var c = new AuthInfo();
			c.ProtocolVersion = Core.ProtocolVersion;
			c.NetworkName = network.NetworkName;
			c.NickName = network.LocalNode.NickName;
			c.NetworkName = network.NetworkName;
			p.Content = c;
			return p;
		}

		public Message CreateReadyMessage(Node MessageTo)
		{
			var p = new Message(network, MessageType.Ready);
			p.To = MessageTo.NodeID;
	
			// TODO: Put anything here?
			p.Content = "READY!";
			return p;
		}

		public Message CreatePingMessage(Node MessageTo, ulong timestamp) {
			var p = new Message(network, MessageType.Ping);
			p.To = MessageTo.NodeID;
			p.Content = timestamp;
			return p;
		}

		public Message CreatePongMessage(Node messageTo, ulong timestamp) {
			var p = new Message(network, MessageType.Pong);
			p.To = messageTo.NodeID;
			p.Content = timestamp;
			return p;
		}

		public Message CreateRequestAvatarMessage (Node messageTo)
		{
			var message = new Message(network, MessageType.RequestAvatar);
			message.To = messageTo.NodeID;
			message.Content = "plzkthx";
			return message;
		}

		public Message CreateAvatarMessage (Node messageTo, byte[] avatarData)
		{
			var message = new Message(network, MessageType.Avatar);
			message.To = messageTo.NodeID;
			message.Content = avatarData;
			return message;
		}

		public Message CreateAddMemoMessage (Memo memo)
		{
			var memoInfo = new MemoInfo (memo);

			var message = new Message (network, MessageType.AddMemo);
			message.Content = memoInfo;
			return message;
		}

		public Message CreateDelMemoMessage(Memo theMemo) {
			var theMessage = new Message(network, MessageType.DeleteMemo);
			theMessage.Content = theMemo.ID;
			return theMessage;
		}

		public Message CreateJoinChatMessage (ChatRoom room)
		{
			var p = new Message(network, MessageType.JoinChat);
			var c = new ChatAction();
			c.RoomId = room.Id;
			c.RoomName = room.Name;
			p.Content = c;
			return p;
		}

		public Message CreateLeaveChatMessage (ChatRoom room) 
		{
			var p = new Message(network, MessageType.LeaveChat);
			var c = new ChatAction();
			c.RoomId = room.Id;
			c.RoomName = room.Name;
			p.Content = c;
			return p;
		}

		public Message CreateConnectionDownMessage(string ConnectionSourceNodeID, string ConnectionDestNodeID) {
			var p = new Message(network, MessageType.ConnectionDown);
			var c = new ConnectionInfo();
			c.SourceNodeID = ConnectionSourceNodeID;
			c.DestNodeID = ConnectionDestNodeID;
			p.Content = c;
			return p;
		}

		public Message CreateConnectionDownMessage(Node ConnectionSourceNode, Node ConnectionDestNode) {
			return CreateConnectionDownMessage(ConnectionSourceNode.NodeID, ConnectionDestNode.NodeID);
		}

		public Message CreateNewSessionKeyMessage(Node sessionWith, byte[] keyExchangeBytes) {
			var p = new Message(network, MessageType.NewSessionKey);
			p.To = sessionWith.NodeID;
			p.Content = keyExchangeBytes;
			return p;
		}

		public Message CreateNonCriticalErrorMessage(Node To, MeshworkError error) {
			return CreateNonCriticalErrorMessage(To.NodeID, error);
		}

		public Message CreateNonCriticalErrorMessage(string To, MeshworkError error) {
			var p = new Message(network, MessageType.NonCriticalError);
			p.To = To;
			p.Content = error;
			return p;
		}

		public Message CreateRequestKeyMessage(Node messageto) {
			var m = new Message(network, MessageType.RequestKey);
			m.To = messageto.NodeID;
			m.Content = "MUST...GET...KEY!!!";
			return m;
		}

		public Message CreateRequestInfoMessage(Node MessageTo) {
			var m = new Message(network, MessageType.RequestInfo);
			m.To = MessageTo.NodeID;
			m.Content = "GIMME GIMME GIMME!";
			return m;
		}

		public Message CreateMyKeyMessage (Node messageTo)
		{
			var m = new Message(network, MessageType.MyKey);

			if (messageTo != null) {
				m.To = messageTo.NodeID;
			}

		    m.Content = new KeyInfo
		    {
		        Key = network.Core.CryptoProvider.ToXmlString(false),
		        Info = network.Core.Settings.NickName
		    };

			return m;
		}

		public Message CreateMyInfoMessage(Node MessageTo)
		{
			var p = new Message(network, MessageType.MyInfo);
			p.To = MessageTo.NodeID;
			var t = network.TrustedNodes[MessageTo.NodeID];

			var nodeInfo = new NodeInfo();
			
			nodeInfo.NodeID = network.LocalNode.NodeID;
			nodeInfo.NickName = network.LocalNode.NickName;
			
			nodeInfo.AvatarSize = network.LocalNode.AvatarSize;
				
			if (MessageTo.IsConnectedLocally || t.AllowNetworkInfo) {
				nodeInfo.DestinationInfos = network.Core.DestinationManager.DestinationInfos;
			}
			if (t.AllowProfile) {
				nodeInfo.RealName = network.LocalNode.RealName;
				nodeInfo.Email = network.LocalNode.Email;
			}
			nodeInfo.ClientName = network.LocalNode.ClientName;
			nodeInfo.ClientVersion = network.LocalNode.ClientVersion;
			if (t.AllowSharedFiles) {
				nodeInfo.Bytes = network.LocalNode.Bytes;
				nodeInfo.Files = network.LocalNode.Files;
			}
	
			var connections = new List<ConnectionInfo>();
			var rooms = new List<ChatRoomInfo>();
			var memos = new List<MemoInfo>();

			foreach (var con in network.Connections) {
				if (con.NodeLocal != MessageTo & con.NodeRemote != MessageTo) {
					if (con.ConnectionState == ConnectionState.Ready | con.ConnectionState == ConnectionState.Remote) {
						var n = new ConnectionInfo();
						var ConnectionSourceNode = con.NodeLocal;
						var ConnectionDestNode = con.NodeRemote;
						n.SourceNodeID = ConnectionSourceNode.NodeID;
						n.SourceNodeNickname = ConnectionSourceNode.NickName;
						n.DestNodeID = ConnectionDestNode.NodeID;
						n.DestNodeNickname = ConnectionDestNode.NickName;
						connections.Add (n);
					}
				}
			}

			foreach (var currentRoom in network.ChatRooms) {
				var roomInfo = new ChatRoomInfo();
				roomInfo.Id = currentRoom.Id;
				roomInfo.Name = currentRoom.Name;
				roomInfo.Users = new string[currentRoom.Users.Count];
				var x = 0;
				foreach (var node in currentRoom.Users.Values) {
					roomInfo.Users[x] = node.NodeID;
					x ++;
				}
				rooms.Add(roomInfo);
			}

			foreach (var currentMemo in network.Memos) {
				if (network.Core.IsLocalNode(currentMemo.Node)) {
					var info = new MemoInfo(currentMemo);
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
			var p = new Message(network, MessageType.ChatroomMessage);
			var c = new ChatMessage();
			c.RoomId = room.Id;
			c.RoomName = room.Name;
			c.Message = messageText;
	//		p.To = "";
			p.Content = c;
			return p;
		}

		public Message CreateMessageMessage(Node MessageTo, string MessageText) {
			var p = new Message(network, MessageType.PrivateMessage);
			p.To = MessageTo.NodeID;
			p.Content = MessageText;
			return p;
		}

		public Message CreateRequestDirectoryMessage (Node messageTo, string requestedDirPath)
		{
			var p = new Message(network, MessageType.RequestDirListing);
			p.To = messageTo.NodeID;
			p.Content = requestedDirPath;
			return p;
		}

		public Message CreateRespondDirListingMessage (Node messageTo, LocalDirectory directory)
		{			
			var info = new SharedDirectoryInfo(directory);
			
			info.Files = directory.Files.Select(f => new SharedFileListing((LocalFile)f, false)).ToArray();
			info.Directories = directory.Directories.Select(d => d.Name).ToArray();

			var message = new Message (network, MessageType.RespondDirListing);
			message.To = messageTo.NodeID;
			message.Content = info;
			return message;
		}

		public Message CreateRequestFileMessage(Node node, IFileTransfer transfer)
		{
			var remoteFile = (RemoteFile)transfer.File;
 			var p = new Message(network, MessageType.RequestFile);
 			p.To = node.NodeID;
			p.Content = new RequestFileInfo(remoteFile.RemoteFullPath, transfer.Id);
 			return p;
		}

		public Message CreateChatInviteMessage (Node messageTo, ChatRoom room, string message, string password)
		{
			var p = new Message(network, MessageType.ChatInvite);
			p.To = messageTo.NodeID;
			var c = new ChatInviteInfo();
			c.RoomId = room.Id;
			c.RoomName = room.Name;
			c.Message = message;
			c.Password = password;
			p.Content = c;
			return p;
		}

		public Message CreateAckMessage(string MessageID, Node MessageTo) {
			var p = new Message(network, MessageType.Ack);
			p.To = MessageTo.NodeID;
			p.Content = MessageID;
			return p;
		}

		public Message CreateSearchRequestMessage(int searchRequestId, string searchString, int page)
		{
			var p = new Message(network, MessageType.SearchRequest);
			var c = new SearchRequestInfo(searchRequestId, searchString, page);
			p.Content = c;
			return p;
		}

		public Message CreateFileDetailsMessage (Node sendTo, LocalFile file)
		{
			var message = new Message(network, MessageType.FileDetails);
			message.To = sendTo.NodeID;
			message.Content = new SharedFileListing(file, true);
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
			var p = new Message(network, MessageType.SearchResult);
			p.To = To.NodeID;
			p.Content = result;
			return p;
		}

		public Message CreateTransportConnectMessage (Node to, string connectionId)
		{
			var msg = new Message (network, MessageType.TransportConnect);
			msg.To = to.NodeID;
			msg.Content = connectionId;
			return msg;
		}

		public Message CreateTransportDataMessage (Node to, string connectionId, byte[] data)
		{
			var msg = new Message (network, MessageType.TransportConnect);
			msg.To = to.NodeID;
			msg.Content = new TransportDataInfo(connectionId, data);
			return msg;

		}
	}
}
