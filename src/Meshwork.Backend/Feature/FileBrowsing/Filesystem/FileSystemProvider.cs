//
// FileSystemProvider.cs: The root of the virtual filesystem
// 
// Author:
//   Eric Butler <eric@filefind.net>
//
//   (C) 2005-2006 FileFind.net (http://filefind.net/)
//

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading;
using Meshwork.Backend.Core;
using Meshwork.Backend.Core.Protocol;
using Meshwork.Backend.Feature.FileTransfer;
using Meshwork.Common;
using Meshwork.Library.Hyena.Data.Sqlite;
using Meshwork.Library.Hyena.Query;
using Mono.Data.Sqlite;
using ConnectionState = System.Data.ConnectionState;

namespace Meshwork.Backend.Feature.FileBrowsing.Filesystem
{
	public delegate T DbMethod<T> (IDbConnection connection);
	public delegate void DbMethod (IDbConnection connection);

	public delegate void DirectoryCallback (IDirectory directory);
	public delegate void FileCallback (IFile file);


	public class FileSystemProvider
	{
		const string SCHEMA_VERSION = "12";

	    private readonly Core.Core core;

	    private readonly RootDirectory rootDirectory;
		string connectionString;
		long yourTotalBytes = -1;
		long yourTotalFiles = -1;

		List<IDbConnection> connections = new List<IDbConnection>();
		List<IDbConnection> workingConnections = new List<IDbConnection>();
		
