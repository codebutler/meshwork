//
// MainSidebar.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2007 Meshwork Authors
//

using System;
using System.Collections;
using Meshwork.Client.GtkClient.Pages;
using Meshwork.Client.GtkClient.SidebarItems;
using Gtk;
using Meshwork.Backend.Feature.FileSearch;

namespace Meshwork.Client.GtkClient.Widgets
{
	public class MainSidebar : VBox
	{
		ISidebarItem selectedItem;

		ListStore topItemsStore;
		ListStore searchItemsModel;
		ListStore bottomItemsStore;

		TreeView topItemsTree;
		TreeView searchItemsTree;
		TreeView bottomItemsTree;

		public delegate void SidebarItemEventHandler (MainSidebar sidebar, ISidebarItem item);
		public event SidebarItemEventHandler ItemAdded;
		public event SidebarItemEventHandler SelectedItemChanged;

		public MainSidebar ()
		{
			FadingAlignment alignment;

			// TOP ITEMS
			topItemsStore = new ListStore(typeof (ISidebarItem));

			topItemsTree = MakeTreeView();
			topItemsTree.Model = topItemsStore;
			this.PackStart(topItemsTree, false, false, 0);
			this.PackStart(new HSeparator(), false, false, 0);

			// SEARCH HEADER
			
			alignment = new FadingAlignment();
			alignment.Add(new Label("Searches"));
			this.PackStart(alignment, false, false, 0);
			alignment.ShowAll();
			
			// SEARCH ITEMS
			
			searchItemsModel = new ListStore(typeof(ISidebarItem));
			
			ScrolledWindow scrolledWindow = new ScrolledWindow();
			scrolledWindow.VscrollbarPolicy = PolicyType.Never;

			searchItemsTree = MakeTreeView();
			searchItemsTree.ButtonPressEvent += searchItemsTree_ButtonPressEvent;
			scrolledWindow.Add(searchItemsTree);
			searchItemsTree.Model = searchItemsModel;
			this.PackStart(scrolledWindow, true, true, 0);
		
			this.PackStart(new HSeparator(), false, false, 0);

			// SPACER
			alignment = new FadingAlignment();
			alignment.HeightRequest = 5;
			this.PackStart(alignment, false, false, 0);
			alignment.ShowAll();

			// BOTTOM ITEMS

			bottomItemsStore = new ListStore(typeof(ISidebarItem));

			bottomItemsTree = MakeTreeView();
			bottomItemsTree.Model = bottomItemsStore;
			this.PackStart(bottomItemsTree, false, false, 0);
		}

		public void ClearActiveUrgency ()
		{
			if (selectedItem != null && selectedItem.PageWidget != null) {
				selectedItem.PageWidget.GrabFocus();
			}
		}

		public ISidebarItem SelectedItem {
			get {
				return selectedItem;
			}
		}

		public void AddBuiltinItems ()
		{
			AppendItem(topItemsStore, new NetworkOverviewItem());
			AppendItem(topItemsStore, new UserBrowserItem());
			AppendItem(topItemsStore, new NewSearchItem());

			/*
			AppendItem(searchItemsModel, WhatsNewSearchItem.Instance);
			AppendItem(searchItemsModel, WhatsPopularSearchItem.Instance);
			AppendItem(searchItemsModel, new SeparatorItem());
			*/

			AppendItem(bottomItemsStore, new ChatsItem());
			AppendItem(bottomItemsStore, new MemosItem());
			AppendItem(bottomItemsStore, new SeparatorItem());
			AppendItem(bottomItemsStore, new TransfersItem());
			AppendItem(bottomItemsStore, new SeparatorItem());
			AppendItem(bottomItemsStore, new ConnectionsItem());
			AppendItem(bottomItemsStore, new StatusItem());
		}

