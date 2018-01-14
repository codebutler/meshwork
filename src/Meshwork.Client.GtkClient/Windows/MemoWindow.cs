//
// MemoWindow.cs: View memo window
//
// Author:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2005-2006 Meshwork Authors
// 

using System;
using Glade;
using Gtk;
using Meshwork.Backend.Core;

namespace Meshwork.Client.GtkClient.Windows
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
		[Widget] Alignment alignmentSignatureInfo;
		[Widget] Label networkLabel;
		[Widget] Button signedByButton;
		[Widget] Image signatureImage;
		
		ListStore fileListStore;
		
		Memo memo;

		public MemoWindow (Memo memo) : base ("MemoWindow")
		{			
			lblSubject.Markup = string.Format("<b>{0}</b>", GLib.Markup.EscapeText(memo.Subject));
			lblPostedBy.Text = memo.Node.ToString();
			lblDate.Text = memo.CreatedOn.ToString();
			txtMemo.Buffer.Text = memo.Text;
			base.Window.Title = memo.Subject;
			networkLabel.Text = memo.Network.NetworkName;

			this.memo = memo;
		
			eventbox2.ModifyBg(StateType.Normal, new Gdk.Color(0xff,0xff,0xff));

			if (!memo.Network.TrustedNodes.ContainsKey(memo.Node.NodeID)) {
				if (Runtime.Core.IsLocalNode(memo.Node)) {
					alignmentSignatureInfo.Visible = false;
				} else {
					lblSignatureStatus.Markup = "<b>Unable to verify digital signature (Node not trusted)</b>";
					signedByButton.Sensitive = false;
					signatureImage.IconName = "dialog-warning";
				}
			} else {
				lblSignatureStatus.Markup = "<b>This memo has a valid digital signature.</b>";				
			}
			lblSignatureInfo.Text = string.Format("{0} ({1})", memo.Node.NickName, memo.Node.NodeID);
		
			fileListStore = new ListStore(typeof(string),typeof(string));
			fileList.Model = fileListStore;
			fileList.AppendColumn("FileName", new CellRendererText(), "text",0);
			fileList.HeadersVisible = false;
			
			/*
			if (memo.FileLinks.Count > 0) {
				foreach (FileLink thisFile in memo.FileLinks) {
					fileListStore.AppendValues ( new object[] { thisFile.FileName + " (" + Utils.FormatBytes(thisFile.FileSize) + ")", thisFile.FilePath });
				}
				hboxFilesList.Visible = true;
			}
			*/
		}
		
		void HandleSignedByButtonClicked (object sender, EventArgs args)
		{
			UserInfoDialog dialog = new UserInfoDialog(base.Window, memo.Network, memo.Node);
			dialog.Run();
		}
		
		public void on_btnClose_clicked (object sender, EventArgs args)
		{
			base.Close();
		}
	}
}
