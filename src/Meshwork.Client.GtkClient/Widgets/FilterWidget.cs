using System;
using System.Text.RegularExpressions;
using Gtk;
using Meshwork.Backend.Feature.FileSearch;

namespace Meshwork.Client.GtkClient.Widgets
{
	public class FilterWidget : VBox
	{
		public event EventHandler FiltersChanged;

		FileSearch search;

		public FilterWidget (FileSearch search)
		{
			this.search = search;
			this.Visible = search.FiltersEnabled;
			this.Shown += this_Shown;
			this.Hidden += this_Hidden;

			foreach (FileSearchFilter filter in search.Filters) {
				AddFilterRow(filter);
			}

			if (search.FiltersEnabled && search.Filters.Count == 0) {
				AddFilter(new FileSearchFilter());
			}
		}

		private void AddFilter (FileSearchFilter filter)
		{
			search.Filters.Add(filter);
			AddFilterRow(filter);
		}
			
		private void AddFilterRow (FileSearchFilter filter)
		{
			FilterWidgetRow newRow = new FilterWidgetRow(filter);
			newRow.Changed += filter_Changed;
			this.PackStart(newRow, false, false, 0);
			newRow.ShowAll();
			// XXX: box.ReorderChild(newRow, 
		}

		private void RemoveFilter (FileSearchFilter filter)
		{
			search.Filters.Remove(filter);
			
			foreach (Widget child in this) {
				FilterWidgetRow row = (FilterWidgetRow)child;
				if (row.Filter == filter) {
					row.Destroy();
					if (this.Children.Length == 0) {
						this.Hide();
					}
					if (FiltersChanged != null) {
						FiltersChanged(this, EventArgs.Empty);
					}
					return;
				}
			}
		}

		private void filter_Changed (object sender, EventArgs args)
		{
			if (FiltersChanged != null) {
				FiltersChanged(this, EventArgs.Empty);
			}
		}

		private void this_Shown (object sender, EventArgs args)
		{
			if (this.Children.Length == 0) {
				AddFilter(new FileSearchFilter());
			}
			search.FiltersEnabled = true;
		}

		private void this_Hidden (object sender, EventArgs args)
		{
			search.FiltersEnabled = false;
		}

		private class FilterWidgetRow : Alignment
		{
			HBox         box;
			ComboBox     fieldComboBox;
			ComboBox     matchTypeComboBox;
			FilterEntry  filterTextEntry;
			Button       addButton;
			Button       removeButton;
			ListStore    matchTypeStore;
			
			public event EventHandler Changed;

			private FileSearchFilter filter;

