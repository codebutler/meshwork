//
// FileSearchFilter.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007 FileFind.net (http://filefind.net)
//

using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using FileFind.Meshwork.Protocol;
using FileFind.Meshwork.Filesystem;

namespace FileFind.Meshwork.Search
{
	public class FileSearchFilter 
	{
		FileSearchFilterComparison comparison;
		FileSearchFilterField field;
		string text = String.Empty;

		Dictionary<FilterType, FileSearchFilterField[]> fileTypeFilterFields = new Dictionary<FilterType, FileSearchFilterField[]> {
			{ FilterType.Video,    new [] { FileSearchFilterField.Resolution } },
			{ FilterType.Audio,    new [] { FileSearchFilterField.Artist, FileSearchFilterField.Album, FileSearchFilterField.Bitrate } },
			{ FilterType.Image,    new [] { FileSearchFilterField.Dimentions } },
			{ FilterType.Document, new [] { FileSearchFilterField.Title, FileSearchFilterField.Author } },
			{ FilterType.Other,    new [] { FileSearchFilterField.FileName, FileSearchFilterField.Size } },
			{ FilterType.Folder,   new [] { FileSearchFilterField.FileName } }
		};

		public FileSearchFilterField Field {
			get {
				return field;
			}
			set {
				field = value;
			}
		}

		public FileSearchFilterComparison Comparison {
			get {
				return comparison;
			}
			set {
				comparison = value;
			}
		}

		public string Text {
			get {
				return text;
			}
			set {
				text = value;
			}
		}

		public bool FilterValid {
			get {
				switch (this.Field) {
					case FileSearchFilterField.Size:
						return Common.ValidateSizeStr(this.text);
					case FileSearchFilterField.FileName:
						return (this.Text.Trim().Length > 0);
					default:
						return true;
				}
			}
		}

		public bool Check (SearchResult result)
		{
			if (!FilterValid) {
				return true;
			}
			
			FilterType resultFilterType = (result.Type == SearchResultType.Directory) ? FilterType.Folder : FileTypeToFilterType(result.FileListing.Type);

			if (fileTypeFilterFields[resultFilterType].Contains(this.Field) ||
			    fileTypeFilterFields[FilterType.Other].Contains(this.Field))
			{
				switch (this.Field) {
					case FileSearchFilterField.FileName:
						switch (this.Comparison) {
							case FileSearchFilterComparison.Contains:
								return (result.Name.ToLower().IndexOf(this.Text.ToLower()) > -1);
							case FileSearchFilterComparison.DoesntContain:
								return (result.Name.ToLower().IndexOf(this.Text.ToLower()) == -1);
							case FileSearchFilterComparison.Regexp:
								return true;
						}
						break;
					case FileSearchFilterField.Size:
						ulong filterSize = Common.SizeStringToBytes(this.Text);
						switch (this.Comparison) {
							case FileSearchFilterComparison.Equals:
								return ((ulong)result.Size == filterSize);
							case FileSearchFilterComparison.LessThanOrEqualTo:
								return ((ulong)result.Size <= filterSize);
							case FileSearchFilterComparison.GreaterOrEqualTo:
								return ((ulong)result.Size >= filterSize);
							default:
								throw new Exception("Invalid filter.");
						}
				}
			} else {
				// Ignore this filter for this result
				return true;
			}
			return true;
		}
		
		public static FilterType FileTypeToFilterType (FileType fileType)
		{
			switch (fileType) {
			case FileType.Audio:
				return FilterType.Audio;
			case FileType.Document:
				return FilterType.Document;
			case FileType.Image:
				return FilterType.Image;
			case FileType.Video:
				return FilterType.Video;
			default:
				return FilterType.Other;
			}
		}

	}

	public enum FileSearchFilterField
	{
		// Generic
		FileName,
		Size,

		// Audio
		Artist,
		Album,
		Bitrate,

		// Video
		Resolution,

		// Images
		Dimentions,

		// Document
		Title,
		Author
	}

	public enum FileSearchFilterComparison
	{
		Contains,
		DoesntContain,
		Regexp,

		Equals,
		LessThanOrEqualTo,
		GreaterOrEqualTo
	}
	
	public enum FilterType
	{
		All,
		Audio,
		Video,
		Image,
		Document,
		Folder,
		Other
	}
}
