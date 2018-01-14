//
// RemoteFile.cs
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2009 Meshwork Authors
//

using System.Collections.Generic;
using Meshwork.Backend.Core;
using Meshwork.Backend.Core.Protocol;
using Meshwork.Common;

namespace Meshwork.Backend.Feature.FileBrowsing.Filesystem
{
	public class RemoteFile : AbstractFile, IRemoteDirectoryItem
	{
		RemoteDirectory m_Parent;
		string m_Name;
		string m_InfoHash;
		string m_SHA1;
		FileType m_Type;
		long m_Size;

		int m_PieceLength;
		string[] m_Pieces;
		
		Dictionary<string, string> m_Metadata;
		
		internal RemoteFile (RemoteDirectory parent, SharedFileListing listing)
		{
			m_Parent = parent;
			
			m_Name = listing.Name;
			UpdateFromInfo(listing);
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
				return "/" + string.Join("/", FullPath.Split('/').Slice(3));
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

		internal void UpdateFromInfo (SharedFileListing listing)
		{
			m_PieceLength = listing.PieceLength;
			m_Pieces      = (listing.Pieces == null) ? new string[0] : listing.Pieces;
			m_InfoHash    = listing.InfoHash;
			m_SHA1        = listing.SHA1;
			m_Type        = listing.Type;
			m_Size        = listing.Size;
		}
	}
}
