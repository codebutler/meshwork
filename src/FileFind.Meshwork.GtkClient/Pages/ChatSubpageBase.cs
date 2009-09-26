//
// ChatSubPage.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.Globalization;
using Gtk;
using GtkSpell;
using Glade;
using System.Collections;
using FileFind.Meshwork;
using GLib;
using MonoDevelop.Components;

namespace FileFind.Meshwork.GtkClient
{
	public abstract class ChatSubpageBase : GladeWidgetExtract, IPage
	{
		[Widget] protected TextView chatTextView;
		[Widget] protected TextView inputTextView;
		[Widget] protected TreeView userList;

		SpellCheck spellCheck;

		public event EventHandler UrgencyHintChanged;

		bool urgencyHint = false;
		bool isActive    = false;

		protected event EventHandler SendMessage;

		public ChatSubpageBase () : base ("FileFind.Meshwork.GtkClient.meshwork.glade", "ChatRoomWindow")
		{
			base.FocusGrabbed += base_FocusGrabbed;

			try {
				spellCheck = new SpellCheck (inputTextView, CultureInfo.CurrentCulture.Name);
			} catch (Exception ex) {
				LoggingService.LogWarning("Spell check is not avaliable because: " + ex.ToString());
			}
			
			TextTag myBoldTag = new TextTag ("Bold");
			myBoldTag.Weight = Pango.Weight.Bold;
			
			TextTag nobodysTimeTag = new TextTag ("NobodysTime");
			nobodysTimeTag.SizePoints = 7;

			TextTag myTimeTag = new TextTag ("MyTime");
			myTimeTag.Foreground = "darkblue";
			myTimeTag.SizePoints = 7;

			TextTag otherTimeTag = new TextTag ("OtherTime");
			otherTimeTag.Foreground = "darkred";
			otherTimeTag.SizePoints = 7;
							
			TextTag myNickTag = new TextTag ("MyNickname");
			myNickTag.Foreground = "darkblue";
			myNickTag.Weight = Pango.Weight.Bold;
				
			TextTag otherNickTag = new TextTag ("OtherNickname");
			otherNickTag.Foreground = "darkred";
			otherNickTag.Weight = Pango.Weight.Bold;
			
			chatTextView.Buffer.TagTable.Add (myBoldTag);
			chatTextView.Buffer.TagTable.Add (nobodysTimeTag);	
			chatTextView.Buffer.TagTable.Add (myTimeTag);
			chatTextView.Buffer.TagTable.Add (otherTimeTag);
			chatTextView.Buffer.TagTable.Add (myNickTag);
			chatTextView.Buffer.TagTable.Add (otherNickTag);
		}
		
		public bool IsActive {
			get {
				return Gui.MainWindow.Window.IsActive && Gui.MainWindow.SelectedPage == ChatsPage.Instance && isActive;
			}
			set {
				isActive = value;
			}
		}

		public bool UrgencyHint {
			get {
				return urgencyHint;
			}
			private set {
				urgencyHint = value;
				if (UrgencyHintChanged != null) {
					UrgencyHintChanged(this, EventArgs.Empty);
				}
			}
		}

		[GLib.ConnectBefore]
		private void inputTextView_KeyPressEvent (object o, KeyPressEventArgs e)
		{
			if (e.Event.Key == Gdk.Key.Return | e.Event.Key == Gdk.Key.KP_Enter) {
				e.RetVal = true;
				if (inputTextView.Buffer.Text != "") {
					SendMessage(this, EventArgs.Empty);
					inputTextView.Buffer.Text = "";
				}
			}
		}

		public virtual void Close ()
		{
			base.Destroy();
		}

		public void AddToChat (Node messageFrom, string messageText)
		{
			TextIter iter = chatTextView.Buffer.GetIterAtOffset(chatTextView.Buffer.Text.Length);
			
			if (chatTextView.Buffer.Text != "") 
				chatTextView.Buffer.Insert(ref iter, Environment.NewLine);
				 
			if (messageFrom == null) {
				chatTextView.Buffer.InsertWithTagsByName(ref iter, "(" + DateTime.Now.ToShortTimeString() + ") *** ", new string[] {"NobodysTime"});
				chatTextView.Buffer.InsertWithTagsByName(ref iter, messageText, new string[] {"Bold"});
			} else {
				if (messageFrom.NodeID == Core.MyNodeID) {
					chatTextView.Buffer.InsertWithTagsByName(ref iter,"(" + DateTime.Now.ToShortTimeString() + ") ", new string[] { "MyTime" });
				} else {
					chatTextView.Buffer.InsertWithTagsByName(ref iter,"(" + DateTime.Now.ToShortTimeString() + ") ", new string[] { "OtherTime" });
				}
					
				if (messageText.StartsWith ("/me ") == true) {
					if (messageFrom.NodeID == Core.MyNodeID) {
						chatTextView.Buffer.InsertWithTagsByName(ref iter, "* " + messageFrom.ToString(), new string[] { "MyNickname" });
					} else {
						chatTextView.Buffer.InsertWithTagsByName(ref iter, "* " + messageFrom.ToString(), new string[] { "OtherNickname" });	 	
					}
					chatTextView.Buffer.Insert(ref iter, messageText.Substring(3));
				} else {
					if (messageFrom.NodeID == Core.MyNodeID) {
						chatTextView.Buffer.InsertWithTagsByName(ref iter, messageFrom.ToString() + ": ", new string[] { "MyNickname" });
					} else {
						chatTextView.Buffer.InsertWithTagsByName(ref iter, messageFrom.ToString() + ": ", new string[] { "OtherNickname" });	 	
					}
					chatTextView.Buffer.Insert(ref iter, messageText);
				}

				if (!IsActive) {
					UrgencyHint = true;
				}
			}
			ScrollToBottom();
		}

		protected void AddInfo (string infoText)
		{
			AddToChat(null, infoText);
		}
		
		private void ScrollToBottom ()
		{
			TextMark EndMark = chatTextView.Buffer.CreateMark ("end", chatTextView.Buffer.EndIter, false);
			chatTextView.ScrollToMark (EndMark, 0.4, true, 0.0, 1.0);
			chatTextView.Buffer.DeleteMark (EndMark);
			chatTextView.QueueDraw();
		}

		private void base_FocusGrabbed (object o, EventArgs args)
		{
			inputTextView.GrabFocus ();
			UrgencyHint = false;
		}

		[GLib.ConnectBefore]
		private void chatTextView_KeyPressEvent (object o, KeyPressEventArgs args)
		{
			inputTextView.GrabFocus();
			inputTextView.ProcessEvent(args.Event);
		}
	}
}
