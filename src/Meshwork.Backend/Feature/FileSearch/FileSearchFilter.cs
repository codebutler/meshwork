//
// FileSearchFilter.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2007 Meshwork Authors
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Meshwork.Backend.Feature.FileBrowsing.Filesystem;

namespace Meshwork.Backend.Feature.FileSearch
{
	public class FileSearchFilter 
	{
		FileSearchFilterComparison comparison;
		FileSearchFilterField field;
		string text = string.Empty;

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
				switch (Field) {
					case FileSearchFilterField.Size:
						return Common.Utils.ValidateSizeStr(text);
					case FileSearchFilterField.FileName:
						if (comparison == FileSearchFilterComparison.Regexp) {
							try {
								new Regex(text);
								return true;
							} catch (ArgumentException) {
								return false;
							}
						}
				        return (Text.Trim().Length > 0);
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
			
			var resultFilterType = (result.Type == SearchResultType.Directory) ? FilterType.Folder : FileTypeToFilterType(result.FileListing.Type);

			if (fileTypeFilterFields[resultFilterType].Contains(Field) ||
			    fileTypeFilterFields[FilterType.Other].Contains(Field))
			{
				switch (Field) {
					case FileSearchFilterField.FileName:
						switch (Comparison) {
							case FileSearchFilterComparison.Contains:
								return (result.Name.ToLower().IndexOf(Text.ToLower()) > -1);
							case FileSearchFilterComparison.DoesntContain:
								return (result.Name.ToLower().IndexOf(Text.ToLower()) == -1);
							case FileSearchFilterComparison.Regexp:
								return Regex.IsMatch(result.Name, text);
						}
						break;
					case FileSearchFilterField.Size:
						var filterSize = Common.Utils.SizeStringToBytes(Text);
						switch (Comparison) {
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
