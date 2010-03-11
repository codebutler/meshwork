//
// FileSystemProvider.cs: The root of the virtual filesystem
// 
// Author:
//   Eric Butler <eric@filefind.net>
//
//   (C) 2005-2006 FileFind.net (http://filefind.net/)
//

using System;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Collections;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using FileFind;
using FileFind.Meshwork;
using FileFind.Meshwork.Collections;
using FileFind.Meshwork.Exceptions;
using FileFind.Meshwork.Protocol;
using Mono.Data.Sqlite;
using Mono.Data;
using System.Data;
using Hyena.Query;

namespace FileFind.Meshwork.Filesystem
{
	public delegate T DbMethod<T> (IDbConnection connection);
	public delegate void DbMethod (IDbConnection connection);

	public delegate void DirectoryCallback (IDirectory directory);
	
	public class FileSystemProvider
	{
		const string SCHEMA_VERSION = "12";
		
		string connectionString;
		long yourTotalBytes = -1;
		long yourTotalFiles = -1;

		List<IDbConnection> connections = new List<IDbConnection>();
		List<IDbConnection> workingConnections = new List<IDbConnection>();
		
		Dictionary<string, List<DirectoryCallback>> remoteDirectoryCallbacks = new Dictionary<string, List<DirectoryCallback>>();

		public static readonly int MAX_RESULTS = 300;

		public static QueryField FileNameField = new QueryField(
			"name", "Name", 
			"File Name", "directoryitems.name", true,
			"name", "filename"
		);
		
		public static QueryField FileTypeField = new QueryField(
			"type", "Type",
			"Type", "directoryitems.type", typeof(FileTypeQueryValue),
			"type", "filetype"
		);
		
		public static QueryField FileSizeField = new QueryField(
			"length", "Length",
			"File Size", "directoryitems.length", typeof(IntegerQueryValue), 
		    "size", "filesize", "length"
		);
		 
		public static QueryField FileSHA1Field = new QueryField(
			"sha1", "SHA1", 
			"SHA1", "directoryitems.sha1", typeof(ExactStringQueryValue),
			"sha1", "sha"
		);
		
		public static QueryField FileInfoHashField = new QueryField(
			"infohash", "InfoHash", 
			"Info Hash", "directoryitems.info_hash", typeof(ExactStringQueryValue),
			"infohash"
		);
		
        public static QueryFieldSet FieldSet = new QueryFieldSet (
	        FileNameField, FileTypeField, FileSizeField, FileSHA1Field , FileInfoHashField
	    );
		
		
		public FileSystemProvider ()
		{
			string path = Path.Combine(Core.Settings.DataPath, "shares.db");

			bool create = false;

			if (!System.IO.File.Exists(path)) {
				create = true;
			}

			connectionString = String.Format("URI=file:{0},version=3;busy_timeout=300000", path);

			if (!create) {
				try {
					// Do some sanity checking here, if anything looks bad, start over
					if (RootDirectory == null) {
						LoggingService.LogWarning("Unable to find root dir");
						create = true;
					} else {
						// Verify version
						string currentVersion = ExecuteScalar("SELECT value FROM properties WHERE name='version'").ToString();
						if (currentVersion != SCHEMA_VERSION) {
							LoggingService.LogWarning("Schema has changed, recreating db.");
							create = true;
						}
					}

				} catch (Exception) { //XXX: Only catch SQLite errors like this!
					// Something is probably wrong with the
					// schema, lets start over.
					create = true;
				}
			}

			if (create) {
				CreateTables();

				// Kill any active connections, they wont be able to reach the new db.
				lock (connections) {
					while (connections.Count > 0) {
						connections[0].Dispose();
						connections.RemoveAt(0);
					}
				}

				// Force a scan.
				Core.Settings.LastShareScan = DateTime.MinValue;
			}
		}
		
