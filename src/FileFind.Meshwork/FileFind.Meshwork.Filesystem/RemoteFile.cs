//
// RemoteFile.cs
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2009 FileFind.net (http://filefind.net)
//

using System;

namespace FileFind.Meshwork.Filesystem
{
	public class RemoteFile : AbstractFile, IRemoteDirectoryItem
	{
		


		public override string FullPath {
					get {
						throw new System.NotImplementedException();
					}
				}

		public override string InfoHash {
					get {
						throw new System.NotImplementedException();
					}
					internal set {
						throw new System.NotImplementedException();
					}
				}

		public override string Name {
					get {
						throw new System.NotImplementedException();
					}
				}

		public override IDirectory Parent {
					get {
						throw new System.NotImplementedException();
					}
				}

		public override int PieceLength {
					get {
						throw new System.NotImplementedException();
					}
					internal set {
						throw new System.NotImplementedException();
					}
				}

		public override string[] Pieces {
					get {
						throw new System.NotImplementedException();
					}
					internal set {
						throw new System.NotImplementedException();
					}
				}

		public override long Size {
			get {
				throw new System.NotImplementedException();
			}
		}

		public override string Type {
			get {
				throw new System.NotImplementedException();
			}
		}

		public override void Reload ()
				{
					throw new System.NotImplementedException();
				}		public RemoteFile ()
		{
		}
		
		public Network Network {
			get {
				return null;
			}
		}
		
		public Node Node {
			get {
				return null;
			}
		}
		
	}
}
