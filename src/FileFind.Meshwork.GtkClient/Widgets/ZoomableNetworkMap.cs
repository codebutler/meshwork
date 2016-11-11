//
// ZoomableNetworkMap.cs: Bringin' sexy back...
//
// Author:
//   Idan Gazit <idan@fastmail.fm>
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net/)
// 

using System;
using System.Collections;
using System.Net;
using Cairo;
using FileFind.Meshwork.GtkClient.Menus;
using Gtk;

namespace FileFind.Meshwork.GtkClient.Widgets
{
	public class NodeGroup
	{
		private ArrayList nodes;
		private Cairo.PointD position;
		private SizeD dimension;
		private string name;

		public NodeGroup ()
		{
			nodes = ArrayList.Synchronized(new ArrayList());
			name = string.Empty;
			position = new PointD(0, 0);
			dimension = new SizeD(0, 0);
		}

		public NodeGroup (string name, PointD position)
		{
			nodes = ArrayList.Synchronized(new ArrayList());
			this.name = name;
			this.position = position;
			this.dimension = new SizeD(0, 0);
		}

		public ArrayList Nodes {
			get { return this.nodes; }
		}

		public PointD Position {
			get { return this.position; }
			set { this.position = value; }
		}

		public SizeD Dimension {
			get { return this.dimension; }
			set { this.dimension = value; }
		}

		public string Name {
			get { return this.name; }
			set { this.name = value; }
		}
	}

	public struct SizeD
	{
		public double Width, Height;

		public SizeD (double width, double height)
		{
			this.Width = width;
			this.Height = height;
		}
	}

	public class ZoomableNetworkMap : ZoomableCairoArea
	{
		bool debug = false;

		#region members
		private ArrayList networkZOrder;

		private NodeGroup selectedGroup;
		private Network selectedNetwork;

		private Node selectedNode;
		private Node hoverNode;
		private bool nodeDragging;
		private double nodeDraggingLastX, nodeDraggingLastY;
		private Random rng;
		#endregion

		#region colors
		private readonly Cairo.Color lightGray = new Cairo.Color(0.8, 0.8, 0.8, 0.8);
		private readonly Cairo.Color mediumGray = new Cairo.Color(0.4, 0.4, 0.4, 1.0);
		private readonly Cairo.Color darkGray = new Cairo.Color(0.3, 0.3, 0.3, 1.0);
		private readonly Cairo.Color lightAlphaGray = new Cairo.Color(0.8, 0.8, 0.8, 0.5);
		private readonly Cairo.Color mediumAlphaGray = new Cairo.Color(0.5, 0.5, 0.5, 0.5);
		private readonly Cairo.Color darkAlphaGray = new Cairo.Color(0.1, 0.1, 0.1, 0.8);
		private readonly Cairo.Color black = new Cairo.Color(0, 0, 0, 1);
		private readonly Cairo.Color white = new Cairo.Color(1, 1, 1, 1);
		private readonly Cairo.Color alphaRed = new Cairo.Color(1, 0, 0, 0.2);
		private readonly Cairo.Color alphaBlue = new Cairo.Color(0, 0, 1, 0.2);
		private readonly Cairo.Color connectionGreen = new Cairo.Color(0, 1, 0, 0.5);
		private readonly Cairo.Color orangeOverlay = new Cairo.Color(1.0, 0.7, 0.0, 0.4);
		private readonly Cairo.Color transparent = new Cairo.Color(0, 0, 0, 0);
		private readonly Cairo.Color networkBG = new Cairo.Color(0.6, 0.6, 0.6, 1.0);
		#endregion

		#region constants
		private static readonly double AvatarDimension = 40.0;
		private static readonly SizeD AvatarSizeD = new SizeD(AvatarDimension, AvatarDimension);
		private static readonly double HalfAvatarDimension = AvatarDimension / 2.0;
		private static readonly double Padding = 4.0;
		private static readonly double NodegroupNameFontSize = 14.0;
		private static readonly double NetworkNameFontSize = 20.0;
		private static Gdk.Pixbuf noticeIcon;
		#endregion

		public delegate void NodeEventHandler (Node selectedNode);
		public event NodeEventHandler SelectedNodeChanged;
		public event NodeEventHandler NodeDoubleClicked;
		public event ErrorEventHandler Error;

