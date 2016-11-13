//
// EditMemoDialog.cs: Post/edit memos
// 
// Authors:
// 	Eric Butler <eric@extermeboredom.net>
//
// This file is part of the Meshwork project
// Copyright (C) 2005-2006 FileFind.net
//

using System;
using Glade;
using Gtk;
using Meshwork.Backend.Core;

namespace Meshwork.Client.GtkClient.Windows
{
	public class EditMemoDialog : GladeDialog
	{
		/* private variables */
		[Widget] Entry subjectEntry;
		[Widget] TextView memoTextView;
		[Widget] TreeView fileList;
		[Widget] ComboBox networksComboBox;
		ListStore networksListStore;
		ListStore fileListStore;
		Memo theMemo;

		/* public constructors */
		public EditMemoDialog (Window parentWindow, Memo editMemo) : base (parentWindow, "EditMemoDialog")
		{
			SharedConstructor ();
			theMemo = editMemo;
			subjectEntry.Text = editMemo.Subject;
			memoTextView.Buffer.Text = editMemo.Text;
			Dialog.Title = "Edit Memo";

			TreeIter iter;
			if (networksListStore.GetIterFirst (out iter)) {
				do {
					Network thisNetwork = networksListStore.GetValue(iter, 0) as Network;
					if (thisNetwork == editMemo.Network) {
						networksComboBox.SetActiveIter (iter);
						networksComboBox.Sensitive = false;
						break;
					}
				} while (networksListStore.IterNext (ref iter));
			}

			//foreach (FileLink f in theMemo.FileLinks) {
				//Dim newItem As ListViewItem = lstvFiles.Items.Add(f.FileName)
				//newItem.SubItems.Add(SetBytes(f.FileSize))
				//newItem.Tag = f
			//}
		}
		
		public EditMemoDialog (Window parentWindow) : base (parentWindow, "EditMemoDialog")
		{
			SharedConstructor ();
			subjectEntry.Text = "";
			memoTextView.Buffer.Text = "";
			//theMemo = new Memo ();
			Dialog.Title = "Post New Memo";
		}

		private void SharedConstructor ()
		{
			fileListStore = new ListStore(typeof(string));
			fileList.Model = fileListStore;
			TreeViewColumn theCol = fileList.AppendColumn("File Attachments", new CellRendererText(), "text", 0 );
			theCol.Sizing = TreeViewColumnSizing.Autosize;

			networksListStore = new ListStore(typeof (object));
			networksListStore.AppendValues(new object());
			foreach (Network network in Runtime.Core.Networks) {
				networksListStore.AppendValues (network);
			}
			networksComboBox.Clear ();
			CellRendererText networkNameCell = new CellRendererText ();
			networksComboBox.PackStart (networkNameCell, true);
			networksComboBox.SetCellDataFunc (networkNameCell, NetworkTextFunc);
			networksComboBox.Model = networksListStore;
			
			networksComboBox.Active = Math.Min(networksListStore.IterNChildren(), 1);
		}
		
		/* private methods */
		private void on_postButton_clicked (object sender, EventArgs e)
		{
			Network network = GetSelectedNetwork();
			
			if (network == null) {
				Gui.ShowMessageDialog ("You must select a network.", Dialog);
				return;
			}
			/*theMemo.FileLinks.Clear();
			
			foreach (object[] row in fileListStore) {
				theMemo.FileLinks.Add(row[0].ToString());
			}*/

			bool isNew = false;
			
			if (theMemo == null) {
				foreach (Memo m in network.Memos) {
					if (m.Subject == subjectEntry.Text & Runtime.Core.IsLocalNode(m.Node)) {
						Gui.ShowMessageDialog ("You have already posted a memo with the specified subject, please select another.",Dialog);
						return;
					}
				}
				theMemo = new Memo(network);
				isNew = true;
			}

			theMemo.Subject = subjectEntry.Text;
			theMemo.Text = memoTextView.Buffer.Text;
			theMemo.Sign ();
		 
			network.PostMemo (theMemo);
			
			if (isNew)
				Gui.ShowMessageDialog("Your memo has been posted.", base.Dialog);
			else
				Gui.ShowMessageDialog("Your memo has been updated.", base.Dialog);
		
			Dialog.Respond(ResponseType.Ok);
			Dialog.Hide();
		}
		
		private void on_cancelButton_clicked (object sender, EventArgs e)
		{
			Dialog.Respond (ResponseType.Cancel);
			Dialog.Hide ();
		}	
		
		
		private void on_addFileButton_clicked (object o, EventArgs e)
		{
		/*	FileSelector fileSelector = new FileSelector();
			if (fileSelector.Run() == (int)ResponseType.Ok) {
				fileListStore.AppendValues ( new object[] { fileSelector.Filename } );
			}
			fileSelector.Destroy();
		*/
		}
		
		private void on_removeFileButton_clicked(object o, EventArgs e)
		{
			TreeModel model;
			TreeIter iter;
			fileList.Selection.GetSelected(out model, out iter);
			if (fileListStore.IterIsValid(iter) == true)
				fileListStore.Remove(ref iter);
		}

		private Network GetSelectedNetwork ()
		{
			TreeIter iter;
			if (networksComboBox.GetActiveIter (out iter)) {
				return networksListStore.GetValue (iter, 0) as Network;
			} 
			return null;
		}
		
		private void NetworkTextFunc (CellLayout layout, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			Network network = (model.GetValue (iter, 0) as Network);
			if (network != null) {
				(cell as CellRendererText).Text = network.NetworkName;
				(cell as CellRendererText).Sensitive = true;
			} else {
				(cell as CellRendererText).Text = "(Select a network)";
				(cell as CellRendererText).Sensitive = false;
			}
		}
	}
}
