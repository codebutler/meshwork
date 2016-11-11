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
			
		#region Constructors
		
		protected LocalDirectory (int id, int parentId, string name, string localPath, string fullPath)
		{
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
			get {
				if (parentId != 0) {
					LocalDirectory parent = null;
					Core.Core.FileSystem.UseConnection(delegate(IDbConnection connection) {
						parent = LocalDirectory.ById(parentId);
					});						
					if (parent == null)
						throw new Exception(string.Format("Parent not found! Name: {0} Id: {1} ParentId: {2}", Name, Id, parentId));
					return parent;
				} else {
					return Core.Core.FileSystem.RootDirectory.MyDirectory;
				}
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
				return LocalFile.ListByParentId(this.id);
			}
		}		
		
		public override IDirectory[] Directories {
			get {
				return LocalDirectory.ListByParentId(this.id);
			}
		}
		
		public override int FileCount {
			get {
				if (fileCount == -1) {
					fileCount = (int)LocalFile.CountByParentId(this.id);
				}
				return fileCount;
			}
		}

		public override int DirectoryCount {
			get {
				if (directoryCount == -1) {
					directoryCount = (int)LocalDirectory.CountByParentId(this.id);
				}
				return directoryCount;
			}
		}	
		

		#endregion
		
		#region Internal Methods
		internal LocalDirectory CreateSubDirectory (string name, string localPath)
		{
			return LocalDirectory.CreateDirectory(this, name, localPath);	
		}
		
		internal LocalFile CreateFile (System.IO.FileInfo info)
		{
			var file = LocalFile.CreateFile(this, info);
			this.InvalidateCache();
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

			Core.Core.FileSystem.UseConnection(delegate (IDbConnection connection) {
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = "DELETE FROM directoryitems WHERE id = @directory_id AND type='D'";
				Core.Core.FileSystem.AddParameter(cmd, "@directory_id", id); 
				Core.Core.FileSystem.ExecuteNonQuery(cmd);
			}, true);

			this.InvalidateCache();
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
		public static LocalDirectory ById (int id)
		{
			return Core.Core.FileSystem.UseConnection<LocalDirectory>(delegate (IDbConnection connection) {
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = "SELECT * FROM directoryitems WHERE id=@id AND type = 'D' LIMIT 1";
				Core.Core.FileSystem.AddParameter(cmd, "@id", id);
				DataSet ds = Core.Core.FileSystem.ExecuteDataSet(cmd);
				if (ds.Tables[0].Rows.Count > 0) {
					return LocalDirectory.FromDataRow(ds.Tables[0].Rows[0]);
				} else {
					return null;
				}
			});
		}

		internal static LocalDirectory FromDataRow (DataRow row)
		{
			int id = Convert.ToInt32(row["id"]);
			int parent_id = -1;
			if ((row["parent_id"] is System.DBNull) == false) {
				parent_id = Convert.ToInt32(row["parent_id"]);
			}

			string name = row["name"].ToString();
			string localPath = row["local_path"].ToString();
			string fullPath = row["full_path"].ToString();

			return new LocalDirectory(id, parent_id, name, localPath, fullPath);
		}
		
		private static LocalDirectory CreateDirectory (LocalDirectory parent, string name, string local_path)
		{
			string fullPath;
			int last_id = -1;
			
			if (string.IsNullOrEmpty(local_path)) {
				throw new ArgumentNullException("local_path");
			}

			if (!System.IO.Directory.Exists(local_path)) {
				throw new ArgumentException("local_path", string.Format("Directory does not exist: '{0}'", local_path));
			}

			if (parent != null) {
				fullPath = PathUtil.Join(parent.FullPath, name);
			} else {
				fullPath = PathUtil.Join("/", name);
			}

			if (fullPath.Length > 1 && fullPath.EndsWith("/")) {
				fullPath = fullPath.Substring(0, fullPath.Length - 1);
			}

			Core.Core.FileSystem.UseConnection(delegate (IDbConnection connection) {
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = @"INSERT INTO directoryitems (type, parent_id, name, local_path, full_path)
					VALUES ('D', @parent_id, @name, @local_path, @full_path);";
				Core.Core.FileSystem.AddParameter(cmd, "@name", name);
				Core.Core.FileSystem.AddParameter(cmd, "@local_path", local_path);
				Core.Core.FileSystem.AddParameter(cmd, "@full_path", fullPath);

				if (parent == null) {
					Core.Core.FileSystem.AddParameter(cmd, "@parent_id", null);
				} else {
					Core.Core.FileSystem.AddParameter(cmd, "@parent_id", (int)parent.Id);
				}
				Core.Core.FileSystem.ExecuteNonQuery(cmd);

				cmd = connection.CreateCommand();
				cmd.CommandText = "SELECT last_insert_rowid()";

				last_id = Convert.ToInt32(Core.Core.FileSystem.ExecuteScalar(cmd));
			}, true);

			if (parent != null) {
				parent.InvalidateCache();
			}

			int parentId = (parent == null) ? -1 : parent.Id;
			return new LocalDirectory(last_id, parentId, name, local_path, fullPath);
		}

		internal static long CountByParentId (Nullable<int> parent_id)
		{
			return Core.Core.FileSystem.UseConnection<long>(delegate (IDbConnection connection) {
				IDbCommand command = connection.CreateCommand();
				string query = null;
				if (parent_id.Equals(null)) {
					query = "SELECT count(id) FROM directoryitems WHERE parent_id ISNULL AND type = 'D'";
				} else {
					query = "SELECT count(id) FROM directoryitems WHERE parent_id = @parent_id AND type = 'D'";
					Core.Core.FileSystem.AddParameter(command, "@parent_id", (int)parent_id);
				}
				command.CommandText = query;
				return (long) Core.Core.FileSystem.ExecuteScalar(command);
			});
		}

		internal static LocalDirectory[] ListByParentId (Nullable<int> parent_id)
		{
			return Core.Core.FileSystem.UseConnection<LocalDirectory[]>(delegate (IDbConnection connection) {
				IDbCommand command = connection.CreateCommand();
				string query = null;
				if (parent_id.Equals(null)) {
					query = "SELECT * FROM directoryitems WHERE parent_id ISNULL AND type = 'D'";
				} else {
					query = "SELECT * FROM directoryitems WHERE parent_id = @parent_id AND type = 'D'";
					Core.Core.FileSystem.AddParameter(command, "@parent_id", (int)parent_id);
				}
				command.CommandText = query;
				DataSet ds = Core.Core.FileSystem.ExecuteDataSet(command);

				LocalDirectory[] results = new LocalDirectory[ds.Tables[0].Rows.Count];
				for (int x = 0; x < ds.Tables[0].Rows.Count; x++) {
					results[x] = LocalDirectory.FromDataRow(ds.Tables[0].Rows[x]);
				}
				return results;
			});
		}
				
		public void DeleteFile (string name)
		{
			Core.Core.FileSystem.UseConnection(delegate (IDbConnection connection) {
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = "DELETE FROM directoryitems WHERE parent_id = @parent_id AND name = @name AND type='F'";
				Core.Core.FileSystem.AddParameter(cmd, "@parent_id", id); 
				Core.Core.FileSystem.AddParameter(cmd, "@name", name);
				Core.Core.FileSystem.ExecuteNonQuery(cmd);
			}, true);

			this.InvalidateCache();
		}

		
		#endregion
	}
}
