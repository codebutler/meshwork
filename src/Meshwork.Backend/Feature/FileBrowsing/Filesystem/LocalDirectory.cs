//
// LocalDirectory.cs
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2009 FileFind.net (http://filefind.net)
//

using System;
using System.Data;
using System.IO;

namespace Meshwork.Backend.Feature.FileBrowsing.Filesystem
{
	public class LocalDirectory : AbstractDirectory, ILocalDirectoryItem
	{
		int    id;
		int    parentId;
		string name;
		string localPath;
		int    fileCount = -1;
		int    directoryCount = -1;
		string fullPath;

	    protected readonly FileSystemProvider fileSystem;
	    
		#region Constructors
		
		protected LocalDirectory (FileSystemProvider fileSystem, int id, int parentId, string name, string localPath, string fullPath)
		{
		    this.fileSystem = fileSystem;
			this.id        = id;
			this.parentId  = parentId;
			this.name      = name;
			this.localPath = localPath;
			this.fullPath  = fullPath;
		}
		#endregion
		
		#region Public Properties
		
		public int Id {
			get {
				return id;
			}
		}
				
		public string LocalPath {
			get {
				return localPath;
			}
		}
		
		public override IDirectory Parent {
			get
			{
			    if (parentId != 0) {
					LocalDirectory parent = null;
					fileSystem.UseConnection(delegate {
						parent = ById(fileSystem, parentId);
					});						
					if (parent == null)
						throw new Exception($"Parent not found! Name: {Name} Id: {Id} ParentId: {parentId}");
					return parent;
				}
			    return fileSystem.RootDirectory.MyDirectory;
			}
		}
		
		public override string FullPath {
			get {
				return fullPath;
			}
		}
		
		public override string Name {
			get {
				return name;
			}
		}			
			
		public override IFile[] Files {
			get {
				return LocalFile.ListByParentId(fileSystem, id);
			}
		}		
		
		public override IDirectory[] Directories {
			get {
				return ListByParentId(fileSystem, id);
			}
		}
		
		public override int FileCount {
			get {
				if (fileCount == -1) {
					fileCount = (int)LocalFile.CountByParentId(fileSystem, id);
				}
				return fileCount;
			}
		}

		public override int DirectoryCount {
			get {
				if (directoryCount == -1) {
					directoryCount = (int)CountByParentId(fileSystem, id);
				}
				return directoryCount;
			}
		}	
		

		#endregion
		
		#region Internal Methods
		internal LocalDirectory CreateSubDirectory (FileSystemProvider fileSystem, string name, string localPath)
		{
			return CreateDirectory(fileSystem, this, name, localPath);
		}
		
		internal LocalFile CreateFile (FileInfo info)
		{
			var file = LocalFile.CreateFile(fileSystem, this, info);
			InvalidateCache();
			return file;
		}
		#endregion
		
		#region Public Methods
		public void Delete ()
		{
			foreach (LocalDirectory subDir in Directories) {
				subDir.Delete();
			}

			foreach (LocalFile file in Files) {
				file.Delete();
			}

			fileSystem.UseConnection(delegate (IDbConnection connection) {
				var cmd = connection.CreateCommand();
				cmd.CommandText = "DELETE FROM directoryitems WHERE id = @directory_id AND type='D'";
				fileSystem.AddParameter(cmd, "@directory_id", id); 
				fileSystem.ExecuteNonQuery(cmd);
			}, true);

			InvalidateCache();
		}
		#endregion
		
		#region Protected Methods		
		protected void InvalidateCache ()
		{
			fileCount = -1;
			directoryCount = -1;
		}		
		#endregion
		
		#region Static methods		
		public static LocalDirectory ById (FileSystemProvider fileSystem, int id)
		{
			return fileSystem.UseConnection(delegate (IDbConnection connection) {
				var cmd = connection.CreateCommand();
				cmd.CommandText = "SELECT * FROM directoryitems WHERE id=@id AND type = 'D' LIMIT 1";
				fileSystem.AddParameter(cmd, "@id", id);
				var ds = fileSystem.ExecuteDataSet(cmd);
				if (ds.Tables[0].Rows.Count > 0) {
					return FromDataRow(fileSystem, ds.Tables[0].Rows[0]);
				}
			    return null;
			});
		}

		internal static LocalDirectory FromDataRow (FileSystemProvider fileSystem, DataRow row)
		{
			var id = Convert.ToInt32(row["id"]);
			var parent_id = -1;
			if ((row["parent_id"] is DBNull) == false) {
				parent_id = Convert.ToInt32(row["parent_id"]);
			}

			var name = row["name"].ToString();
			var localPath = row["local_path"].ToString();
			var fullPath = row["full_path"].ToString();

			return new LocalDirectory(fileSystem, id, parent_id, name, localPath, fullPath);
		}
		
		private static LocalDirectory CreateDirectory (FileSystemProvider fileSystem, LocalDirectory parent, string name, string local_path)
		{
			string fullPath;
			var last_id = -1;
			
			if (string.IsNullOrEmpty(local_path)) {
				throw new ArgumentNullException("local_path");
			}

			if (!Directory.Exists(local_path)) {
				throw new ArgumentException("local_path", $"Directory does not exist: '{local_path}'");
			}

			if (parent != null) {
				fullPath = PathUtil.Join(parent.FullPath, name);
			} else {
				fullPath = PathUtil.Join("/", name);
			}

			if (fullPath.Length > 1 && fullPath.EndsWith("/")) {
				fullPath = fullPath.Substring(0, fullPath.Length - 1);
			}

			fileSystem.UseConnection(delegate (IDbConnection connection) {
				var cmd = connection.CreateCommand();
				cmd.CommandText = @"INSERT INTO directoryitems (type, parent_id, name, local_path, full_path)
					VALUES ('D', @parent_id, @name, @local_path, @full_path);";
				fileSystem.AddParameter(cmd, "@name", name);
				fileSystem.AddParameter(cmd, "@local_path", local_path);
				fileSystem.AddParameter(cmd, "@full_path", fullPath);

				if (parent == null) {
					fileSystem.AddParameter(cmd, "@parent_id", null);
				} else {
					fileSystem.AddParameter(cmd, "@parent_id", parent.Id);
				}
				fileSystem.ExecuteNonQuery(cmd);

				cmd = connection.CreateCommand();
				cmd.CommandText = "SELECT last_insert_rowid()";

				last_id = Convert.ToInt32(fileSystem.ExecuteScalar(cmd));
			}, true);

			if (parent != null) {
				parent.InvalidateCache();
			}

			var parentId = (parent == null) ? -1 : parent.Id;
			return new LocalDirectory(fileSystem, last_id, parentId, name, local_path, fullPath);
		}

		internal static long CountByParentId (FileSystemProvider fileSystem, int? parent_id)
		{
			return fileSystem.UseConnection(delegate (IDbConnection connection) {
				var command = connection.CreateCommand();
				string query = null;
				if (parent_id.Equals(null)) {
					query = "SELECT count(id) FROM directoryitems WHERE parent_id ISNULL AND type = 'D'";
				} else {
					query = "SELECT count(id) FROM directoryitems WHERE parent_id = @parent_id AND type = 'D'";
					fileSystem.AddParameter(command, "@parent_id", (int)parent_id);
				}
				command.CommandText = query;
				return (long) fileSystem.ExecuteScalar(command);
			});
		}

		internal static LocalDirectory[] ListByParentId (FileSystemProvider fileSystem, int? parent_id)
		{
			return fileSystem.UseConnection(delegate (IDbConnection connection) {
				var command = connection.CreateCommand();
				string query = null;
				if (parent_id.Equals(null)) {
					query = "SELECT * FROM directoryitems WHERE parent_id ISNULL AND type = 'D'";
				} else {
					query = "SELECT * FROM directoryitems WHERE parent_id = @parent_id AND type = 'D'";
					fileSystem.AddParameter(command, "@parent_id", (int)parent_id);
				}
				command.CommandText = query;
				var ds = fileSystem.ExecuteDataSet(command);

				var results = new LocalDirectory[ds.Tables[0].Rows.Count];
				for (var x = 0; x < ds.Tables[0].Rows.Count; x++) {
					results[x] = FromDataRow(fileSystem, ds.Tables[0].Rows[x]);
				}
				return results;
			});
		}
				
		public void DeleteFile (string name)
		{
			fileSystem.UseConnection(delegate (IDbConnection connection) {
				var cmd = connection.CreateCommand();
				cmd.CommandText = "DELETE FROM directoryitems WHERE parent_id = @parent_id AND name = @name AND type='F'";
				fileSystem.AddParameter(cmd, "@parent_id", id); 
				fileSystem.AddParameter(cmd, "@name", name);
				fileSystem.ExecuteNonQuery(cmd);
			}, true);

			InvalidateCache();
		}

		
		#endregion
	}
}
