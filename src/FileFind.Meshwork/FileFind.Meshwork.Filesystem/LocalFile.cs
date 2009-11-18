//
// LocalFile.cs
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2009 FileFind.net (http://filefind.net)
//

using System;
using System.Data;
using System.Collections.Generic;

namespace FileFind.Meshwork.Filesystem
{
	public class LocalFile : AbstractFile, ILocalDirectoryItem
	{
		private string fileName;
		private string fileType = "File";
		private string infoHash;
		private string sha1;
		private string localPath;
		private long fileSize;
		private int id;
		private int parentId;
		private int pieceLength;
		private string[] pieces;
		private Dictionary<string, string> metadata;

		private LocalFile (DataRow row)
		{
			this.id = Convert.ToInt32(row["id"]);
			this.parentId = Convert.ToInt32(row["parent_id"]);
			Reload(row);
		}

		public override IDirectory Parent {
			get {
				LocalDirectory parent = null;
				Core.FileSystem.UseConnection(delegate(IDbConnection connection) {
					parent = LocalDirectory.ById(parentId);	
					if (parent == null)
						throw new Exception(String.Format("Parent not found! Name: {0} Id: {1} ParentId: {2}", Name, Id, parentId));
				});	
				return parent;
			}
		}

		public override string Name {
			get { return fileName; }
		}

		public int ParentId {
			get { return parentId; }
		}

		public int Id {
			get { return id; }
		}

		public string SHA1 {
			get { return sha1; }
			internal set { sha1 = value; }
		}

		public override string InfoHash {
			get { return infoHash; }
			internal set { infoHash = value; }
		}

		public override string[] Pieces {
			get {
				if (pieces == null) {
					Core.FileSystem.UseConnection(delegate(IDbConnection connection) {
						IDbCommand cmd = connection.CreateCommand();
						cmd.CommandText = "SELECT hash FROM filepieces WHERE file_id = @id ORDER BY piece_num";
						Core.FileSystem.AddParameter(cmd, "@id", id);
						DataSet ds = Core.FileSystem.ExecuteDataSet(cmd);
						pieces = new string[ds.Tables[0].Rows.Count];
						for (int x = 0; x < ds.Tables[0].Rows.Count; x++) {
							pieces[x] = ds.Tables[0].Rows[x]["hash"].ToString();
						}
					});
				}
				return pieces;
			}
			internal set { pieces = value; }
		}

		public override int PieceLength {
			get { return pieceLength; }
			internal set { pieceLength = value; }
		}

		public string LocalPath {
			get { return localPath; }
		}

		public override long Size {
			get { return fileSize; }
		}

		public override string Type {
			get { return fileType; }
		}
		
		public override Dictionary<string, string> Metadata {
			get { return metadata; }
		}

		void Reload ()
		{
			DataRow row = null;
			
			Core.FileSystem.UseConnection(delegate(IDbConnection connection) {
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = "SELECT * FROM directoryitems WHERE id=@id AND type = 'F' LIMIT 1";
				Core.FileSystem.AddParameter(cmd, "@id", id);
				DataSet ds = Core.FileSystem.ExecuteDataSet(cmd);
				row = ds.Tables[0].Rows[0];
			});
			
			Reload(row);
		}

		void Reload (DataRow row)
		{
			pieces = null;
			
			this.fileName = row["name"].ToString();
			this.infoHash = row["info_hash"].ToString();
			this.sha1 = row["sha1"].ToString();
			this.localPath = row["local_path"].ToString();
			this.fileSize = Convert.ToInt64(row["length"]);
			this.pieceLength = (!(row["piece_length"] is DBNull)) ? Convert.ToInt32(row["piece_length"]) : 0;
			//XXX: Add other columns here!
		}

		public void Delete ()
		{
			((LocalDirectory)Parent).DeleteFile(this.Name);
		}

		static internal long CountByParentId (Nullable<int> parent_id)
		{
			return Core.FileSystem.UseConnection<long>(delegate(IDbConnection connection) {
				IDbCommand command = connection.CreateCommand();
				string query = null;
				if (parent_id.Equals(null)) {
					query = "SELECT count(id) FROM directoryitems WHERE parent_id ISNULL AND type = 'F'";
				} else {
					query = "SELECT count(id) FROM directoryitems WHERE parent_id = @parent_id AND type = 'F'";
					Core.FileSystem.AddParameter(command, "@parent_id", parent_id);
				}
				command.CommandText = query;
				return (long)Core.FileSystem.ExecuteScalar(command);
			});
		}

		static internal LocalFile[] ListByParentId (Nullable<int> parent_id)
		{
			return Core.FileSystem.UseConnection<LocalFile[]>(delegate(IDbConnection connection) {
				IDbCommand command = connection.CreateCommand();
				string query = null;
				if (parent_id.Equals(null)) {
					query = "SELECT * FROM directoryitems WHERE parent_id ISNULL AND type = 'F'";
				} else {
					query = "SELECT * FROM directoryitems WHERE parent_id = @parent_id AND type = 'F'";
					Core.FileSystem.AddParameter(command, "@parent_id", parent_id);
				}
				command.CommandText = query;
				DataSet ds = Core.FileSystem.ExecuteDataSet(command);
				
				LocalFile[] results = new LocalFile[ds.Tables[0].Rows.Count];
				for (int x = 0; x < ds.Tables[0].Rows.Count; x++) {
					results[x] = new LocalFile(ds.Tables[0].Rows[x]);
				}
				return results;
			});
		}


		public static LocalFile FromDataRow (DataRow row)
		{
			return new LocalFile(row);
		}

		internal void Save ()
		{
			Core.FileSystem.UseConnection(delegate(IDbConnection connection) {
				IDbTransaction transaction = connection.BeginTransaction();
				try {
					IDbCommand cmd = connection.CreateCommand();
					cmd.CommandText = "\r\n\t\t\t\t\t\tUPDATE directoryitems \r\n\t\t\t\t\t\tSET \r\n\t\t\t\t\t\t\tsha1         = @sha1,\r\n\t\t\t\t\t\t\tinfo_hash    = @info_hash,\r\n\t\t\t\t\t\t\tpiece_length = @piece_length\r\n\t\t\t\t\t\tWHERE id = @id";
					Core.FileSystem.AddParameter(cmd, "@sha1", sha1);
					Core.FileSystem.AddParameter(cmd, "@info_hash", infoHash);
					Core.FileSystem.AddParameter(cmd, "@piece_length", pieceLength);
					Core.FileSystem.AddParameter(cmd, "@id", id);
					Core.FileSystem.ExecuteNonQuery(cmd);
					
					cmd = connection.CreateCommand();
					cmd.CommandText = "DELETE FROM filepieces WHERE file_id = @file_id";
					Core.FileSystem.AddParameter(cmd, "@file_id", id);
					Core.FileSystem.ExecuteNonQuery(cmd);
					
					cmd = connection.CreateCommand();
					cmd.CommandText = "INSERT INTO filepieces (file_id, piece_num, hash) VALUES (@file_id, @piece_num, @hash)";
					Core.FileSystem.AddParameter(cmd, "@file_id", id);
					
					IDbDataParameter pieceNumParam = cmd.CreateParameter();
					pieceNumParam.ParameterName = "@piece_num";
					cmd.Parameters.Add(pieceNumParam);
					
					IDbDataParameter hashParam = cmd.CreateParameter();
					hashParam.ParameterName = "@hash";
					cmd.Parameters.Add(hashParam);
					
					for (int x = 0; x < pieces.Length; x++) {
						pieceNumParam.Value = x;
						hashParam.Value = pieces[x];
						Core.FileSystem.ExecuteNonQuery(cmd);
					}
					
					transaction.Commit();
					
					// FIXME: Fire off a global FileChanged event!!! 
					// There may be other File instances that need to update.
					// I suppose a WeakRef cache may be another option though!!
					
				} catch (Exception ex) {
					transaction.Rollback();
					throw ex;
				}
			}, true);
		}

		static internal LocalFile CreateFile (LocalDirectory parentDirectory, System.IO.FileInfo info)
		{
			return CreateFile(parentDirectory, info.Name, info.FullName, info.Length);
		}

		static internal LocalFile CreateFile (LocalDirectory parentDirectory, string name, string localpath, long length)
		{
			int last_id = -1;
			
			Core.FileSystem.UseConnection(delegate(IDbConnection connection) {
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = "INSERT INTO directoryitems (type, name, local_path, parent_id, length) VALUES ('F', @name, @local_path, @parent_id, @length);";
				Core.FileSystem.AddParameter(cmd, "@name", name);
				Core.FileSystem.AddParameter(cmd, "@local_path", localpath);
				Core.FileSystem.AddParameter(cmd, "@parent_id", parentDirectory.Id);
				Core.FileSystem.AddParameter(cmd, "@length", length);
				
				Core.FileSystem.ExecuteNonQuery(cmd);
				
				cmd = connection.CreateCommand();
				cmd.CommandText = "SELECT last_insert_rowid()";
				
				last_id = Convert.ToInt32(cmd.ExecuteScalar());
			}, true);
			
			return LocalFile.ById(last_id);
		}

		static internal LocalFile ById (int id)
		{
			return Core.FileSystem.UseConnection<LocalFile>(delegate(IDbConnection connection) {
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = "SELECT * FROM directoryitems WHERE id=@id AND type = 'F' LIMIT 1";
				Core.FileSystem.AddParameter(cmd, "@id", id);
				DataSet ds = Core.FileSystem.ExecuteDataSet(cmd);
				if (ds.Tables[0].Rows.Count > 0) {
					return LocalFile.FromDataRow(ds.Tables[0].Rows[0]);
				} else {
					return null;
				}
			});
		}
	}
}
