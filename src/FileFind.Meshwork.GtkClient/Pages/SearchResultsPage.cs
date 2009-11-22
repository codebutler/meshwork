using System;
using System.Collections;
using System.Collections.Generic;
using FileFind;
using FileFind.Meshwork;
using FileFind.Meshwork.Filesystem;
using FileFind.Meshwork.Search;
using FileFind.Meshwork.Protocol;
using Gtk;

namespace FileFind.Meshwork.GtkClient 
{
	internal class SearchResultsPage : VBox, IPage
	{
		Toolbar         toolbar;
		TreeView        resultsTree;
		TreeModelFilter resultsFilter;
		TreeModelSort   resultsSort;
		ListStore       resultsStore;
		ListStore       typeStore;
		TreeView        typeTree;
		FilterWidget    filterWidget;
		FileSearch      search;
		Gdk.Pixbuf      folderPixbuf;
		Gdk.Pixbuf      unknownPixbuf;
		ToggleButton    filterButton;
		Menu            resultPopupMenu;
		ImageMenuItem 	downloadResultMenuItem;
		ImageMenuItem	resultPropertiesMenuItem;
		ImageMenuItem	browseResultMenuItem;
		ToolButton      downloadToolButton;
		ToolButton      browseToolButton;

		List<TreeViewColumn> audioColumns;
		List<TreeViewColumn> videoColumns;
		List<TreeViewColumn> imageColumns;
		List<TreeViewColumn> fileOnlyColumns;
		List<TreeViewColumn> folderOnlyColumns;

		public event EventHandler UrgencyHintChanged;

		// For performance.
		Dictionary<FilterType, int> resultCountByTypeCache;
		int resultTotalCountCache = 0;

