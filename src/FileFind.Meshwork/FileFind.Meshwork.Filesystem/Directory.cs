//
// Directory.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.IO;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FileFind.Meshwork;
using FileFind.Meshwork.Protocol;

namespace FileFind.Meshwork.Filesystem
{
	public class Directory : IDirectoryItem
	{
		FileSystemProvider fs;

		int id;
		int parent_id;
		string name;
		string localPath;
		string node_id;
		bool requested;
		string fullPath;
		string directoryType = null;
		Directory parent;
		Nullable<long> fileCount = null;
		Nullable<long> directoryCount = null;

		File[] files;
		Directory[] directories;

		Network network;
		Node node;

		public static Directory ById (FileSystemProvider fs, int id)
		{
			return fs.UseConnection<Directory>(delegate (IDbConnection connection) {
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = "SELECT * FROM directoryitems WHERE id=@id AND type = 'D' LIMIT 1";
				fs.AddParameter(cmd, "@id", id);
				DataSet ds = fs.ExecuteDataSet(cmd);
				if (ds.Tables[0].Rows.Count > 0) {
					return Directory.FromDataRow(fs, ds.Tables[0].Rows[0]);
				} else {
					return null;
				}
			});
		}

		public static Directory CreateDirectory (FileSystemProvider fs, string fullPath, Node node)
		{
			string[] pathSplit = fullPath.Split('/');
		
			Directory preveousDir = fs.RootDirectory;
			for (int xx = 0; xx < pathSplit.Length; xx++) {
				string pathPart = pathSplit[xx];
				if (pathPart == String.Empty) {
					continue;
				}
				if (preveousDir.HasSubdirectory(pathPart) == false) {
					preveousDir = preveousDir.CreateSubdirectory(pathPart, node);
				} else {
					preveousDir = preveousDir.GetSubdirectory(pathPart);
				}
			}
			return preveousDir;
		}

		public static Directory CreateDirectory (FileSystemProvider fs, Directory parent, string name)
		{
			return CreateDirectory(fs, parent, name, null, null);
		}

		public static Directory CreateDirectory (FileSystemProvider fs, Directory parent, string name, Node node)
		{
			return CreateDirectory(fs, parent, name, null, node);
		}

		public static Directory CreateDirectory (FileSystemProvider fs, Directory parent, string name, string local_path)
		{
			return CreateDirectory(fs, parent, name, local_path, null);
		}

		private static Directory CreateDirectory (FileSystemProvider fs, Directory parent, string name, string local_path, Node node)
		{
			string fullPath;
			int last_id = -1;
			
			if (parent != null && parent != Core.FileSystem.RootDirectory && name != "/") {
				if (node == null && String.IsNullOrEmpty(local_path)) {
					throw new ArgumentNullException("local_path");
				}

				if (!String.IsNullOrEmpty(local_path) && !System.IO.Directory.Exists(local_path)) {
					throw new ArgumentException("local_path", String.Format("Directory does not exist: '{0}'", local_path));
				}
			}

			if (parent != null) {
				fullPath = PathUtil.Join(parent.FullPath, name);
			} else {
				fullPath = PathUtil.Join("/", name);
			}

			if (fullPath.Length > 1 && fullPath.EndsWith("/")) {
				fullPath = fullPath.Substring(0, fullPath.Length - 1);
			}

			string nodeId = null;

			fs.UseConnection(delegate (IDbConnection connection) {
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = "INSERT INTO directoryitems (type, parent_id, name, local_path, node_id, full_path) VALUES ('D', @parent_id, @name, @local_path, @node_id, @full_path);";
				fs.AddParameter(cmd, "@name", name);
				fs.AddParameter(cmd, "@full_path", fullPath);
				fs.AddParameter(cmd, "@local_path", local_path);
				if (node != null) {
					nodeId = node.NodeID;
				} else if (name != String.Empty) {
					nodeId = Core.MyNodeID;
				} else {
					nodeId = null;
				}
				fs.AddParameter(cmd, "@node_id", nodeId);

				if (parent == null) {
					fs.AddParameter(cmd, "@parent_id", null);
				} else {
					fs.AddParameter(cmd, "@parent_id", (int)parent.Id);
				}
				cmd.ExecuteNonQuery();

				cmd = connection.CreateCommand();
				cmd.CommandText = "SELECT last_insert_rowid()";

				last_id = Convert.ToInt32(cmd.ExecuteScalar());
			}, true);

			if (parent != null) {
				parent.InvalidateCache();
			}

			int parentId = (parent == null) ? -1 : parent.Id;
			return new Directory(fs, last_id, parentId, name, nodeId, true, fullPath, local_path);
		}

