//
// MessageContentClasses.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FileFind.Collections;
using FileFind.Meshwork.Filesystem;
using FileFind.Meshwork.FileTransfer.BitTorrent;
using FileFind.Meshwork.Destination;

namespace FileFind.Meshwork.Protocol
{
	[Serializable]
	public struct NodeInfo
	{
		public string NodeID;
		public string NickName;
		public string RealName;
		public string Email;
		public string ClientName;
		public string ClientVersion;
		public long Bytes;
		public long Files;
		public long AvatarSize;
		public ConnectionInfo[] KnownConnections;
		public ChatRoomInfo[] KnownChatRooms;
		public MemoInfo[] KnownMemos;
		public DestinationInfo[] DestinationInfos;
	}

	[Serializable]
	public struct HelloInfo
	{
		public string MyNickName;
		public ConnectionInfo[] KnownConnections;
		public ChatRoomInfo[] KnownChatRooms;
		public MemoInfo[] KnownMemos;
	}
	
	[Serializable]
	public struct ChatAction
	{
		public string RoomId;
		public string RoomName;
	}

	[Serializable]
	public struct KeyInfo
	{
		public string Info;
		public string Key;
	}

	[Serializable]
	public struct ChatMessage
	{
		public string RoomId;
		public string RoomName;
		public string Message;
	}

	[Serializable]
	public struct AuthInfo
	{
		public int    ProtocolVersion;
		public string NetworkName;
		public string NickName;
	}

	[Serializable]
	public class SharedDirectoryInfo : ISharedListing
	{
		string m_Name;
		string m_FullPath;
		string[] m_Directories;
		SharedFileListing[] m_Files;
		
		public string Name {
			get {
				return m_Name;
			}
		}
		
		public string FullPath {
			get {
				return m_FullPath;
			}
		}
		
		public string[] Directories {
			get {
				return m_Directories;
			}
			// FIXME: Remove this setter.
			set {
				m_Directories = value;
			}
		}
		
		public SharedFileListing[] Files {
			get {
				return m_Files;
			}
			// FIXME: Remove this setter.
			set {
				m_Files = value;
			}
		}
		
		public long Size {
			get {
				return this.Files == null ? 0 : this.Files.Length;
			}
		}
		
		public SharedDirectoryInfo (LocalDirectory dir)
		{
			m_Name = dir.Name;
			
			// FIXME: Ugly: Remove '/local' from begining of path
			m_FullPath = "/" + String.Join("/", dir.FullPath.Split('/').Slice(2));
		}
	}

	[Serializable]
	public struct SearchRequestInfo
	{
		public int Id;
		public string Query;
		public int Page;

		public SearchRequestInfo (int id, string query, int page)
		{
			this.Id    = id;
			this.Query = query;
			this.Page  = page;
		}
	}

	[Serializable]
	public struct SearchResultInfo
	{
		public int SearchId;
		public SharedDirectoryInfo[] Directories;
		public SharedFileListing[] Files;
		public bool ExeededLimit;
		public int Page;
	}

	public interface ISharedListing
	{
		string Name {
			get;
		}
	
		string FullPath {
			get;
		}
			
		long Size {
			get;
		}
	}
	
	[Serializable]
	public class SharedFileListing : ISharedListing
	{
		string   fullPath;
		string   name;
		long     size;
		FileType type;
		string   infoHash;
		int      pieceLength;
		string[] pieces;
		SerializableDictionary<string, string> metadata;
		
		public string Name {
			get {
				return name;
			}
		}

		public string FullPath {
			get {
				return fullPath;
			}
		}

		public long Size {
			get {
				return size;
			}
		}
		
		public string InfoHash {
			get {
				return infoHash;
			}
		}

		public FileType Type {
			get {
				return type;
			}
		}
		
		public int PieceLength {
			get {
				return pieceLength;
			}
		}
		
		public string[] Pieces {
			get {
				return pieces;
			}
		}
		
		public SerializableDictionary<string, string> Metadata {
			get {
				return metadata;
			}
		}
		
		public SharedFileListing (LocalFile file, bool includePieces)
		{
			if (file.InfoHash == null) {
				throw new ArgumentException("File must have InfoHash");
			}

			this.name = file.Name;
			this.fullPath =  "/" + String.Join("/", file.FullPath.Split('/').Slice(2));
			this.size = file.Size;
			this.infoHash = file.InfoHash;
			this.type = FileType.Other; // FIXME: Use real file type.
			this.pieceLength = file.PieceLength;
			
			if (includePieces)
				this.pieces = file.Pieces;
		}
	}

	[Serializable]
	public struct ConnectionInfo
	{
		public ConnectionInfo(string SourceNodeID, string SourceNodeNickname, string DestNodeID, string DestNodeNickname) {
			this.SourceNodeID = SourceNodeID;
			this.SourceNodeNickname = SourceNodeNickname;
			this.DestNodeID = DestNodeID;
			this.DestNodeNickname = DestNodeNickname;
		}
		public string SourceNodeID;
		public string SourceNodeNickname;
		public string DestNodeID;
		public string DestNodeNickname;
	}
	
	[Serializable]
	public struct ChatInviteInfo
	{
		public string RoomId;
		public string RoomName;
		public string Message;
		public string Password;
	}

	[Serializable]
	public struct ChatRoomInfo
	{
		public string Id;
		public string Name;
		//public string Topic;
		public string[] Users; /* List of NodeIDs */
	}

	[Serializable]
	public struct TransportDataInfo
	{
		public string ConnectionID;
		public byte[] Data;

		public TransportDataInfo (string connectionId, byte[] data)
		{
			this.ConnectionID = connectionId;
			this.Data = data;
		}
	}

	[Serializable]
	public struct RequestFileInfo
	{
		public string FullPath;
		public string TransferId;

		public RequestFileInfo (string fullPath, string transferId)
		{
			this.FullPath = fullPath;
			this.TransferId = transferId;
		}
	}
}
