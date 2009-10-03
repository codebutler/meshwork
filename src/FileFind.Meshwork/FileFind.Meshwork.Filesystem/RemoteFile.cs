//
// RemoteFile.cs
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2009 FileFind.net (http://filefind.net)
//

using System;
using FileFind.Meshwork.Protocol;

namespace FileFind.Meshwork.Filesystem
{
	public class RemoteFile : AbstractFile, IRemoteDirectoryItem
	{
		RemoteDirectory m_Parent;
		string m_Name;
		string m_InfoHash;
		FileType m_Type;
		long m_Size;

		int m_PieceLength = 0;
		string[] m_Pieces;
		
		internal RemoteFile (RemoteDirectory parent, SharedFileListing listing)
		{
			m_Parent = parent;
			
			m_Name = listing.Name;
			m_InfoHash = listing.InfoHash;
			m_Type = listing.Type;
			m_Size = listing.Size;
			
			m_Pieces = new string[0];
		}

		public override string InfoHash {
			get { return m_InfoHash; }
			internal set {
				m_InfoHash = value;
			}
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
		
		public override void Reload ()
		{
			// FIXME: Reload from disk cache!
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
			internal set {
				m_PieceLength = value;
			}
		}

		public override string[] Pieces {
			get {
				return m_Pieces;
			}
			internal set {
				m_Pieces = value;
			}
		}
		
		public void UpdateWithInfo (SharedFileDetails details)
		{
			m_PieceLength = details.PieceLength;
			m_Pieces = details.Pieces;	
			m_InfoHash = details.InfoHash;
			
			// FIXME: Update cache!!
		}
	}
}