		public bool BeginGetDirectory (string path, DirectoryCallback callback)
		{
			// FIXME: BARGH
			if (!path.StartsWith("/")) path = "/" + path;		
			if (path.EndsWith("/")) path = path.Substring(0, path.Length - 1);
			
			// LocalDirectory and NetworkDirectory objects can always be returned immediately.
			string[] parts = path.Split('/');
			if ((parts.Length > 1 && parts[1] == "local") || parts.Length < 3) {
				var directory = GetDirectory(path);
				callback(directory);
				return true;
			} else {
				RemoteDirectory directory = (RemoteDirectory) GetDirectory(path);
				if (directory != null) {
					 if (directory.State != RemoteDirectoryState.ContentsUnrequested) {
						callback(directory);
						return true;
					}
				}
				
				lock (remoteDirectoryCallbacks) {
					if (!remoteDirectoryCallbacks.ContainsKey(path)) {
						remoteDirectoryCallbacks.Add(path, new List<DirectoryCallback>());
					}
					var list = remoteDirectoryCallbacks[path];
					list.Add(callback);
				}				
				
				var network = PathUtil.GetNetwork(path);
				network.RequestDirectoryListing(path);
				return false;
			}
		}

		internal LocalDirectory GetLocalDirectory (string path)
		{
			if (path.Length > 1 && path.EndsWith("/"))
				path = path.Substring(0, path.Length - 1);
			
			string[] parts = path.Split('/');
			if (parts.Length < 3) {
				return (LocalDirectory)GetDirectory(path);
			} else {			
				return Core.FileSystem.UseConnection<LocalDirectory>(delegate (IDbConnection connection) {
					string query = "SELECT * FROM directoryitems WHERE type = 'D' AND full_path = @full_path LIMIT 1";
					IDbCommand command = connection.CreateCommand();
					command.CommandText = query;
					Core.FileSystem.AddParameter(command, "@full_path", path);
					DataSet data = Core.FileSystem.ExecuteDataSet(command);
					if (data.Tables[0].Rows.Count > 0)
						return LocalDirectory.FromDataRow(data.Tables[0].Rows[0]);
					else
						return null;
				});
			}
		}
		
		// FIXME: Eventually, this method should become private.
		internal IDirectory GetDirectory (string path)
		{
			if (!path.StartsWith("/")) path = "/" + path;		
			if (path.EndsWith("/")) path = path.Substring(0, path.Length - 1);
			IDirectory directory = Core.FileSystem.RootDirectory;
			if (path.Length > 0) {
				string[] pathParts = path.Substring(1).Split('/');
				foreach (string dirName in pathParts)
				{
					directory = directory.GetSubdirectory(dirName);
					if (directory == null)
						return null;
				}
			}
			return directory;
		}
		
		public IFile GetFile (string path)
		{
			string directoryPath = String.Join("/", path.Split('/').Slice(0, -2));
			string fileName = path.Split('/').Slice(-1, -1)[0];
			IDirectory directory = GetDirectory(directoryPath);
			if (directory != null) {
				return directory.GetFile(fileName);
			} else {
				return null;
			}
		}
		
		internal T UseConnection<T> (DbMethod<T> method)
		{
			return UseConnection(method, false);
		}

		internal T UseConnection<T> (DbMethod<T> method, bool write)
		{
			IDbConnection theConnection;

			// Try to let any pending reads go through if we need to write,
			// since it locks everything.
			if (write) {
				DateTime start = DateTime.Now;
				while (workingConnections.Count > 0) {
					System.Threading.Thread.Sleep(1);
					if ((DateTime.Now - start).TotalSeconds >= 1) {
						// After a second, give up and go anyway.
						break;
					}
				}
			}

			lock (connections) {
				theConnection = connections.Find(delegate (IDbConnection c) { return c.State == System.Data.ConnectionState.Open; });
				connections.Remove(theConnection);
			}

			if (theConnection == null) {
				theConnection = CreateDbConnection();
			}
			
			workingConnections.Add(theConnection);

			T result = method(theConnection);

			workingConnections.Remove(theConnection);

			lock (connections) {
				connections.Add(theConnection);
			}

			return result;
		}
		
		internal void UseConnection (DbMethod method)
		{
			UseConnection(method, false);
		}

		internal void UseConnection (DbMethod method, bool write)
		{
			UseConnection((DbMethod<object>) delegate (IDbConnection connection) {
				method(connection);
				return null;
			}, write);
		}	
		
