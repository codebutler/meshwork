//
// FileSearchItem.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007 FileFind.net (http://filefind.net)
//

using FileFind.Meshwork.GtkClient.Pages;

namespace FileFind.Meshwork.GtkClient.SidebarItems
{
	internal class FileSearchItem : ISidebarItem
	{
		Gdk.Pixbuf        icon;
		SearchResultsPage pageWidget;
		FileSearch        search;

		public FileSearchItem (FileSearch search)
		{
			icon = Gui.LoadIcon(16, "system-search");
			this.search = search;
			pageWidget = new SearchResultsPage(search);
		}

		public virtual string Name {
			get {
				return search.Name;
			}
		}

		public int Count {
			get {
				return search.Results.Count;
			}
		}

		public virtual Gdk.Pixbuf Icon {
			get {
				return icon;
			}
		}

		public Gtk.Widget PageWidget {
			get {
				return pageWidget;
			}
		}

		public FileSearch Search {
			get {
				return search;
			}
		}

		public void Destroy ()
		{
			pageWidget.Destroy();
		}
	}
}
