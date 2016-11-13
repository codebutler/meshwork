//
// LocalFile.cs
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2009 FileFind.net (http://filefind.net)
//

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace Meshwork.Backend.Feature.FileBrowsing.Filesystem
{
	public class LocalFile : AbstractFile, ILocalDirectoryItem
	{
	    private readonly FileSystemProvider fileSystem;

	    private string fileName;
	    private string infoHash;
		private string sha1;
	    private int pieceLength;
		private string[] pieces;
	    private long _size;
	    private Dictionary<string, string> _metadata;
	    private string _fullPath;


	    private LocalFile (FileSystemProvider fileSystem, DataRow row)
	    {
	        this.fileSystem = fileSystem;

			Id = Convert.ToInt32(row["id"]);
			ParentId = Convert.ToInt32(row["parent_id"]);
			Reload(row);
		}

		private LocalFile (int id, int parentId, string name, string localPath, long length, string fullPath)
		{
			Id = id;
			ParentId = parentId;
			fileName = name;
			LocalPath = localPath;
			_size = length;
			_fullPath = fullPath;
		}

		public override IDirectory Parent {
			get {
				LocalDirectory parent = null;
			    fileSystem.UseConnection(delegate
				{
					parent = LocalDirectory.ById(fileSystem, ParentId);
					if (parent == null)
						throw new Exception($"Parent not found! Name: {Name} Id: {Id} ParentId: {ParentId}");
				});
				return parent;
			}
		}

		public override string Name => fileName;

	    public int ParentId { get; }

	    public int Id { get; }

	    public override string SHA1 => sha1;

	    public override string InfoHash => infoHash;

	    public override string[] Pieces {
			get {
				if (pieces == null) {
					fileSystem.UseConnection(delegate(IDbConnection connection) {
						var cmd = connection.CreateCommand();
						cmd.CommandText = "SELECT hash FROM filepieces WHERE file_id = @id ORDER BY piece_num";
					    fileSystem.AddParameter(cmd, "@id", Id);
						var ds = fileSystem.ExecuteDataSet(cmd);
						pieces = new string[ds.Tables[0].Rows.Count];
						for (var x = 0; x < ds.Tables[0].Rows.Count; x++) {
							pieces[x] = ds.Tables[0].Rows[x]["hash"].ToString();
						}
					});
				}
				return pieces;
			}
		}

		public override int PieceLength => pieceLength;

	    public string LocalPath { get; private set; }

	    public override long Size => _size;

	    public override string Type { get; } = "File";

	    public override Dictionary<string, string> Metadata => _metadata;

	    public override string FullPath => _fullPath;

	    private void Reload ()
		{
			DataRow row = null;
			
		    fileSystem.UseConnection(delegate(IDbConnection connection) {
				var cmd = connection.CreateCommand();
				cmd.CommandText = "SELECT * FROM directoryitems WHERE id=@id AND type = 'F' LIMIT 1";
		        fileSystem.AddParameter(cmd, "@id", Id);
				var ds = fileSystem.ExecuteDataSet(cmd);
				row = ds.Tables[0].Rows[0];
			});
			
			Reload(row);
		}

	    private void Reload (DataRow row)
		{
			pieces = null;
			
			fileName = row["name"].ToString();
			infoHash = row["info_hash"].ToString();
			sha1 = row["sha1"].ToString();
			LocalPath = row["local_path"].ToString();
			_size = Convert.ToInt64(row["length"]);
			pieceLength = (!(row["piece_length"] is DBNull)) ? Convert.ToInt32(row["piece_length"]) : 0;
			_fullPath = row["full_path"].ToString();
		}

		public void Delete ()
		{
			((LocalDirectory)Parent).DeleteFile(Name);
		}

		internal static long CountByParentId (FileSystemProvider fs, int? parentId)
		{
			return fs.UseConnection(delegate(IDbConnection connection) {
				var command = connection.CreateCommand();
				string query = null;
				if (parentId.Equals(null)) {
					query = "SELECT count(id) FROM directoryitems WHERE parent_id ISNULL AND type = 'F'";
				} else {
					query = "SELECT count(id) FROM directoryitems WHERE parent_id = @parent_id AND type = 'F'";
					fs.AddParameter(command, "@parent_id", parentId);
				}
				command.CommandText = query;
				return (long)fs.ExecuteScalar(command);
			});
		}

		internal static LocalFile[] ListByParentId (FileSystemProvider fs, int? parentId)
		{
			return fs.UseConnection(delegate(IDbConnection connection) {
				var command = connection.CreateCommand();
				string query = null;
				if (parentId.Equals(null)) {
					query = "SELECT * FROM directoryitems WHERE parent_id ISNULL AND type = 'F'";
				} else {
					query = "SELECT * FROM directoryitems WHERE parent_id = @parent_id AND type = 'F'";
					fs.AddParameter(command, "@parent_id", parentId);
				}
				command.CommandText = query;
				var ds = fs.ExecuteDataSet(command);
				
				var results = new LocalFile[ds.Tables[0].Rows.Count];
				for (var x = 0; x < ds.Tables[0].Rows.Count; x++) {
					results[x] = new LocalFile(fs, ds.Tables[0].Rows[x]);
				}
				return results;
			});
		}

		public static LocalFile FromDataRow (FileSystemProvider fileSystem, DataRow row)
		{
			return new LocalFile(fileSystem, row);
		}
		
		internal void Update (string infoHash, string sha1, int pieceLength, string[] pieces)
		{
			this.infoHash = infoHash;
			this.sha1 = sha1;
			this.pieceLength = pieceLength;
			this.pieces = pieces;
			Save();
		}

		internal void Save ()
		{
		    fileSystem.UseConnection(delegate(IDbConnection connection) {
				var transaction = connection.BeginTransaction();
				try {
					var cmd = connection.CreateCommand();
					cmd.CommandText = @"
						UPDATE directoryitems 
						SET 
							sha1         = @sha1,
							info_hash    = @info_hash,
							piece_length = @piece_length
						WHERE id = @id";
					fileSystem.AddParameter(cmd, "@sha1", sha1);
					fileSystem.AddParameter(cmd, "@info_hash", infoHash);
					fileSystem.AddParameter(cmd, "@piece_length", pieceLength);
					fileSystem.AddParameter(cmd, "@id", Id);
					fileSystem.ExecuteNonQuery(cmd);
					
					cmd = connection.CreateCommand();
					cmd.CommandText = "DELETE FROM filepieces WHERE file_id = @file_id";
					fileSystem.AddParameter(cmd, "@file_id", Id);
					fileSystem.ExecuteNonQuery(cmd);
					
					cmd = connection.CreateCommand();
					cmd.CommandText = "INSERT INTO filepieces (file_id, piece_num, hash) VALUES (@file_id, @piece_num, @hash)";
				    fileSystem.AddParameter(cmd, "@file_id", Id);
					
					var pieceNumParam = cmd.CreateParameter();
					pieceNumParam.ParameterName = "@piece_num";
					cmd.Parameters.Add(pieceNumParam);
					
					var hashParam = cmd.CreateParameter();
					hashParam.ParameterName = "@hash";
					cmd.Parameters.Add(hashParam);
					
					for (var x = 0; x < pieces.Length; x++) {
						pieceNumParam.Value = x;
						hashParam.Value = pieces[x];
					    fileSystem.ExecuteNonQuery(cmd);
					}
					
					transaction.Commit();
					
					// FIXME: Fire off a global FileChanged event!!! 
					// There may be other File instances that need to update.
					// I suppose a WeakRef cache may be another option though!!
					
				} catch (Exception) {
					transaction.Rollback();
					throw;
				}
			}, true);
		}

		internal static LocalFile CreateFile (FileSystemProvider fs, LocalDirectory parentDirectory, FileInfo info)
		{
			return CreateFile(fs, parentDirectory, info.Name, info.FullName, info.Length);
		}

	    private static LocalFile CreateFile (FileSystemProvider fs, LocalDirectory parentDirectory, string name, string localpath, long length)
		{
			var lastId = -1;

			var fullPath = PathUtil.Join(parentDirectory.FullPath, name);
			if (fullPath.Length > 1 && fullPath.EndsWith("/")) {
				fullPath = fullPath.Substring(0, fullPath.Length - 1);
			}

			fs.UseConnection(connection => {
			    var cmd = connection.CreateCommand();
			    cmd.CommandText = @"INSERT INTO directoryitems (type, name, local_path, parent_id, length, full_path)
					VALUES ('F', @name, @local_path, @parent_id, @length, @full_path);";
			    fs.AddParameter(cmd, "@name", name);
			    fs.AddParameter(cmd, "@local_path", localpath);
			    fs.AddParameter(cmd, "@parent_id", parentDirectory.Id);
			    fs.AddParameter(cmd, "@length", length);
			    fs.AddParameter(cmd, "@full_path", fullPath);

			    fs.ExecuteNonQuery(cmd);

			    cmd = connection.CreateCommand();
			    cmd.CommandText = "SELECT last_insert_rowid()";

			    lastId = Convert.ToInt32(fs.ExecuteScalar(cmd));
			}, true);

			return new LocalFile(lastId, parentDirectory.Id, name, localpath, length, fullPath);
		}

		internal static LocalFile ById (FileSystemProvider fs, int id)
		{
			return fs.UseConnection(connection => {
			    var cmd = connection.CreateCommand();
			    cmd.CommandText = "SELECT * FROM directoryitems WHERE id=@id AND type = 'F' LIMIT 1";
			    fs.AddParameter(cmd, "@id", id);
			    var ds = fs.ExecuteDataSet(cmd);
			    return ds.Tables[0].Rows.Count > 0 ? FromDataRow(fs, ds.Tables[0].Rows[0]) : null;
			});
		}
	}
}