		public SearchResultsPage (FileSearch search)
		{
			VPaned         paned;
			TreeViewColumn column;
			ToolItem       spacerItem;
			ToolItem       filterItem;
			Alignment      filterAlignment;
			ToolButton     searchAgainToolButton;
			
			this.search = search;
	
			downloadToolButton = new ToolButton(new Image("gtk-save", IconSize.LargeToolbar), "Download");
			downloadToolButton.IsImportant = true;
			downloadToolButton.Sensitive = false;
			downloadToolButton.Clicked += DownloadToolButtonClicked;
			
			searchAgainToolButton = new ToolButton(new Image("gtk-refresh", IconSize.LargeToolbar), "Search Again");
			searchAgainToolButton.IsImportant = true;
			searchAgainToolButton.Clicked += SearchAgainToolButtonClicked;		
			
			spacerItem = new ToolItem();
			spacerItem.Expand = true;

			filterButton = new ToggleButton("Filter Results");
			filterButton.Image = new Image(Gui.LoadIcon(16, "application-x-executable"));
			filterButton.Toggled += delegate (object o, EventArgs args) {
				this.ShowFilter = filterButton.Active;
			};

			filterAlignment = new Alignment(0.5f, 0.5f, 0, 0);
			filterAlignment.Add(filterButton);

			filterItem = new ToolItem();
			filterItem.Add(filterAlignment);

			browseToolButton = new ToolButton(new Image("gtk-open", IconSize.LargeToolbar), "Browse");
			browseToolButton.IsImportant = true;
			browseToolButton.Sensitive = false;
			browseToolButton.Clicked += BrowseToolButtonClicked;

			toolbar = new Toolbar();
			toolbar.ToolbarStyle = ToolbarStyle.BothHoriz;
			toolbar.Insert(downloadToolButton, -1);
			toolbar.Insert(browseToolButton, -1);
			toolbar.Insert(spacerItem, -1);
			toolbar.Insert(filterItem, -1);
			toolbar.Insert(new SeparatorToolItem(), -1);
			toolbar.Insert(searchAgainToolButton, -1);
			toolbar.ShowAll();

			this.PackStart(toolbar, false, false, 0);

			resultCountByTypeCache = new Dictionary<FilterType, int>();

			Gdk.Pixbuf audioPixbuf = Gui.LoadIcon(16, "audio-x-generic");
			Gdk.Pixbuf videoPixbuf = Gui.LoadIcon(16, "video-x-generic");
			Gdk.Pixbuf imagePixbuf = Gui.LoadIcon(16, "image-x-generic");
			Gdk.Pixbuf docPixbuf = Gui.LoadIcon(16, "x-office-document");
			unknownPixbuf = Gui.LoadIcon(16, "text-x-generic");
			folderPixbuf = Gui.LoadIcon(16, "folder");
			
			typeStore = new ListStore(typeof(Gdk.Pixbuf), typeof(string), typeof(FilterType));
			typeStore.AppendValues(null, "All Results", FilterType.All);
			typeStore.AppendValues(null, "-");
			typeStore.AppendValues(audioPixbuf, "Audio", FilterType.Audio);
			typeStore.AppendValues(videoPixbuf, "Video", FilterType.Video);
			typeStore.AppendValues(imagePixbuf, "Images", FilterType.Image);
			typeStore.AppendValues(docPixbuf, "Documents", FilterType.Document);
			typeStore.AppendValues(folderPixbuf, "Folders", FilterType.Folder);
			typeStore.AppendValues(unknownPixbuf, "Other", FilterType.Other);
			
			typeTree = new TreeView();
			typeTree.HeadersVisible = false;
			typeTree.RowSeparatorFunc = delegate (TreeModel m, TreeIter i) {
				string text = (string)m.GetValue(i, 1);
				return (text == "-");
			};
			typeTree.Selection.Changed += TypeSelectionChanged;

			typeTree.Model = typeStore;
			
			CellRendererPixbuf pixbufCell = new CellRendererPixbuf();
			CellRendererText textCell = new CellRendererText();
			CellRendererText countTextCell = new CellRendererText();
			countTextCell.Sensitive = false;
			countTextCell.Alignment = Pango.Alignment.Right;
			countTextCell.Xalign = 1;

			column = new TreeViewColumn();
			column.PackStart(pixbufCell, false);
			column.PackStart(textCell, true);
			column.PackStart(countTextCell, false);
			column.AddAttribute(pixbufCell, "pixbuf", 0);
			column.AddAttribute(textCell, "text", 1);
			column.SetCellDataFunc(countTextCell, new TreeCellDataFunc(TypeCountCellFunc));

			typeTree.AppendColumn(column);

			TreeView artistTree = new TreeView();
			artistTree.HeadersVisible = false;

			TreeView albumTree = new TreeView();
			albumTree.HeadersVisible = false;

			HBox topBox = new HBox();
			topBox.PackStart(Gui.AddScrolledWindow(typeTree), true, true, 0);
			topBox.PackStart(Gui.AddScrolledWindow(artistTree), true, true, 1);
			topBox.PackStart(Gui.AddScrolledWindow(albumTree), true, true, 0);
			topBox.Homogeneous = true;

			resultsStore = new ListStore(typeof(SearchResult));
			resultsStore.RowInserted += delegate {
				Refilter();
			};
			resultsStore.RowDeleted += delegate {
				Refilter();
			};
			resultsTree = new TreeView();
			resultsTree.RowActivated += resultsTree_RowActivated;
			resultsTree.ButtonPressEvent += resultsTree_ButtonPressEvent;
			resultsTree.Selection.Changed += ResultsTreeSelectionChanged;

			imageColumns = new List<TreeViewColumn>();
			audioColumns = new List<TreeViewColumn>();
			videoColumns = new List<TreeViewColumn>();
			fileOnlyColumns = new List<TreeViewColumn>();
			folderOnlyColumns = new List<TreeViewColumn>();

			column = new TreeViewColumn();
			column.Title = "File Name";
			column.Clickable = true;
			column.Sizing = TreeViewColumnSizing.Autosize;
			column.Resizable = true;
			column.SortColumnId = 0;
			//resultsTree.ExpanderColumn = column;

			CellRenderer cell = new CellRendererPixbuf();
			column.PackStart(cell, false);
			column.SetCellDataFunc(cell, new TreeCellDataFunc(IconFunc));

			cell = new CellRendererText();
			column.PackStart(cell, true);
			column.SetCellDataFunc(cell, new TreeCellDataFunc(FileNameFunc));

			resultsTree.AppendColumn(column);

			column = resultsTree.AppendColumn("Codec", new CellRendererText(), new TreeCellDataFunc(CodecFunc));
			column.Clickable = true;
			column.Sizing = TreeViewColumnSizing.Fixed;
			column.FixedWidth = 120;
			column.Resizable = true;
			column.SortColumnId = 1;
			videoColumns.Add(column);

			column = resultsTree.AppendColumn("Format", new CellRendererText(), new TreeCellDataFunc(FormatFunc));
			column.Clickable = true;
			column.Sizing = TreeViewColumnSizing.Fixed;
			column.FixedWidth = 90;
			column.Resizable = true;
			column.SortColumnId = 2;
			imageColumns.Add(column);

			column = resultsTree.AppendColumn("Resolution", new CellRendererText(), new TreeCellDataFunc(ResolutionFunc));
			column.Clickable = true;
			column.Sizing = TreeViewColumnSizing.Fixed;
			column.FixedWidth = 90;
			column.Resizable = true;
			column.SortColumnId = 3;
			videoColumns.Add(column);
			imageColumns.Add(column);

			column = resultsTree.AppendColumn("Artist", new CellRendererText(), new TreeCellDataFunc(ArtistFunc));
			column.Clickable = true;
			column.Sizing = TreeViewColumnSizing.Fixed;
			column.FixedWidth = 110;
			column.Resizable = true;
			column.SortColumnId = 4;
			audioColumns.Add(column);

			column = resultsTree.AppendColumn("Album", new CellRendererText(), new TreeCellDataFunc(AlbumFunc));
			column.Clickable = true;
			column.Sizing = TreeViewColumnSizing.Fixed;
			column.FixedWidth = 110;
			column.Resizable = true;
			column.SortColumnId = 5;
			audioColumns.Add(column);
		
			column = resultsTree.AppendColumn("Bitrate", new CellRendererText(), new TreeCellDataFunc(BitrateFunc));
			column.Clickable = true;
			column.Sizing = TreeViewColumnSizing.Fixed;
			column.FixedWidth = 70;
			column.Resizable = true;
			column.SortColumnId = 6;
			audioColumns.Add(column);

			column = resultsTree.AppendColumn("Size", new CellRendererText(), new TreeCellDataFunc(SizeFunc));
			column.Clickable = true;
			column.Sizing = TreeViewColumnSizing.Fixed;
			column.FixedWidth = 70;
			column.SortColumnId = 7;
			column.Resizable = true;
			fileOnlyColumns.Add(column);

			column = resultsTree.AppendColumn("Sources", new CellRendererText(), new TreeCellDataFunc(SourcesFunc));
			column.Clickable = true;
			column.Sizing = TreeViewColumnSizing.Fixed;
			column.FixedWidth = 85;
			column.SortColumnId = 8;
			column.Resizable = true;
			fileOnlyColumns.Add(column);
			
			column = resultsTree.AppendColumn("User", new CellRendererText(), new TreeCellDataFunc(UserFunc));
			column.Clickable = true;
			column.Sizing = TreeViewColumnSizing.Fixed;
			column.FixedWidth = 85;
			column.SortColumnId = 8;
			column.Resizable = true;
			folderOnlyColumns.Add(column);
			
			column = resultsTree.AppendColumn("Full Path", new CellRendererText(), new TreeCellDataFunc(FullPathFunc));
			column.Clickable = true;
			column.Resizable = true;
			column.SortColumnId = 9;

			column = resultsTree.AppendColumn("Info Hash", new CellRendererText(), new TreeCellDataFunc(InfoHashFunc));
			column.Clickable = true;
			column.Resizable = true;
			column.SortColumnId = 10;
			fileOnlyColumns.Add(column);

			resultsFilter = new TreeModelFilter(resultsStore, null);
			resultsFilter.VisibleFunc = resultsFilterFunc;

			resultsSort = new TreeModelSort(resultsFilter);
			for (int x = 0; x < resultsTree.Columns.Length; x++) {
				resultsSort.SetSortFunc(x, resultsSortFunc);
			}
			resultsTree.Model = resultsSort;

			ScrolledWindow resultsTreeSW = new ScrolledWindow();
			resultsTreeSW.Add(resultsTree);

			paned = new VPaned();
			paned.Add1(topBox);
			paned.Add2(resultsTreeSW);
			paned.Position = 160;
			paned.ShowAll();

			filterWidget = new FilterWidget(search);
			filterWidget.FiltersChanged += filterWidget_FiltersChanged;
			filterWidget.Hidden += filterWidget_Hidden;
		
			this.PackStart(filterWidget, false, false, 0);
			this.PackStart(paned, true, true, 0);

			TypeSelectionChanged(typeTree, EventArgs.Empty);

			search.NewResults += (NewResultsEventHandler)DispatchService.GuiDispatch(new NewResultsEventHandler(search_NewResults));
			search.ClearedResults += (EventHandler)DispatchService.GuiDispatch(new EventHandler(search_ClearedResults));

			resultPopupMenu = new Menu();
			
			browseResultMenuItem = new ImageMenuItem("Browse");
			browseResultMenuItem.Image = new Image(Gui.LoadIcon(16, "document-open"));
			browseResultMenuItem.Activated += BrowseToolButtonClicked;
			resultPopupMenu.Append(browseResultMenuItem);
			
			downloadResultMenuItem = new ImageMenuItem("Download");
			downloadResultMenuItem.Image = new Image(Gui.LoadIcon(16, "go-down"));
			downloadResultMenuItem.Activated += DownloadToolButtonClicked;
			resultPopupMenu.Append(downloadResultMenuItem);

			resultPropertiesMenuItem = new ImageMenuItem(Gtk.Stock.Properties, null);
			resultPropertiesMenuItem.Activated += FilePropertiesButtonClicked;
			resultPopupMenu.Append(resultPropertiesMenuItem);
		}