		private IDbConnection CreateDbConnection ()
		{
			SqliteConnection connection = new SqliteConnection(connectionString);
			connection.Open ();
			return (IDbConnection)connection;
		}
		
		public RootDirectory RootDirectory {
			get {
				return RootDirectory.Instance;
			}
		}
		
		public long TotalDirectories {
			get {
				return UseConnection<long>(delegate (IDbConnection connection) {
					string query = "SELECT count(*) FROM directoryitems WHERE type='D'";
					IDbCommand command = connection.CreateCommand();
					command.CommandText = query;
					object result = ExecuteScalar(command);
					return (result == null) ? 0 : (long)result;
				});
			}
		}
		public long TotalFiles {
			get {
				return UseConnection<long>(delegate (IDbConnection connection) {
					string query = "SELECT count(*) FROM directoryitems WHERE type='F'";
					IDbCommand command = connection.CreateCommand();
					command.CommandText = query;
					object result = ExecuteScalar(command);
					return (result == null) ? 0 : (long)result;
				});
			}
		}

		public long TotalBytes {
			get {
				return UseConnection<long>(delegate (IDbConnection connection) {
					string query = "SELECT sum(length) FROM directoryitems WHERE type='F'";
					IDbCommand command = connection.CreateCommand();
					command.CommandText = query;
					object result = ExecuteScalar(command);
					return (result == null) ? 0 : (long)result;
				});
			}
		}

		public long YourTotalFiles {
			get {
				if (yourTotalFiles != -1) {
					return yourTotalFiles;
				} else {
					return UseConnection<long>(delegate (IDbConnection connection) {
						string query = "SELECT count(*) FROM directoryitems WHERE type='F'";
						IDbCommand command = connection.CreateCommand();
						command.CommandText = query;
						object result = ExecuteScalar(command);
						yourTotalFiles = (result is DBNull) ? 0 : (long)result;
						return yourTotalFiles;
					});
				}
			}
		}

		public long YourTotalBytes {
			get {
				if (yourTotalBytes != -1) {
					return yourTotalBytes;
				} else {
					return UseConnection<long>(delegate (IDbConnection connection) {
						string query = "SELECT sum(length) FROM directoryitems WHERE type='F'";
						IDbCommand command = connection.CreateCommand();
						command.CommandText = query;
						object result = ExecuteScalar(command);
						yourTotalBytes = (result is DBNull) ? 0 : (long)result;
						return yourTotalBytes;
					});
				}
			}
		}

		public SearchResultInfo SearchFiles(string query)
		{
			string           sql;
			IDbCommand       command;
			DataSet          ds;
			int              x;
			SearchResultInfo result;
						
			var directories = new List<string>();
			var files = new List<SharedFileListing>();

			result = new SearchResultInfo();
			
			var queryNode = Hyena.Query.UserQueryParser.Parse(query, FileSystemProvider.FieldSet);
			var queryFragment = queryNode.ToSql(FileSystemProvider.FieldSet);
					
			var sb = new StringBuilder();
			sb.Append("SELECT * FROM directoryitems WHERE ");
			sb.Append(queryFragment);
			sb.AppendFormat(" LIMIT {0}", MAX_RESULTS.ToString());
						
			UseConnection(delegate (IDbConnection connection) {				
				command = connection.CreateCommand();
				command.CommandText = sb.ToString();
				
				ds = ExecuteDataSet(command);
				
				for (x = 0; x < ds.Tables[0].Rows.Count; x++) {
					if (ds.Tables[0].Rows[x]["type"].ToString() == "F") {
						files.Add(new SharedFileListing(LocalFile.FromDataRow(ds.Tables[0].Rows[x]), false));
					} else {
						LocalDirectory dir = LocalDirectory.FromDataRow(ds.Tables[0].Rows[x]);
						// FIXME: Ugly: Remove '/local' from begining of path
						string path = "/" + String.Join("/", dir.FullPath.Split('/').Slice(2));
						directories.Add(path);
					}
				}
			});
			
			result.Files = files.ToArray();
			result.Directories = directories.ToArray();

			return result;
		}

		public void InvalidateCache()
		{
			yourTotalBytes = -1;
			yourTotalFiles = -1;
		}

