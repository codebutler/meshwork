using System;
using System.Threading;
using System.Reflection;
using Gtk;
using Glade;
using FileFind.Meshwork;
using FileFind.Meshwork.GtkClient;

namespace FileFind.Meshwork.DebugPlugin
{
	public class DebugWindow  : GladeWindow
	{
		DebugPlugin plugin;

		[Widget] ComboBox         networkComboBox;
		[Widget] CheckButton      enableTrafficLogCheckButton;
		[Widget] TreeView         trafficTreeView;
		[Widget] TextView         messageTextView;
		[Widget] RadioToolButton  showAllToolButton;
		[Widget] RadioToolButton  showIncomingToolButton;
		[Widget] RadioToolButton  showOutgoingToolButton;
		[Widget] ToggleToolButton showPingPongToolButton;
		[Widget] Label            networkLabel;
		[Widget] Label            fromLabel;
		[Widget] Label            toLabel;
		[Widget] Label            typeLabel;
		[Widget] Label            idLabel;
		[Widget] Label            timestampLabel;

		[Widget] ComboBox   messageSenderToComboBox;
		[Widget] SpinButton messageSenderSizeSpinButton;
		[Widget] Button     messageSenderSendButton;
		[Widget] TextView   messageSenderLogTextView;

		ListStore       store;
		TreeModelFilter filter;

		public DebugWindow (DebugPlugin plugin) :
			base (Assembly.GetAssembly(typeof(DebugWindow)), "debugplugin.glade", "DebugWindow")
		{
			this.plugin = plugin;

			trafficTreeView.Selection.Changed += SelectionChanged;

			trafficTreeView.AppendColumn("", new CellRendererText(), new TreeCellDataFunc(IconFunc));
			trafficTreeView.AppendColumn("To/From", new CellRendererText(), new TreeCellDataFunc(ToFromFunc));
			trafficTreeView.AppendColumn("Type", new CellRendererText(), new TreeCellDataFunc(TypeFunc));

			store = new ListStore(typeof(MessageInfo));

			filter = new TreeModelFilter(store, null);
			filter.VisibleFunc = filter_VisibleFunc;

			trafficTreeView.Model = filter;

			ListStore networkStore = new ListStore(typeof(string), typeof(Network));
			networkStore.AppendValues("All Networks", null);
			networkComboBox.Model = networkStore;
			networkComboBox.Active = 0;

			foreach (Network network in Core.Networks) {
				Core_NetworkAdded(network);
			}

			Core.NetworkAdded += Core_NetworkAdded;
			Core.NetworkRemoved += Core_NetworkRemoved;

			ReloadMessages();

			enableTrafficLogCheckButton.Active = true;
			plugin.EnableTrafficLog = true;

			((ToolItem)base.GetWidget("toolbutton2")).Expand = true;

			messageSenderToComboBox.Clear();
			messageSenderToComboBox.Model = new ListStore(typeof(string), typeof(Network), typeof(Node));
			CellRendererText textCell = new CellRendererText();
			messageSenderToComboBox.PackStart(textCell, true);
			messageSenderToComboBox.AddAttribute(textCell, "markup", 0);
		}

		public void AddMessage (MessageInfo info)
		{
			Application.Invoke(delegate {
				store.AppendValues(info);
			});
		}

		public void ReloadMessages ()
		{
			store.Clear();

			foreach (MessageInfo info in plugin.Messages) {
				AddMessage(info);
			}
		}

		private void Core_NetworkAdded (Network network)
		{
			Application.Invoke(delegate {
				((ListStore)networkComboBox.Model).AppendValues(network.NetworkName, network);
			});

			network.UserOnline += network_UserOnline;
		}

		private void Core_NetworkRemoved (Network network)
		{
			network.UserOnline -= network_UserOnline;
		}

		private void network_UserOnline (Network network, Node node)
		{
			Application.Invoke(delegate {
				string text = String.Format("{0} ({1})", node.NickName, network.NetworkName);
				Console.WriteLine("Hello ! " + text);

				((ListStore)messageSenderToComboBox.Model).AppendValues(text, network, node);
				Console.WriteLine("Added!!");
			});
		}

		private void networkComboBox_changed_cb (object sender, EventArgs args)
		{
			filter.Refilter();
		}

		private void IconFunc (TreeViewColumn tree_column, CellRenderer cell,
				       TreeModel tree_model, TreeIter iter)
		{
			MessageInfo info = (MessageInfo)tree_model.GetValue(iter, 0);
			if (info is SentMessageInfo) {
				(cell as CellRendererText).Text = "Out";
			} else {
				(cell as CellRendererText).Text = "In";
			}
		}

		private void ToFromFunc (TreeViewColumn tree_column, CellRenderer cell,
					 TreeModel tree_model, TreeIter iter)
		{
			MessageInfo info = (MessageInfo)tree_model.GetValue(iter, 0);

			if (info != null) {
				
				/*
				Node node = null;

				if (info.Connection.Transport.Network != null) {
					if (info is SentMessageInfo) {
						node = info.Connection.Transport.Network.Nodes[info.Message.To];
					} else {
						node = info.Connection.Transport.Network.Nodes[info.Message.From];
					}
					(cell as CellRendererText).Text = node.ToString();
				} else { */

					string nodeId = null;

					if (info is SentMessageInfo) {
						nodeId = info.Message.To;
					} else {
						nodeId = info.Message.From;
					}
					
					if (info.Connection.Transport.Network != null && info.Connection.Transport.Network.Nodes.ContainsKey(nodeId)) {
						Node node = info.Connection.Transport.Network.Nodes[nodeId];
						(cell as CellRendererText).Text = node.ToString();
					} else if (nodeId == Network.BroadcastNodeID) {
						(cell as CellRendererText).Text = "(Broadcast)";
					} else {
						(cell as CellRendererText).Text = String.Format("({0})", nodeId);
					}
				/*}*/
			}
		}

