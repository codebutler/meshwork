//
// MemosPage.cs:
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2005-2006 FileFind.net
// 

using Gtk;
using System;
using FileFind.Meshwork;

namespace FileFind.Meshwork.GtkClient 
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

			column = memoList.AppendColumn(String.Empty,
			                               new CellRendererPixbuf(),
						       new TreeCellDataFunc(MemoAttachmentFunc));
			column.Widget = new Gtk.Image(Gui.LoadIcon(16, "attachment-col"));
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

			foreach (Network network in Core.Networks) {
				Core_NetworkAdded (network);
			}

			Core.NetworkAdded +=
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
				winViewMemo viewMemoWindow = new winViewMemo (selectedMemo);
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
			object item = (object) model.GetValue (iter, 0);
			if (item is Memo) {
				Memo memo = (Memo)item;
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
				(cell as CellRendererText).Text = memo.Network.Nodes [memo.WrittenByNodeID].ToString ();
				(cell as CellRendererText).Weight = memo.Unread ? (int)Pango.Weight.Bold : (int)Pango.Weight.Normal;
			} else {
				(cell as CellRendererText).Text = String.Empty;
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
				(cell as CellRendererText).Text = String.Empty;
			}
		}

		private void network_MemoAdded (Network network, Memo memo)
		{
			TreeIter iter = memoTreeStore.AddItem (network, memo);
			if (memo.WrittenByNodeID == Core.MyNodeID) {
				memoList.Selection.SelectIter (iter);
				memoList.GrabFocus();
			}

			LogManager.Current.WriteToLog ("Memo added: " + memo.Subject  + " by: " + network.Nodes[memo.WrittenByNodeID].ToString());
			UpdateMemoList ();

			memoCount += 1;
		}

		private void network_MemoUpdated(Network network, Memo memo)
		{
			LogManager.Current.WriteToLog ("Memo updated: " + memo.Subject  + " by: " + network.Nodes[memo.WrittenByNodeID].ToString());
			UpdateMemoList ();
		}

		private void network_MemoDeleted(Network network, Memo memo)
		{
			memoTreeStore.RemoveItem (network, memo);
			Gui.MainWindow.RefreshCounts();
			LogManager.Current.WriteToLog("Memo deleted: " + memo.Subject);

			memoCount -= 1;
		}
	}
}