		void SearchAgainToolButtonClicked (object sender, EventArgs e)
		{
			search.Repeat();
		}

		void BrowseToolButtonClicked (object sender, EventArgs e)
		{
			 try {
				TreeIter iter;
				if (resultsTree.Selection.GetSelected(out iter)) {
					SearchResult selectedResult = resultsTree.Model.GetValue(iter, 0) as SearchResult;	
					if (selectedResult != null) {
						string path = PathUtil.Join(selectedResult.Node.Directory.FullPath, 
						                            String.Join("/", selectedResult.FullPath.Split('/').Slice(0, -2)));
						
						UserBrowserPage.Instance.NavigateTo(path);
						Gui.MainWindow.SelectedPage = UserBrowserPage.Instance;					
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError(ex);
				Gui.ShowErrorDialog(ex.Message);
			}
		}

		void DownloadToolButtonClicked (object sender, EventArgs e)
		{				
			try {
				TreeIter iter;
				if (resultsTree.Selection.GetSelected(out iter)) {
					SearchResult selectedResult = resultsTree.Model.GetValue(iter, 0) as SearchResult;					
					if (selectedResult != null && selectedResult.Type == SearchResultType.File) { 
						selectedResult.Download();
					}			
				}					
			} catch (Exception ex) {
				LoggingService.LogError(ex);
				Gui.ShowErrorDialog(ex.Message);
			}
		}
		
		void FilePropertiesButtonClicked (object sender, EventArgs args)
		{
			try {
				TreeIter iter;
				if (resultsTree.Selection.GetSelected(out iter)) {
					SearchResult selectedResult = resultsTree.Model.GetValue(iter, 0) as SearchResult;
					if (selectedResult != null && selectedResult.Type == SearchResultType.File) {
						var win = new FilePropertiesWindow(selectedResult.Node, (SharedFileListing)selectedResult.FileListing);
						win.Show();
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError(ex);
				Gui.ShowErrorDialog(ex.Message);
			}
		}

		void ResultsTreeSelectionChanged (object sender, EventArgs e)
		{
			TreeIter iter;
			if (resultsTree.Selection.GetSelected(out iter)) {
				SearchResult result = resultsTree.Model.GetValue(iter, 0) as SearchResult;
				if (result != null) {
					browseToolButton.Sensitive = true;
					if (result.Type == SearchResultType.File)
						downloadToolButton.Sensitive = true;
					else
						downloadToolButton.Sensitive = false;
				} else {
					downloadToolButton.Sensitive = false;
					browseToolButton.Sensitive = false;
				}
			} else {	
				browseToolButton.Sensitive = false;
				downloadToolButton.Sensitive = false;
			}
		}

		public void search_NewResults (FileSearch sender, SearchResult[] newResults)
		{
			AppendResults(newResults);

			RecountTypes();
			Gui.MainWindow.RefreshCounts();
		}
		
		public void search_ClearedResults (object sender, EventArgs args)
		{
			resultsStore.Clear();
		}

		private void AppendResults (SearchResult[] results)
		{
			// XXX: This won't group same files from multiple nodes!
			foreach (SearchResult result in results) {
				resultsStore.AppendValues(result);
			}
		}

		public bool UrgencyHint {
			get {
				return false;
			}
		}

		public FileSearch Search {
			get {
				return search;
			}
		}

		public bool ShowFilter {
			set {
				filterWidget.Visible = value;
				Refilter();
				RecountTypes();
			}
		}

		private void filterWidget_FiltersChanged (object sender, EventArgs args)
		{
			Refilter();
			RecountTypes();
		}

		private bool resultsFilterFunc (TreeModel model, TreeIter iter)
		{
			SearchResult result = (SearchResult)model.GetValue(iter, 0);
			if (result != null) {
				return result.Visible;
			} else {
				return false;
			}
		}

		private int resultsSortFunc (TreeModel model, TreeIter a, TreeIter b)
		{
			SearchResult first = (SearchResult)model.GetValue(a, 0);
			SearchResult second = (SearchResult)model.GetValue(b, 0);
			
			int columnId;
			SortType order;
			if (resultsSort.GetSortColumnId(out columnId, out order)) {
				switch (columnId) {
					case 0: // File Name
						return StringComparer.CurrentCulture.Compare(first.Name, second.Name);
					case 7: // Size
						return first.Size.CompareTo(second.Size);
					case 8: // Sources
						int numberOfSourcesOne = (first.Type == SearchResultType.File) ? search.AllFileResults[first.InfoHash].Count : -1;
						int numberOfSourcesTwo = (second.Type == SearchResultType.File) ? search.AllFileResults[second.InfoHash].Count : -1;
						return numberOfSourcesOne.CompareTo(numberOfSourcesTwo);
					case 9: // Full Path
						return StringComparer.CurrentCulture.Compare(first.FullPath, second.FullPath);
				}
			}
			return 0;
		}

		private void Refilter ()
		{
			FilterType selectedFilterType = FilterType.All;

			TreeIter i;
			if (typeTree.Selection.GetSelected(out i)) {
				selectedFilterType = (FilterType)typeTree.Model.GetValue(i, 2);
			}

			foreach (SearchResult result in search.Results) {
				if (result.Type == SearchResultType.File) {
					if (selectedFilterType != FilterType.All) {
						var filterType = FileSearchFilter.FileTypeToFilterType(result.FileListing.Type);
						result.Visible = (filterType == selectedFilterType);
					} else {
						result.Visible = true;
					}
				} else if (result.Type == SearchResultType.Directory) {
					result.Visible = (selectedFilterType == FilterType.All || selectedFilterType == FilterType.Folder);
				}

				if (result.Visible) {
					if (search.FiltersEnabled) {
						result.Visible = search.CheckAllFilters(result);
					} else {
						result.Visible = true;
					}
				}
			}
			
			if (resultsFilter != null) {
				resultsFilter.Refilter();
			}
		}

		private void RecountTypes()
		{
			resultCountByTypeCache.Clear();
			resultTotalCountCache = 0;

			foreach (SearchResult result in search.Results) {
				var filterType = (result.Type == SearchResultType.File) ? FileSearchFilter.FileTypeToFilterType(result.FileListing.Type) : FilterType.Folder;
				if (!resultCountByTypeCache.ContainsKey(filterType)) {
					resultCountByTypeCache[filterType] = 0;
				}
				if (search.FiltersEnabled) {
					if (search.CheckAllFilters(result)) {
						resultCountByTypeCache[filterType] += 1;
						resultTotalCountCache += 1;
					}
				} else {
					resultCountByTypeCache[filterType] += 1;
					resultTotalCountCache += 1;
				}
			}
			typeTree.QueueDraw();
		}

		private void TypeCountCellFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			CellRendererText textCell = (CellRendererText)cell;

			if (model.GetValue(iter, 2) == null)
				return;
			
			FilterType filterType = (FilterType)model.GetValue(iter, 2);
			if (filterType != FilterType.All) {
				FilterType type = (FilterType)model.GetValue(iter, 2);
				if (resultCountByTypeCache.ContainsKey(type)) {
					textCell.Text = resultCountByTypeCache[type].ToString();
				} else {
					textCell.Text = "0";
				}
			} else {
				textCell.Text = resultTotalCountCache.ToString();
			}
		}
			
		private void IconFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			SearchResult result = (SearchResult)model.GetValue(iter, 0);
			if (result.Type == SearchResultType.File) {
				(cell as CellRendererPixbuf).Pixbuf = unknownPixbuf;
			} else {
				(cell as CellRendererPixbuf).Pixbuf = folderPixbuf;
			}
		}

		private void FileNameFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			SearchResult result = (SearchResult)model.GetValue(iter, 0);
			(cell as CellRendererText).Text = result.Name;
		}

		private void CodecFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			//SearchResult result = (SearchResult)model.GetValue(iter, 0);
			(cell as CellRendererText).Text = String.Empty;
		}

