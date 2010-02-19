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
		public MeshworkTracker(Uri announceUrl)
			: base (announceUrl)
		{
			CanScrape = false;
		}
		

		public override void Scrape (ScrapeParameters parameters, object state)
		{
			throw new NotSupportedException();
		}

		public override void Announce (AnnounceParameters parameters, object state)
		{
			AnnounceResponseEventArgs e = new AnnounceResponseEventArgs(this, state, true);
			RaiseAnnounceComplete(e);
		}
	}
}