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
using System.Xml.Serialization;
using System.Runtime.InteropServices;
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
		public string RoomName;
		public string PasswordTest;
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
	public struct SharedDirectoryInfo
	{
		public string Name;
		public string FullPath;
		public string[] Directories;
		public SharedFileListing[] Files;
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
		public SharedDirListing[] Directories;
		public SharedFileListing[] Files;
		public bool ExeededLimit;
		public int Page;
	}

	[Serializable]
	public struct SharedFileDetails
	{
		public string Name;
		public long Size;
		public string InfoHash;

		public string DirPath;

		public int PieceLength;
		public string[] Pieces;

		public SharedFileDetails(IFile file)
		{
			this.Name = file.Name;
			this.Size = file.Size;
			this.InfoHash = file.InfoHash;

			this.DirPath = file.Parent.FullPath;
			this.PieceLength = file.PieceLength;
			this.Pieces = file.Pieces;
		}
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
		
		public SharedFileListing()
		{

		}

		public SharedFileListing(string name, string fullpath, string infoHash, long size, FileType type)
		{
			this.name = name;
			this.fullPath = fullpath;
			this.infoHash = infoHash;
			this.size = size;
			this.type = type;
		}

		public SharedFileListing(IFile file)
		{
			if (file.InfoHash == null) {
				throw new ArgumentException("File must have InfoHash");
			}

			this.name = file.Name;
			this.fullPath = file.FullPath;
			this.size = file.Size;
			this.infoHash = file.InfoHash;
			this.type = FileType.Other; // XXX: <<-
		}
	}

	[Serializable]
	public class SharedDirListing : ISharedListing
	{
		string name;
		string fullPath;
		SharedFileListing[] files;

		public SharedDirListing ()
		{

		}

		public SharedDirListing (IDirectory dir)
		{
			this.name = dir.Name;
			this.fullPath = dir.FullPath;
		}

		public SharedDirListing (string name, string fullPath)
		{
			this.name = name;
			this.fullPath = fullPath;
		}

		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}

		public string FullPath {
			get {
				return fullPath;
			}
		}
		
		public long Size {
			get {
				if (files != null) {
					return files.Length;
				} else {
					return 0;
				}
			}
		}

		public SharedFileListing[] Files {
			get {
				return files;
			}
			set {
				files = value;
			}
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
		public string RoomName;
		public string Message;
		public string Password;
	}

	[Serializable]
	public struct ChatRoomInfo
	{
		public string Name;
		public string Topic;
		public string[] Users; /* List of NodeIDs */
		public string PasswordTest;
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
