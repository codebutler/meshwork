//
// FileSearch.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2007 Meshwork Authors
//

using System;
using System.Collections.Generic;
using Meshwork.Backend.Core;
using Meshwork.Backend.Core.Protocol;
using Meshwork.Common;
using Meshwork.Common.Serialization;

namespace Meshwork.Backend.Feature.FileSearch
{
	public delegate void NewResultsEventHandler (FileSearch search, SearchResult[] results);

	public class FileSearch
	{
	    private readonly Core.Core core;
	    string name;
		string query;
		bool filtersEnabled;
		List<FileSearchFilter> filters;
		List<string> networkIds;
		[NonSerialized] List<SearchResult> results;
		[NonSerialized] Dictionary<string, List<SearchResult>> allFileResults;

		int id;

		public event NewResultsEventHandler NewResults;
		public event EventHandler ClearedResults;

		public FileSearch (Core.Core core)
		{
		    this.core = core;
		    filters = new List<FileSearchFilter>();
			networkIds = new List<string>();

			results = new List<SearchResult>();
			allFileResults = new Dictionary<string, List<SearchResult>>();

			id = new Random().Next();
		}

		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}

		public string Query {
			get {
				return query;
			}
			set {
				if (value == null) {
					throw new ArgumentNullException();
				}
			    if (value.Trim() == string.Empty) {
			        throw new ArgumentException("Query may not be empty.");
			    }

			    query = value.ToLower();
			}
		}

		public int Id {
			get {
				return id;
			}
		}

		public bool FiltersEnabled {
			get {
				return filtersEnabled;
			}
			set {
				filtersEnabled = value;
			}
		}

		public List<FileSearchFilter> Filters {
			get {
				return filters;
			}
		}

		public List<string> NetworkIds {
			get {
				return networkIds;
			}
		}
		
		public void Repeat ()
		{
			id = new Random().Next();
			results.Clear();
			allFileResults.Clear();
			
			if (ClearedResults != null)
				ClearedResults(this, EventArgs.Empty);
			
			foreach (var network in core.Networks) {
				if (networkIds.Count == 0 || networkIds.IndexOf(network.NetworkID) > -1) { 
					network.FileSearch(this);
				}
			}
		}

	    [DontSerialize]
	    public IList<SearchResult> Results {
			get {
				return results.AsReadOnly();
			}
		}

	    [DontSerialize]
	    public ReadOnlyDictionary<string,List<SearchResult>> AllFileResults {
			get {
				// XXX: Can we make the List<SearchResult> readonly too?
				return new ReadOnlyDictionary<string,List<SearchResult>>(allFileResults);
			}
		}

		public void AppendResults (Node node, SearchResultInfo resultInfo)
		{
			var newResults = new List<SearchResult>();

			if (resultInfo.SearchId != id) {
				throw new ArgumentException("Results are for a different search.");
			}

			foreach (var dir in resultInfo.Directories) {
				var directoryResult = new SearchResult(this, node, dir);
				results.Add(directoryResult);
				newResults.Add(directoryResult);
			}

			foreach (var file in resultInfo.Files) {
				var result = new SearchResult(this, node, file);
				results.Add(result);
				newResults.Add(result);

				if (!allFileResults.ContainsKey(file.InfoHash)) {
					allFileResults[file.InfoHash] = new List<SearchResult>();
				}
				allFileResults[file.InfoHash].Add(result);
			}
			
			if (NewResults != null) {
				NewResults(this, newResults.ToArray());
			}
		}

		public bool CheckAllFilters (SearchResult result)
		{
			foreach (var filter in filters) {
				if (!filter.Check(result)) {
					return false;
				}
			}
			return true;
		}
	}
}