		private void CreateTables ()
		{
			using (IDbConnection connection = CreateDbConnection()) {
				using (IDbTransaction transaction = connection.BeginTransaction()) {
					
					// If any of these tables exist, drop them before trying to re-create.
					string[] tablesToDrop = new string[] { "properties", "directoryitems", "filepieces" };
					foreach (string tableName in tablesToDrop) {
						IDbCommand dropCommand = connection.CreateCommand();
						dropCommand.CommandText = String.Format("DROP TABLE IF EXISTS {0}", tableName);
						ExecuteNonQuery(dropCommand);
					}

					IDbCommand command = connection.CreateCommand();

					command.CommandText = @"
					CREATE TABLE properties (id    INTEGER PRIMARY KEY AUTOINCREMENT,
								 name  TEXT,
								 value TEXT);
					";
					ExecuteNonQuery(command);

					command.CommandText = "INSERT INTO properties (name, value) VALUES (\"version\", @version)";
					AddParameter (command, "@version", SCHEMA_VERSION);
					ExecuteNonQuery(command);

					command = connection.CreateCommand();
					command.CommandText = @"
						CREATE TABLE directoryitems (
							id           INTEGER PRIMARY KEY AUTOINCREMENT,
							type         TEXT(1),
							name         TEXT NOT NULL,
							parent_id    INTEGER,
							length       INTEGER,
							piece_length INTEGER,
							local_path   TEXT,
							info_hash    TEXT,
							sha1         TEXT,
							requested    BOOL,
							full_path    TEXT,
							UNIQUE (parent_id, name)
						);
					";
					ExecuteNonQuery(command);

					// XXX: SQLite triggers are not recursive, so
					// this leaves orphaned files and subdirectories.
					// http://www.sqlite.org/cvstrac/tktview?tn=1720
					command = connection.CreateCommand();
					command.CommandText = @"
					CREATE TRIGGER directoryitems_tr1 AFTER DELETE ON directoryitems BEGIN
						DELETE FROM directoryitems WHERE parent_id = old.id;
					END;
					";
					ExecuteNonQuery(command);

					command = connection.CreateCommand();
					command.CommandText = @"
					CREATE TABLE filepieces (id INTEGER PRIMARY KEY AUTOINCREMENT,
								 file_id    INTEGER,
								 piece_num  INTEGER,
								 hash       TEXT);
					";
					ExecuteNonQuery(command);

					command = connection.CreateCommand();
					command.CommandText = @"
					CREATE TRIGGER directoryitems_tr2 AFTER DELETE ON directoryitems BEGIN
						DELETE FROM filepieces WHERE file_id = old.id;
					END;
					";
					ExecuteNonQuery(command);

					command = connection.CreateCommand();
					command.CommandText = "CREATE INDEX directoryitems_parent_id ON directoryitems (parent_id);";
					ExecuteNonQuery(command);

					command = connection.CreateCommand();
					command.CommandText = "CREATE INDEX directoryitems_local_path ON directoryitems (local_path);";
					ExecuteNonQuery(command);
					
					command = connection.CreateCommand();
					command.CommandText = "CREATE INDEX directoryitems_type ON directoryitems (type);";
					ExecuteNonQuery(command);
					
					command = connection.CreateCommand();
					command.CommandText = "CREATE INDEX directoryitems_name ON directoryitems (name);";
					ExecuteNonQuery(command);
					
					command = connection.CreateCommand();
					command.CommandText = "CREATE INDEX directoryitems_full_path ON directoryitems (full_path);";
					ExecuteNonQuery(command);
					
					command = connection.CreateCommand();
					command.CommandText = "CREATE INDEX directoryitems_length ON directoryitems (length);";
					ExecuteNonQuery(command);
					
					command = connection.CreateCommand();
					command.CommandText = "CREATE INDEX filepieces_file_id ON filepieces (file_id);";
					ExecuteNonQuery(command);
					
					transaction.Commit();
				}
			}
		}

		public void AddParameter (IDbCommand command, string name, object value)
		{
			IDbDataParameter param = command.CreateParameter();
			param.ParameterName = name;
			param.Value = value;
			command.Parameters.Add(param);
		}
		
