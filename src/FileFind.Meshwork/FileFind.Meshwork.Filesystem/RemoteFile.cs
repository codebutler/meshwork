//
// RemoteFile.cs
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2009 FileFind.net (http://filefind.net)
//

using System;
using System.Collections.Generic;
using FileFind.Meshwork.Protocol;

namespace FileFind.Meshwork.Filesystem
{
	public class RemoteFile : AbstractFile, IRemoteDirectoryItem
	{
		RemoteDirectory m_Parent;
		string m_Name;
		string m_InfoHash;
		string m_SHA1;
		FileType m_Type;
		long m_Size;

		int m_PieceLength = 0;
		string[] m_Pieces;
		
		Dictionary<string, string> m_Metadata;
		
		internal RemoteFile (RemoteDirectory parent, SharedFileListing listing)
		{
			m_Parent = parent;
			m_Parent.Network.ReceivedFileDetails += HandleNetworkReceivedFileDetails;
			
			m_Name = listing.Name;
			m_InfoHash = listing.InfoHash;
			m_SHA1 = listing.SHA1;
			m_Type = listing.Type;
			m_Size = listing.Size;
			
			m_PieceLength = listing.PieceLength;
			
			if (listing.Pieces != null) {
				m_Pieces = listing.Pieces;
			} else {
				m_Pieces = new string[0];
			}
		}

		public override string InfoHash {
			get { return m_InfoHash; }
		}
		
		public override string SHA1 {
			get { return m_SHA1; }
		}

		public override string Name {
			get { return m_Name; }
		}

		public override IDirectory Parent {
			get { return m_Parent; }
		}

		public override long Size {
			get { return m_Size; }
		}

		public override string Type {
			get { return m_Type.ToString(); }
		}
		
		public Network Network {
			get { return m_Parent.Network; }
		}

		public Node Node {
			get { return m_Parent.Node; }
		}
		
		public string RemoteFullPath {
			get {
				return "/" + String.Join("/", this.FullPath.Split('/').Slice(3));
			}
		}

		public override int PieceLength {
			get {
				return m_PieceLength;
			}
		}

		public override string[] Pieces {
			get {
				return m_Pieces;
			}
		}
		
		public override Dictionary<string, string> Metadata {
			get { return m_Metadata; }
		}

		void HandleNetworkReceivedFileDetails (Network network, RemoteFile remoteFile)
		{
			if (remoteFile.FullPath == this.FullPath) {
				m_PieceLength = remoteFile.PieceLength;
				m_Pieces = remoteFile.Pieces;
				m_InfoHash = remoteFile.InfoHash;
				m_SHA1 = remoteFile.SHA1;
			}
		}
	}
}
