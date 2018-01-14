//
// MessageContentClasses.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2006 Meshwork Authors
//

using System;
using System.Collections.Generic;
using Meshwork.Backend.Core.Destination;
using Meshwork.Backend.Feature.FileBrowsing.Filesystem;
using Meshwork.Common;

namespace Meshwork.Backend.Core.Protocol
{
	public class NodeInfo
	{
		public string NodeID { get; set; }
		public string NickName { get; set; }
		public string RealName { get; set; }
		public string Email { get; set; }
		public string ClientName { get; set; }
		public string ClientVersion { get; set; }
		public long Bytes { get; set; }
		public long Files { get; set; }
		public long AvatarSize { get; set; }
		public ConnectionInfo[] KnownConnections { get; set; }
		public ChatRoomInfo[] KnownChatRooms { get; set; }
		public MemoInfo[] KnownMemos { get; set; }
		public DestinationInfo[] DestinationInfos { get; set; }
	}

	public class HelloInfo
	{
		public string MyNickName { get; set; }
		public ConnectionInfo[] KnownConnections { get; set; }
		public ChatRoomInfo[] KnownChatRooms { get; set; }
		public MemoInfo[] KnownMemos { get; set; }
	}

	public class ChatAction
	{
		public string RoomId { get; set; }
		public string RoomName { get; set; }
	}

	public class KeyInfo
	{
		public string Info { get; set; } // FIXME: Is this needed?
		public string Key { get; set; }
	}

	public class ChatMessage
	{
		public string RoomId { get; set; }
		public string RoomName { get; set; }
		public string Message { get; set; }
	}

	public class AuthInfo
	{
		public int ProtocolVersion { get; set; }
		public string NetworkName { get; set; }
		public string NickName { get; set; }
	}

	public class SharedDirectoryInfo : ISharedListing
	{
		public SharedDirectoryInfo() { }

		public SharedDirectoryInfo(LocalDirectory dir)
		{
			Name = dir.Name;

			// FIXME: Ugly: Remove '/local' from begining of path
			FullPath = "/" + string.Join("/", dir.FullPath.Split('/').Slice(2));
		}

		public string Name { get; set; }
		public string FullPath { get; set; }
		public string[] Directories { get; set; }
		public SharedFileListing[] Files { get; set; }

		public long Size
		{
			get
			{
				return Files == null ? 0 : Files.Length;
			}
		}
	}

	public class SearchRequestInfo
	{
		public SearchRequestInfo() { }

		public SearchRequestInfo(int id, string query, int page)
		{
			Id = id;
			Query = query;
			Page = page;
		}

		public int Id { get; set; }
		public string Query { get; set; }
		public int Page { get; set; }
	}

	public interface ISharedListing
	{
		string Name { get; }
		string FullPath { get; }
		long Size { get; }
	}

	public class SharedFileListing : ISharedListing
	{
		public SharedFileListing() { }

		public SharedFileListing(LocalFile file, bool includePieces)
		{
			if (file.InfoHash == null)
			{
				throw new ArgumentException("File must have InfoHash");
			}

			Name = file.Name;
			FullPath = "/" + string.Join("/", file.FullPath.Split('/').Slice(2));
			Size = file.Size;
			InfoHash = file.InfoHash;
			SHA1 = file.SHA1;
			Type = FileType.Other; // FIXME: Use real file type.
			PieceLength = file.PieceLength;

			if (includePieces)
			{
				Pieces = file.Pieces;
			}
		}

		public string Name { get; set; }
		public string FullPath { get; set; }
		public long Size { get; set; }
		public string InfoHash { get; set; }
		public string SHA1 { get; set; }
		public FileType Type { get; set; }
		public int PieceLength { get; set; }
		public string[] Pieces { get; set; }
		public Dictionary<string, string> Metadata { get; set; }
	}

	public class ConnectionInfo
	{
		public ConnectionInfo() { }

		public ConnectionInfo(
			string sourceNodeID,
			string sourceNodeNickname,
			string destNodeID,
			string destNodeNickname)
		{
			SourceNodeID = sourceNodeID;
			SourceNodeNickname = sourceNodeNickname;
			DestNodeID = destNodeID;
			DestNodeNickname = destNodeNickname;
		}

		public string SourceNodeID { get; set; }
		public string SourceNodeNickname { get; set; }
		public string DestNodeID { get; set; }
		public string DestNodeNickname { get; set; }
	}

	public class ChatInviteInfo
	{
		public string RoomId { get; set; }
		public string RoomName { get; set; }
		public string Message { get; set; }
		public string Password { get; set; }
	}

	public class ChatRoomInfo
	{
		public string Id { get; set; }
		public string Name { get; set; }
		//public string Topic { get; set; }
		public string[] Users { get; set; } /* List of NodeIDs */
	}

	public class TransportDataInfo
	{
		public TransportDataInfo() { }

		public TransportDataInfo(string connectionId, byte[] data)
		{
			ConnectionID = connectionId;
			Data = data;
		}

		public string ConnectionID { get; set; }
		public byte[] Data { get; set; }
	}

	public class RequestFileInfo
	{
		public RequestFileInfo() { }

		public RequestFileInfo(string fullPath, string transferId)
		{
			FullPath = fullPath;
			TransferId = transferId;
		}

		public string FullPath { get; set; }
		public string TransferId { get; set; }
	}

	public class MemoInfo
	{
		public MemoInfo() { }

		public MemoInfo(Memo memo)
		{
			ID = memo.ID;
			FromNodeID = memo.Node.NodeID;
			CreatedOn = memo.CreatedOn;
			Signature = memo.Signature;
			Subject = memo.Subject;
			Text = memo.Text;
		}

		public string ID { get; set; }
		public string FromNodeID { get; set; }
		public DateTime CreatedOn { get; set; }
		public byte[] Signature { get; set; }
		public string Subject { get; set; }
		public string Text { get; set; }
	}

	public class SearchResultInfo
	{
		public int SearchId { get; set; }
		public string[] Directories { get; set; }
		public SharedFileListing[] Files { get; set; }
	}
}