		public void SelectPage (IPage page)
		{
			ISidebarItem result = null;
			TreeView[] trees = new TreeView[] { topItemsTree, searchItemsTree, bottomItemsTree };
			foreach (TreeView tree in trees) {
				tree.Model.Foreach(delegate (TreeModel model, TreePath path, TreeIter iter) {
					ISidebarItem item = (ISidebarItem)model.GetValue(iter, 0);
					if (item.PageWidget == (Widget)page) {
						tree.Selection.SelectIter(iter);
						result = item;
						return true;
					} else {
						return false;
					}
				});
				if (result != null) {
					break;
				}
			}
		}

		// XXX: Rename
		public void RefreshCounts ()
		{
			topItemsTree.QueueDraw();
			searchItemsTree.QueueDraw();
			bottomItemsTree.QueueDraw();
		}

		private TreeView MakeTreeView ()
		{
			TreeView tree = new TreeView();
			tree.Selection.Changed += treeSelection_Changed;

			tree.Mapped += delegate (object sender, EventArgs args) {
				// Set bg color
				Gdk.Color color = Gdk.Color.Zero;
				Gdk.Color.Parse("#CFD7E2", ref color);
				//tree.ModifyBase(StateType.Normal, color);
			};

			tree.HeadersVisible = false;
			tree.CanFocus = false;

			CellRendererPixbuf pixbufCell = new CellRendererPixbuf();
			CellRendererText textCell = new CellRendererText();
			CellRendererText countTextCell = new CellRendererText();
			countTextCell.Sensitive = false;
			countTextCell.Alignment = Pango.Alignment.Right;
			countTextCell.Xalign = 1;
			CellRendererPixbuf closeCell = new CellRendererPixbuf();

			TreeViewColumn column = new TreeViewColumn();
			column.PackStart(pixbufCell, false);
			column.PackStart(textCell, true);
			column.PackStart(countTextCell, false);
			column.PackStart(closeCell, false);
			column.SetCellDataFunc(pixbufCell, new TreeCellDataFunc(ItemPixbufCellFunc));
			column.SetCellDataFunc(textCell, new TreeCellDataFunc(ItemTextCellFunc));
			column.SetCellDataFunc(countTextCell, new TreeCellDataFunc(ItemCountCellFunc));

			tree.AppendColumn(column);
			tree.ExpanderColumn = column;
			tree.RowSeparatorFunc = delegate (TreeModel m, TreeIter i) {
				return (m.GetValue(i, 0) is SeparatorItem);
			};
			
			return tree;
		}

		private void treeSelection_Changed (object o, EventArgs args)
		{
			TreeView tree = ((TreeSelection)o).TreeView;
			TreeIter iter;
			ISidebarItem thisSelectedItem = null;

			if (tree.Selection.GetSelected(out iter)) {
				thisSelectedItem = (ISidebarItem) tree.Model.GetValue(iter, 0);
				selectedItem = thisSelectedItem;
			}

			if (SelectedItemChanged != null) {
				SelectedItemChanged(this, thisSelectedItem);
			}

			if (thisSelectedItem != null) {
				TreeView[] treeviews = new TreeView[]{ topItemsTree, searchItemsTree, bottomItemsTree };
				foreach (TreeView t in treeviews) {
					if (t != null && t != tree) {
						t.Selection.UnselectAll();
					}
				}
			}
		}

		private TreeIter AppendItem (ListStore store, ISidebarItem item)
		{
			TreeIter iter = store.AppendValues(item);
			if (ItemAdded != null) {
				ItemAdded(this, item);
			}
			if (item.PageWidget != null) {
				((IPage)item.PageWidget).UrgencyHintChanged += page_UrgencyHintChanged;
			}
			return iter;
		}

		public void AddNewSearch (FileSearch search)
		{
			FileSearchItem item = new FileSearchItem(search);
			TreeIter i = AppendItem(searchItemsModel, item);
			searchItemsTree.Selection.SelectIter(i);
			item.PageWidget.GrabFocus();
		}

		public void RemoveSearch (FileSearch search)
		{
			TreeIter iter;
			if (searchItemsModel.GetIterFirst(out iter)) {
				do {
					ISidebarItem item = (ISidebarItem)searchItemsModel.GetValue(iter, 0);
					if (item is FileSearchItem && ((FileSearchItem)item).Search == search) {
						searchItemsModel.Remove(ref iter);
						item.Destroy();
						return;
					}
				} while (searchItemsModel.IterNext(ref iter));
			}

			throw new InvalidOperationException("Unknown search.");
		}