		internal static long CountByParentId (FileSystemProvider fs, Nullable<int> parent_id)
		{
			return fs.UseConnection<long>(delegate (IDbConnection connection) {
				IDbCommand command = connection.CreateCommand();
				string query = null;
				if (parent_id.Equals(null)) {
					query = "SELECT count(id) FROM directoryitems WHERE parent_id ISNULL AND type = 'D'";
				} else {
					query = "SELECT count(id) FROM directoryitems WHERE parent_id = @parent_id AND type = 'D'";
					fs.AddParameter(command, "@parent_id", (int)parent_id);
				}
				command.CommandText = query;
				return (long) command.ExecuteScalar();
			});
		}

		internal static Directory[] ListByParentId (FileSystemProvider fs, Nullable<int> parent_id)
		{
			return fs.UseConnection<Directory[]>(delegate (IDbConnection connection) {
				IDbCommand command = connection.CreateCommand();
				string query = null;
				if (parent_id.Equals(null)) {
					query = "SELECT * FROM directoryitems WHERE parent_id ISNULL AND type = 'D'";
				} else {
					query = "SELECT * FROM directoryitems WHERE parent_id = @parent_id AND type = 'D'";
					fs.AddParameter(command, "@parent_id", (int)parent_id);
				}
				command.CommandText = query;
				DataSet ds = fs.ExecuteDataSet(command);

				Directory[] results = new Directory[ds.Tables[0].Rows.Count];
				for (int x = 0; x < ds.Tables[0].Rows.Count; x++) {
					results[x] = Directory.FromDataRow(fs, ds.Tables[0].Rows[x]);
				}
				return results;
			});
		}

		public static Directory FindByLocalPath (FileSystemProvider fs, string localPath)
		{
			return fs.UseConnection<Directory>(delegate (IDbConnection connection) {
				IDbCommand command = connection.CreateCommand();
				command.CommandText = "SELECT * FROM directoryitems WHERE local_path = @local_path LIMIT 1";
				fs.AddParameter (command, "@local_path", localPath);
				DataSet ds = fs.ExecuteDataSet(command);
				if (ds.Tables[0].Rows.Count > 0) {
					return Directory.FromDataRow(fs, ds.Tables[0].Rows[0]);
				} else {
					return null;
				}
			});
		}

		internal static Directory FromDataRow (FileSystemProvider fs, DataRow row)
		{
			int id = Convert.ToInt32(row["id"]);
			int parent_id = -1;
			if ((row["parent_id"] is System.DBNull) == false) {
				parent_id = Convert.ToInt32(row["parent_id"]);
			}

			string name = row["name"].ToString();
			string node_id = row["node_id"].ToString();
			string fullPath = row["full_path"].ToString();
			string localPath = row["local_path"].ToString();
			bool requested = false;

			if ((row["requested"] is System.DBNull) == false) {
				if (row["requested"].ToString() == "0") {
					requested = false;
				} else if (row["requested"].ToString() == "1") {
					requested = true;
				}
			}

			return new Directory(fs, id, parent_id, name, node_id, requested, fullPath, localPath);
		}

		private Directory (FileSystemProvider fs, int id, int parentId, string name, string nodeId, bool requested, string fullPath, string localPath)
		{
			this.fs = fs;
			this.id = id;
			this.parent_id = parentId;
			this.name = name;
			this.node_id = nodeId;
			this.requested = requested;
			this.fullPath = fullPath;
			this.localPath = localPath;
		}

		public Directory CreateSubdirectory (string name)
		{
			/* XXX: I dont know what the idea here was
			if (String.IsNullOrEmpty(this.LocalPath)) {
				throw new Exception("Dir is not local: " + this.FullPath);
			}
			*/

			return CreateSubdirectory(name, null);
		}
		
		public Directory CreateSubdirectory (string name, Node node)
		{
			if (node == null) {
				return Directory.CreateDirectory(fs, this, name, Path.Combine(this.LocalPath, name));
			} else {
				return Directory.CreateDirectory(fs, this, name, node);
			}
		}