		private void TypeFunc (TreeViewColumn tree_column, CellRenderer cell,
				       TreeModel tree_model, TreeIter iter)
		{
			MessageInfo info = (MessageInfo)tree_model.GetValue(iter, 0);
			if (info != null) {
				(cell as CellRendererText).Text = info.Message.Type.ToString();
			}
		}

		private void SelectionChanged (object sender, EventArgs args)
		{
			TreeIter iter;
			if (trafficTreeView.Selection.GetSelected(out iter)) {
				MessageInfo info = (MessageInfo)trafficTreeView.Model.GetValue(iter, 0);
				try {
					networkLabel.Text   = info.Connection.Transport.Network.NetworkName;
					fromLabel.Text      = info.Message.From;
					toLabel.Text        = info.Message.To;
					typeLabel.Text      = info.Message.Type.ToString();
					idLabel.Text        = info.Message.MessageID.ToString();
					timestampLabel.Text = Common.ParseUnixTimestamp(info.Message.Timestamp).ToString();

					object content = info.Message.Content;
					string typeName = (content != null) ? content.GetType().ToString() : "null";
					string json = FileFind.Serialization.JSON.Serialize(content);
					json = FileFind.JSONFormatter.FormatJSON(json);
					json = json.Replace("\t", "  ");
					messageTextView.Buffer.Text = "// " + typeName + Environment.NewLine + json;
				} catch (Exception ex) {
					messageTextView.Buffer.Text = "Unable to display message content:\n" + ex.ToString();
				}
			}
		}

		private void enableTrafficLogCheckButton_toggled_cb (object sender, EventArgs args)
		{
			plugin.EnableTrafficLog = enableTrafficLogCheckButton.Active;
		}

		private void clearToolButton_clicked_cb (object sender, EventArgs args)
		{
			store.Clear();
		}

		private void showAllToolButton_toggled_cb (object sender, EventArgs args)
		{
			filter.Refilter();
		}

		private void showIncomingToolButton_toggled_cb (object sender, EventArgs args)
		{
			filter.Refilter();
		}

		private void showOutgoingToolButton_toggled_cb (object sender, EventArgs args)
		{
			filter.Refilter();
		}

		private void showPingPongToolButton_toggled_cb (object sender, EventArgs args)
		{
			filter.Refilter();
		}

		private bool filter_VisibleFunc (TreeModel model, TreeIter iter)
		{
			MessageInfo info = (MessageInfo)model.GetValue(iter, 0);

			if (info == null) {
				return false;
			}

			TreeIter itz;
			if (networkComboBox.GetActiveIter(out itz)) {
				Network selectedNetwork = (Network)networkComboBox.Model.GetValue(itz, 1);
				if (selectedNetwork != null) {
					if (info.Connection.Transport.Network != selectedNetwork) {
						return false;
					}
				}
			}

			if (info.Message.Type == MessageType.Ping || info.Message.Type == MessageType.Pong) {
				return (showPingPongToolButton.Active);
			}

			if (info is SentMessageInfo) {
				return (showAllToolButton.Active || showOutgoingToolButton.Active);
			} else if (info is ReceivedMessageInfo) {
				return (showAllToolButton.Active || showIncomingToolButton.Active);
			}

			return false;
		}

		private void gcButton_clicked_cb (object sender, EventArgs args)
		{
			System.GC.Collect();
		}

		private void messageSenderSendButton_Clicked (object sender, EventArgs args)
		{
			TreeIter iter;
			if (messageSenderToComboBox.GetActiveIter(out iter)) {

				Network network = (Network)messageSenderToComboBox.Model.GetValue(iter, 1);
				Node node = (Node)messageSenderToComboBox.Model.GetValue(iter, 2);

				for (int x = 0; x < 10; x++) {
					Thread thread = new Thread(delegate () {
						try {
							for (int y = 0; y < 10; y++) {
								double kilobytes = messageSenderSizeSpinButton.Value;
								string data = new String('X', (int)kilobytes * 1024);

								Message message = new Message(network, MessageType.Test);
								message.To = node.NodeID;
								message.Content = data;

								AckMethod m = new AckMethod();
								m.Method += MessageSent;
								network.AckMethods.Add(message.MessageID, m);

								network.SendRoutedMessage(message);
							
								Gtk.Application.Invoke(delegate {
									messageSenderLogTextView.Buffer.Text += "\nSending...";
								});
							}
						} catch (Exception ex) {
							Gtk.Application.Invoke(delegate {
								messageSenderLogTextView.Buffer.Text += "\n" + ex;
							});
						}
					});
					thread.Start();
				}
			}
		}

		private void MessageSent (DateTime timeReceived, object[] args)
		{
			Application.Invoke(delegate {
				messageSenderLogTextView.Buffer.Text += "\nSent!!";
			});
		}
	}
}