		public IEnumerable FileSearches ()
		{
			TreeIter iter;
			searchItemsModel.GetIterFirst(out iter);
			do {
				ISidebarItem item = (ISidebarItem)searchItemsModel.GetValue(iter, 0);
				if (item is FileSearchItem) {
					yield return item;
				}
			} while (searchItemsModel.IterNext(ref iter));
		}

		private void ItemPixbufCellFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			ISidebarItem item = (ISidebarItem) model.GetValue(iter, 0);
			(cell as CellRendererPixbuf).Pixbuf = item.Icon;
		}
	
		private void ItemTextCellFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			ISidebarItem item = (ISidebarItem) model.GetValue(iter, 0);
			if (item.PageWidget != null && ((IPage)item.PageWidget).UrgencyHint)
				(cell as CellRendererText).Markup = "<b>" + item.Name + "</b>";
			else
				(cell as CellRendererText).Text = item.Name;
		}
	
		private void ItemCountCellFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			ISidebarItem item = (ISidebarItem) model.GetValue(iter, 0);
			if (item.Count > -1) {
				if (item.PageWidget != null && ((IPage)item.PageWidget).UrgencyHint)
					(cell as CellRendererText).Markup = "<b>" + Common.Utils.FormatNumber(item.Count) + "</b>";
				else
					(cell as CellRendererText).Text = Common.Utils.FormatNumber(item.Count);
			} else {
				(cell as CellRendererText).Text = string.Empty;
			}
		}

		private void page_UrgencyHintChanged (object o, EventArgs args)
		{
			RefreshCounts();

			Window mainWindow = (Window)this.Toplevel;
			if (!mainWindow.IsActive) {
				// YUCK!!
				bool urgencyHint = false;
				TreeView[] trees = new TreeView[] { topItemsTree, searchItemsTree, bottomItemsTree };
				foreach (TreeView tree in trees) {
					tree.Model.Foreach(delegate (TreeModel model, TreePath path, TreeIter iter) {
						ISidebarItem item = (ISidebarItem)model.GetValue(iter, 0);
						if (item.PageWidget != null && ((IPage)item.PageWidget).UrgencyHint == true) {
							urgencyHint = true;
							return true;
						} else {
							return false;
						}
					});
					if (urgencyHint) {
						Gui.SetWindowUrgencyHint(mainWindow, true);
						return;
					}
				}
				Gui.SetWindowUrgencyHint(mainWindow, false);
			}
		}

		[GLib.ConnectBefore]
		private void searchItemsTree_ButtonPressEvent (object sender, ButtonPressEventArgs args)
		{
			TreePath path;
			TreeIter iter;

			if (searchItemsTree.GetPathAtPos((int)args.Event.X, (int)args.Event.Y, out path)) {
				searchItemsTree.Selection.SelectPath(path);
			} else {
				searchItemsTree.Selection.UnselectAll();
			}

			if (args.Event.Button == 3) {
				FileSearchItem selectedItem = null;
				if (searchItemsTree.Selection.GetSelected(out iter)) {
					selectedItem = (FileSearchItem)searchItemsModel.GetValue(iter, 0);
				}
				
				if (selectedItem != null) {
					if (selectedItem is WhatsPopularSearchItem || selectedItem is WhatsNewSearchItem) {
						Runtime.BuiltinActions["SidebarRemoveSearch"].Sensitive = false;
					} else {
						Runtime.BuiltinActions["SidebarRemoveSearch"].Sensitive = true;
					}
					Runtime.BuiltinActions["SidebarSearchAgain"].Sensitive = true;
				} else {
					Runtime.BuiltinActions["SidebarRemoveSearch"].Sensitive = false;
					Runtime.BuiltinActions["SidebarSearchAgain"].Sensitive = false;
				}

				Menu menu = (Menu)Runtime.UIManager.GetWidget("/SidebarSearchPopupMenu");	
				menu.Popup();
			}
		}
	}
}
