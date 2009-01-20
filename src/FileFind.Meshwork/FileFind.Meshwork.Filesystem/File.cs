//
// File.cs: A file in the Meshwork virtual filesystem.
// 
// Author:
//   Eric Butler <eric@filefind.net>
//
//   (C) 2005-2006 FileFind.net (http://filefind.net/)
//

using System;
using System.Data;

namespace FileFind.Meshwork.Filesystem
{
	public class File : FileFind.Meshwork.Object, IDirectoryItem
	{
		private string fullPath;
		private string fileName;
		private string fileType = "File";
		private string infoHash;
		private string sha1;
		private string nodeId;
		private string localPath;
		private long fileSize;
		private Directory parent;
		private int id;
		private int parent_id;
		private int pieceLength;
		private string[] pieces;

		FileSystemProvider fs;

		Node    node;
		Network network;

		public Directory Parent {
			get {
				if (parent == null) {
					parent = Directory.ById(Core.FileSystem, parent_id);
				}

				return parent;
			}
		}

		public int ParentId {
			get {
				return parent_id;
			}
		}

		public int Id {
			get {
				return id;
			}
		}
		
		public string NodeID {
			get {
				return nodeId;
			}
		}

		public string SHA1 {
			get {
				return sha1;
			}
			internal set {
				sha1 = value;
			}
		}
		
		public string InfoHash {
			get {
				return infoHash;
			}
			internal set {
				infoHash = value;
			}
		}

		public string[] Pieces {
			get {
				if (pieces == null) {
					fs.UseConnection(delegate (IDbConnection connection) {
						IDbCommand cmd = connection.CreateCommand();
						cmd.CommandText = "SELECT hash FROM filepieces WHERE file_id = @id ORDER BY piece_num";
						fs.AddParameter(cmd, "@id", id);
						DataSet ds = fs.ExecuteDataSet(cmd);
						pieces = new string[ds.Tables[0].Rows.Count];
						for (int x = 0; x < ds.Tables[0].Rows.Count; x++) {
							pieces[x] = ds.Tables[0].Rows[x]["hash"].ToString();
						}
					});
				}
				return pieces;
			}
			internal set {
				pieces = value;
			}
		}

		public int PieceLength {
			get {
				return pieceLength;
			}
			internal set {
				pieceLength = value;
			}
		}

		internal static File CreateFile (FileSystemProvider fs, Nullable<int> parent_directory_id, System.IO.FileInfo info)
		{
			return CreateFile(fs, parent_directory_id, info.Name, info.FullName, info.Length, null);
		}

		internal static File CreateFile (FileSystemProvider fs, Nullable<int> parent_directory_id, System.IO.FileInfo info, Node node)
		{
			return CreateFile(fs, parent_directory_id, info.Name, info.FullName, info.Length, node);
		}

		internal static File CreateFile (FileSystemProvider fs, Nullable<int> parent_directory_id, string name, string localpath, long length)
		{
			Directory parentDirectory = Directory.ById(fs, (int)parent_directory_id);
			return CreateFile (fs, parentDirectory, name, localpath, length, null);
		}

		internal static File CreateFile (FileSystemProvider fs, Nullable<int> parent_directory_id, string name, string localpath, long length, Node node)
		{
			Directory parentDirectory = Directory.ById(fs, (int)parent_directory_id);
			return CreateFile (fs, parentDirectory, name, localpath, length, node);
		}

		internal static File CreateFile (FileSystemProvider fs, Directory parentDirectory, string name, string localpath, long length)
		{
			return CreateFile(fs, parentDirectory, name, localpath, length, null);
		}