		public ZoomableNetworkMap ()
		{
			noticeIcon = Gui.LoadIcon(24, "dialog-information");
			rng = new Random();
			
			networkZOrder = ArrayList.Synchronized(new ArrayList());
			
			selectedGroup = null;
			nodeDragging = false;
			nodeDraggingLastX = 0;
			nodeDraggingLastY = 0;
			
			Core.NetworkAdded += (NetworkEventHandler)DispatchService.GuiDispatch(new NetworkEventHandler(AddNetwork));
			Core.NetworkRemoved += (NetworkEventHandler)DispatchService.GuiDispatch(new NetworkEventHandler(DeleteNetwork));
			
			foreach (Network network in Core.Networks) {
				AddNetwork(network);
			}
			
			base.ButtonPressEvent += base_ButtonPressEvent;
			base.ButtonReleaseEvent += base_ButtonReleaseEvent;
			base.MotionNotifyEvent += base_MotionNotifyEvent;
			
			this.render = DrawContents;
			this.overlay = DrawOverlay;
			this.QueueDraw();
		}

		#region network add/delete/update
		private void AddNetwork (Network network)
		{
			lock (this) {
				
				if (debug)
					Console.Out.WriteLine("*** Adding network {0}.", network.NetworkName);
				
				PointD position = new PointD(rng.Next(200), rng.Next(200));
				
				Hashtable nodegroups = Hashtable.Synchronized(new Hashtable());
				Hashtable node2nodegroup = Hashtable.Synchronized(new Hashtable());
				network.Properties["nodegroups"] = nodegroups;
				network.Properties["node2nodegroup"] = node2nodegroup;
				network.Properties["rect"] = new Rectangle(position.X, position.Y, 0, 0);
				networkZOrder.Add(network);
				
				foreach (Node node in network.Nodes.Values)
					AddNode(node, network);
				
				network.NewIncomingConnection += delegate(Network eNetwork, LocalNodeConnection connection) { Gtk.Application.Invoke(delegate { this.QueueDraw(); }); };
				
				network.ConnectingTo += delegate(Network eNetwork, LocalNodeConnection connection) { Gtk.Application.Invoke(delegate { this.QueueDraw(); }); };
				
				network.UserOnline += (NodeOnlineOfflineEventHandler)DispatchService.GuiDispatch(new NodeOnlineOfflineEventHandler(network_UserOnline));
				network.UserOffline += (NodeOnlineOfflineEventHandler)DispatchService.GuiDispatch(new NodeOnlineOfflineEventHandler(network_UserOffline));
				network.UpdateNodeInfo += (UpdateNodeInfoEventHandler)DispatchService.GuiDispatch(new UpdateNodeInfoEventHandler(network_UpdateNodeInfo));
				
				network.ConnectionUp += delegate(INodeConnection connection) { Gtk.Application.Invoke(delegate { this.QueueDraw(); }); };
				
				network.ConnectionDown += delegate(INodeConnection connection) { Gtk.Application.Invoke(delegate { this.QueueDraw(); }); };
				
				network.CleanupFinished += delegate(object sender, EventArgs e) { Gtk.Application.Invoke(delegate { this.QueueDraw(); }); };
			}
			
			this.QueueDraw();
		}

		private void network_UserOnline (Network eNetwork, Node node)
		{
			AddNode(node, eNetwork);
			this.QueueDraw();
		}

		private void network_UserOffline (Network eNetwork, Node node)
		{
			DeleteNode(node, eNetwork);
			this.QueueDraw();
		}

		private void network_UpdateNodeInfo (Network eNetwork, string oldNick, Node node)
		{
			UpdateNode(node, eNetwork);
			this.QueueDraw();
		}

		private void DeleteNetwork (Network network)
		{
			lock (this) {
				if (debug)
					Console.Out.WriteLine("*** Deleting network {0}.", network.NetworkName);
				
				foreach (Node node in network.Nodes.Values)
					DeleteNode(node, network);
				
				network.Properties.Remove("nodegroups");
				network.Properties.Remove("node2nodegroup");
				network.Properties.Remove("rect");
				networkZOrder.Remove(network);
			}
			
			this.QueueDraw();
		}
		#endregion

		#region add/delete/update node
		private void AddNode (Node node, Network network)
		{
			Rectangle networkRect = (Rectangle)network.Properties["rect"];
			AddNode(node, network, new PointD(networkRect.X + rng.Next(75), networkRect.Y + rng.Next(75)));
		}

