//
// ShareBuilder.cs: Index shared directories
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007 FileFind.net (http://filefind.net)
//

using System;
using System.Collections;
using System.IO;
using System.Threading;
using Meshwork.Backend.Core;
using Meshwork.Backend.Feature.FileBrowsing.Filesystem;
using ErrorEventHandler = System.IO.ErrorEventHandler;

namespace Meshwork.Backend.Feature.FileIndexing
{
	public delegate void ShareBuilderFileEventHandler (ShareBuilder builder, string filePath);
	
	public class ShareBuilder
	{
	    private readonly Core.Core core;
	    Thread thread;

		public event EventHandler StartedIndexing;
		public event EventHandler FinishedIndexing;
		public event EventHandler StoppedIndexing;
		public event ShareBuilderFileEventHandler IndexingFile;
		public event ErrorEventHandler ErrorIndexing;

		internal ShareBuilder (Core.Core core)
		{
		    this.core = core;
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

			LocalDirectory myDirectory = core.FileSystem.RootDirectory.MyDirectory;
			
			// Remove files/directories from db that no longer exist on the filesystem.
		    core.FileSystem.PurgeMissing();
			
			// If any dirs were removed from the list in settings, remove them from db.
			foreach (LocalDirectory dir in myDirectory.Directories) {
				if (!((IList) core.Settings.SharedDirectories).Contains(dir.LocalPath)) {
					dir.Delete();
				}
			}
			
			var lastScanAgo = (DateTime.Now - core.Settings.LastShareScan);
			if (Math.Abs(lastScanAgo.TotalHours) >= 1) {
				LoggingService.LogDebug("Starting directory scan. Last scan was {0} minutes ago.", Math.Abs(lastScanAgo.TotalMinutes));				
				foreach (var directoryName in core.Settings.SharedDirectories) {
					var info = new DirectoryInfo(directoryName);	
					if (Directory.Exists(directoryName)) {
						ProcessDirectory(myDirectory, info);
					} else {
						LoggingService.LogWarning("Directory does not exist: {0}.", info.FullName);
					}
				}
				
			    core.Settings.LastShareScan = DateTime.Now;
				
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

		private void ProcessDirectory (LocalDirectory parentDirectory, DirectoryInfo directoryInfo)
		{
			if (parentDirectory == null) {
				throw new ArgumentNullException("parentDirectory");
			}
			if (directoryInfo == null) {
				throw new ArgumentNullException("directoryInfo");
			}

			try {
				if (directoryInfo.Name.StartsWith(".") == false) {
					var directory = (LocalDirectory)parentDirectory.GetSubdirectory(directoryInfo.Name);

					if (directory == null) {
						directory = parentDirectory.CreateSubDirectory(core.FileSystem, directoryInfo.Name, directoryInfo.FullName);
					}

					foreach (var fileInfo in directoryInfo.GetFiles()) {
						if (fileInfo.Name.StartsWith(".") == false) {
							
							if (IndexingFile != null)
								IndexingFile(this, fileInfo.FullName);
							
							var file = (LocalFile)directory.GetFile(fileInfo.Name);
							if (file == null) {
								file = directory.CreateFile(fileInfo);
							}
						    if (string.IsNullOrEmpty(file.InfoHash)) {
							    core.ShareHasher.HashFile(file);
							}
						}
					}

					foreach (var subDirectoryInfo in directoryInfo.GetDirectories()) {
						ProcessDirectory(directory, subDirectoryInfo);
					}
				}
			} catch (ThreadAbortException) {
				// Canceled, ignore error.
			} catch (Exception ex) {
				LoggingService.LogError("Error while re-indexing shared files:", ex);
				if (ErrorIndexing != null) {
					ErrorIndexing(this, new ErrorEventArgs(ex));
				}
			}
		}
	}
}
