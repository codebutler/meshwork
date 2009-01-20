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

		public bool Check (SharedFileListing result)
		{
			if (!FilterValid) {
				return true;
			}

			// I hate C# sometimes
			Dictionary<FileType, FileSearchFilterField[]> fileTypeFilterFields;
			fileTypeFilterFields = new Dictionary<FileType, FileSearchFilterField[]>();
			fileTypeFilterFields[FileType.Video] = new FileSearchFilterField[] { FileSearchFilterField.Resolution };
			fileTypeFilterFields[FileType.Audio] = new FileSearchFilterField[] { FileSearchFilterField.Artist, FileSearchFilterField.Album, FileSearchFilterField.Bitrate };
			fileTypeFilterFields[FileType.Image] = new FileSearchFilterField[] { FileSearchFilterField.Dimentions };
			fileTypeFilterFields[FileType.Document] = new FileSearchFilterField[] { FileSearchFilterField.Title, FileSearchFilterField.Author };
			fileTypeFilterFields[FileType.Other] = new FileSearchFilterField[] { FileSearchFilterField.FileName, FileSearchFilterField.Size };

			if ((fileTypeFilterFields[result.Type] as IList).IndexOf(this.Field) > -1 ||
			    (fileTypeFilterFields[FileType.Other] as IList).IndexOf(this.Field) > -1)
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
								break;
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
						break;
				}
			} else {
				// We don't care about this filter
				return true;
			}
			return true;
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
}
