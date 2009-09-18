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

namespace FileFind.Meshwork
{
	public class ShareHasher
	{
		List<Thread>		threads = new List<Thread>();
		AutoResetEvent 		mutex = new AutoResetEvent(false);
		List<ShareHasherTask>	queue = new List<ShareHasherTask>();
		ShareHasherTaskComparer comparer = new ShareHasherTaskComparer();

		public event EventHandler QueueFinished;
		
		delegate void HashCaller (ShareHasherTask task);

		internal ShareHasher ()
		{

		}

		internal IAsyncResult BeginHashFile (LocalFile file, AsyncCallback callback, object state)
		{
			if (file.LocalPath == null || file.LocalPath == String.Empty) {
				throw new ArgumentException("Can only hash files with a local path", "file");
			}

			HashCaller caller = new HashCaller(Hash);
			return caller.BeginInvoke(new ShareHasherTask(file), callback, state);
		}

		internal void EndHashFile (IAsyncResult asyncResult)
		{
			((HashCaller)((AsyncResult)asyncResult).AsyncDelegate).EndInvoke(asyncResult);
		}

		internal void HashFilesEventually (List<LocalFile> files)
		{
			if (files.Count == 0) {
				return;
			}

			foreach (LocalFile file in files) {
				if (file.LocalPath == null || file.LocalPath == String.Empty) {
					throw new ArgumentException("Can only hash files with a local path", "file");
				}

				queue.Add(new ShareHasherTask(file));
			}

			queue.Sort(comparer); // <-- XXX: This gets called way too often!
			mutex.Set();
		}

		internal void ClearQueue ()
		{
			queue.Clear();
		}

		internal void Start ()
		{
			if (threads.Count == 0) {
				// Just create one for now.
				Thread thread = new Thread(DoHashing);
				thread.Start();
				threads.Add(thread);
			}
		}

		internal void Stop ()
		{
			while (threads.Count > 0) {
				Thread thread = threads[0];
				thread.Abort();
				threads.Remove(thread);
			}
		}

		private void DoHashing ()
		{
			try {
				while (true) {
					WaitUntilAvaliable ();
					
					ShareHasherTask task;

					lock (queue) {
						task = queue[0];
						queue.RemoveAt(0);
					}

					try {
						Hash(task);

						lock (queue) {
							if (queue.Count == 0) {
								if (QueueFinished != null) {
									QueueFinished(this, EventArgs.Empty);
								}
							}
						}

					} catch (Exception ex) {
						// XXX: Do something here!
						LoggingService.LogError("Problem while hashing file.", ex);
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

		private void Hash(ShareHasherTask task)
		{
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

			task.File.InfoHash = Common.BytesToString(torrent.InfoHash); 
			task.File.Pieces = pieces;
			task.File.PieceLength = torrent.PieceLength;
			task.File.SHA1 = Common.BytesToString(torrent.Files[0].SHA1);
			task.File.Save();
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

		private class ShareHasherTask
		{
			public LocalFile File;

			public ShareHasherTask(LocalFile file)
			{
				this.File = file;
			}
		}

		private class ShareHasherTaskComparer : Comparer<ShareHasherTask>
		{
			public override int Compare (ShareHasherTask first, ShareHasherTask second)
			{
				return first.File.Size.CompareTo(second.File.Size);
			}
		}
	}

	internal class HasherAsyncResult : IAsyncResult
	{
		object state;

		public HasherAsyncResult(object state)
		{
			this.state = state;
		}

		public object AsyncState {
			get {
				return state;
			}
		}

		public WaitHandle AsyncWaitHandle {
			get {
				throw new NotImplementedException();
			}
		}

		public bool CompletedSynchronously {
			get {
				throw new NotImplementedException();
			}
		}

		public bool IsCompleted {
			get {
				throw new NotImplementedException();
			}
		}
	}
}
