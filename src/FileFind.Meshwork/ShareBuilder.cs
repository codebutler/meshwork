//
// ShareBuilder.cs: Index shared directories
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007 FileFind.net (http://filefind.net)
//

using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using IO=System.IO;
using System.Threading;
using FileFind.Meshwork.Filesystem;
using System.Data;

namespace FileFind.Meshwork
{
	public delegate void ShareBuilderFileEventHandler (ShareBuilder builder, string filePath);
	
	public class ShareBuilder
	{
		Thread thread = null;

		public event EventHandler StartedIndexing;
		public event EventHandler FinishedIndexing;
		public event EventHandler StoppedIndexing;
		public event ShareBuilderFileEventHandler IndexingFile;
		public event ErrorEventHandler ErrorIndexing;

		internal ShareBuilder ()
		{
		}

		public bool Going {
			get {
				return thread != null;
			}
		}

		internal void Start ()
		{
			if (thread == null) {
				thread = new Thread (DoStart);
				thread.Start();
			} else {
				throw new InvalidOperationException("Already in progress.");
			}
		}

		private void DoStart ()
		{
			LoggingService.LogInfo("Started re-index of shared files...");
			
			if (StartedIndexing != null) {
				StartedIndexing (this, EventArgs.Empty);
			}

			LocalDirectory myDirectory = Core.FileSystem.RootDirectory.MyDirectory;
			
			// Remove files/directories from db that no longer exist on the filesystem.
			Core.FileSystem.PurgeMissing();
			
			// If any dirs were removed from the list in settings, remove them from db.
			foreach (LocalDirectory dir in myDirectory.Directories) {
				if (!Core.Settings.SharedDirectories.Contains(dir.LocalPath)) {
					dir.Delete();
				}
			}
			
			TimeSpan lastScanAgo = (DateTime.Now - Core.Settings.LastShareScan);
			if (Math.Abs(lastScanAgo.TotalHours) >= 1) {
				LoggingService.LogDebug("Starting directory scan. Last scan was {0} minutes ago.", Math.Abs(lastScanAgo.TotalMinutes));				
				foreach (string directoryName in Core.Settings.SharedDirectories) {
					IO.DirectoryInfo info = new IO.DirectoryInfo(directoryName);	
					if (IO.Directory.Exists(directoryName)) {
						ProcessDirectory(myDirectory, info);
					} else {
						LoggingService.LogWarning("Directory does not exist: {0}.", info.FullName);
					}
				}
				
				Core.Settings.LastShareScan = DateTime.Now;
				
			} else {
				LoggingService.LogDebug("Skipping directory scan because last scan was {0} minutes ago.", Math.Abs(lastScanAgo.TotalMinutes));
			}
			
			LoggingService.LogInfo("Finished re-index of shared files...");
			
			thread = null;
			
			if (FinishedIndexing != null) {
				FinishedIndexing (this, EventArgs.Empty);
			}
		}

		internal void Stop ()
		{
			if (thread != null) {
				thread.Abort();
				thread = null;
				
				LoggingService.LogInfo("Aborted re-index of shared files...");
				
				if (StoppedIndexing != null) {
					StoppedIndexing(this, EventArgs.Empty);
				}
			}
		}

		private void ProcessDirectory (LocalDirectory parentDirectory, IO.DirectoryInfo directoryInfo)
		{
			if (parentDirectory == null) {
				throw new ArgumentNullException("parentDirectory");
			}
			if (directoryInfo == null) {
				throw new ArgumentNullException("directoryInfo");
			}

			try {
				if (directoryInfo.Name.StartsWith(".") == false) {
					LocalDirectory directory = (LocalDirectory)parentDirectory.GetSubdirectory(directoryInfo.Name);

					if (directory == null) {
						directory = parentDirectory.CreateSubDirectory(directoryInfo.Name, directoryInfo.FullName);
					}

					foreach (IO.FileInfo fileInfo in directoryInfo.GetFiles()) {
						if (fileInfo.Name.StartsWith(".") == false) {
							
							if (IndexingFile != null)
								IndexingFile(this, fileInfo.FullName);
							
							LocalFile file = (LocalFile)directory.GetFile(fileInfo.Name);
							if (file == null) {
								file = directory.CreateFile(fileInfo);
							} else {								
								// XXX: Update file info
							}
							if (String.IsNullOrEmpty(file.InfoHash)) {
								Core.ShareHasher.HashFile(file);
							}
						}
					}

					foreach (IO.DirectoryInfo subDirectoryInfo in directoryInfo.GetDirectories()) {
						ProcessDirectory(directory, subDirectoryInfo);
					}
				}
			} catch (ThreadAbortException) {
				// Canceled, ignore error.
			} catch (Exception ex) {
				LoggingService.LogError("Error while re-indexing shared files:", ex);
				if (ErrorIndexing != null) {
					ErrorIndexing(this, ex);
				}
			}
		}
	}
}
