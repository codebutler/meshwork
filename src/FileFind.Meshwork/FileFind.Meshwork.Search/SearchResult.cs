//
// SearchResult.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007 FileFind.net (http://filefind.net)
//

using System;
using System.Collections.Generic;
using System.Collections;
using FileFind.Meshwork.Protocol;

namespace FileFind.Meshwork.Search
{
	public class SearchResult
	{
		SearchResult       parent;
		bool               visible = true;
		ISharedListing     listing;
		SearchResultType   type;
		string             infoHash;
		List<SearchResult> children;
		Node               node;
		
		public SearchResult (SearchResultType type, string infoHash, ISharedListing listing) 
		{
			this.type = type;
			this.infoHash = infoHash;
			this.listing = listing;
			
			children = new List<SearchResult>();
		}
		
		public string InfoHash {
			get {
				return infoHash;
			}
		}
		
		public SearchResultType Type {
			get {
				return type;
			}
		}
		
		public ISharedListing Listing {
			get {
				return listing;
			}
		}

		public SearchResult[] Children {
			get {
				return children.ToArray();
			}
		}

		public Node Node {
			get {
				return node;
			}
			internal set {
				node = value;
			}
		}
		
		public void Add (SearchResult result)
		{
			children.Add(result);
			result.Parent = this;
		}

		public bool Visible {
			get {
				if (listing == null) {
					return !children.TrueForAll(delegate (SearchResult result) {
						return !result.Visible;
					});
				} else {
					return visible;
				}
			}
			set {
				visible = value;
			}
		}

		// This is just a shortcut
		public SearchResult FirstChild {
			get {
				if (children.Count > 0) {
					return children[0];
				} else {
					return null;
				}
			}
		}

		public SearchResult FirstVisibleChild {
			get {
				foreach (SearchResult child in children) {
					if (child.Visible) {
						return child;
					}
				}
				return null;
			}
		}

		public SearchResult Parent {
			get {
				return parent;
			}
			private set {
				parent = value;
			}
		}
	}
	
	public enum SearchResultType 
	{
		File,
		Directory
	}
}