		private void AddNode (Node node, Network network, PointD position)
		{
			lock (network) {
				if (debug)
					Console.Out.WriteLine("*** Adding node {0} to network {1}...", node.NickName, network.NetworkName);
				Hashtable nodegroups = network.Properties["nodegroups"] as Hashtable;
				Hashtable node2nodegroup = network.Properties["node2nodegroup"] as Hashtable;
				
				IPAddress address = GetExternalIPv4Address(node);
				string nodeIP = (address.Equals(IPAddress.None)) ? node.NickName : address.ToString();
				
				if (nodegroups.ContainsKey(nodeIP)) {
					NodeGroup ng = nodegroups[nodeIP] as NodeGroup;
					ng.Nodes.Add(node);
					node2nodegroup.Add(node, ng);
					if (debug)
						Console.Out.WriteLine(" -> Added node {0} to network {1} / nodegroup {2}", node.NickName, network.NetworkName, ng.Name);
				} else {
					NodeGroup ng = new NodeGroup(nodeIP, position);
					ng.Nodes.Add(node);
					node2nodegroup.Add(node, ng);
					nodegroups.Add(nodeIP, ng);
					if (debug)
						Console.Out.WriteLine(" -> Added node {0} to network {1} / new nodegroup {2}", node.NickName, network.NetworkName, ng.Name);
				}
			}
		}

		private void DeleteNode (Node node, Network network)
		{
			lock (network) {
				if (debug)
					Console.Out.WriteLine("*** Deleting node {0} from network {1}.", node.NickName, network.NetworkName);
				Hashtable nodegroups = network.Properties["nodegroups"] as Hashtable;
				Hashtable node2nodegroup = network.Properties["node2nodegroup"] as Hashtable;
				//string nodeIP = (node.ExternalIP == string.Empty) ? node.NickName : node.ExternalIP;
				
				if (!node2nodegroup.ContainsKey(node))
					throw new Exception("Unable to delete node: could not find the nodegroup which contains the specified node: " + node.NickName);
				
				NodeGroup ng = node2nodegroup[node] as NodeGroup;
				if (debug)
					Console.Out.WriteLine(" -> Found node {0} in nodegroup {1} / network {2}.", node.NickName, ng.Name, network.NetworkName);
				ng.Nodes.Remove(node);
				node2nodegroup.Remove(node);
				if (debug)
					Console.Out.WriteLine(" -> Deleted node {0} in nodegroup {1} / network {2}.", node.NickName, ng.Name, network.NetworkName);
				
				// If nodegroup is empty, delete that too.
				if (ng.Nodes.Count == 0) {
					if (debug)
						Console.Out.WriteLine(" -> Nodegroup {0} / network {1} empty, deleting.", ng.Name, network.NetworkName);
					nodegroups.Remove(ng.Name);
					if (debug)
						Console.Out.WriteLine(" -> Nodegroup {0} / network {1} deleted.", ng.Name, network.NetworkName);
				}
			}
		}

		private void UpdateNode (Node node, Network network)
		{
			try {
				lock (network) {
					if (debug)
						Console.Out.WriteLine("*** Updating node {0} from network {1}.", node.NickName, network.NetworkName);
					Hashtable node2nodegroup = network.Properties["node2nodegroup"] as Hashtable;
					
					if (!node2nodegroup.ContainsKey(node))
						throw new Exception("Unable to modify node: could not find the nodegroup which contains the specified node: " + node.NickName);
					
					NodeGroup ng = node2nodegroup[node] as NodeGroup;
					PointD position = ng.Position;
					DeleteNode(node, network);
					AddNode(node, network, position);
					if (debug)
						Console.Out.WriteLine("### Node update complete for node {0} from network {1}.", node.NickName, network.NetworkName);
				}
			} catch (Exception ex) {
				OnError(ex);
			}
		}
		#endregion

