using Gtk;
using System.Text.RegularExpressions;

namespace Meshwork.Client.GtkClient
{
	public static class DndUtils
	{
		public enum TargetType {
			UriList
		}

		public static readonly TargetEntry TargetUriList =
			new TargetEntry ("text/uri-list", 0, (uint) TargetType.UriList);

		public static string SelectionDataToString (Gtk.SelectionData data)
		{
			return System.Text.Encoding.UTF8.GetString (data.Data);
		}

		public static string [] SplitSelectionData (Gtk.SelectionData data)
		{
			string s = SelectionDataToString (data);
			return SplitSelectionData (s);
		}

		public static string [] SplitSelectionData (string data)
		{
			return Regex.Split (data, "\r\n");
		}
	}
}
