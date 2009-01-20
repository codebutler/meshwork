//
// ExpanderBar.cs: A sidebar widget with collapsable items
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2005 FileFind.net (http://www.filefind.net)
// 

using System;

namespace FileFind.Meshwork.GtkClient
{
	public class ExpanderBar : Gtk.VBox
	{
		Gtk.VPaned lastPaned;
		
		public ExpanderBar ()
		{
			lastPaned = new Gtk.VPaned ();
			this.PackStart (lastPaned, true, true, 0);
			lastPaned.Show ();
		}

		public ExpanderBarItem AddItem (ExpanderBarItem item)
		{
			item.Show ();
			if (lastPaned.Child1 == null) {
				lastPaned.Pack1 (item, false, true);
			} else if (lastPaned.Child2 == null) {
				lastPaned.Pack2 (item, false, true);
			} else {
				Gtk.VPaned newPaned = new Gtk.VPaned ();
				Gtk.Container parent = (Gtk.Container)lastPaned.Parent;
				lastPaned.Reparent (newPaned);
				parent.Add (newPaned);
				newPaned.Show ();

				newPaned.Pack1 (lastPaned, false, true);
				lastPaned = newPaned;
				lastPaned.Pack2 (item, false, true);
			}
		
			item.Show ();
			return item;
		}
	}

	public class ExpanderBarItem : Gtk.HBox
	{
		Gtk.Widget content;

		int oldHeight;
		bool isCollapsed = false;

		Gtk.Button collapseButton;
		Gtk.HBox headerBox;
		Gtk.Image collapseImage;

		public ExpanderBarItem (string title, Gtk.Widget widget, bool expand) : this (title, widget)
		{
			if (expand == true)
				collapseButton.Visible = false;
		}
			
		public ExpanderBarItem (string title, Gtk.Widget widget)
		{
			if (widget == null) {
				throw new Exception ("Widget cannot be null");
			}

			base.BorderWidth = 1;

			Gtk.VBox box = new Gtk.VBox ();

			headerBox = new Gtk.HBox ();
			headerBox.HeightRequest = 23;
			headerBox.ExposeEvent += headerBox_ExposeEvent;
			headerBox.NoShowAll = true;

			Gtk.Label headerLabel = new Gtk.Label ();
			headerLabel.Xalign = 0;
			headerLabel.Xpad = 5;
			headerLabel.Markup = "<b>" + title + "</b>";

			Banshee.Widgets.FadingAlignment labelAlignment = new Banshee.Widgets.FadingAlignment();
			labelAlignment.Add(headerLabel);
			headerBox.PackStart (labelAlignment, true, true, 0);
			
			collapseImage = new Gtk.Image(new Gdk.Pixbuf (null, "FileFind.Meshwork.GtkClient.arrow_down.png"));
			collapseButton = new Gtk.Button (collapseImage);
			collapseButton.Clicked += new EventHandler (OnCollapseButtonClicked);
			collapseButton.Relief = Gtk.ReliefStyle.None;
			collapseButton.NoShowAll = true;
			headerBox.PackEnd (collapseButton, false, false, 0);
			
			box.PackStart (headerBox, false, false, 0);

			content = widget;
			box.PackEnd (content);
			
			this.Add (box);
			box.Show ();
			headerBox.Show ();
			collapseButton.Show ();
			collapseImage.Show();
			labelAlignment.ShowAll();
			widget.Show ();
		}
	
		public bool ShowHeader {
			get {
				return headerBox.Visible;
			}
			set {
				headerBox.Visible = value;
			}
		}

		public bool ShowBorder {
			get {
				return (base.BorderWidth != 0);
			}
			set {
				if (value)
					base.BorderWidth = 1;
				else
					base.BorderWidth = 0;
			}
		}

		private void headerBox_ExposeEvent (object o, Gtk.ExposeEventArgs args)
		{
			if (isCollapsed == false && ShowHeader) {
				Gtk.Widget widget = (Gtk.Widget)o;
				Gdk.Window window = widget.GdkWindow;
	
				Gdk.Color color = base.Style.Background (Gtk.StateType.Normal);
				Gdk.GC gc = GtkHelper.GetDarkenedGC (base.GdkWindow, color, 2);

				window.DrawLine(gc, widget.Allocation.X, 
						widget.Allocation.Y + widget.Allocation.Height -1, 
						widget.Allocation.X + widget.Allocation.Width,
						widget.Allocation.Y + widget.Allocation.Height -1);
			}
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			if (base.IsDrawable) {
				//Gdk.GC gc = base.Style.MidGC (Gtk.StateType.Active);

				Gdk.Color color = base.Style.Background (Gtk.StateType.Normal);
				Gdk.GC gc = GtkHelper.GetDarkenedGC (base.GdkWindow, color, 2);

				int x = Allocation.X;
				int y = Allocation.Y;
				int h = Allocation.Height;
				int w = Allocation.Width;
				if (collapseButton.Visible == false) {
					y --;
					h --;
				}

				if (ShowBorder) {
					base.GdkWindow.DrawRectangle (gc, false, x, y, w - 1, h);
				}
			}

			return base.OnExposeEvent (evnt);
		}

		// int pad = 1;

		/*
		protected override void OnSizeRequested (ref Gtk.Requisition rect)
		{
			if (Child != null) {
				int width, height;
				Child.GetSizeRequest (out width, out height);

				if (width == -1 || height == -1)
					width = height = 80;

				rect.Width = width + (pad * 2);
				rect.Height = height + (pad * 2);
			}
			base.OnSizeRequested (ref rect);
		}*/
		
		/*
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);

			if (Child != null) {
				Gdk.Rectangle childAllocation = Allocation;
				childAllocation.X += pad;
				childAllocation.Y += pad;
				childAllocation.Width -= pad * 2;
				childAllocation.Height -= pad * 2;
				Child.SizeAllocate (childAllocation);
			}
		}*/
		
		int oldPosition;

		private void OnCollapseButtonClicked (object o, EventArgs args)
		{
			if (isCollapsed == true) {
				content.Visible = true;
				isCollapsed = false;
				this.HeightRequest = oldHeight;
				Gtk.Box parentBox = (Gtk.Box)this.Parent;
				Gtk.Paned paned = BoxToPaned (parentBox);
				paned.Position = oldPosition;
				collapseImage.Pixbuf = new Gdk.Pixbuf (null, "FileFind.Meshwork.GtkClient.arrow_down.png");
			} else {
				content.Visible = false;
				isCollapsed = true;
				oldHeight = this.Allocation.Height;
				this.HeightRequest = headerBox.Allocation.Height + 3;
				Gtk.Paned parentPaned = (Gtk.Paned)this.Parent;
				oldPosition = parentPaned.Position;
				PanedToBox (parentPaned);
				collapseImage.Pixbuf = new Gdk.Pixbuf (null, "FileFind.Meshwork.GtkClient.arrow_up.png");
			}
		}

		/*protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
		}*/
		
		private Gtk.Box PanedToBox (Gtk.Paned paned)
		{
			Gtk.Box newBox = null;
			
			if (paned is Gtk.VPaned)
				newBox = new Gtk.VBox(false, 5);
			else if (paned is Gtk.HPaned)
				newBox = new Gtk.HBox(false, 5);
			else
				throw new ArgumentException("paned must be of type VPaned or HPaned. paned was " + paned.GetType());
			
			Gtk.Container parent = (Gtk.Container)paned.Parent;
			Gtk.Widget widget1 = paned.Child1;
			Gtk.Widget widget2 = paned.Child2;
			 
			  
			paned.Remove(widget1);
			paned.Remove(widget2);
		 
			newBox.PackStart(widget1,true,true,0);
			newBox.PackEnd(widget2,false,false,0);
			
			parent.Remove(paned);
			paned.Destroy();
			
			parent.Add(newBox);		
			newBox.Show();
			widget1.Show();
			widget2.Show();
			 
			
			return newBox;
		}

		private Gtk.Paned BoxToPaned (Gtk.Box box)
		{
			Gtk.Paned newPaned = null;
			
			if (box is Gtk.VBox)
				newPaned = new Gtk.VPaned();
			else if (box is Gtk.HBox)
				newPaned = new Gtk.HPaned();
			else
				throw new ArgumentException("box must be of type HBox or VBox. box was " + box.GetType().ToString());
				
			Gtk.Container parent = (Gtk.Container)box.Parent;
			Gtk.Widget widget1 = box.Children[0];
			Gtk.Widget widget2 = box.Children[1];
			
			box.Remove(widget1);
			box.Remove(widget2);
			
			newPaned.Pack1 (widget1, false, true);
			newPaned.Pack2 (widget2, false, true);
			
			parent.Remove(box);
			box.Destroy();
			
			parent.Add(newPaned);
			newPaned.Show();

			widget1.Show();
			widget2.Show();
			
			return newPaned;
		}
	}
}