		private void FormatFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			//SearchResult result = (SearchResult)model.GetValue(iter, 0);
			(cell as CellRendererText).Text = String.Empty;
		}

		private void ResolutionFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			//SearchResult result = (SearchResult)model.GetValue(iter, 0);
			(cell as CellRendererText).Text = String.Empty;
		}

		private void ArtistFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			//SearchResult result = (SearchResult)model.GetValue(iter, 0);
			(cell as CellRendererText).Text = String.Empty;
		}

		private void AlbumFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			//SearchResult result = (SearchResult)model.GetValue(iter, 0);
			(cell as CellRendererText).Text = String.Empty;
		}

		private void BitrateFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			//SearchResult result = (SearchResult)model.GetValue(iter, 0);
			(cell as CellRendererText).Text = String.Empty;
		}

		private void SizeFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			SearchResult result = (SearchResult)model.GetValue(iter, 0);
			if (result.Type == SearchResultType.File) {
				(cell as CellRendererText).Text = Common.FormatBytes(result.Size);
			} else  {
				(cell as CellRendererText).Text = String.Empty;
			}
		}

		private void SourcesFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			SearchResult result = (SearchResult)model.GetValue(iter, 0);

			if (result.Type == SearchResultType.File) {
				int numberOfSources = search.AllFileResults[result.InfoHash].Count;
				if (numberOfSources == 1) {
					(cell as CellRendererText).Text = "1 source";
				} else {
					(cell as CellRendererText).Text = numberOfSources + " sources";
				}
			} else {
				(cell as CellRendererText).Text = String.Empty;
			}
		}
		
		private void UserFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			SearchResult result = (SearchResult)model.GetValue(iter, 0);
			(cell as CellRendererText).Text = result.Node.ToString();
		}

		private void InfoHashFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			SearchResult result = (SearchResult)model.GetValue(iter, 0);
			if (result.Type == SearchResultType.File) {
				(cell as CellRendererText).Text = result.InfoHash;
			} else {
				(cell as CellRendererText).Text = String.Empty;
			}
		}

		private void FullPathFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			SearchResult result = (SearchResult)model.GetValue(iter, 0);
			(cell as CellRendererText).Text = result.FullPath;
		}

		private void filterWidget_Hidden (object sender, EventArgs args)
		{
			filterButton.Active = false;
			RecountTypes();
		}

		private void ToggleColumns(IEnumerable columnList, bool visible)
		{
			if (columnList == null) {
				return;
			}

			foreach (TreeViewColumn col in columnList) {
				col.Visible = visible;
			}
		}

		private void TypeSelectionChanged (object o, EventArgs args)
		{
			Refilter();

			TreeIter iter;
			if (typeTree.Selection.GetSelected(out iter)) {
				FilterType filterType = (FilterType)typeTree.Model.GetValue(iter, 2);
				switch (filterType) {
					case FilterType.Audio:
						ToggleColumns(imageColumns, false);
						ToggleColumns(videoColumns, false);
						ToggleColumns(audioColumns, true);
						ToggleColumns(fileOnlyColumns, true);			
						ToggleColumns(folderOnlyColumns, false);
						return;
					case FilterType.Video:
						ToggleColumns(imageColumns, false);
						ToggleColumns(audioColumns, false);
						ToggleColumns(videoColumns, true);
						ToggleColumns(fileOnlyColumns, true);			
						ToggleColumns(folderOnlyColumns, false);
						return;
					case FilterType.Image:
						ToggleColumns(audioColumns, false);
						ToggleColumns(videoColumns, false);
						ToggleColumns(imageColumns, true);
						ToggleColumns(fileOnlyColumns, true);			
						ToggleColumns(folderOnlyColumns, false);
						return;
					case FilterType.Folder:
						ToggleColumns(audioColumns, false);
						ToggleColumns(videoColumns, false);
						ToggleColumns(imageColumns, false);
						ToggleColumns(fileOnlyColumns, false);			
						ToggleColumns(folderOnlyColumns, true);
						return;
				}
			} 
			ToggleColumns(audioColumns, false);
			ToggleColumns(videoColumns, false);
			ToggleColumns(imageColumns, false);
			ToggleColumns(fileOnlyColumns, true);			
			ToggleColumns(folderOnlyColumns, false);
		}

		private void resultsTree_RowActivated (object sender, RowActivatedArgs args)
		{
			try {
				TreeIter iter;
				if (resultsTree.Model.GetIter(out iter, args.Path)) {
					SearchResult selectedResult = (SearchResult)resultsTree.Model.GetValue(iter, 0);
					if (selectedResult.Type == SearchResultType.File) { 
						selectedResult.Download();
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError(ex);
				Gui.ShowErrorDialog(ex.Message);
			}
		}

		[GLib.ConnectBefore]
		private void resultsTree_ButtonPressEvent (object sender, ButtonPressEventArgs args)
		{
			TreePath path;
			TreeIter iter;

			if (resultsTree.GetPathAtPos((int)args.Event.X, (int)args.Event.Y, out path)) {
				resultsTree.Selection.SelectPath(path);
			} else {
				resultsTree.Selection.UnselectAll();
			}

			if (args.Event.Button == 3) {
				if (resultsTree.Selection.GetSelected(out iter)) {
					SearchResult selectedResult = (SearchResult)resultsTree.Model.GetValue(iter, 0);
					if (selectedResult.Type == SearchResultType.File) { 
						downloadResultMenuItem.Sensitive = true;
						resultPropertiesMenuItem.Sensitive = true;
					} else {
						downloadResultMenuItem.Sensitive = false;
						resultPropertiesMenuItem.Sensitive = false;
					}
					browseResultMenuItem.Sensitive = true;
				} else {
						downloadResultMenuItem.Sensitive = false;
						resultPropertiesMenuItem.Sensitive = false;
						browseResultMenuItem.Sensitive = false;
				}
				resultPopupMenu.ShowAll();
				resultPopupMenu.Popup();
			}
		}
	}
}
