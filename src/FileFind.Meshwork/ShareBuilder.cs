//
// ShareBuilder.cs: Index shared directories
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007 FileFind.net (http://filefind.net)
//

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using IO=System.IO;
using System.Threading;
using FileFind.Meshwork.Filesystem;
using System.Data;

namespace FileFind.Meshwork
{
	public class ShareBuilder
	{
		Thread thread = null;

		public event EventHandler StartedIndexing;
		public event EventHandler FinishedIndexing;
		public event EventHandler StoppedIndexing;
		public event ErrorEventHandler ErrorIndexing;

		List<LocalFile> filesToHash = new List<LocalFile>();

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

			// XXX: Figure out what top-level directories
			// are in the database, that are no longer in
			// Core.Settings.SharedDirectories, and remove them.
			
			// Remove files/directories that no longer exist
			Core.FileSystem.PurgeMissing();
			
			foreach (string directoryName in Core.Settings.SharedDirectories) {
				IO.DirectoryInfo info = new IO.DirectoryInfo(directoryName);

				if (IO.Directory.Exists(directoryName)) {
					ProcessDirectory(myDirectory, info);
				} else {
					// XXX: Inform the user somehow
					// that this is missing. Don't
					// just remove it though, it
					// could be a non-mounted
					// removable drive.
				}
			}
			
			LoggingService.LogInfo("We need to hash " + filesToHash.Count + " files.");

			// XXX: This is here because right now HashFilesEventually gets
			// called for the same files over and over if ShareBuilder gets
			// restarted. Replace with some better intelligence.
			Core.ShareHasher.ClearQueue();

			Core.ShareHasher.HashFilesEventually(filesToHash);
			filesToHash.Clear();
			
			LoggingService.LogInfo("Finished re-index of shared files...");
			
			if (FinishedIndexing != null) {
				FinishedIndexing (this, EventArgs.Empty);
			}
			thread = null;
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
							LocalFile file = null;
							if (!directory.HasFile(fileInfo.Name)) {
								file = directory.CreateFile(fileInfo);
							} else {
								file = (LocalFile)directory.GetFile(fileInfo.Name);
								// XXX: Update file info
							}
							if (file.InfoHash == null || file.InfoHash == String.Empty) {
								filesToHash.Add(file);
							}
						}
					}

					foreach (IO.DirectoryInfo subDirectoryInfo in directoryInfo.GetDirectories()) {
						ProcessDirectory(directory, subDirectoryInfo);
					}

					// XXX: Now enumerate through every dir/file that's
					// already in our share, and check that it still exists
					// on the filesystem.
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