		#region mouse/motion event handling
		private void base_ButtonPressEvent (object sender, Gtk.ButtonPressEventArgs args)
		{
			Gdk.EventButton eb = args.Event;
			double mouseX = eb.X;
			double mouseY = eb.Y;
			DeviceToUser(ref mouseX, ref mouseY);
			if (debug)
				Console.Out.WriteLine("Button Pressed {0}: user coords {1},{2}", eb.Button, mouseX, mouseY);
			PointD thePoint = new PointD(mouseX, mouseY);
			NodeGroup firstNodeGroupHit = null;
			Node firstNodeHit = null;
			
			foreach (Network network in networkZOrder) {
				Rectangle networkRect = (Rectangle)network.Properties["rect"];
				if (Inside(thePoint, networkRect)) {
					if (debug)
						Console.Out.WriteLine("Click inside network {0}", network.NetworkName);
					Hashtable nodegroups = (Hashtable)network.Properties["nodegroups"];
					foreach (NodeGroup ng in nodegroups.Values) {
						//PointD ngPosition = new PointD (ng.Position.X + networkRect.X,
						//                              ng.Position.Y + networkRect.Y);
						if (Inside(thePoint, ng.Position, ng.Dimension)) {
							if (debug)
								Console.Out.WriteLine("Click inside group {0}", ng.Name);
							firstNodeGroupHit = ng;
							foreach (Node n in ng.Nodes) {
								PointD np = (PointD)n.Properties["position"];
								//	np = new PointD (np.X + ngPosition.X, 
								//	                 np.Y + ngPosition.Y);
								if (Inside(thePoint, np, AvatarSizeD)) {
									firstNodeHit = n;
									if (debug)
										Console.Out.WriteLine("Click inside node {0}", n);
									break;
									// clicked node n
								}
							}
							break;
							// click in ng but not any node
						}
						
					}
					break;
					// click in network but not any ng
				}
			}
			
			if (eb.Button == 1 | eb.Button == 3) {
				selectedNode = firstNodeHit;
				
				if (SelectedNodeChanged != null)
					SelectedNodeChanged(selectedNode);
			}
			
			switch (eb.Button) {
			case 1:
				selectedGroup = firstNodeGroupHit;
				
				if (selectedGroup != null) {
					this.nodeDragging = true;
					nodeDraggingLastX = eb.X;
					nodeDraggingLastY = eb.Y;
					
					// we're dragging...
					this.GdkWindow.Cursor = new Gdk.Cursor(Gdk.CursorType.Plus);
				}
				
				if (selectedNode != null) {
					if (eb.Type == Gdk.EventType.TwoButtonPress) {
						ResetDrag();
						if (NodeDoubleClicked != null)
							NodeDoubleClicked(selectedNode);
					}
				}
				
				break;
			
			case 3:
				if (firstNodeGroupHit == null) {
					// clicked on map background
					Menu mapMenu = (Menu)Runtime.UIManager.GetWidget("/MapPopupMenu");
					mapMenu.Popup();
					
				} else {
					if (firstNodeHit != null) {
						UserMenu userMenu = new UserMenu(firstNodeHit.Network, firstNodeHit);
						userMenu.Popup();
					}
				}
				break;
			}
		}

		private void base_ButtonReleaseEvent (object sender, Gtk.ButtonReleaseEventArgs args)
		{
			ResetDrag();
		}

		private void ResetDrag ()
		{
			this.nodeDragging = false;
			nodeDraggingLastX = 0;
			nodeDraggingLastY = 0;
			this.GdkWindow.Cursor = new Gdk.Cursor(Gdk.CursorType.LeftPtr);
		}

		private void base_MotionNotifyEvent (object sender, Gtk.MotionNotifyEventArgs args)
		{
			Gdk.EventMotion em = args.Event;
			if (debug)
				Console.Out.WriteLine("Motion event: nodeDragging {0}, user coords {1},{2}", nodeDragging, em.X, em.Y);
			double mouseX = em.X;
			double mouseY = em.Y;
			DeviceToUser(ref mouseX, ref mouseY);
			PointD thePoint = new PointD(mouseX, mouseY);
			hoverNode = null;
			foreach (Network network in networkZOrder) {
				foreach (Node n in network.Nodes.Values) {
					if (n.Properties.ContainsKey("position")) {
						PointD np = (PointD)n.Properties["position"];
						if (Inside(thePoint, np, AvatarSizeD)) {
							hoverNode = n;
							break;
						}
					}
				}
			}
			
			//if (debug) Console.Out.WriteLine ("MotionNotifyEventHandler called!");
			if (!nodeDragging) {
				if (debug)
					Console.Out.WriteLine("not nodedragging motion!");
				this.QueueDraw();
				return;
			}
			
			mouseX = em.X;
			mouseY = em.Y;
			
			double deltaX, deltaY;
			deltaX = mouseX - nodeDraggingLastX;
			deltaY = mouseY - nodeDraggingLastY;
			PointD pos = selectedGroup.Position;
			pos.X += (deltaX / this.ScaleFactor);
			pos.Y += (deltaY / this.ScaleFactor);
			nodeDraggingLastX = em.X;
			nodeDraggingLastY = em.Y;
			selectedGroup.Position = pos;
			this.QueueDraw();
		}

		#endregion

		private bool Inside (PointD thePoint, Rectangle rect)
		{
			bool tl = thePoint.X >= rect.X && thePoint.Y >= rect.Y;
			bool br = thePoint.X < rect.X + rect.Width && thePoint.Y < rect.Y + rect.Height;
			return tl && br;
		}