			public FilterWidgetRow (FileSearchFilter filter) : base (0, 0, 1, 1)
			{
				TreeIter iter;
				CellRendererText textCell;
				ListStore store;

				this.filter = filter;

				matchTypeStore = new ListStore(typeof(string), typeof(FileSearchFilterComparison));

				textCell = new CellRendererText();

				matchTypeComboBox = new ComboBox();
				matchTypeComboBox.Model = matchTypeStore;
				matchTypeComboBox.PackStart(textCell, true);
				matchTypeComboBox.AddAttribute(textCell, "text", 0);
				matchTypeComboBox.RowSeparatorFunc = ComboSeparatorFunc;
				matchTypeComboBox.Changed += MatchTypeChanged;

				textCell = new CellRendererText();
				store = new ListStore(typeof(string), typeof(FilterEntryMode), typeof(FileSearchFilterField));

				filterTextEntry = new FilterEntry(filter);
				filterTextEntry.Changed += FilterTextChanged;

				fieldComboBox = new ComboBox();
				fieldComboBox.PackStart(textCell, true);
				fieldComboBox.AddAttribute(textCell, "text", 0);
				fieldComboBox.SetCellDataFunc(textCell, FieldComboDataFunc);
				store.AppendValues("File Name", FilterEntryMode.String, FileSearchFilterField.FileName);
				store.AppendValues("Size", FilterEntryMode.Size, FileSearchFilterField.Size);
				store.AppendValues("-");
				store.AppendValues("(Audio)", null);
				store.AppendValues("Artist", FilterEntryMode.String, FileSearchFilterField.Artist);
				store.AppendValues("Album", FilterEntryMode.String, FileSearchFilterField.Album);
				store.AppendValues("Bitrate", FilterEntryMode.Speed, FileSearchFilterField.Bitrate);
				store.AppendValues("-");
				store.AppendValues("(Video)", null);
				store.AppendValues("Resolution", FilterEntryMode.Dimentions, FileSearchFilterField.Resolution);
				store.AppendValues("-");
				store.AppendValues("(Images)", null);
				store.AppendValues("Dimentions", FilterEntryMode.Dimentions, FileSearchFilterField.Dimentions);
				fieldComboBox.Model = store;
				fieldComboBox.RowSeparatorFunc = ComboSeparatorFunc;
				fieldComboBox.Changed += FieldChanged;
				/*
				if (fieldComboBox.Model.GetIterFirst(out iter)) {
					fieldComboBox.SetActiveIter(iter);
				}
				*/

				addButton = new Button();
				addButton.Relief = ReliefStyle.None;
				addButton.Image = new Image(Gui.LoadIcon(16, "list-add"));
				addButton.Clicked += AddButtonClicked;

				removeButton = new Button();
				removeButton.Relief = ReliefStyle.None;
				removeButton.Image = new Image(Gui.LoadIcon(16, "list-remove"));
				removeButton.Clicked += RemoveButtonClicked;

				box = new HBox();
				box.PackStart(fieldComboBox, false, false, 0);
				box.PackStart(matchTypeComboBox, false, false, 3);
				box.PackStart(filterTextEntry, true, true, 0);
				box.PackStart(removeButton, false, false, 0);
				box.PackStart(addButton, false, false, 0);

				this.TopPadding = 3;
				this.BottomPadding = 3;
				this.Add(box);

				fieldComboBox.Model.GetIterFirst(out iter);
				do {
					FileSearchFilterField field = (FileSearchFilterField)fieldComboBox.Model.GetValue(iter, 2);
					if (field == filter.Field) {
						fieldComboBox.SetActiveIter(iter);
						break;
					}
				} while (fieldComboBox.Model.IterNext(ref iter));


				matchTypeComboBox.Model.GetIterFirst(out iter);
				do {
					FileSearchFilterComparison comp = (FileSearchFilterComparison)matchTypeComboBox.Model.GetValue(iter, 1);
					if (comp == filter.Comparison) {
						matchTypeComboBox.SetActiveIter(iter);
						break;
					}
				} while (matchTypeComboBox.Model.IterNext(ref iter));

				filterTextEntry.Text = filter.Text;
			}

			public FileSearchFilter Filter {
				get {
					return filter;
				}
			}

			private void FilterTextChanged (object sender, EventArgs args)
			{
				filter.Text = filterTextEntry.Text;

				if (Changed != null) {
					Changed(this, EventArgs.Empty);
				}
			}

		 	private void MatchTypeChanged (object sender, EventArgs args)
			{
				TreeIter iter;
				if (matchTypeComboBox.GetActiveIter(out iter)) {
					filter.Comparison = (FileSearchFilterComparison)matchTypeComboBox.Model.GetValue(iter, 1);
				}

				if (Changed != null) {
					Changed(this, EventArgs.Empty);
				}
			}

