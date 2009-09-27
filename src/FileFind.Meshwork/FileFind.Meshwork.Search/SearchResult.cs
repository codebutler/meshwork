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
		
		public SearchResult (SearchResultType type, Node node, string infoHash)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			
			if (node == null)
				throw new ArgumentNullException("node");
			
			if (infoHash == null)
				throw new ArgumentNullException("infoHash");
					
			this.node = node;
			this.type = type;
			this.infoHash = infoHash;
			
			children = new List<SearchResult>();
		}
			
		
		public SearchResult (SearchResultType type, Node node, ISharedListing listing) 
		{
			if (type == null)
				throw new ArgumentNullException("type");
			
			if (node == null)
				throw new ArgumentNullException("node");
			
			if (listing == null)
				throw new ArgumentNullException("listing");
					
			this.node = node;
			this.type = type;
			this.listing = listing;
			
			children = new List<SearchResult>();
		}

		public string InfoHash {
			get {
				if (type == SearchResultType.File) {
					if (listing != null)
						return ((SharedFileListing)listing).InfoHash;
					else
						return infoHash;
				} else {
					return null;
				}
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