		public static Directory GetDirectory (FileSystemProvider fs, string fullPath)
		{
			if (String.IsNullOrEmpty(fullPath)) {
				throw new ArgumentNullException("fullPath");
			}

			if (fullPath.Length > 1 && fullPath.EndsWith("/")) {
				fullPath = fullPath.Substring(0, fullPath.Length - 1);
			}

			return fs.UseConnection<Directory>(delegate (IDbConnection connection) {
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = "SELECT * FROM directoryitems WHERE full_path = @full_path AND type = 'D' LIMIT 1";
				fs.AddParameter(cmd, "@full_path", fullPath);
				DataSet ds = fs.ExecuteDataSet(cmd);

				if (ds.Tables[0].Rows.Count > 0) {
					return Directory.FromDataRow(fs, ds.Tables[0].Rows[0]);
				} else {
					return null;
				}
			});
		}

		public static Directory GetSubdirectory (FileSystemProvider fs, long parentDirectoryId, string name)
		{
			return fs.UseConnection<Directory>(delegate (IDbConnection connection) {
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = "SELECT * FROM directoryitems WHERE parent_id = @parent_id AND name = @name AND type = 'D' LIMIT 1";
				fs.AddParameter(cmd, "@parent_id", parentDirectoryId);
				fs.AddParameter(cmd, "@name", name);
				DataSet ds = fs.ExecuteDataSet(cmd);

				if (ds.Tables[0].Rows.Count > 0) {
					return Directory.FromDataRow(fs, ds.Tables[0].Rows[0]);
				} else {
					return null;
				}
			});
		}

		public static long GetSubdirectoryId (FileSystemProvider fs, long parentDirectoryId, string name)
		{
			return fs.UseConnection<long>(delegate (IDbConnection connection) {
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = "SELECT id FROM directoryitems WHERE parent_id = @parent_id AND name = @name AND type = 'D' LIMIT 1";
				fs.AddParameter(cmd, "@parent_id", parentDirectoryId);
				fs.AddParameter(cmd, "@name", name);
				object result = cmd.ExecuteScalar();
				if (result == null) {
					return -1;
				} else {
					return Convert.ToInt64(result);
				}
			});
		}

		public string NodeID {
			get {
				return node_id;
			}
			set {
				node_id = value;
			}
		}

		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}

		public int Id {
			get {
				return id;
			}
		}

		public long FileCount {
			get {
				if (files != null) {
					return files.Length;
				}

				if (fileCount.Equals(null)) {
					fileCount = File.CountByParentId(fs, this.id);
				}
				return (long)fileCount;
			}
		}

		public File[] Files {
			get {
				if (files == null) {
					files = File.ListByParentId(fs, this.id);
				}
				return files;
			}
		}

		public long DirectoryCount {
			get {
				if (directories != null) {
					return directories.Length;
				}

				if (directoryCount.Equals(null)) {
					directoryCount = Directory.CountByParentId(fs, this.id);
				}
				return (long)directoryCount;
			}
		}

		public Directory[] Directories {
			get {
				if (directories == null) {
					directories = Directory.ListByParentId(fs, this.id);
				}
				return directories;
			}
		}

		public File CreateFile (string name, string local_path, long length)
		{
			return CreateFile(name, local_path, length, null);
		}

		public File CreateFile (FileInfo info)
		{
			return CreateFile(info.Name, info.FullName, info.Length, null);
		}

		public File CreateFile (SharedFileDetails details, Node node)
		{
			// XXX: Set InfoHash and Pieces too!!!
			return File.CreateFile(fs, this, details.Name, null, details.Size, node);
		}

		public File CreateFile (SharedFileListing info, Node node)
		{
			return CreateFile(info.Name, null, info.Size, node);
		}

		public File CreateFile (string name, string local_path, long length, Node node)
		{
			return File.CreateFile(fs, this, name, local_path, length, node);
		}

		public Directory GetSubdirectory (string name)
		{
			foreach (Directory subdir in Directories) {
				if (subdir.Name == name) {
					return subdir;
				}
			}
			return null;
		}

		public bool HasSubdirectory (string name)
		{
			return fs.UseConnection<bool>(delegate (IDbConnection connection) {
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = "SELECT id FROM directoryitems WHERE parent_id = @parent_id AND name = @name AND type='D' LIMIT 1";
				fs.AddParameter(cmd, "@parent_id", id); 
				fs.AddParameter(cmd, "@name", name);
				return cmd.ExecuteScalar() != null;
			});
		}