		public DataSet ExecuteDataSet (IDbCommand command)
		{
			LoggingService.LogDebug("ExecuteDataSet: {0}", GetCommandTextWithParameters(command));
			IDbDataAdapter adapter = new SqliteDataAdapter((SqliteCommand)command);
			DataSet ds = new DataSet();
			adapter.Fill(ds);
			return ds;
		}

		public object ExecuteScalar (string query)
		{
			object result = null;
			UseConnection(delegate (IDbConnection connection) {
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = query;
				LoggingService.LogDebug("ExecuteScalar: {0}", GetCommandTextWithParameters(cmd));
				result = cmd.ExecuteScalar();
			});
			return result;
		}
		
		public object ExecuteScalar (IDbCommand command)
		{
			LoggingService.LogDebug("ExecuteScalar: {0}", GetCommandTextWithParameters(command));
			return command.ExecuteScalar();
		}
		
		public int ExecuteNonQuery (IDbCommand command)
		{
			LoggingService.LogDebug("ExecuteNonQuery: {0}", GetCommandTextWithParameters(command));
			return command.ExecuteNonQuery();
		}

		internal void PurgeMissing ()
		{
			List<string> idsToDelete = new List<string>();
			
			UseConnection(delegate (IDbConnection connection) {
				IDbCommand command = connection.CreateCommand();
				command.CommandText = "SELECT id,local_path,type FROM directoryitems WHERE local_path IS NOT NULL";

				// XXX: This is a try-finally instead of a using because of a compiler bug.
				// Put this back once it's fixed in a release.
				IDataReader reader = null;
				try {
					reader = command.ExecuteReader();

					while (reader.Read()) {
						string id = reader.GetInt32(0).ToString();
						string path = reader.GetString(1);
						string type = reader.GetString(2);
						if (type == "D") {
							if (!String.IsNullOrEmpty(path) && !System.IO.Directory.Exists(path)) {
								idsToDelete.Add(id);
							}
						} else if (type == "F") {
							if (!System.IO.File.Exists(path)) {
								idsToDelete.Add(id);
							}
						}
					}
				} finally {
					if (reader != null) {
						reader.Dispose();
					}
				}

				if (idsToDelete.Count > 0) {
					command = connection.CreateCommand();
					command.CommandText = String.Format("DELETE FROM directoryitems WHERE id IN ({0})", String.Join(",", idsToDelete.ToArray()));
					ExecuteNonQuery(command);
				}
			});
		}
		
		// This is intended *for display only*! 
		string GetCommandTextWithParameters (IDbCommand command)
		{
			string text = command.CommandText;
			foreach (IDbDataParameter parameter in command.Parameters) {
				if (parameter.Value == null) {
					text = text.Replace(parameter.ParameterName, "NULL");
				} else if (parameter.Value is String) {
					text = text.Replace(parameter.ParameterName, "'" + parameter.Value.ToString() + "'");
				} else {
					text = text.Replace(parameter.ParameterName, parameter.Value.ToString());
				}
			}
			return text;
		}
	
		internal void ProcessRespondDirListingMessage (Network network, Node messageFrom, SharedDirectoryInfo info)
		{			
			string fullPath = PathUtil.Join(messageFrom.Directory.FullPath, info.FullPath);
			
			// FIXME: BARGH
			if (!fullPath.StartsWith("/")) fullPath = "/" + fullPath;		
			if (fullPath.EndsWith("/")) fullPath = fullPath.Substring(0, fullPath.Length - 1);
			
			RemoteDirectory remoteDirectory = (RemoteDirectory) GetDirectory(fullPath);
			remoteDirectory.UpdateFromInfo(info);
			
			lock (remoteDirectoryCallbacks) {
				if (remoteDirectoryCallbacks.ContainsKey(fullPath)) {
					foreach (var callback in remoteDirectoryCallbacks[fullPath]) {
						callback(remoteDirectory);
					}
					remoteDirectoryCallbacks.Remove(fullPath);
				}
			}
			
			network.RaiseReceivedDirListing(messageFrom, remoteDirectory);
		}
	}
}
