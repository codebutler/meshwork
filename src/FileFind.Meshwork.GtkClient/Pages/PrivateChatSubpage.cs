//
// ChatRoomSubpage.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using Gtk;

namespace FileFind.Meshwork.GtkClient.Pages
{
	public class PrivateChatSubpage : ChatSubpageBase
	{
		Node            node;
		TrustedNodeInfo trustedNodeInfo;
		Network         network;
		ListStore       userListStore;

		public PrivateChatSubpage (Network network, Node node) : base ()
		{
			this.node = node;
			this.network = network;
			this.trustedNodeInfo = network.TrustedNodes[node.NodeID];

			if (trustedNodeInfo == null) {
				throw new Exception("Cannot have a private conversation with an untrusted node.");
			}

			base.userList.Parent.Visible = false;

			base.SendMessage += base_SendMessage;

			AddToChat(null, string.Format("Now talking with {0} ({1}).", trustedNodeInfo.Identifier, Common.FormatFingerprint(trustedNodeInfo.NodeID)));
			AddToChat(null, "This conversation is secure.");
		}
	
		public void SetUserOffline ()
		{
			AddInfo(node + " is offline.");
			inputTextView.Sensitive = false;
		}

		public void SetUserOnline ()
		{
			AddInfo(node + " is online.");
			inputTextView.Sensitive = true;
		}
		
		public void UserInfoChanged (string oldNick)
		{
			if (oldNick != null) {
				AddToChat(null, string.Format("{0} is now known as {1}", oldNick, node.NickName));
			}
		}

		public Node Node {
			get {
				return node;
			}
		}

		public override void Close ()
		{
			Gui.RemovePrivateMessageWindow(network, node);
			base.Close();
		}

		private void base_SendMessage (object sender, EventArgs args)
		{
			AddToChat(network.LocalNode, inputTextView.Buffer.Text);

			Message message = network.SendPrivateMessage(node, inputTextView.Buffer.Text);
			
			AckMethod method = new AckMethod ();
			method.Method += (AckMethod.MethodEventHandler)DispatchService.GuiDispatch(new AckMethod.MethodEventHandler(OnMessageReceived));
			method.MessageID = message.MessageID;
			network.AckMethods.Add (method.MessageID, method);
		
			LoggingService.LogDebug("Sending message...");
		}

		private void OnMessageReceived (DateTime timeReceived, object[] args)
		{
			LoggingService.LogDebug("Your last message was successfully delivered.");
		}
	}
}