		Dictionary<string, List<DirectoryCallback>> remoteDirectoryCallbacks = new Dictionary<string, List<DirectoryCallback>>();
		Dictionary<string, List<FileCallback>> remoteFileCallbacks = new Dictionary<string, List<FileCallback>>();
		
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
		
		
		public FileSystemProvider (Core.Core core)
		{
		    this.core = core;
            this.rootDirectory = new RootDirectory(core);

		    var path = Path.Combine(core.Settings.DataPath, "shares.db");

			var create = !File.Exists(path);

			connectionString = $"URI=file:{path},version=3;busy_timeout=300000";

			if (!create) {
				try {
					// Do some sanity checking here, if anything looks bad, start over
					if (RootDirectory == null) {
						LoggingService.LogWarning("Unable to find root dir");
						create = true;
					} else {
						// Verify version
						var currentVersion = ExecuteScalar("SELECT value FROM properties WHERE name='version'").ToString();
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
				core.Settings.LastShareScan = DateTime.MinValue;
			}
		}

		public bool BeginGetDirectory (string path, DirectoryCallback callback)
		{
			path = PathUtil.CleanPath(path);

			// LocalDirectory and NetworkDirectory objects can always be returned immediately.
			var parts = path.Split('/');
			if ((parts.Length > 1 && parts[1] == "local") || parts.Length < 3) {
				var directory = GetDirectory(path);
				callback(directory);
				return true;
			} else {
				var directory = (RemoteDirectory) GetDirectory(path);
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

				var network = PathUtil.GetNetwork(core, path);
				network.RequestDirectoryListing(path);
				return false;
			}
		}

		public bool BeginGetFileDetails (string path, FileCallback callback)
		{
			path = PathUtil.CleanPath(path);

			var file = GetFile(path);
			if (file != null) {
				callback(file);
				return true;
			}

			// If file is local and wasn't found, throw error.
			var parts = path.Split('/');
			if (parts.Length > 1 && parts[1] == "local") {
				throw new Exception("File does not exist");
			}

			// If remote file, request it!

			lock (remoteFileCallbacks) {
				if (!remoteFileCallbacks.ContainsKey(path))
					remoteFileCallbacks.Add(path, new List<FileCallback>());
				var list = remoteFileCallbacks[path];
				list.Add(callback);
			}

			var network = PathUtil.GetNetwork(core, path);
			network.RequestFileDetails(path);
			return false;
		}

		internal LocalDirectory GetLocalDirectory (string path)
		{
			if (path.Length > 1 && path.EndsWith("/"))
				path = path.Substring(0, path.Length - 1);

			var parts = path.Split('/');
			if (parts.Length < 3) {
				return (LocalDirectory)GetDirectory(path);
			}
		    return core.FileSystem.UseConnection(delegate (IDbConnection connection) {
		        var query = "SELECT * FROM directoryitems WHERE type = 'D' AND full_path = @full_path LIMIT 1";
		        var command = connection.CreateCommand();
		        command.CommandText = query;
		        core.FileSystem.AddParameter(command, "@full_path", path);
		        var data = core.FileSystem.ExecuteDataSet(command);
		        if (data.Tables[0].Rows.Count > 0)
		            return LocalDirectory.FromDataRow(this, data.Tables[0].Rows[0]);
		        return null;
		    });
		}

		// FIXME: Eventually, this method should become private.
		internal IDirectory GetDirectory (string path)
		{
			if (!path.StartsWith("/")) path = "/" + path;
			if (path.EndsWith("/")) path = path.Substring(0, path.Length - 1);
			IDirectory directory = core.FileSystem.RootDirectory;
			if (path.Length > 0) {
				var pathParts = path.Substring(1).Split('/');
				foreach (var dirName in pathParts)
				{
					directory = directory.GetSubdirectory(dirName);
					if (directory == null)
						return null;
				}
			}
			return directory;
		}

		RemoteDirectory GetOrCreateRemoteDirectory (string path)
		{
			IDirectory directory = GetDirectory(path) as RemoteDirectory;
			if (directory != null)
				return (RemoteDirectory) directory;

			directory = core.FileSystem.RootDirectory;
			var pathParts = path.Substring(1).Split('/');
			foreach (var dirName in pathParts) {
				var curDir = directory;
				directory = directory.GetSubdirectory(dirName);
				if (directory == null) {
					directory = ((RemoteDirectory)curDir).CreateSubdirectory(dirName);
				}
			}
			return (RemoteDirectory) directory;
		}

		RemoteFile GetOrCreateRemoteFile (string path, SharedFileListing listing, out bool created)
		{
			created = false;

			IFile file = GetFile(path) as RemoteFile;
			if (file != null)
				return (RemoteFile) file;

			created = true;

			var dirPath = string.Join("/", path.Split('/').Slice(0, -2));
			var directory = GetOrCreateRemoteDirectory(dirPath);
			file = directory.CreateFile(listing);

			return (RemoteFile) file;
		}

		public IFile GetFile (string path)
		{
			var directoryPath = string.Join("/", path.Split('/').Slice(0, -2));
			var fileName = path.Split('/').Slice(-1, -1)[0];
			var directory = GetDirectory(directoryPath);
			if (directory != null) {
				return directory.GetFile(fileName);
			}
		    return null;
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
				var start = DateTime.Now;
				while (workingConnections.Count > 0) {
					Thread.Sleep(1);
					if ((DateTime.Now - start).TotalSeconds >= 1) {
						// After a second, give up and go anyway.
						break;
					}
				}
			}

			lock (connections) {
				theConnection = connections.Find(delegate (IDbConnection c) { return c.State == ConnectionState.Open; });
				connections.Remove(theConnection);
			}

			if (theConnection == null) {
				theConnection = CreateDbConnection();
			}

			workingConnections.Add(theConnection);

			var result = method(theConnection);

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
			var connection = new SqliteConnection(connectionString);
			connection.Open ();
			return connection;
		}

		public RootDirectory RootDirectory {
			get { return rootDirectory; }
		}

		public long TotalDirectories {
			get {
				return UseConnection(delegate (IDbConnection connection) {
					var query = "SELECT count(*) FROM directoryitems WHERE type='D'";
					var command = connection.CreateCommand();
					command.CommandText = query;
					var result = ExecuteScalar(command);
					return (result == null) ? 0 : (long)result;
				});
			}
		}
		public long TotalFiles {
			get {
				return UseConnection(delegate (IDbConnection connection) {
					var query = "SELECT count(*) FROM directoryitems WHERE type='F'";
					var command = connection.CreateCommand();
					command.CommandText = query;
					var result = ExecuteScalar(command);
					return (result == null) ? 0 : (long)result;
				});
			}
		}

		public long TotalBytes {
			get {
				return UseConnection(delegate (IDbConnection connection) {
					var query = "SELECT sum(length) FROM directoryitems WHERE type='F'";
					var command = connection.CreateCommand();
					command.CommandText = query;
					var result = ExecuteScalar(command);
					return (result == null) ? 0 : (long)result;
				});
			}
		}

		public long YourTotalFiles {
			get
			{
			    if (yourTotalFiles != -1) {
					return yourTotalFiles;
				}
			    return UseConnection(delegate (IDbConnection connection) {
			        var query = "SELECT count(*) FROM directoryitems WHERE type='F'";
			        var command = connection.CreateCommand();
			        command.CommandText = query;
			        var result = ExecuteScalar(command);
			        yourTotalFiles = (result is DBNull) ? 0 : (long)result;
			        return yourTotalFiles;
			    });
			}
		}

		public long YourTotalBytes {
			get
			{
			    if (yourTotalBytes != -1) {
					return yourTotalBytes;
				}
			    return UseConnection(delegate (IDbConnection connection) {
			        var query = "SELECT sum(length) FROM directoryitems WHERE type='F'";
			        var command = connection.CreateCommand();
			        command.CommandText = query;
			        var result = ExecuteScalar(command);
			        yourTotalBytes = (result is DBNull) ? 0 : (long)result;
			        return yourTotalBytes;
			    });
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

			var queryNode = UserQueryParser.Parse(query, FieldSet);
			var queryFragment = queryNode.ToSql(FieldSet);

			var sb = new StringBuilder();
			sb.Append("SELECT * FROM directoryitems WHERE ");
			sb.Append(queryFragment);
			sb.AppendFormat(" LIMIT {0}", MAX_RESULTS);

			UseConnection(delegate (IDbConnection connection) {
				command = connection.CreateCommand();
				command.CommandText = sb.ToString();

				ds = ExecuteDataSet(command);

				for (x = 0; x < ds.Tables[0].Rows.Count; x++) {
					if (ds.Tables[0].Rows[x]["type"].ToString() == "F") {
						files.Add(new SharedFileListing(LocalFile.FromDataRow(core.FileSystem, ds.Tables[0].Rows[x]), false));
					} else {
						var dir = LocalDirectory.FromDataRow(this, ds.Tables[0].Rows[x]);
						// FIXME: Ugly: Remove '/local' from begining of path
						var path = "/" + string.Join("/", dir.FullPath.Split('/').Slice(2));
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
			using (var connection = CreateDbConnection()) {
				using (var transaction = connection.BeginTransaction()) {

					// If any of these tables exist, drop them before trying to re-create.
					string[] tablesToDrop = { "properties", "directoryitems", "filepieces" };
					foreach (var tableName in tablesToDrop) {
						var dropCommand = connection.CreateCommand();
						dropCommand.CommandText = string.Format("DROP TABLE IF EXISTS {0}", tableName);
						ExecuteNonQuery(dropCommand);
					}

					var command = connection.CreateCommand();

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
			var param = command.CreateParameter();
			param.ParameterName = name;
			param.Value = value;
			command.Parameters.Add(param);
		}

		public DataSet ExecuteDataSet (IDbCommand command)
		{
			LoggingService.LogDebug("ExecuteDataSet: {0}", GetCommandTextWithParameters(command));
			IDbDataAdapter adapter = new SqliteDataAdapter((SqliteCommand)command);
			var ds = new DataSet();
			adapter.Fill(ds);
			return ds;
		}

		public object ExecuteScalar (string query)
		{
			object result = null;
			UseConnection(delegate (IDbConnection connection) {
				var cmd = connection.CreateCommand();
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
			var idsToDelete = new List<string>();

			UseConnection(delegate (IDbConnection connection) {
				var command = connection.CreateCommand();
				command.CommandText = "SELECT id,local_path,type FROM directoryitems WHERE local_path IS NOT NULL";

				// XXX: This is a try-finally instead of a using because of a compiler bug.
				// Put this back once it's fixed in a release.
				IDataReader reader = null;
				try {
					reader = command.ExecuteReader();

					while (reader.Read()) {
						var id = reader.GetInt32(0).ToString();
						var path = reader.GetString(1);
						var type = reader.GetString(2);
						if (type == "D") {
							if (!string.IsNullOrEmpty(path) && !Directory.Exists(path)) {
								idsToDelete.Add(id);
							}
						} else if (type == "F") {
							if (!File.Exists(path)) {
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
					command.CommandText = string.Format("DELETE FROM directoryitems WHERE id IN ({0})", string.Join(",", idsToDelete.ToArray()));
					ExecuteNonQuery(command);
				}
			});
		}

		// This is intended *for display only*!
		string GetCommandTextWithParameters (IDbCommand command)
		{
			var text = command.CommandText;
			foreach (IDbDataParameter parameter in command.Parameters) {
				if (parameter.Value == null) {
					text = text.Replace(parameter.ParameterName, "NULL");
				} else if (parameter.Value is string) {
					text = text.Replace(parameter.ParameterName, "'" + parameter.Value + "'");
				} else {
					text = text.Replace(parameter.ParameterName, parameter.Value.ToString());
				}
			}
			return text;
		}

		internal void ProcessRespondDirListingMessage (Network network, Node messageFrom, SharedDirectoryInfo info)
		{
			var fullPath = PathUtil.Join(messageFrom.Directory.FullPath, info.FullPath);

			var node = PathUtil.GetNode(core, fullPath);
			if (node != messageFrom)
				throw new Exception("Directory was for a different node");

			var remoteDirectory = GetOrCreateRemoteDirectory(fullPath);
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

		internal void ProcessFileDetailsMessage (Network network, Node messageFrom, SharedFileListing info)
		{
			var fullPath = PathUtil.Join(messageFrom.Directory.FullPath, info.FullPath);

			var node = PathUtil.GetNode(core, fullPath);
			if (node != messageFrom)
				throw new Exception("Directory was for a different node");

			var created = false;
			var remoteFile = GetOrCreateRemoteFile(fullPath, info, out created);
			if (!created)
				remoteFile.UpdateFromInfo(info);

			lock (remoteFileCallbacks) {
				if (remoteFileCallbacks.ContainsKey(fullPath)) {
					foreach (var callback in remoteFileCallbacks[fullPath]) {
						callback(remoteFile);
					}
				}
				remoteFileCallbacks.Remove(fullPath);
			}

			network.RaiseReceivedFileDetails(remoteFile);

			// FIXME: Get rid of all this, just listen for above network.ReceivedFileDetails event!
			var transfer = core.FileTransferManager.GetTransfer(remoteFile);
			if (transfer != null && transfer.Status == FileTransferStatus.WaitingForInfo) {
				((IFileTransferInternal)transfer).DetailsReceived();
			}
		}
	}
}