		public File GetFile (string name)
		{
			foreach (File file in Files) {
				if (file.Name == name) {
					return file;
				}
			}
			return null;
		}

		public bool HasFile (string name)
		{
			foreach (File file in Files) {
				if (file.Name == name) {
					return true;
				}
			}
			return false;
		}

		public void DeleteFile (string name)
		{
			fs.UseConnection(delegate (IDbConnection connection) {
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = "DELETE FROM directoryitems WHERE parent_id = @parent_id AND name = @name AND type='F'";
				fs.AddParameter(cmd, "@parent_id", id); 
				fs.AddParameter(cmd, "@name", name);
				cmd.ExecuteNonQuery();
			}, true);

			this.InvalidateCache();
		}

		public bool Requested {
			get {
				return requested;
			}
			set {
				requested = value;

				fs.UseConnection(delegate (IDbConnection connection) {
					IDbCommand cmd = connection.CreateCommand();
					cmd.CommandText = "UPDATE directoryitems SET requested=@requested WHERE id = @id";
					fs.AddParameter(cmd, "@id", id); 
					fs.AddParameter(cmd, "@requested", requested);
					cmd.ExecuteNonQuery();
				}, true);
			}
		}
		
		public Directory Parent {
			get {
				if (parent == null) {
					fs.UseConnection(delegate (IDbConnection connection) {
				 		parent = Directory.ById(fs, parent_id);
					});
				}
				return parent;
			}
		}

		public string FullPath {
			get {
				return fullPath;
			}
		}

		public long Size {
			get {
				return (FileCount + DirectoryCount);
			}
		}

		public string Type {
			get {
				return null;
			}
		}

		public string LocalPath {
			get {
				return localPath;
			}
			set {
				localPath = value;
			}
		}

		public void Delete ()
		{
			foreach (Directory subDir in Directories) {
				subDir.Delete();
			}

			foreach (File file in Files) {
				file.Delete();
			}

			fs.UseConnection(delegate (IDbConnection connection) {
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = "DELETE FROM directoryitems WHERE id = @directory_id AND type='D'";
				fs.AddParameter(cmd, "@directory_id", id); 
				cmd.ExecuteNonQuery();
			}, true);

			if (parent != null) {
				parent.InvalidateCache();
			}
			this.InvalidateCache();
		}

		public void ClearFiles ()
		{
			fs.UseConnection(delegate (IDbConnection connection) {
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = "DELETE FROM directoryitems WHERE parent_id = @parent_id AND type='F'";
				fs.AddParameter(cmd, "@parent_id", id); 
				cmd.ExecuteNonQuery();
			}, true);

			this.InvalidateCache();
		}

		public void ClearDirectories ()
		{
			fs.UseConnection(delegate (IDbConnection connection) {
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = "DELETE FROM directoryitems WHERE parent_id = @parent_id AND type='D'";
				fs.AddParameter(cmd, "@parent_id", id); 
				cmd.ExecuteNonQuery();
			}, true);

			this.InvalidateCache();
		}
	
		/*
		public Directory Clone ()
		{
			Directory directory = new Directory (name);
			directory.Files = files;
			directory.Directories = directories;
			directory.Parent = parent;
			//directory.LocalPath = localPath;
			return directory;
		}
		*/

		/*
		public SearchResultInfo FindFile (string fileName, SearchMode searchMode,
		                                  bool caseSensitive, string[] extensions)
		{
			SearchResultInfo result = new SearchResultInfo();
			List<SearchResult> results = new List<SearchResult>();

			if (caseSensitive == false) {
				fileName = fileName.ToLower();
			}

			FindFilePrivate (fileName, searchMode, caseSensitive, extensions, this, results);

			if (results.Count > 15) {
				result.ExeededLimit = true;
			}

			result.Files = results.ToArray ();
			return result;
		}
		*/

		
		/*
		private void FindFilePrivate (string fileName, SearchMode searchMode,
		                              bool caseSensitive, string[] extensions,
		                              Directory directory,
					      List<SearchResult> results)
		{
			foreach (File file in directory.Files) {
				bool includeMe = false;
				string currentFileName = file.Name;
				if (caseSensitive == false) {
					currentFileName = currentFileName.ToLower();
				}

				if (searchMode == SearchMode.ExactPhrase) {
					if (currentFileName == fileName) {
						includeMe = true;
					}
				} else if (searchMode == SearchMode.RegEx) {
					if (Regex.Match(currentFileName, fileName).Success == true) {
						includeMe = true;
					}
				} else if (searchMode == SearchMode.Wildcard) {
					if (FileFind.Common.WildcardMatch(currentFileName, fileName) == true) {
						includeMe = true;
					}
				}
				
				if (includeMe) {
					SearchResult resultFile = new SearchResult();
					resultFile.FilePath = file.FullPath;
					resultFile.FileSize = file.Size;
					results.Add (resultFile);

					if (results.Count > 15) {
						return;
					}
				}
			}
			foreach (Directory subDirectory in directory.Directories) {
				FindFilePrivate (fileName, searchMode,
				                 caseSensitive, extensions, 
						 subDirectory, results);

				if (results.Count > 15) {
					return;
				}
			}
		}
		*/

