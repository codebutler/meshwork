//
// Message.cs: Reperesents a Meshwork message
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2005 FileFind.net (http://filefind.net)
// 
 
using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using FileFind.Meshwork.Exceptions;
using FileFind.Meshwork.Protocol;

namespace FileFind.Meshwork
{
	public class Message
	{
		Network network;
		
		private Message (Network network, byte[] data)
		{
			if (network == null) {
				throw new ArgumentNullException("network");
			}
			
			if (data == null) {
				throw new ArgumentNullException("data");
			}
			
			this.network = network;
			
			// Read message header
			
			int offset = 0;
				    
			signatureLength = EndianBitConverter.ToUInt64(data, offset);
			offset += 8;
			
			signature = new byte[signatureLength];
			Buffer.BlockCopy(data, offset, signature, 0, (int)signatureLength);
			offset += (int)signatureLength;

			from = System.Text.Encoding.ASCII.GetString(data, offset, 32);
			offset += 32;
			
			to = System.Text.Encoding.ASCII.GetString(data, offset, 32);
			offset += 32;

			type = (MessageType)data[offset];
			offset += 1;
			
			id = System.Text.Encoding.ASCII.GetString(data, offset, 32);
			offset += 32;
			
			timestamp = EndianBitConverter.ToUInt64(data, offset);
			offset += 8;

			contentLength = EndianBitConverter.ToInt32(data, offset);
			offset += 4;

			int remainingLength = data.Length - offset;
			if (remainingLength != contentLength) {
				throw new Exception(String.Format("Message size mismatch! Content length should be {0}, was {1}", contentLength, remainingLength));
			}

			byte[] contentBuffer = new byte[contentLength];
			Buffer.BlockCopy(data, offset, contentBuffer, 0, contentLength);
			
			// Now deserialize content

			if (Message.TypeIsEncrypted(type) && to == Network.BroadcastNodeID) {
				// Decrypt if needed
				if (From != Core.MyNodeID) {
					if (network.Nodes.ContainsKey(From)) {
						contentBuffer = Security.Encryption.Decrypt(network.Nodes[From].CreateDecryptor(), contentBuffer);
					} else {
						throw new Exception(String.Format("Node not found: {0}", From));
					}
				} else {
					contentBuffer = Security.Encryption.Decrypt(network.Nodes[To].CreateDecryptor(), contentBuffer);
				}

				if (From != Core.MyNodeID) {
					if (network.TrustedNodes.ContainsKey(from)) {
						bool validSignature = network.TrustedNodes[from].Crypto.VerifyData (contentBuffer, new SHA1CryptoServiceProvider(), signature);
						if (validSignature == false)
							throw new InvalidSignatureException();
					} else {
						throw new Exception ("Unable to verify message signature! (Type: " + type.ToString() + ")");
					}
				} else {
					bool validSignature = Core.CryptoProvider.VerifyData (contentBuffer, new SHA1CryptoServiceProvider(), signature);
					if (validSignature == false) {
						throw new InvalidSignatureException();
					}
				}
			}

			content = Serialization.Binary.Deserialize(contentBuffer);
		}
		
		public static Message Parse (Network network, byte[] data)
		{
			Message message = new Message(network, data);
			return message;
		}

		public Message (Network network, MessageType type)
		{
			if (network == null) {
				throw new ArgumentNullException("network");
			}
			
			this.from = network.LocalNode.NodeID;
			this.type = type;
			this.id = network.CreateMessageID();
			this.timestamp = FileFind.Common.GetUnixTimestamp();

			this.network = network;
		}

		// Messages use the order as layed out:

		ulong signatureLength;
		byte[] signature;				// Cryptographic signature of everything below (8 bytes)

		string from;					// MD5(PublicKey) of sender (32 bytes)
		string to = Network.BroadcastNodeID;		// MD5(PublicKey) of recepient
		MessageType type; 				// MessageTypes enum (1 byte)
		string id;					// Message ID - Randomly generated
		ulong timestamp;				// Unix timestamp of message creation time (8 bytes)
		int contentLength;				// Size of content (4 bytes)
		
		object content;					// Message content (represented as a byte[])

		// --

		public string From {
			get {
				return from;
			}
			set {
				if (!network.Nodes.ContainsKey(value)) {
					throw new Exception ("The specified node was not found (" + value + ").");
				} else {
					this.from = value;
				}
			}
		}

		public string To {
			get {
				if (this.to == null)
					return Network.BroadcastNodeID;
				else
					return this.to;
			}
			set {
				if (value == null) {
					throw new ArgumentNullException("value");
				}

				if ((!network.Nodes.ContainsKey(value)) & value != Network.BroadcastNodeID) {
					throw new Exception ("The specified node was not found (" + value + ").");
				} else {
					this.to = value;
				}
			}
		}
		
		public MessageType Type {
			get {
				return type;
			}
			set {
				this.type = value;
			}
		}

		public string MessageID {
			set {
				if (value.Length == 32)
					this.id = value;
				else
					throw new InvalidOperationException("`MessageID' must be 32 characters.");
			}
			get {
				return id;
			}
		}
		
		public ulong Timestamp {
			set {
				//TODO: verify that it is a valid unix epoch timestamp
				this.timestamp = value;
			}
			get {
				return timestamp;
			}
		}

		public object Content {
			get {
				return content;
			}
			set {
				content = value;
			}
		}

		// --
		
		public byte[] GetAssembledData()
		{
			int index = 0;
			byte[] buffer;

			byte[] contentBytes = Serialization.Binary.Serialize(content);
			
			// Sign before encrypting
			this.signature = Core.CryptoProvider.SignData(contentBytes, new SHA1CryptoServiceProvider());
			this.signatureLength = (ulong)this.signature.Length;
			
			if (Message.TypeIsEncrypted(type) && to == Network.BroadcastNodeID) {

				if (!network.Nodes.ContainsKey(to)) {
					throw new Exception ("network.Nodes[to] was null!... to was " +  to);
				}
			
				// Encrypt if needed
				contentBytes = Security.Encryption.Encrypt(network.Nodes[to].CreateEncryptor(), contentBytes);
			}
			
			signatureLength = (ulong)signature.Length;
			contentLength = contentBytes.Length;
			
			buffer = new byte[8 + (int)signatureLength + 32 + 32 + 1 + 32 + 8 + 4 + contentBytes.Length];
			//buffer = new byte[(int)signatureLength + 32 + 32 + 1 + 32 + 8 + 8 + contentBytes.Length];

			AppendULongToBuffer(signatureLength, ref buffer, ref index);	// 8
			AppendBytesToBuffer(signature, ref buffer, ref index);		// ?

			AppendStringToBuffer(from, ref buffer, ref index);		// 32
			AppendStringToBuffer(to, ref buffer, ref index);		// 32
			AppendByteToBuffer((byte)type, ref buffer, ref index);		// 1
			AppendStringToBuffer(id, ref buffer, ref index);		// 32

			AppendULongToBuffer(timestamp, ref buffer, ref index);		// 8
			AppendIntToBuffer(contentLength, ref buffer, ref index);	// 4

			AppendBytesToBuffer(contentBytes, ref buffer, ref index);		// ?

			if (index != buffer.Length)
				throw new Exception ("Array was the wrong size: " + index + " " + buffer.Length);

			return buffer;
		}
		
		private static void AppendIntToBuffer (int data, ref byte[] buffer, ref int index)
		{
			Buffer.BlockCopy(EndianBitConverter.GetBytes(data), 
					0, 
					buffer, 
					index, 
					4);
			index += 4;
		}

		private static void AppendULongToBuffer (ulong data, ref byte[] buffer, ref int index)
		{
			Buffer.BlockCopy(EndianBitConverter.GetBytes(data), 
					0, 
					buffer, 
					index, 
					8);
			index += 8;
		}
		
		private static void AppendStringToBuffer (string data, ref byte[] buffer, ref int index)
		{
			byte[] bytes = System.Text.Encoding.ASCII.GetBytes(data);
			Buffer.BlockCopy(bytes, 0, buffer, index, data.Length);
			index += data.Length;
		}
		
		private static void AppendBytesToBuffer (byte[] data, ref byte[] buffer, ref int index)
		{
			Buffer.BlockCopy(data, 0, buffer, index, data.Length);
			index += data.Length;
		}
		
		private static void AppendByteToBuffer (byte data, ref byte[] buffer, ref int index)
		{
			Buffer.SetByte(buffer, index, data);
			//Buffer.BlockCopy(data, 0, buffer, index, 1);
			index += 1;
		}
		
		// XXX: Move all this somewhere else!
		public static Dictionary<MessageType, Type> MessageTypeToType;
		static Message ()
		{
			/*
			Message.MessageTypeToType = new Dictionary<MessageType,Type>();
			Message.MessageTypeToType[MessageType.Auth] 		= typeof(AuthInfo);
			Message.MessageTypeToType[MessageType.AuthReply] 	= typeof(AuthInfo);
			Message.MessageTypeToType[MessageType.PrivateMessage] 	= typeof(string);
			Message.MessageTypeToType[MessageType.ChatroomMessage] 	= typeof(ChatMessage);
			Message.MessageTypeToType[MessageType.MyInfo] 		= typeof(NodeInfo);
			Message.MessageTypeToType[MessageType.Ready] 		= typeof(string);
			Message.MessageTypeToType[MessageType.JoinChat] 	= typeof(ChatAction);
			Message.MessageTypeToType[MessageType.LeaveChat] 	= typeof(ChatAction);
			Message.MessageTypeToType[MessageType.ConnectionDown] 	= typeof(ConnectionInfo);
			Message.MessageTypeToType[MessageType.Ping]	 	= typeof(ulong);
			Message.MessageTypeToType[MessageType.Pong] 		= typeof(ulong);
			Message.MessageTypeToType[MessageType.RequestDirListing] = typeof(string);
			Message.MessageTypeToType[MessageType.RespondDirListing] = typeof(SharedDirectoryInfo);
			Message.MessageTypeToType[MessageType.Ack] 		= typeof(string);
			Message.MessageTypeToType[MessageType.SearchResult] 	= typeof(SearchResultInfo);
			Message.MessageTypeToType[MessageType.SearchRequest] 	= typeof(SearchInfo);
			Message.MessageTypeToType[MessageType.RequestFile] 	= typeof(RequestFileInfo);
			Message.MessageTypeToType[MessageType.NonCriticalError] = typeof(MeshworkException);
			Message.MessageTypeToType[MessageType.CriticalError] 	= typeof(MeshworkException);
			Message.MessageTypeToType[MessageType.RequestInfo] 	= typeof(string);
			Message.MessageTypeToType[MessageType.RequestKey]	= typeof(string);
			Message.MessageTypeToType[MessageType.MyKey]		= typeof(KeyInfo);
			Message.MessageTypeToType[MessageType.ChatInvite] 	= typeof(ChatInviteInfo);
			Message.MessageTypeToType[MessageType.SendFile] 	= typeof(SendFileInfo);
			Message.MessageTypeToType[MessageType.AddMemo]		= typeof(MemoInfo);
			Message.MessageTypeToType[MessageType.DeleteMemo] 	= typeof(string);
			Message.MessageTypeToType[MessageType.Hello] 		= typeof(HelloInfo);
			Message.MessageTypeToType[MessageType.NewSessionKey] 	= typeof(byte[]);
//			Message.MessageTypeToType[MessageType.OfferFile] 	= typeof();
			*/
		}

		public static bool TypeIsEncrypted (MessageType type)
		{
			if (Network.InsecureMessageTypes.Contains(type) || Network.LocalOnlyMessageTypes.Contains(type) || Network.UnencryptedMessageTypes.Contains(type)) {
				return false;
			} else {
				return true;
			}
		}
	}	

	public enum MessageType : byte
	{
		Auth 			= 0x00,
		AuthReply 		= 0x01,
		PrivateMessage 		= 0x02,
		ChatroomMessage 	= 0x03,
		MyInfo 			= 0x04,
		Ready 			= 0x05,
		JoinChat 		= 0x06,
		LeaveChat		= 0x07,
		ConnectionDown		= 0x08,
		Ping			= 0x09,
		Pong			= 0x0A,
		RequestDirListing	= 0x0B,
		RespondDirListing	= 0x0C,
		Ack			= 0x0D,
		SearchResult		= 0x0E,
		SearchRequest		= 0x0F,
		RequestFile		= 0x10,
		NonCriticalError	= 0x11,
		CriticalError		= 0x12,
		RequestInfo		= 0x13,
		RequestKey		= 0x14,
		MyKey			= 0x15,
		ChatInvite		= 0x16,
		SendFile		= 0x17,
		AddMemo			= 0x18,
		DeleteMemo		= 0x19,
		Hello			= 0x1A,
		NewSessionKey		= 0x1B,
		FileDetails 		= 0x1C,
		TransportConnect	= 0x1D,
		TransportDisconnect	= 0x1E,
		TransportData		= 0x1F,
		TransportError		= 0x20,
		RequestAvatar           = 0x21,
		Avatar                  = 0x22,
		Test                    = 0x23
	}
}