		internal static File CreateFile (FileSystemProvider fs, Directory parentDirectory, string name, string localpath, long length, Node node)
		{
			int    last_id  = -1;
			string fullPath = PathUtil.Join(parentDirectory.FullPath, name);

			fs.UseConnection(delegate (IDbConnection connection) {
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = "INSERT INTO directoryitems (type, name, local_path, parent_id, length, node_id, full_path) VALUES ('F', @name, @local_path, @parent_id, @length, @node_id, @full_path);";
				fs.AddParameter(cmd, "@name", name);
				fs.AddParameter(cmd, "@full_path", fullPath);
				fs.AddParameter(cmd, "@local_path", localpath);
				fs.AddParameter(cmd, "@parent_id", parentDirectory.Id);
				fs.AddParameter(cmd, "@length", length);
				if (node != null) {
					fs.AddParameter(cmd, "@node_id", node.NodeID);
				} else {
					if (parentDirectory.NodeID == Core.MyNodeID) {
						fs.AddParameter(cmd, "@node_id", Core.MyNodeID);
					} else {
						throw new ArgumentNullException("node");
					}
				}
				cmd.ExecuteNonQuery();

				cmd = connection.CreateCommand();
				cmd.CommandText = "SELECT last_insert_rowid()";

				last_id = Convert.ToInt32(cmd.ExecuteScalar());
			}, true);
			
			return File.ById(fs, last_id);
		}

		public static File GetFile (FileSystemProvider fs, string fullPath)
		{
			string directoryName = fullPath.Substring(0, fullPath.LastIndexOf("/"));
			string fileName      = fullPath.Substring(directoryName.Length + 1);

			Directory directory = Directory.GetDirectory(fs, directoryName);
			
			if (directory != null) {
				File file = directory.GetFile(fileName);
				return file;
			} else {
				return null;
			}
		}

		public static bool Exists (FileSystemProvider fs, long parentDirectoryId, string fileName)
		{
			return fs.UseConnection<bool>(delegate (IDbConnection connection) {
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = "SELECT id FROM directoryitems WHERE parent_id = @parent_id AND name = @name AND type='F' LIMIT 1";
				fs.AddParameter(cmd, "@parent_id", parentDirectoryId); 
				fs.AddParameter(cmd, "@name", fileName);
				return cmd.ExecuteScalar() != null;
			});
		}
	

		public void Reload ()
		{
			DataRow row = null;

			fs.UseConnection(delegate (IDbConnection connection) {
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = "SELECT * FROM directoryitems WHERE id=@id AND type = 'F' LIMIT 1";
				fs.AddParameter(cmd, "@id", id);
				DataSet ds = fs.ExecuteDataSet(cmd);
				row = ds.Tables[0].Rows[0];
			});

			Reload(row);
		}

		private void Reload (DataRow row)
		{
			pieces = null;

			this.parent_id   = Convert.ToInt32(row["parent_id"]);
			this.fullPath    = row["full_path"].ToString();
			this.fileName    = row["name"].ToString();
			this.infoHash    = row["info_hash"].ToString();
			this.sha1        = row["sha1"].ToString();
			this.localPath   = row["local_path"].ToString();
			this.fileSize    = Convert.ToInt64(row["length"]);
			this.nodeId      = row["node_id"].ToString();
			this.pieceLength = (!(row["piece_length"] is DBNull)) ? Convert.ToInt32(row["piece_length"]) : 0;
			//XXX: Add other columns here!
		}

