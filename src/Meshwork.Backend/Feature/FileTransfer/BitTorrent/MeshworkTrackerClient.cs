using System;
using MonoTorrent.Client.Tracker;

namespace Meshwork.Backend.Feature.FileTransfer.BitTorrent
{
	public class MeshworkTracker : Tracker
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
			var e = new AnnounceResponseEventArgs(this, state, true);
			RaiseAnnounceComplete(e);
		}
	}
}