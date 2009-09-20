//
// MemoWindow.cs: View memo window
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2005-2006 FileFind.net
// 

using System;
using System.Collections;
using Gtk;
using Glade;
using GLib;
using FileFind.Meshwork;
using FileFind.Meshwork.Filesystem;

namespace FileFind.Meshwork.GtkClient
{
	public class MemoWindow : GladeWindow
	{
		[Widget] Label lblSubject;
		[Widget] Label lblPostedBy;
		[Widget] Label lblDate;
		[Widget] TextView txtMemo;
		[Widget] HBox hboxFilesList;
		[Widget] TreeView fileList;
		[Widget] Label lblSignatureStatus;
		[Widget] Label lblSignatureInfo;
		[Widget] EventBox eventbox2;
		[Widget] Label lblExpireDate;
		[Widget] Alignment alignmentSignatureInfo;
		[Widget] Label networkLabel;
		
		ListStore fileListStore;
		
		Memo memo;

		public MemoWindow (Memo memo) : base ("MemoWindow")
		{
			Node theNode = memo.Network.Nodes[memo.WrittenByNodeID];
			lblSubject.Markup = "<b>" + memo.Subject + "</b>";
			lblPostedBy.Text = theNode.ToString();
			lblDate.Text = memo.CreatedOn.ToString();
			lblExpireDate.Text = "Expires: " + memo.CreatedOn.AddDays(2).ToString();
			txtMemo.Buffer.Text = memo.Text;
			base.Window.Title = memo.Subject;
			networkLabel.Text = memo.Network.NetworkName;

			this.memo = memo;
		
			eventbox2.ModifyBg(StateType.Normal, new Gdk.Color(0xff,0xff,0xff));

			if (!memo.Network.TrustedNodes.ContainsKey(memo.WrittenByNodeID)) {
				if (memo.WrittenByNodeID == memo.Network.LocalNode.NodeID) {
					alignmentSignatureInfo.Visible = false;
				} else {
					lblSignatureStatus.Markup = "<b>Unable to verify digital signature (Reason: node not trusted)</b>";
					lblSignatureInfo.Text = "Meshwork cannot verify the authenticity of this memo.";
				}
			} else {
				lblSignatureStatus.Markup = "<b>This memo has a valid digital signature.</b>";
			lblSignatureInfo.Markup = "<span underline=\"single\" foreground=\"blue\">" + theNode.NickName + " (" + theNode.NodeID + ")</span>";
			}
		
			fileListStore = new ListStore(typeof(string),typeof(string));
			fileList.Model = fileListStore;
			fileList.AppendColumn("FileName", new CellRendererText(), "text",0);
			fileList.HeadersVisible = false;
			
			/*
			if (memo.FileLinks.Count > 0) {
				foreach (FileLink thisFile in memo.FileLinks) {
					fileListStore.AppendValues ( new object[] { thisFile.FileName + " (" + FileFind.Common.FormatBytes(thisFile.FileSize) + ")", thisFile.FilePath });
				}
				hboxFilesList.Visible = true;
			}
			*/
		}
		
		public void on_btnClose_clicked (object sender, EventArgs args)
		{
			base.Close();
		}
	}
}
