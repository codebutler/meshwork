//
// ShareHasher.cs: Hashes files in the user's share
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007 FileFind.net (http://filefind.net)
//

using System;
using System.Collections.Generic;
using System.Threading;
using MonoTorrent.Common;
using FileFind.Meshwork.Filesystem;
using System.Runtime.Remoting.Messaging;
using System.Text;

/* TODO
 * 
 * Replace Started/Finished/HashingFile events with just "Changed"
 * Check to see if file is already in queue.
 * Don't cache LocalFile object in ShareHasherTask. Store path instead and check that file still exists in share before hashing. If not, ignore.
 * 
 */

namespace FileFind.Meshwork
{
	public delegate void ShareHasherTaskEventHandler (ShareHasherTask task);
	
	public class ShareHasher
	{
		// Keeps track of worker threads and what their current task is, if any.
		Dictionary<Thread, ShareHasherTask> threads = new Dictionary<Thread, ShareHasherTask>();
		
		AutoResetEvent mutex = new AutoResetEvent(false);
		List<ShareHasherTask> queue = new List<ShareHasherTask>();
		ShareHasherTaskComparer comparer = new ShareHasherTaskComparer();
		
		int threadCount;
		
		public event EventHandler QueueChanged;
		public event ShareHasherTaskEventHandler StartedHashingFile;
		public event ShareHasherTaskEventHandler FinishedHashingFile;

		internal ShareHasher ()
		{
			threadCount = System.Environment.ProcessorCount;
		}
		
		internal void HashFile (LocalFile file)
		{
			HashFile(file, null);
		}

		internal void HashFile (LocalFile file, AsyncCallback callback)
		{
			if (file.LocalPath == null)
				throw new ArgumentNullException("file");
			
			if (!System.IO.File.Exists(file.LocalPath))
				throw new ArgumentException("File does not exist");
			
			lock (queue) {
				// Check to see if this file is already queued.
				foreach (ShareHasherTask existingTask in queue) {
					if (existingTask.File.LocalPath == file.LocalPath)
						return;
				}				
				// If not, add it!
				var task = new ShareHasherTask(file, callback);
				queue.Add(task);
				queue.Sort(comparer);
				
				if (QueueChanged != null)
					QueueChanged(this, EventArgs.Empty);
			}
			mutex.Set();
		}

		internal void Start ()
		{
			lock (threads) {
				while (threads.Count < threadCount) {
					Thread thread = new Thread(DoHashing);
					thread.Start();
					threads.Add(thread, null);
				}
			}
		}

		internal void Stop ()
		{
			lock (threads) {
				foreach (Thread thread in threads.Keys) {
					thread.Abort();
				}
				threads.Clear();
			}
		}

		private void DoHashing ()
		{
			try {
				while (true) {
					WaitUntilAvaliable ();
					
					ShareHasherTask task;

					lock (queue) {
						if (queue.Count == 0)
							continue;
						
						task = queue[0];
						queue.RemoveAt(0);
						
						if (QueueChanged != null)
							QueueChanged(this, EventArgs.Empty);
					}
					
					lock (threads) {
						threads[Thread.CurrentThread] = task;
					}

					try {
						Hash(task);
					} catch (Exception ex) {
						// XXX: Do something here!
						LoggingService.LogError("Problem while hashing file.", ex);
					}
					
					lock (threads) {
						threads[Thread.CurrentThread] = null;
					}
				}
			} catch (ThreadAbortException) {
				// Someone called Stop(), that's OK.

			} catch (Exception ex) {
				// XXX: Do something here, we've aborted
				// everything!
				LoggingService.LogError("AAHHHH!!!", ex);
				throw ex;
			}
		}
	
		public int FilesRemaining {
			get {
				return queue.Count;
			}
		}

		public bool Going {
			get {
				return (queue.Count > 0 && threads.Count > 0);
			}
		}
		
		public int CurrentFileCount {
			get {
				int r = 0;
				lock (threads) {
					foreach (Thread thread in threads.Keys) {
						ShareHasherTask task = threads[thread];
						if (task != null)
							r++;
					}
				}
				return r;
			}
		}
		
		public string CurrentFiles {
			get {
				var builder = new StringBuilder();
				lock (threads) {
					foreach (Thread thread in threads.Keys) {
						ShareHasherTask task = threads[thread];
						if (task != null) {
							builder.AppendLine(task.File.LocalPath);
						}
					}
				}
				return builder.ToString();
			}
		}				

		private void Hash(ShareHasherTask task)
		{
			if (StartedHashingFile != null)
				StartedHashingFile(task);
			
			/* Create the torrent */
			TorrentCreator creator = new TorrentCreator();
			// Have to put something bogus here, otherwise MonoTorrent crashes!
			creator.Announces.Add(new MonoTorrentCollection<string>());
			creator.Announces[0].Add(String.Empty);

			creator.Path = task.File.LocalPath;
			Torrent torrent = Torrent.Load(creator.Create());

			/* Update the database */
			string[] pieces = new string[torrent.Pieces.Count];
			for (int x = 0; x < torrent.Pieces.Count; x++) {
				byte[] hash = torrent.Pieces.ReadHash(x);
				pieces[x] = Common.BytesToString(hash);
			}

			task.File.Update(Common.BytesToString(torrent.InfoHash),
			                 Common.BytesToString(torrent.Files[0].SHA1), 
			                 torrent.PieceLength, pieces);
					
			if (FinishedHashingFile != null)
				FinishedHashingFile(task);
			
			if (task.Callback != null)
				task.Callback(null);
		}

		private void WaitUntilAvaliable ()
		{
			bool shouldWait = false;
			lock (queue) {
				if (queue.Count == 0) {
					shouldWait = true;
				}
			}
			if (shouldWait) {
				mutex.WaitOne ();
			}
		}

		private class ShareHasherTaskComparer : Comparer<ShareHasherTask>
		{
			public override int Compare (ShareHasherTask first, ShareHasherTask second)
			{
				// FIXME: Anything that has a callback should be sorted first!
				return first.File.Size.CompareTo(second.File.Size);
			}
		}
	}
	
	
	public class ShareHasherTask
	{
		LocalFile m_File;
		AsyncCallback m_Callback;
		
		public ShareHasherTask(LocalFile file)
		{
			m_File = file;
		}
		
		public ShareHasherTask(LocalFile file, AsyncCallback callback)
		{
			m_File = file;
			m_Callback = callback;
		}
		
		public LocalFile File {
			get {
				return m_File;
			}
		}

		public AsyncCallback Callback {
			get {
				return m_Callback;
			}
		}
	}
}
