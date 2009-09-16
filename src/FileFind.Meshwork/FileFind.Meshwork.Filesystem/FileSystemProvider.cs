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
using FileFind;
using FileFind.Meshwork;
using FileFind.Meshwork.Collections;
using FileFind.Meshwork.Exceptions;
using FileFind.Meshwork.Protocol;
using Mono.Data.SqliteClient;
using Mono.Data;
using System.Data;

namespace FileFind.Meshwork.Filesystem
{
	public delegate T DbMethod<T> (IDbConnection connection);
	public delegate void DbMethod (IDbConnection connection);

	public class FileSystemProvider
	{
		const string SCHEMA_VERSION = "9";

		string connectionString;
		long yourTotalBytes = -1;
		long yourTotalFiles = -1;

		List<IDbConnection> connections = new List<IDbConnection>();
		List<IDbConnection> workingConnections = new List<IDbConnection>();

		public FileSystemProvider ()
		{
			string path = Path.Combine(Core.Settings.DataPath, "shares.db");

			bool create = false;

			if (!System.IO.File.Exists(path)) {
				create = true;
			}

			connectionString = String.Format("URI=file:{0},version=3", path);

			if (!create) {
				try {
					// Do some sanity checking here, if anything looks bad, start over
					if (RootDirectory == null) {
						Console.Error.WriteLine("Unable to find root dir");
						create = true;
					} else {
						// Verify version
						string currentVersion = ExecuteScalar("SELECT value FROM properties WHERE name='version'").ToString();
						if (currentVersion != SCHEMA_VERSION) {
							Console.Error.WriteLine("Schema has changed, recreating db.");
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
				
				RootDirectory.MyDirectory.InvalidateCache();

				// Force a scan.
				Core.Settings.LastShareScan = DateTime.MinValue;
			}
		}

		public IDirectory GetDirectory (string path)
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
			// FIXME: !!!
			throw new NotImplementedException();
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
			connection.BusyTimeout = 300000; // SQLite is stupid
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
					object result = command.ExecuteScalar();
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
					object result = command.ExecuteScalar();
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
					object result = command.ExecuteScalar();
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
						object result = command.ExecuteScalar();
						yourTotalFiles = (result == null) ? 0 : (long)result;
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
						object result = command.ExecuteScalar();
						yourTotalBytes = (result == null) ? 0 : (long)result;
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
			Dictionary<int, SharedDirListing> directories;
			Dictionary<int, List<SharedFileListing>> directoryFiles;

			result = new SearchResultInfo();

			// XXX: Anything else?
			query = query.Replace(@"%", @"\%");
			query = query.Replace(@"\", @"\\");

			UseConnection(delegate (IDbConnection connection) {
				// First, find all matching directories.
				sql = @"SELECT * FROM directoryitems WHERE type = 'D' AND name LIKE @name ESCAPE '\'";
				command = connection.CreateCommand();
				command.CommandText = sql;
				AddParameter(command, "@name", "%" + query + "%");
				ds = ExecuteDataSet(command);
				
				List<string> directoryIds = new List<string>();
				
				directories = new Dictionary<int, SharedDirListing>();
				for (x = 0; x < ds.Tables[0].Rows.Count; x++) {
					DataRow row = ds.Tables[0].Rows[x];
					LocalDirectory localDir = LocalDirectory.FromDataRow(row);
					directories.Add(localDir.Id, new SharedDirListing(localDir));
					directoryIds.Add(localDir.Id.ToString());
				}

				// Next, find all files within matching directories.
				string directoryIdsStr = String.Join(",", directoryIds.ToArray());
				sql = @"SELECT * FROM directoryitems WHERE type = 'F' AND info_hash IS NOT NULL AND parent_id IN (" + directoryIdsStr + ")";
				command = connection.CreateCommand();
				command.CommandText = sql;
				//AddParameter(command, "@ids", directoryIds.ToString());

				ds = ExecuteDataSet(command);
				
				directoryFiles = new Dictionary<int, List<SharedFileListing>>();
				for (x = 0; x < ds.Tables[0].Rows.Count; x++) {
					LocalFile file = LocalFile.FromDataRow(ds.Tables[0].Rows[x]);
					if (!directoryFiles.ContainsKey(file.ParentId)) {
						directoryFiles[file.ParentId] = new List<SharedFileListing>();
					}
					directoryFiles[file.ParentId].Add(new SharedFileListing(file));
				}

				foreach (int id in directoryFiles.Keys) {
					directories[id].Files = directoryFiles[id].ToArray();
				}

				// Remove directories that have no files
				// XXX: Dont use two extra loops for this!
				List<int> toRemove = new List<int>();
				foreach (KeyValuePair<int,SharedDirListing> pair in directories) {
					SharedDirListing dir = pair.Value;
					if (dir.Files == null || dir.Files.Length == 0) {
						toRemove.Add(pair.Key);
					}
				}
				foreach (int id in toRemove) {
					directories.Remove(id);
				}

				result.Directories = new SharedDirListing[directories.Count];
				x = 0;
				foreach (SharedDirListing dir in directories.Values) {
					result.Directories[x] = dir;
					x ++;
				}

				// Now find all other files.
				// XXX: Why doesn't ESCAPE work?!
				sql = @"SELECT * FROM directoryitems WHERE type = 'F' AND info_hash IS NOT NULL AND name LIKE @name AND parent_id NOT IN (" + directoryIds.ToString() + ")";// ESCAPE '\'";
				command = connection.CreateCommand();
				command.CommandText = sql;
				//AddParameter(command, "@ids", directoryIds.ToString());
				AddParameter(command, "@name", "%" + query + "%");

				ds = ExecuteDataSet(command);
				
				result.Files = new SharedFileListing[ds.Tables[0].Rows.Count];
				for (x = 0; x < ds.Tables[0].Rows.Count; x++) {
					result.Files[x] = new SharedFileListing(LocalFile.FromDataRow(ds.Tables[0].Rows[x]));
				}
			});

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
						dropCommand.ExecuteNonQuery();
					}

					IDbCommand command = connection.CreateCommand();

					command.CommandText = @"
					CREATE TABLE properties (id    INTEGER PRIMARY KEY AUTOINCREMENT,
								 name  TEXT,
								 value TEXT);
					";
					command.ExecuteNonQuery();

					command.CommandText = "INSERT INTO properties (name, value) VALUES (\"version\", @version)";
					AddParameter (command, "@version", SCHEMA_VERSION);
					command.ExecuteNonQuery();

					command = connection.CreateCommand();
					command.CommandText = @"
					CREATE TABLE directoryitems (id           INTEGER PRIMARY KEY AUTOINCREMENT,
								     type         TEXT(1),
								     name         TEXT NOT NULL,
								     parent_id    INTEGER,
								     length       INTEGER,
								     piece_length INTEGER,
								     local_path   TEXT,
								     info_hash    TEXT,
								     sha1         TEXT,
								     requested    BOOL,
								     UNIQUE (parent_id, name)
					);
					";
					command.ExecuteNonQuery();

					// XXX: SQLite triggers are not recursive, so
					// this leaves orphaned files and subdirectories.
					// http://www.sqlite.org/cvstrac/tktview?tn=1720
					command = connection.CreateCommand();
					command.CommandText = @"
					CREATE TRIGGER directoryitems_tr1 AFTER DELETE ON directoryitems BEGIN
						DELETE FROM directoryitems WHERE parent_id = old.id;
					END;
					";
					command.ExecuteNonQuery();

					command = connection.CreateCommand();
					command.CommandText = @"
					CREATE TABLE filepieces (id INTEGER PRIMARY KEY AUTOINCREMENT,
								 file_id    INTEGER,
								 piece_num  INTEGER,
								 hash       TEXT);
					";
					command.ExecuteNonQuery();

					command = connection.CreateCommand();
					command.CommandText = @"
					CREATE TRIGGER directoryitems_tr2 AFTER DELETE ON directoryitems BEGIN
						DELETE FROM filepieces WHERE file_id = old.id;
					END;
					";
					command.ExecuteNonQuery();

					command = connection.CreateCommand();
					command.CommandText = "CREATE INDEX directoryitems_parent_id ON directoryitems (parent_id);";
					command.ExecuteNonQuery();

					command = connection.CreateCommand();
					command.CommandText = "CREATE INDEX directoryitems_local_path ON directoryitems (local_path);";
					command.ExecuteNonQuery();

					command = connection.CreateCommand();
					command.CommandText = "CREATE INDEX filepieces_file_id ON filepieces (file_id);";
					command.ExecuteNonQuery();

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
				result = cmd.ExecuteScalar();
			});
			return result;
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
						string id = reader.GetString(0);
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

				command = connection.CreateCommand();
				command.CommandText = String.Format("DELETE FROM directoryitems WHERE id IN ({0})", String.Join(",", idsToDelete.ToArray()));
				command.ExecuteNonQuery();
			});
		}
	}
}
