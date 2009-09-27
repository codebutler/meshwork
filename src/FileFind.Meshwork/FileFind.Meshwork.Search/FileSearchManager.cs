//
// FileSearchManager.cs: Keeps track of active file searches.
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using System;
using FileFind.Meshwork;
using FileFind.Meshwork.Protocol;
using System.Collections.Generic;

namespace FileFind.Meshwork.Search
{
	public delegate void FileSearchEventHandler (FileSearch search);

	public class FileSearchManager
	{
		List<FileSearch> fileSearches = new List<FileSearch>();

		public event FileSearchEventHandler SearchAdded;
		public event FileSearchEventHandler SearchRemoved;

		internal FileSearchManager ()
		{
			Core.NetworkAdded   += Core_NetworkAdded;
			Core.NetworkRemoved += Core_NetworkRemoved;
		}

		public FileSearch NewFileSearch (string query, string networkId)
		{
			FileSearch search = new FileSearch();
			search.Name = query;
			search.Query = query;

			if (networkId != null) {
				search.NetworkIds.Add(networkId);
			}
			
			AddFileSearch(search);

			return search;
		}

		public void AddFileSearch (FileSearch search)
		{
			fileSearches.Add(search);

			if (SearchAdded != null) {
				SearchAdded(search);
			}
			
			foreach (Network network in Core.Networks) {
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
			foreach (FileSearch thisSearch in fileSearches) {
				if (thisSearch.Id == args.Info.SearchId) {
					thisSearch.AppendResults(args.Node, args.Info);
					return;
				}
			}
			LoggingService.LogWarning("Unexpected search reply.");
		}
	}
}