		internal void Save ()
		{
			fs.UseConnection(delegate (IDbConnection connection) {
				IDbTransaction transaction = connection.BeginTransaction();
				try {
					IDbCommand cmd = connection.CreateCommand();
					cmd.CommandText = @"
						UPDATE directoryitems 
						SET 
							sha1         = @sha1,
							info_hash    = @info_hash,
							piece_length = @piece_length
						WHERE id = @id";
					fs.AddParameter(cmd, "@sha1", sha1);
					fs.AddParameter(cmd, "@info_hash", infoHash);
					fs.AddParameter(cmd, "@piece_length", pieceLength);
					fs.AddParameter(cmd, "@id", id);
					cmd.ExecuteNonQuery();

					cmd = connection.CreateCommand();
					cmd.CommandText = "DELETE FROM filepieces WHERE file_id = @file_id";
					fs.AddParameter(cmd, "@file_id", id);
					cmd.ExecuteNonQuery();

					cmd = connection.CreateCommand();
					cmd.CommandText = "INSERT INTO filepieces (file_id, piece_num, hash) VALUES (@file_id, @piece_num, @hash)";
					fs.AddParameter(cmd, "@file_id", id);

					IDbDataParameter pieceNumParam = cmd.CreateParameter();
					pieceNumParam.ParameterName = "@piece_num";
					cmd.Parameters.Add(pieceNumParam);

					IDbDataParameter hashParam = cmd.CreateParameter();
					hashParam.ParameterName = "@hash";
					cmd.Parameters.Add(hashParam);
					
					for (int x = 0; x < pieces.Length; x++) {
						pieceNumParam.Value = x;
						hashParam.Value = pieces[x];
						cmd.ExecuteNonQuery();
					}
					
					transaction.Commit();
				} catch (Exception ex) {
					transaction.Rollback();
					throw ex;
				}
			}, true);
		}

		internal File (FileSystemProvider fs, DataRow row)
		{
			this.fs = fs;
			this.id = Convert.ToInt32(row["id"]);
			Reload(row);
		}

		public string FullPath {
			get {
				if (Parent != null && String.IsNullOrEmpty(fullPath)) {
					fullPath = PathUtil.Join(Parent.FullPath, Name);
				}
				return fullPath;
			}
		}

		public void Delete ()
		{
			Parent.DeleteFile(this.Name);
		}

		public File Clone (bool includePath)
		{
			throw new NotImplementedException();
			/*
			File f = new File(this.Parent, this.Name);
			f.Size = this.Size;
			f.FileMD5 = this.FileMD5;
			f.LocalPath = this.LocalPath;
			return f;
			*/
		}
		#region DirectoryItem Members

		public string Name {
			get {
				return fileName;
			}
		}
 
		public string LocalPath {
			get {
				if (IsMine) {
					if (!String.IsNullOrEmpty(localPath)) {
						return localPath;
					} else {
						throw new Exception("Local path is missing.");
					}
				} else {
					throw new Exception("File is not local");
				}
			}
		}

		public long Size {
			get {
				return fileSize;
			}
		}

		public string Type {
			get {
				return fileType;
			}
		}

		#endregion

		internal static long CountByParentId (FileSystemProvider fs, Nullable<int> parent_id)
		{
			return fs.UseConnection<long>(delegate (IDbConnection connection) {
				IDbCommand command = connection.CreateCommand();
				string query = null;
				if (parent_id.Equals(null)) {
					query = "SELECT count(id) FROM directoryitems WHERE parent_id ISNULL AND type = 'F'";
				} else {
					query = "SELECT count(id) FROM directoryitems WHERE parent_id = @parent_id AND type = 'F'";
					fs.AddParameter(command, "@parent_id", parent_id);
				}
				command.CommandText = query;
				return (long) command.ExecuteScalar();
			});
		}

		internal static File[] ListByParentId (FileSystemProvider fs, Nullable<int> parent_id)
		{
			return fs.UseConnection<File[]>(delegate (IDbConnection connection) {
				IDbCommand command = connection.CreateCommand();
				string query = null;
				if (parent_id.Equals(null)) {
					query = "SELECT * FROM directoryitems WHERE parent_id ISNULL AND type = 'F'";
				} else {
					query = "SELECT * FROM directoryitems WHERE parent_id = @parent_id AND type = 'F'";
					fs.AddParameter(command, "@parent_id", parent_id);
				}
				command.CommandText = query;
				DataSet ds = fs.ExecuteDataSet(command);

				File[] results = new File[ds.Tables[0].Rows.Count];
				for (int x = 0; x < ds.Tables[0].Rows.Count; x++) {
					results[x] = new File(fs, ds.Tables[0].Rows[x]);
				}
				return results;
			});
		}