		private bool Inside (PointD thePoint, PointD rectOrigin, SizeD rectSize)
		{
			bool tl = thePoint.X >= rectOrigin.X && thePoint.Y >= rectOrigin.Y;
			bool br = thePoint.X < rectOrigin.X + rectSize.Width && thePoint.Y < rectOrigin.Y + rectSize.Height;
			return tl && br;
		}

		private SizeD CalculateNodeGroupTitleTextSize (NodeGroup ng, Cairo.Context gc)
		{
			Pango.Layout layout = new Pango.Layout(this.PangoContext);
			layout.FontDescription = this.PangoContext.FontDescription.Copy();
			layout.FontDescription.Size = Pango.Units.FromDouble(NodegroupNameFontSize);
			layout.SetText(ng.Name);
			
			Gdk.Size size = GetTextSize(layout);
			return new SizeD(size.Width, size.Height);
		}

		private SizeD CalculateNodeGroupSize (NodeGroup ng, Cairo.Context gc)
		{
			var titleTextSize = CalculateNodeGroupTitleTextSize(ng, gc);
			var titleTextHeight = titleTextSize.Height + (Padding * 2.0);
			var heightNodeCount = (ng.Nodes.Count < 3) ? 1 : ng.Nodes.Count;
			
			var width = Math.Max(titleTextSize.Width + (Padding * 2.0), 
			                     (ng.Nodes.Count * AvatarDimension) + (Padding * 2.0) + (Padding * ng.Nodes.Count));
			var height = (heightNodeCount * AvatarDimension) + titleTextHeight + (Padding * 2.0) + (Padding * heightNodeCount);
			return new SizeD(width, height);
		}

		private void RenderNodeGroup (NodeGroup ng, Network network, Cairo.Context gc)
		{
			gc.Save();
			
			SizeD size = CalculateNodeGroupSize(ng, gc);
			ng.Dimension = size;
			CreateRoundedRectPath(gc, ng.Position.X, ng.Position.Y, size.Width, size.Height, 20);
			gc.Color = new Cairo.Color(0, 0, 0, 0.5);
			gc.FillPreserve();
			
			if (selectedGroup == ng) {
				gc.Save();
				gc.Color = orangeOverlay;
				gc.StrokePreserve();
				gc.Restore();
			}
			
			var titleTextSize = CalculateNodeGroupTitleTextSize(ng, gc);
			var titleSize = new SizeD(size.Width, titleTextSize.Height + (Padding * 2.0));
			
			gc.Clip();
			gc.Rectangle(ng.Position.X, ng.Position.Y, titleSize.Width, titleSize.Height);
			gc.Fill();			
			gc.ResetClip();
			
			gc.Color = lightGray;
			
			double hostTextX = ng.Position.X + (titleSize.Width / 2.0) - (titleTextSize.Width / 2.0);
			double hostTextY = ng.Position.Y + (titleSize.Height / 2.0) - (titleTextSize.Height / 2.0);
			gc.MoveTo(hostTextX, hostTextY /* + titleTextSize.Height */);
			
			Pango.Layout layout = new Pango.Layout(this.PangoContext);
			layout.FontDescription = this.PangoContext.FontDescription.Copy();
			layout.FontDescription.Size = Pango.Units.FromDouble(NodegroupNameFontSize);
			layout.SetText(ng.Name);
			Pango.CairoHelper.ShowLayout(gc, layout);
					
			SizeD nodesSize = CalculateNodeGroupSize(ng, gc);
			
			if (ng.Nodes.Count == 1) {
				double positionY = ng.Position.Y + titleSize.Height + Padding;
				double positionX = ng.Position.X + (ng.Dimension.Width / 2.0) - HalfAvatarDimension;
				RenderNode(gc, (Node)ng.Nodes[0], positionX, positionY);
			} else if (ng.Nodes.Count == 2) {
				// position them side-by-side, separated by (padding) number of pixels, centered in the
				// space.
				double positionY = ng.Position.Y + titleSize.Height + Padding;
				double position1X = ng.Position.X + (ng.Dimension.Width / 2.0) - (Padding / 2.0) - AvatarDimension;
				double position2X = position1X + Padding + AvatarDimension;
				RenderNode(gc, (Node)ng.Nodes[0], position1X, positionY);
				RenderNode(gc, (Node)ng.Nodes[1], position2X, positionY);
			} else {
				double deg = 0;
				double x = 0;
				double y = 0;
				
				var contentY = ng.Position.Y + titleSize.Height;
				var contentHeight = size.Height - titleSize.Height;
				var middle = new System.Drawing.Point(Convert.ToInt32(ng.Position.X + size.Width - (size.Width / 2.0)),
				                                      Convert.ToInt32(contentY + contentHeight - (contentHeight / 2.0)));
								
				int nodeSize = Convert.ToInt32(AvatarDimension);
				for (int i = 0; i < ng.Nodes.Count; i++) {
					x = Math.Sin(deg) * ((size.Width / 2.0) - (nodeSize)) + middle.X - (nodeSize / 2.0);
					y = Math.Cos(deg) * ((contentHeight / 2.0) - (nodeSize)) + middle.Y - (nodeSize / 2.0);
					RenderNode(gc, (Node)ng.Nodes[i], x, y);
					deg += Math.PI / (ng.Nodes.Count / 2.0);
				}
			}
			gc.Restore();
		}

