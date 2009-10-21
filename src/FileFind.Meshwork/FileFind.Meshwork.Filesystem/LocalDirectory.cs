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

namespace FileFind.Meshwork.Filesystem
{
	public class LocalDirectory : AbstractDirectory, ILocalDirectoryItem
	{
		int              id;
		int              parentId;
		string           name;
		string           localPath;
		int              fileCount = -1;
		int              directoryCount = -1;
		
		#region Constructors
		
		protected LocalDirectory (int id, int parentId, string name, string localPath)
		{
			this.id        = id;
			this.parentId  = parentId;
			this.name      = name;
			this.localPath = localPath;
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
					Core.FileSystem.UseConnection(delegate(IDbConnection connection) {
						parent = LocalDirectory.ById(parentId);
					});						
					if (parent == null)
						throw new Exception(String.Format("Parent not found! Name: {0} Id: {1} ParentId: {2}", Name, Id, parentId));
					return parent;
				} else {
					return Core.FileSystem.RootDirectory.MyDirectory;
				}
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
			return LocalFile.CreateFile(this, info);
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

			Core.FileSystem.UseConnection(delegate (IDbConnection connection) {
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = "DELETE FROM directoryitems WHERE id = @directory_id AND type='D'";
				Core.FileSystem.AddParameter(cmd, "@directory_id", id); 
				Core.FileSystem.ExecuteNonQuery(cmd);
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
			return Core.FileSystem.UseConnection<LocalDirectory>(delegate (IDbConnection connection) {
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = "SELECT * FROM directoryitems WHERE id=@id AND type = 'D' LIMIT 1";
				Core.FileSystem.AddParameter(cmd, "@id", id);
				DataSet ds = Core.FileSystem.ExecuteDataSet(cmd);
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

			return new LocalDirectory(id, parent_id, name, localPath);
		}
		
		private static LocalDirectory CreateDirectory (LocalDirectory parent, string name, string local_path)
		{
			string fullPath;
			int last_id = -1;
			
			if (String.IsNullOrEmpty(local_path)) {
				throw new ArgumentNullException("local_path");
			}

			if (!System.IO.Directory.Exists(local_path)) {
				throw new ArgumentException("local_path", String.Format("Directory does not exist: '{0}'", local_path));
			}

			if (parent != null) {
				fullPath = PathUtil.Join(parent.FullPath, name);
			} else {
				fullPath = PathUtil.Join("/", name);
			}

			if (fullPath.Length > 1 && fullPath.EndsWith("/")) {
				fullPath = fullPath.Substring(0, fullPath.Length - 1);
			}

			Core.FileSystem.UseConnection(delegate (IDbConnection connection) {
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = "INSERT INTO directoryitems (type, parent_id, name, local_path) VALUES ('D', @parent_id, @name, @local_path);";
				Core.FileSystem.AddParameter(cmd, "@name", name);
				Core.FileSystem.AddParameter(cmd, "@local_path", local_path);

				if (parent == null) {
					Core.FileSystem.AddParameter(cmd, "@parent_id", null);
				} else {
					Core.FileSystem.AddParameter(cmd, "@parent_id", (int)parent.Id);
				}
				Core.FileSystem.ExecuteNonQuery(cmd);

				cmd = connection.CreateCommand();
				cmd.CommandText = "SELECT last_insert_rowid()";

				last_id = Convert.ToInt32(cmd.ExecuteScalar());
			}, true);

			if (parent != null) {
				parent.InvalidateCache();
			}

			int parentId = (parent == null) ? -1 : parent.Id;
			return new LocalDirectory(last_id, parentId, name, local_path);
		}

		internal static long CountByParentId (Nullable<int> parent_id)
		{
			return Core.FileSystem.UseConnection<long>(delegate (IDbConnection connection) {
				IDbCommand command = connection.CreateCommand();
				string query = null;
				if (parent_id.Equals(null)) {
					query = "SELECT count(id) FROM directoryitems WHERE parent_id ISNULL AND type = 'D'";
				} else {
					query = "SELECT count(id) FROM directoryitems WHERE parent_id = @parent_id AND type = 'D'";
					Core.FileSystem.AddParameter(command, "@parent_id", (int)parent_id);
				}
				command.CommandText = query;
				return (long) Core.FileSystem.ExecuteScalar(command);
			});
		}

		internal static LocalDirectory[] ListByParentId (Nullable<int> parent_id)
		{
			return Core.FileSystem.UseConnection<LocalDirectory[]>(delegate (IDbConnection connection) {
				IDbCommand command = connection.CreateCommand();
				string query = null;
				if (parent_id.Equals(null)) {
					query = "SELECT * FROM directoryitems WHERE parent_id ISNULL AND type = 'D'";
				} else {
					query = "SELECT * FROM directoryitems WHERE parent_id = @parent_id AND type = 'D'";
					Core.FileSystem.AddParameter(command, "@parent_id", (int)parent_id);
				}
				command.CommandText = query;
				DataSet ds = Core.FileSystem.ExecuteDataSet(command);

				LocalDirectory[] results = new LocalDirectory[ds.Tables[0].Rows.Count];
				for (int x = 0; x < ds.Tables[0].Rows.Count; x++) {
					results[x] = LocalDirectory.FromDataRow(ds.Tables[0].Rows[x]);
				}
				return results;
			});
		}
				
		public void DeleteFile (string name)
		{
			Core.FileSystem.UseConnection(delegate (IDbConnection connection) {
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = "DELETE FROM directoryitems WHERE parent_id = @parent_id AND name = @name AND type='F'";
				Core.FileSystem.AddParameter(cmd, "@parent_id", id); 
				Core.FileSystem.AddParameter(cmd, "@name", name);
				Core.FileSystem.ExecuteNonQuery(cmd);
			}, true);

			this.InvalidateCache();
		}

		
		#endregion
	}
}