			private void FieldChanged (object o, EventArgs args)
			{
				matchTypeStore.Clear();

				TreeIter iter;
				fieldComboBox.GetActiveIter(out iter);

				FilterEntryMode mode = (FilterEntryMode)fieldComboBox.Model.GetValue(iter, 1);

				filter.Field = (FileSearchFilterField)fieldComboBox.Model.GetValue(iter, 2);

				filterTextEntry.Mode = mode;

				if (mode == FilterEntryMode.String) {
					matchTypeStore.AppendValues("contains", FileSearchFilterComparison.Contains);
					matchTypeStore.AppendValues("doesn't contain", FileSearchFilterComparison.DoesntContain);
					matchTypeStore.AppendValues("-");
					matchTypeStore.AppendValues("matches regexp", FileSearchFilterComparison.Regexp);
				} else if (mode == FilterEntryMode.Dimentions) {
					// Ech...
					matchTypeStore.AppendValues("is at least", FileSearchFilterComparison.GreaterOrEqualTo);
					matchTypeStore.AppendValues("is no more than", FileSearchFilterComparison.LessThanOrEqualTo);
					matchTypeStore.AppendValues("is", FileSearchFilterComparison.Equals);
				} else {
					matchTypeStore.AppendValues("is at least", FileSearchFilterComparison.GreaterOrEqualTo);
					matchTypeStore.AppendValues("is no more than", FileSearchFilterComparison.LessThanOrEqualTo);
					matchTypeStore.AppendValues("is", FileSearchFilterComparison.Equals);
				}
				
				if (matchTypeStore.GetIterFirst(out iter)) {
					matchTypeComboBox.SetActiveIter(iter);
				}

				if (Changed != null) {
					Changed(this, EventArgs.Empty);
				}
			}

			private void AddButtonClicked (object o, EventArgs args) 
			{
				FilterWidget parent = (FilterWidget)this.Parent;
				parent.AddFilter(new FileSearchFilter());
			}

			private void RemoveButtonClicked (object o, EventArgs args)
			{
				FilterWidget parent = (FilterWidget)this.Parent;
				parent.RemoveFilter(this.Filter);
			}

			private bool ComboSeparatorFunc (TreeModel m, TreeIter i)
			{
				string text = (string)m.GetValue(i, 0);
				return (text == "-");
			}

			private void FieldComboDataFunc (CellLayout layout, CellRenderer cell, TreeModel m, TreeIter i)
			{
				cell.Sensitive = (m.GetValue(i, 1) != null);
			}

			private class FilterEntry : Entry 
			{
				FakeTooltip tooltip;
				FileSearchFilter filter;
				FilterEntryMode mode;

				public FilterEntry (FileSearchFilter filter) 
				{
					this.filter = filter;
					
					tooltip = new FakeTooltip(this);
					this.FocusInEvent += this_FocusChangeEvent;
					this.FocusOutEvent += this_FocusChangeEvent;
					this.Changed += this_Changed;

					/*
					ListStore store = new ListStore(typeof(string), typeof(string));
					store.AppendValues("(VGA)", "640x480");
					store.AppendValues("(SVGA)", "800x600");
					store.AppendValues("(720p)", "1280x720");
					store.AppendValues("(1080i)", "1920x1080");

					CellRendererText textCell = new CellRendererText();

					EntryCompletion completion = new EntryCompletion();
					completion.MinimumKeyLength = 0;
					completion.PopupSingleMatch = true;
					completion.PopupCompletion = true;
					completion.TextColumn = 1;
					completion.PackStart(textCell, true);
					completion.AddAttribute(textCell, "text", 0);
					completion.Model = store;

					this.Completion = completion;
					*/
				}
	
				public FilterEntryMode Mode {
					get {
						return mode;
					}
					set {
						mode = value;
						HideShowTooltip();
					}
				}

				private void HideShowTooltip ()
				{
					UpdateTooltipText();
					if (this.HasFocus && !string.IsNullOrEmpty(tooltip.Text) && this.Text.Length > 0 && 
					    (mode != FilterEntryMode.String || 
					     (mode == FilterWidget.FilterWidgetRow.FilterEntryMode.String && 
					      filter.Comparison == FileSearchFilterComparison.Regexp )))
					{
						tooltip.Show();
						return;
					}
					tooltip.Hide();
				}

				private void this_Changed (object o, EventArgs args)
				{
					HideShowTooltip();
				}