		private SizeD CalculateNetworkTitleSize (Network network, Cairo.Context gc)
		{
			Pango.Layout layout = new Pango.Layout(this.PangoContext);
			layout.SetText(network.NetworkName);
			Gdk.Size size = GetTextSize(layout);
			return new SizeD(size.Width, size.Height);
		}

		private void CalculateNetworkSize (Network network, Cairo.Context gc)
		{
			if (network == null) {
				throw new ArgumentNullException("network");
			}
			
			if (gc == null) {
				throw new ArgumentNullException("gc");
			}
			
			SizeD titleSize = CalculateNetworkTitleSize(network, gc);
			
			double networkX = -1;
			double networkY = -1;
			double networkWidth = titleSize.Width;
			double networkHeight = titleSize.Height;
			
			Hashtable nodegroups = network.Properties["nodegroups"] as Hashtable;
			
			foreach (NodeGroup ng in nodegroups.Values) {
				if (networkX == -1 | networkY == -1) {
					networkX = ng.Position.X - 14;
					networkY = ng.Position.Y - 14;
				} else {
					networkX = Math.Min(networkX, ng.Position.X - 14);
					networkY = Math.Min(networkY, ng.Position.Y - 14);
				}
			}
			
			foreach (NodeGroup ng in nodegroups.Values) {
				SizeD ngSize = CalculateNodeGroupSize(ng, gc);
				networkWidth = Math.Max(networkWidth, ngSize.Width + ng.Position.X - networkX);
				networkHeight = Math.Max(networkHeight, ngSize.Height + ng.Position.Y - networkY);
			}
			
			Rectangle newNetworkRect = new Rectangle(networkX, networkY - titleSize.Height, networkWidth + 14, networkHeight + 14 + titleSize.Height);
			
			network.Properties["rect"] = newNetworkRect;
		}

		private int RenderNotice (string text, Cairo.Context gc, int yOffset)
		{
			gc.Save();
			
			int leftPadding = 20;
			int rightPadding = 20;
			int topPadding = 20 + yOffset;
			int innerPadding = 5;
			int textWidth, textHeight;
			
			// Setup text
			Pango.Layout layout = Pango.CairoHelper.CreateLayout(gc);
			layout.FontDescription = this.PangoContext.FontDescription.Copy();
			layout.FontDescription.Size = layout.FontDescription.Size + Pango.Units.FromPixels(3);
			layout.Wrap = Pango.WrapMode.Word;
			layout.Width = Pango.Units.FromPixels(this.Allocation.Width - (leftPadding + innerPadding + noticeIcon.Width + innerPadding + rightPadding + innerPadding));
			layout.SetMarkup(text);
			layout.GetSize(out textWidth, out textHeight);
			textWidth = Pango.Units.ToPixels(textWidth);
			textHeight = Pango.Units.ToPixels(textHeight);
			
			// Render background
			gc.Color = new Cairo.Color(0.59, 0.59, 0.59, 0.6);
			CreateRoundedRectPath(gc, leftPadding, topPadding, textWidth + 50, textHeight + (innerPadding * 2.0), 10);
			gc.Fill();
			
			// Render icon
			RenderPixbufToSurf(gc, noticeIcon, leftPadding + innerPadding, topPadding + innerPadding + ((textHeight / 2.0) - (noticeIcon.Height / 2.0)));
			
			// Render text
			gc.MoveTo(leftPadding + innerPadding + noticeIcon.Width + innerPadding, topPadding + innerPadding);
			gc.Color = white;
			Pango.CairoHelper.ShowLayout(gc, layout);
			
			gc.Restore();
			
			return textHeight + topPadding - yOffset;
		}

