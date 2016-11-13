//
// FileSearchManager.cs: Keeps track of active file searches.
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using System;
using System.Collections.Generic;
using Meshwork.Backend.Core;

namespace Meshwork.Backend.Feature.FileSearch
{
	public delegate void FileSearchEventHandler (FileSearch search);

	public class FileSearchManager
	{
	    private readonly Core.Core core;

		List<FileSearch> fileSearches = new List<FileSearch>();

	    public event FileSearchEventHandler SearchAdded;
		public event FileSearchEventHandler SearchRemoved;

		internal FileSearchManager (Core.Core core)
		{
		    this.core = core;
			core.NetworkAdded   += Core_NetworkAdded;
			core.NetworkRemoved += Core_NetworkRemoved;
		}

		public FileSearch NewFileSearch (string query, string networkId)
		{
		    var search = new FileSearch(core)
		    {
		        Name = query,
		        Query = query
		    };

		    if (networkId != null) {
				search.NetworkIds.Add(networkId);
			}
			
			AddFileSearch(search);

			return search;
		}

		public void AddFileSearch (FileSearch search)
		{
			fileSearches.Add(search);

		    SearchAdded?.Invoke(search);

		    foreach (var network in core.Networks) {
				if (search.NetworkIds.Count == 0 || search.NetworkIds.IndexOf(network.NetworkID) > -1) { 
					network.FileSearch(search);
				}
			}
		}

		public void RemoveFileSearch (FileSearch search)
		{
			if (fileSearches.Contains(search)) {
				fileSearches.Remove(search);
				if (SearchRemoved != null) {
					SearchRemoved(search);
				}
			} else {
				throw new InvalidOperationException("Search is not known.");
			}
		}

		private void Core_NetworkAdded (Network network)
		{
			network.ReceivedSearchResult += network_ReceivedSearchResult;
		}

		private void Core_NetworkRemoved (Network network)
		{
			network.ReceivedSearchResult -= network_ReceivedSearchResult;
		}

		private void network_ReceivedSearchResult (Network network, SearchResultInfoEventArgs args)
		{
			foreach (var thisSearch in fileSearches) {
				if (thisSearch.Id == args.Info.SearchId) {
					thisSearch.AppendResults(args.Node, args.Info);
					return;
				}
			}
			LoggingService.LogWarning("Unexpected search reply.");
		}
	}
}
