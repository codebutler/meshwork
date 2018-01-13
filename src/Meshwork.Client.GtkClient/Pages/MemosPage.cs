//
// MemosPage.cs:
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2005-2006 FileFind.net
// 

using System;
using Meshwork.Client.GtkClient.Menus;
using Meshwork.Client.GtkClient.Widgets;
using Meshwork.Client.GtkClient.Windows;
using Gtk;
using Meshwork.Backend.Core;

namespace Meshwork.Client.GtkClient.Pages
{
	public class MemosPage : VBox, IPage
	{
		int memoCount = 0;
		TreeView memoList;
		NetworkGroupedTreeStore<Memo> memoTreeStore;
		
		public event EventHandler UrgencyHintChanged;

		static MemosPage instance;
		public static MemosPage Instance {
			get {
				if (instance == null) {
					instance = new MemosPage();
				}
				return instance;
			}
		}

		private MemosPage ()
		{
			ScrolledWindow swindow = new ScrolledWindow();

			memoList = new TreeView ();
			swindow.Add(memoList);

			memoTreeStore = new NetworkGroupedTreeStore<Memo>(memoList);
			memoList.Model = memoTreeStore;

			TreeViewColumn column;

			column = memoList.AppendColumn("Subject",
			                               new CellRendererText(), 
					               new TreeCellDataFunc(MemoSubjectDataFunc));
			column.Expand = true;
			column.Resizable = true;

			column = memoList.AppendColumn(string.Empty,
			                               new CellRendererPixbuf(),
						       new TreeCellDataFunc(MemoAttachmentFunc));
			column.Widget = new Gtk.Image(new Gdk.Pixbuf(null, "Meshwork.Client.GtkClient.Resources.Images.attachment-col-small.png"));
			column.Widget.Show();

			column = memoList.AppendColumn("Posted By",
					               new CellRendererText (),
					               new TreeCellDataFunc (MemoByDataFunc));

			column.Resizable = true;

			column = memoList.AppendColumn("Date",
					               new CellRendererText (),
					               new TreeCellDataFunc (MemoDateDataFunc));

			column.Resizable = true;

			memoList.RowActivated += memoList_RowActivated;
			memoList.ButtonPressEvent += memoList_ButtonPressEvent;

			this.PackStart(swindow, true, true, 0);
			swindow.ShowAll();

			foreach (Network network in Runtime.Core.Networks) {
				Core_NetworkAdded (network);
			}

		    Runtime.Core.NetworkAdded +=
				(NetworkEventHandler)DispatchService.GuiDispatch(
					new NetworkEventHandler(Core_NetworkAdded)
				);
		}

		public void UpdateMemoList ()
		{
			memoList.QueueDraw ();
			Gui.MainWindow.RefreshCounts();
		}

		public bool UrgencyHint {
			get {
				return false;
			}
		}

		public int MemoCount {
			get {
				return memoCount;
			}
		}

		private void Core_NetworkAdded (Network network)
		{
			network.MemoAdded   += (MemoEventHandler) DispatchService.GuiDispatch (new MemoEventHandler(network_MemoAdded));
			network.MemoDeleted += (MemoEventHandler) DispatchService.GuiDispatch (new MemoEventHandler(network_MemoDeleted));
			network.MemoUpdated += (MemoEventHandler) DispatchService.GuiDispatch (new MemoEventHandler(network_MemoUpdated));
			
			foreach (Memo memo in network.Memos) {
				var m = memo;
				Application.Invoke(delegate {
					network_MemoAdded(network, m);
				});
			}
		}

		private Memo GetSelectedMemo ()
		{
			TreeIter iter;
			TreeModel model;
			if (memoList.Selection.GetSelected (out model, out iter) == true) {
				object item = model.GetValue (iter, 0);
				if (item is Memo) {
					return (Memo) model.GetValue (iter, 0);
				} else {
					return null;
				}
			} else {
				return null;
			}
		}

		private void memoList_RowActivated (object o, RowActivatedArgs args)
		{
			Memo selectedMemo = GetSelectedMemo ();
			if (selectedMemo != null) {
				MemoWindow viewMemoWindow = new MemoWindow (selectedMemo);
				viewMemoWindow.Show ();
			}	
		}

		[GLib.ConnectBefore]
		private void memoList_ButtonPressEvent (object o, ButtonPressEventArgs args)
		{
			TreePath path;
			if (memoList.GetPathAtPos ((int)args.Event.X, (int)args.Event.Y, out path)) {
				memoList.Selection.SelectPath (path);
			} else {
				memoList.Selection.UnselectAll ();
			}
			if (args.Event.Button == 3) {
				MemoMenu menu = new MemoMenu();
				menu.Popup(GetSelectedMemo());
			}
		}

		private void MemoAttachmentFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			object item = (object)model.GetValue (iter, 0);
			if (item is Memo) {
				// FIXME:
				// Memo memo = (Memo)item;
				//if (something) {
					//(cell as CellRendererPixbuf).Pixbuf = Gui.LoadIcon (16, "mail-attachment");
				//} else {
					(cell as CellRendererPixbuf).Pixbuf = null;
				//}
			} else {
				(cell as CellRendererPixbuf).Pixbuf = null;
			}
		}

		private void MemoSubjectDataFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			object item = (object) model.GetValue (iter, 0);
			if (item is Memo) {
				Memo memo = (Memo)item;
				(cell as CellRendererText).Text = memo.Subject;
				(cell as CellRendererText).Weight = memo.Unread ? (int)Pango.Weight.Bold : (int)Pango.Weight.Normal;
			} else {
				(cell as CellRendererText).Text = (item as Network).NetworkName;
				(cell as CellRendererText).Weight = (int)Pango.Weight.Bold;
			}
		}

		private void MemoByDataFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			object item = (object) model.GetValue (iter, 0);
			if (item is Memo) {
				Memo memo = (Memo)item;
				(cell as CellRendererText).Text = memo.Node.ToString();
				(cell as CellRendererText).Weight = memo.Unread ? (int)Pango.Weight.Bold : (int)Pango.Weight.Normal;
			} else {
				(cell as CellRendererText).Text = string.Empty;
			}
		}

		private void MemoDateDataFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			object item = (object) model.GetValue (iter, 0);
			if (item is Memo) {
				Memo memo = (Memo)item;
				(cell as CellRendererText).Text = memo.CreatedOn.ToString("g");
				(cell as CellRendererText).Weight = memo.Unread ? (int)Pango.Weight.Bold : (int)Pango.Weight.Normal;
			} else {
				(cell as CellRendererText).Text = string.Empty;
			}
		}

		private void network_MemoAdded (Network network, Memo memo)
		{
			TreeIter iter = memoTreeStore.AddItem (network, memo);
			if (Runtime.Core.IsLocalNode(memo.Node)) {
				memoList.Selection.SelectIter (iter);
				memoList.GrabFocus();
			}
			
			UpdateMemoList ();

			memoCount += 1;
		}

		private void network_MemoUpdated(Network network, Memo memo)
		{
			UpdateMemoList ();
		}

		private void network_MemoDeleted(Network network, Memo memo)
		{
			memoTreeStore.RemoveItem (network, memo);
			Gui.MainWindow.RefreshCounts();

			memoCount -= 1;
		}
	}
}
