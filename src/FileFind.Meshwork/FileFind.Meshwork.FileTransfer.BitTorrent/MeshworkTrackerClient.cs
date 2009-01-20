using System;
using System.Threading;
using MonoTorrent.Common;
using MonoTorrent.Client;
using MonoTorrent.BEncoding;
using FileFind.Meshwork;
using FileFind.Meshwork.FileTransfer;
using FileFind.Meshwork.FileTransfer.BitTorrent;
using MonoTorrent.Client.Tracker;

namespace FileFind.Meshwork.FileTransfer.BitTorrent
{
	public class MeshworkTracker : MonoTorrent.Client.Tracker.Tracker
	{
		IFileTransfer transfer;
		
		public MeshworkTracker(Uri announceUrl)
		{
			CanScrape = false;
		}
		
		public override WaitHandle Scrape (byte[] infohash, TrackerConnectionID id)
		{
			return null;  // Scrape is unsupported, no need to implement
		}
		
		public override WaitHandle Announce (AnnounceParameters parameters)
		{
			return null; // Announcing does nothing as we will only be loading active connections
		}
	}
}