				private void this_FocusChangeEvent (object o, EventArgs args)
				{
					HideShowTooltip();
				}

				private void UpdateTooltipText ()
				{
					string entryText = this.Text;
					
					if (mode == FilterEntryMode.String && filter.Comparison == FileSearchFilterComparison.Regexp) {
						try {
							new Regex(entryText);
							tooltip.Text = string.Empty;
						} catch (ArgumentException ex) {
							tooltip.Text = ex.Message;
						}
						return;
					} else if (mode == FilterEntryMode.Size || mode == FilterEntryMode.Speed) {

						ulong  num;
						string unitName;
						if (Common.Utils.ParseSizeString(entryText, out num, out unitName)) {
							tooltip.Text = string.Format("{0} <b>{1}{2}</b>", FormatNumber(num),
													  unitName,
													  num > 1 ? "s" : "");
							return;
						} else {
							tooltip.Text = "Please enter a valid size.";
							return;
						}

					} else if (mode == FilterEntryMode.Dimentions) {
						// Remove all whitespace
						entryText = Regex.Replace(entryText, @"\s+", "");

						// Try to parse out WxH
						Match match = Regex.Match(entryText, @"^(\d+)x(\d+)$");
						if (match.Success) {
							ulong width;
							ulong height;
							if (ulong.TryParse(match.Groups[1].Captures[0].Value, out width) && 
							    ulong.TryParse(match.Groups[2].Captures[0].Value, out height)) {
								tooltip.Text = string.Format("<b>Width:</b> {0}px, <b>Height:</b> {1}px", width, height);
								return;
							} else {
								tooltip.Text = "Size too large.";
								return;
							}
						} else {
							tooltip.Text = "Enter size as WxH.";
							return;
						}
					}

					// If we ended up here, something isn't right!
					tooltip.Text = "Failure parsing value!";
				}

				private string FormatNumber(ulong number)
				{
					System.Globalization.NumberFormatInfo nfi = new System.Globalization.CultureInfo("en-US", false ).NumberFormat;
					nfi.NumberDecimalDigits = 0;
					return number.ToString("N", nfi);
				}

				private class FakeTooltip : Window
				{
					Widget attachTo;
					Label label;

					public FakeTooltip (Widget attachTo) : base (WindowType.Popup)
					{
						this.attachTo = attachTo;

						this.SkipPagerHint = true;
						this.SkipTaskbarHint = true;
						this.Decorated = false;
						this.TypeHint = Gdk.WindowTypeHint.Tooltip;
						this.WindowPosition = WindowPosition.None;
						this.TypeHint = Gdk.WindowTypeHint.Tooltip;

						this.Resizable = false;

						this.Shown += this_Shown;

						label = new Label("Hello Mr. World!");
						this.Add(label);
						label.Show();
					}

					public string Text {
						get {
							return label.Text;
						}
						set {
							label.Markup = value;
						}
					}

					private void this_Shown (object o, EventArgs args)
					{
						Relocate();
					}

					private void Relocate ()
					{
						/*
						 * 
						int x, y, xx, yy, h;
						h = attachTo.SizeRequest().Height;
						attachTo.GdkWindow.GetRootOrigin(out x, out y);
						attachTo.GdkWindow.GetPosition(out xx, out yy);

						// This appears to be a GTK bug...
						// After hiding a window, you cannot move it
						// back to the previous position without
						// moving it somewhere else first.
						// Go figure.
						this.Move(x + xx + 1, y + yy + h + 1);

						this.Move(x + xx, y + yy + h);
						*/
						
						int windowX, windowY;
						this.attachTo.ParentWindow.GetPosition(out windowX, out windowY);
												
						int x = windowX + this.attachTo.Allocation.X;
						int y = windowY + this.attachTo.Allocation.Y + this.attachTo.Allocation.Height;
						
						this.Move(x, y);
					}
				}
			}

			private enum FilterEntryMode
			{
				String,
				Speed,
				Size,
				Dimentions
			}
		}
	}
}