		private void RenderNetwork (Network network, Cairo.Context gc)
		{
			lock (network) {
				Rectangle rect = (Rectangle)network.Properties["rect"];
				SizeD titleSize = CalculateNetworkTitleSize(network, gc);
				Hashtable nodegroups = network.Properties["nodegroups"] as Hashtable;
				
				gc.Save();
				
				// Draw network box
				gc.Color = networkBG;
				gc.Rectangle(rect);
				gc.Fill();
				
				// Draw title, 2px/2px offset from top/left corner
				gc.Color = white;
				gc.MoveTo(rect.X + 2, rect.Y + 2 /* + titleSize.Height */);

				Pango.Layout layout = new Pango.Layout(this.PangoContext);
				layout.FontDescription = this.PangoContext.FontDescription.Copy();
				layout.FontDescription.Size = Pango.Units.FromDouble(NetworkNameFontSize);
				layout.SetText(network.NetworkName);
				Pango.CairoHelper.ShowLayout(gc, layout);
				
				foreach (NodeGroup ng in nodegroups.Values)
					RenderNodeGroup(ng, network, gc);
				
				gc.Restore();
				
				RenderConnections(network, gc);
			}
		}

		private void RenderConnections (Network network, Cairo.Context gc)
		{
			foreach (INodeConnection inode in network.Connections) {
				if (debug)
					Console.Out.WriteLine("drawing connection {0}", inode.ToString());
				Node node1 = inode.NodeLocal;
				Node node2 = inode.NodeRemote;
				if (node1 == null || node2 == null)
					continue;
				if (!node1.Properties.ContainsKey("position") || !node2.Properties.ContainsKey("position"))
					continue;
				
				PointD node1p = (PointD)node1.Properties["position"];
				PointD node2p = (PointD)node2.Properties["position"];
				int zone = GetNodeZone(node1p, node2p);
				PointD start = new PointD();
				PointD c1 = new PointD();
				PointD c2 = new PointD();
				PointD end = new PointD();
				double mid;
				switch (zone) {
				case 1:
				case 5:
					start = new PointD(node1p.X + HalfAvatarDimension, node1p.Y);
					end = new PointD(node2p.X + HalfAvatarDimension, node2p.Y + AvatarDimension);
					// end Y < start Y in screen coords for zone 1,5
					mid = end.Y + ((start.Y - end.Y) / 2.0);
					c1 = new PointD(start.X, mid);
					c2 = new PointD(end.X, mid);
					break;
				
				case 4:
				case 6:
					start = new PointD(node1p.X + AvatarDimension, node1p.Y + HalfAvatarDimension);
					end = new PointD(node2p.X, node2p.Y + HalfAvatarDimension);
					// start X < end X in screen coords for zone 4,6
					mid = start.X + ((end.X - start.X) / 2.0);
					c1 = new PointD(mid, start.Y);
					c2 = new PointD(mid, end.Y);
					break;
				
				case 3:
				case 7:
					start = new PointD(node1p.X + HalfAvatarDimension, node1p.Y + AvatarDimension);
					end = new PointD(node2p.X + HalfAvatarDimension, node2p.Y);
					// start Y < end Y in screen coords for zone 3,7
					mid = start.Y + ((end.Y - start.Y) / 2.0);
					c1 = new PointD(start.X, mid);
					c2 = new PointD(end.X, mid);
					break;
				
				case 0:
				case 2:
					start = new PointD(node1p.X, node1p.Y + HalfAvatarDimension);
					end = new PointD(node2p.X + AvatarDimension, node2p.Y + HalfAvatarDimension);
					// end X < start X in screen coords for zone 0,2
					mid = end.X + ((start.X - end.X) / 2.0);
					c1 = new PointD(mid, start.Y);
					c2 = new PointD(mid, end.Y);
					break;
				default:
					
					continue;
				}
				
				DrawConnection(gc, start, c1, c2, end);
			}
		}