		public static File GetFile (FileSystemProvider fs, Directory parent, string name)
		{
			return File.GetFile(fs, parent.Id, name);
		}

		public static File GetFile (FileSystemProvider fs, int parentId, string name)
		{
			return fs.UseConnection<File>(delegate (IDbConnection connection) {
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = "SELECT * FROM directoryitems WHERE parent_id= @parent_id AND name = @name AND type = 'F' LIMIT 1";
				fs.AddParameter(cmd, "@parent_id", parentId);
				fs.AddParameter(cmd, "@name", name);
				DataSet ds = fs.ExecuteDataSet(cmd);
				if (ds.Tables[0].Rows.Count > 0) {
					return new File(fs, ds.Tables[0].Rows[0]);
				} else {
					return null;
				}
			});
		}

		public static File ById (FileSystemProvider fs, int id)
		{
			return fs.UseConnection<File>(delegate (IDbConnection connection) {
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = "SELECT * FROM directoryitems WHERE id=@id AND type = 'F' LIMIT 1";
				fs.AddParameter(cmd, "@id", id);
				DataSet ds = fs.ExecuteDataSet(cmd);
				if (ds.Tables[0].Rows.Count > 0) {
					return new File(fs, ds.Tables[0].Rows[0]);
				} else {
					return null;
				}
			});
		}

		public Network Network {
			get {
				/*if (network == null) {
					string path = FullPath;
					if (path == null) {
						throw new ArgumentNullException("path");
					}

					string[] pathParts = path.Split('/');
					if (pathParts.Length > 1) {
						// "My Shared Files"
						if (pathParts[1] == Core.MyNodeID) {
							return null;
						}

						this.network = Core.GetNetwork(pathParts[1]);
					} else {
						return null;
					}
				}*/

				if (network == null) {
					// Don't attempt to find network if there's no NodeId,
					// or if this is one of our files - there isn't one.
					if (nodeId != null && nodeId != Core.MyNodeID) {
						Console.WriteLine("Getting network for path {0}", FullPath);
						string[] pathParts = FullPath.Split('/');
						this.network = Core.GetNetwork(pathParts[1]);
					}
				}
				
				return network;

				/*
				Directory directory = Parent;
				while (directory.Parent.Parent != null) {
					if (directory.Parent != null) {
						directory = directory.Parent;
					} else {
						// Local file, no network
						return null;
					}
				}

				return Core.GetNetwork(directory.Name);
				*/
			}
		}

		public Node Node {
			get {
				if (node == null) {
					if (nodeId != null && nodeId != Core.MyNodeID) {
						if (Network == null) {
							throw new Exception("Unable to determine network.");
						} else {
							node = Network.Nodes[nodeId];
							if (node == null) {
								throw new Exception("Unable to find node.");
							}
						}
					}
				}

				/*
				if (node == null) {
					string path = FullPath;
					if (path == null) {
						throw new ArgumentNullException("path");
					}

					Network network = this.Network;
					if (network == null) {
						return null;
					}
					string[] pathParts = path.Split('/');
					if (pathParts.Length > 2) {
						node = network.Nodes[pathParts[2]];
					} else {
						return null;
					}
				}
				*/

				return node;

				/*
				Directory directory = Parent;
				while (directory.Parent.Parent.Parent != null) {
					if (directory.Parent != null) {
						directory = directory.Parent;
					} else {
						// Local file, no node
						return null;
					}
				}

				return Network.GetNode(directory.Name);
				*/
			}
		}

		public bool IsMine {
			get {
				return (nodeId == Core.MyNodeID);
			}
		}
		
		public bool Equals (File other)
		{
			return (other.Id == this.Id);
		}
	}

	public enum FileType
	{
		Audio,
		Video,
		Image,
		Document,
		Other
	}
}
