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
using FileFind.Meshwork.Filesystem;

namespace FileFind.Meshwork.Search
{
	public class SearchResult
	{
		FileSearch     m_Search;
		bool           m_Visible = true;
		Node           m_Node;
		
		SharedFileListing m_Listing;
		
		string         m_FullPath;
		string         m_InfoHash;
		
		public SearchResult (FileSearch search, Node node, SharedFileListing listing) : this (search, node)
		{
			if (listing == null)
				throw new ArgumentNullException("listing");
					
			m_Listing = listing;
		}
		
		public SearchResult (FileSearch search, Node node, string directoryFullPath) : this (search, node)
		{
			m_FullPath = directoryFullPath;
			
			/* The InfoHash property is used as a key elsewhere, so just
			 * set it to something unique for directories */
			m_InfoHash = Common.SHA512Str(directoryFullPath);
		}
		
		SearchResult (FileSearch search, Node node)
		{
			if (search == null)
				throw new ArgumentNullException("search");
			
			if (node == null)
				throw new ArgumentNullException("node");
				
			m_Search = search;
			m_Node = node;
		}

		public string InfoHash {
			get {
				if (m_Listing is SharedFileListing) {
					return ((SharedFileListing)m_Listing).InfoHash;
				} else {
					return m_InfoHash;
				}
			}
		}
		
		public SearchResultType Type {
			get {
				return (m_Listing == null) ? SearchResultType.Directory : SearchResultType.File;
			}
		}
		
		public SharedFileListing FileListing {
			get {
				return m_Listing;
			}
		}
		
		public string Name {
			get {
				return (m_Listing != null) ? m_Listing.Name : PathUtil.GetBaseName(m_FullPath);
			}
		}
		
		public string FullPath {
			get {
				return (m_Listing != null) ? m_Listing.FullPath : m_FullPath;
			}
		}
		
		public long Size {
			get {
				return (m_Listing != null) ? m_Listing.Size : -1;
			}
		}

		public Node Node {
			get {
				return m_Node;
			}
		}
		
		public bool Visible {
			get {
				return m_Visible;
			}
			set {
				m_Visible = value;
			}
		}
		
		public void Download ()
		{
			if (Type == SearchResultType.File) {
				// FIXME: Find other sources for file!
				m_Node.Network.DownloadFile(m_Node, m_Listing);
			} else {
				throw new InvalidOperationException("Cannot download directories");
			}
		}
	}
	
	public enum SearchResultType
	{
		File,
		Directory
	}
}