		private void RenderNode (Context gc, Node n, double x, double y)
		{
			double xBelow, yBelow;
			xBelow = Math.Floor(x) + 0.5;
			yBelow = Math.Floor(y) + 0.5;
			
			Gdk.Pixbuf avatar = Gui.AvatarManager.GetAvatar(n);
			
			double avatarWidth = (avatar.Width < 40) ? avatar.Width : 40;
			double avatarHeight = (avatar.Height < 40) ? avatar.Height : 40;
			
			DrawRoundedRectThumb(gc, xBelow, yBelow, 40, 40, 10, 1, transparent, lightAlphaGray, avatar,
			avatarWidth, avatarHeight);
			
			
			if (selectedNode == n) {
				CreateRoundedRectPath(gc, xBelow, yBelow, 40, 40, 20);
				gc.Color = orangeOverlay;
				gc.Stroke();
			}
			
			// TODO: This is a mess...
			if (hoverNode == n) {
				Pango.Layout layout = new Pango.Layout (this.PangoContext);
				layout.SetText (n.ToString());
				
				Gdk.Size te = GetTextSize(layout);
				
				double textX = xBelow + 20 - (te.Width / 2.0);
				double textY = yBelow + 20 - (te.Height / 2.0);
				double padding = 3;
				
				CreateRoundedRectPath(gc, textX - padding, textY - padding, te.Width + (padding * 2.0), te.Height + (padding * 2.0), 20);
				gc.Color = darkGray;
				gc.Fill();
				
				CreateRoundedRectPath(gc, textX - padding, textY - padding, te.Width + (padding * 2.0), te.Height + (padding * 2.0), 20);
				gc.Color = lightGray;
				gc.Stroke();
				
				gc.Color = new Cairo.Color(255, 255, 255, 1);
				gc.MoveTo(textX, textY);
				
				Pango.CairoHelper.ShowLayout(gc, layout);
			}
			
			n.Properties["position"] = new PointD(x, y);
		}

		private void DrawContents (Cairo.Context gc)
		{
			try {
				//if (debug) Console.Out.WriteLine ("Drawing NetworkMap Contents...");
				gc.Save();
				lock (this) {
					
					// TODO: Deal with overlapping nodes -- connections don't handle this yet.				
					
					// XXX: Eww..
					ArrayList networks = (ArrayList)networkZOrder.Clone();
					networks.Reverse();
					foreach (Network network in networks) {
						
						// XXX: Don't do this on every draw,
						// only when things actually change!
						CalculateNetworkSize(network, gc);
						
						if (network.ReadyLocalConnections.Length > 0) {
							RenderNetwork(network, gc);
						}
					}
				}
				
				gc.Restore();
			} catch (Exception ex) {
				OnError(ex);
			}
		}

		private void DrawOverlay (Cairo.Context gc)
		{
			int yOffset = 0;
			foreach (Network network in Core.Networks) {
				if (network.ReadyLocalConnections.Length == 0) {
					yOffset += RenderNotice(string.Format("You are not connected to anybody on the <b>{0}</b> network.\n<span size=\"small\">Select \"Connect to a Friend\" above to get started.</span>", network.NetworkName), gc, yOffset);
				}
			}
		}

		private void DrawConnection (Cairo.Context gc, PointD start, PointD c1, PointD c2, PointD end)
		{
			gc.Save();
			gc.LineWidth = (2.0 / this.ScaleFactor);
			gc.Color = connectionGreen;
			gc.MoveTo(start.X, start.Y);
			gc.CurveTo(c1.X, c1.Y, c2.X, c2.Y, end.X, end.Y);
			gc.Stroke();
			gc.Restore();
		}

		private int GetNodeZone (PointD node1, PointD node2)
		{
			//double halfAvatarDimension = AvatarDimension / 2.0;
			
			// zones are, clockwise, from top-left: 1,5,4,6,7,3,2,0
			// start at zone 0, and make three comparisons:
			// If you're in the right half, then add 4
			// if you're in the bottom half, add 2
			// if |deltaY| > |deltaX|, add 1 -- this separates between eighths in quadrants.
			PointD delta = new PointD(node2.X - node1.X, node2.Y - node1.Y);
			int zone = 0;
			if (node2.X >= node1.X)
				zone = 4;
			// right half zones 4-7
			if (node2.Y >= node1.Y)
				zone += 2;
			// bottom quadrant, +2
			if (Math.Abs(delta.Y) >= Math.Abs(delta.X))
				zone += 1;
			
			return zone;
		}

		public void SelectNode (Node node)
		{
			selectedNode = node;
			this.QueueDraw();
		}

		protected virtual void OnError (Exception ex)
		{
			if (Error != null) {
				Error(this, ex);
			} else {
				LoggingService.LogError("Error in the network map:", ex);
			}
		}

		private IPAddress GetExternalIPv4Address (Node node)
		{
			if (node.DestinationInfos != null) {
				foreach (DestinationInfo info in node.DestinationInfos) {
					if (info.TypeName == "FileFind.Meshwork.Destination.TCPIPv4Destination") {
						IPAddress address = IPAddress.Parse(info.Data[0]);
						if (!Common.IsInternalIP(address)) {
							return address;
						}
					}
				}
			}
			return IPAddress.None;
		}
	}
}
