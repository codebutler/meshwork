//
// ShareWatcher.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

// XXX:
// CRAP. Directories can have more than one local path, becuase they get merged.
// We dont want to store this in the db at all, local_path should be nil for directories.
//


using System;
using System.Data;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using MFS=FileFind.Meshwork.Filesystem;

namespace FileFind.Meshwork
{
	public class ShareWatcher
	{
		Dictionary<string, FileSystemWatcher> watchers = new Dictionary<string, FileSystemWatcher>();
		
		bool running;
		AutoResetEvent mutex = new AutoResetEvent(false);
		Thread changedFilesThread;
		Dictionary<string, ChangedFileInfo> changedFiles = new Dictionary<string, ChangedFileInfo>();

		MFS.FileSystemProvider fs;

		public ShareWatcher (MFS.FileSystemProvider fs)
		{
			this.fs = fs;
			changedFilesThread = new Thread(ChangedFileWatcher);
		}

		public void Start ()
		{
			running = true;
			changedFilesThread.Start();
			foreach (string path in Core.Settings.SharedDirectories) {
				FileSystemWatcher watcher = new FileSystemWatcher(path);
				watcher.IncludeSubdirectories = true;
				watcher.Created += watcher_Changed;
				watcher.Changed += watcher_Changed;
				watcher.Deleted += watcher_Deleted;
				watchers.Add(path, watcher);
				watcher.EnableRaisingEvents = true;
			}
		}

		public void Stop ()
		{
			running = false;
			if (changedFilesThread.IsAlive) {
				changedFilesThread.Join();
			}
		}

		private void watcher_Changed (object sender, FileSystemEventArgs args)
		{
			try {
				if (System.IO.Directory.Exists(args.FullPath)) {
					HandleDirectoryChanged(args.FullPath);
				} else {
					lock (changedFiles) {
						if (!changedFiles.ContainsKey(args.FullPath)) {
							ChangedFileInfo info = new ChangedFileInfo();
							info.LastChangeSeen = DateTime.Now;
							info.FileSize = new FileInfo(args.FullPath).Length;
							changedFiles.Add(args.FullPath, info);
						} else {
							ChangedFileInfo info = changedFiles[args.FullPath];
							info.LastChangeSeen = DateTime.Now;
						}
					}
					mutex.Set();
				}
			} catch (Exception ex) {
				Console.Error.WriteLine(ex);
			}
		}	
		
		private void ChangedFileWatcher ()
		{
			try {
				while (running) {
					int count = 0;
					lock (changedFiles) {
						count = changedFiles.Count;
					}
					if (count == 0) {
						mutex.WaitOne();
					} else {
						Thread.Sleep(1000);
					}
					lock (changedFiles) {
						List<string> toRemove = new List<string>();
						foreach (KeyValuePair<string, ChangedFileInfo> pair in changedFiles) {
							if ((DateTime.Now - pair.Value.LastChangeSeen).TotalSeconds >= 5) {
								long size = new FileInfo(pair.Key).Length;
								if (size == pair.Value.FileSize) {
									HandleFileChanged(pair.Key);
									toRemove.Add(pair.Key);
								} else {
									pair.Value.FileSize = size;
								}
							}
						}
						foreach (string key in toRemove) {
							changedFiles.Remove(key);
						}
					}
				}
			} catch (Exception ex) {
				Console.Error.WriteLine(ex);
			}
		}

		object directoryChangeLock = new object();
		private void HandleDirectoryChanged (string path)
		{
			// Do these one at a time.
			lock (directoryChangeLock) {
				DirectoryInfo info = new DirectoryInfo(path);
				MFS.IDirectoryItem item = GetFromLocalPath(path);
				if (item == null && info != null) {
					// New Directory!

					MFS.Directory parentDirectory = GetParentDirectory(info);
					if (parentDirectory != null) {
						Console.WriteLine("NEW DIR !! " + path);
						parentDirectory.CreateSubdirectory(info.Name);
					} else {
						// No parent directory, this happens because
						// we can get events out of order.
						Console.WriteLine("NEW DIR NO PARENT !! " + path);
						CreateDirectoryForLocalPath(path);
					}
				}
			}
		}

		private void HandleFileChanged (string path)
		{
			FileInfo info = new FileInfo(path);
			MFS.IDirectoryItem item = GetFromLocalPath(path);

			if (item == null) {
				// New File!
				MFS.Directory parentDirectory = GetParentDirectory(info);

				Console.WriteLine("NEW FILE!! IN " + parentDirectory.FullPath);
			} else {
				// Updated File!
				Console.WriteLine("NOTE: Changed file detected, however handling this is not currently supported. Path: {0}", item.FullPath);
			}
		}

		private void watcher_Deleted (object sender, FileSystemEventArgs args)
		{
			try {
				MFS.IDirectoryItem item = GetFromLocalPath(args.FullPath);
				if (item != null) {
					item.Delete();
				}
			} catch (Exception ex) {
				Console.Error.WriteLine(ex);
			}
		}

		private class ChangedFileInfo
		{
			public long FileSize;
			public DateTime LastChangeSeen;
		}

		// XXX: Move this elsewhere.
		private MFS.IDirectoryItem GetFromLocalPath (string localPath)
		{
			Console.WriteLine("GET FROM LOCAL !!! " + localPath);
			return fs.UseConnection<MFS.IDirectoryItem>(delegate (IDbConnection connection) {
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = "SELECT * FROM directoryitems WHERE local_path = @local_path";
				fs.AddParameter(cmd, "@local_path", localPath);
				DataSet ds = fs.ExecuteDataSet(cmd);
				if (ds.Tables[0].Rows.Count > 0) {
					string type = ds.Tables[0].Rows[0]["type"].ToString();
					if (type == "F") {
						return (MFS.IDirectoryItem) new MFS.File(fs, ds.Tables[0].Rows[0]);
					} else {
						return (MFS.IDirectoryItem) MFS.Directory.FromDataRow(fs, ds.Tables[0].Rows[0]);
					}
				} else {
					return null;
				}
			});
		}

		// XXX: Move this too!
		private void CreateDirectoryForLocalPath (string localPath)
		{
			/*
			DirectoryInfo directoryInfo = new DirectoryInfo(localPath);
			Directory directory;

			while (directory == null) {
				if (Core.SharedDirectories.Contains(directoryInfo.FullName)) {
					// We've gone up too high, give up!
					throw new Exception("Eeep");
				}

				directory = (MFS.Directory) GetFromLocalPath(directoryInfo.FullName);
				if (directory != null) {
					
					// OK, We have a place to start. Create from here.

				} else {
					directoryInfo = directoryInfo.Parent;
				}
			}

			*/
		}

		// XXX: Move this too!
		private MFS.Directory GetParentDirectory (FileSystemInfo info)
		{
			DirectoryInfo directoryInfo = (info is FileInfo) ? ((FileInfo)info).Directory : (DirectoryInfo)info;

			Console.WriteLine("GET PARENT DIRECTORY " + directoryInfo.FullName);

			if (Core.Settings.SharedDirectories.Contains(directoryInfo.FullName)) {
				return Core.MyDirectory;
			} else {
				return (MFS.Directory)GetFromLocalPath(directoryInfo.Parent.FullName);
			}
		}
	}
}