		public static string GetPathPart (string path, int index)
		{
			string[] parts = path.Substring (1).Split ('/');
			return parts[index];
		}

		public Network Network {
			get {
				if (network == null) {
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
					}  else {
						return null;
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
						node = network.GetNode(pathParts[2]);
					} else {
						return null;
					}
				}

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

		internal void InvalidateCache ()
		{
			directories = null;
			files = null;
		}

		internal void BulkAddFiles (SharedFileListing[] files, Node node)
		{
			fs.UseConnection(delegate (IDbConnection connection) {
				IDbTransaction transaction = connection.BeginTransaction();
					
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = @"INSERT INTO directoryitems (type, name, parent_id, length, node_id, full_path)
						    VALUES ('F', @name, @parent_id, @length, @node_id, @full_path);";
				fs.AddParameter(cmd, "@parent_id", this.Id);
				if (node != null) {
					fs.AddParameter(cmd, "@node_id", node.NodeID);
				} else {
					throw new ArgumentNullException("node");
				}

				IDbDataParameter nameParam = cmd.CreateParameter();
				nameParam.ParameterName = "@name";
				cmd.Parameters.Add(nameParam);

				IDbDataParameter fullPathParam = cmd.CreateParameter();
				fullPathParam.ParameterName = "@full_path";
				cmd.Parameters.Add(fullPathParam);

				IDbDataParameter lengthParam = cmd.CreateParameter();
				lengthParam.ParameterName = "@length";
				cmd.Parameters.Add(lengthParam);

				try {
					foreach (SharedFileListing file in files) {
						string name   = file.Name;
						long   length = file.Size;

						string fullPath = PathUtil.Join(this.FullPath, name);

						nameParam.Value     = name;
						fullPathParam.Value = fullPath;
						lengthParam.Value   = length;
						
						cmd.ExecuteNonQuery();
					}

					transaction.Commit();

				} catch (Exception ex) {
					transaction.Rollback();
					throw ex;
				}
			}, true);

			this.InvalidateCache();
		}

		internal void BulkAddSubdirectories (string[] directories, Node node)
		{
			fs.UseConnection(delegate (IDbConnection connection) {
				IDbTransaction transaction = connection.BeginTransaction();
					
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = @"INSERT INTO directoryitems (type, parent_id, name, node_id, full_path)
				                   VALUES ('D', @parent_id, @name, @node_id, @full_path);";
	
				fs.AddParameter(cmd, "@parent_id", (int)this.Id);

				if (node != null) {
					fs.AddParameter(cmd, "@node_id", node.NodeID);
				} else {
					throw new ArgumentNullException("node");
				}

				IDbDataParameter nameParam = cmd.CreateParameter();
				nameParam.ParameterName = "@name";
				cmd.Parameters.Add(nameParam);

				IDbDataParameter fullPathParam = cmd.CreateParameter();
				fullPathParam.ParameterName = "@full_path";
				cmd.Parameters.Add(fullPathParam);

				try {
					foreach (string name in directories) {
						string fullPath = PathUtil.Join(this.FullPath, name);
						if (fullPath.Length > 1 && fullPath.EndsWith("/")) {
							fullPath = fullPath.Substring(0, fullPath.Length - 1);
						}

						nameParam.Value     = name;
						fullPathParam.Value = fullPath;
					
						cmd.ExecuteNonQuery();
					}

					transaction.Commit();

				} catch (Exception ex) {
					transaction.Rollback();
					throw ex;
				}

			}, true);

			this.InvalidateCache();
		}
	}
}
