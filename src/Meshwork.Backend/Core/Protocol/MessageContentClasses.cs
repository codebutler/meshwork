//
// MessageContentClasses.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.Collections.Generic;
using Meshwork.Backend.Core.Destination;
using Meshwork.Backend.Feature.FileBrowsing.Filesystem;
using Meshwork.Common;

namespace Meshwork.Backend.Core.Protocol
{
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

	public struct HelloInfo
	{
		public string MyNickName;
		public ConnectionInfo[] KnownConnections;
		public ChatRoomInfo[] KnownChatRooms;
		public MemoInfo[] KnownMemos;
	}
	
	public struct ChatAction
	{
		public string RoomId;
		public string RoomName;
	}

	public struct KeyInfo
	{
		public string Info; // FIXME: Is this needed?
		public string Key;
	}

	public struct ChatMessage
	{
		public string RoomId;
		public string RoomName;
		public string Message;
	}

	public struct AuthInfo
	{
		public int    ProtocolVersion;
		public string NetworkName;
		public string NickName;
	}

	public class SharedDirectoryInfo : ISharedListing
	{
	    public string Name { get; }

	    public string FullPath { get; }

	    public string[] Directories { get;
	        // FIXME: Remove this setter.
	        set; }

	    public SharedFileListing[] Files { get;
	        // FIXME: Remove this setter.
	        set; }

	    public long Size {
			get {
				return Files == null ? 0 : Files.Length;
			}
		}

		public SharedDirectoryInfo (LocalDirectory dir)
		{
			Name = dir.Name;

			// FIXME: Ugly: Remove '/local' from begining of path
			FullPath = "/" + string.Join("/", dir.FullPath.Split('/').Slice(2));
		}
	}

	public struct SearchRequestInfo
	{
		public int Id;
		public string Query;
		public int Page;

		public SearchRequestInfo (int id, string query, int page)
		{
			Id    = id;
			Query = query;
			Page  = page;
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

	public class SharedFileListing : ISharedListing
	{
	    public string Name { get; }

	    public string FullPath { get; }

	    public long Size { get; }

	    public string InfoHash { get; }

	    public string SHA1 { get; }

	    public FileType Type { get; }

	    public int PieceLength { get; }

	    public string[] Pieces { get; }

	    public Dictionary<string, string> Metadata { get; }

	    public SharedFileListing (LocalFile file, bool includePieces)
		{
			if (file.InfoHash == null) {
				throw new ArgumentException("File must have InfoHash");
			}

			Name = file.Name;
			FullPath =  "/" + string.Join("/", file.FullPath.Split('/').Slice(2));
			Size = file.Size;
			InfoHash = file.InfoHash;
			SHA1 = file.SHA1;
			Type = FileType.Other; // FIXME: Use real file type.
			PieceLength = file.PieceLength;

			if (includePieces)
				Pieces = file.Pieces;
		}
	}

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
	
	public struct ChatInviteInfo
	{
		public string RoomId;
		public string RoomName;
		public string Message;
		public string Password;
	}

	public struct ChatRoomInfo
	{
		public string Id;
		public string Name;
		//public string Topic;
		public string[] Users; /* List of NodeIDs */
	}

	public struct TransportDataInfo
	{
		public string ConnectionID;
		public byte[] Data;

		public TransportDataInfo (string connectionId, byte[] data)
		{
			ConnectionID = connectionId;
			Data = data;
		}
	}

	public struct RequestFileInfo
	{
		public string FullPath;
		public string TransferId;

		public RequestFileInfo (string fullPath, string transferId)
		{
			FullPath = fullPath;
			TransferId = transferId;
		}
	}
}
