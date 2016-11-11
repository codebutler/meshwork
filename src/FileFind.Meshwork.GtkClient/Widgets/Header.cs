//
// Header.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;

namespace FileFind.Meshwork.GtkClient.Widgets
{
	public class Header : Gtk.HBox
	{
		Gtk.Label label;
		Gtk.Button closeButton;

		public event EventHandler CloseClicked;

		public Header (string text) : base ()
		{
			base.SetSizeRequest (-1, 30);

			Gtk.Alignment alignment = new Gtk.Alignment (0, 0, 1, 1);
			alignment.TopPadding = 1;
			alignment.LeftPadding = 5;
			alignment.RightPadding = 0;
			alignment.BottomPadding = 1;
			base.Add (alignment);

			//Select ();
				
			Gtk.HBox box = new Gtk.HBox ();
			alignment.Add (box);
			
			label = new Gtk.Label ();
			label.Ypad = 3;
		//	label.Xpad = 3;
			//base.Add (label);
			box.PackStart (label, true, true, 0);
			label.SetAlignment (0f, 0.5f);
			label.Justify = Gtk.Justification.Left;

			label.Markup = "<b>" + text + "</b>";
	
			closeButton = new Gtk.Button ();
			closeButton.Add (new Gtk.Image ("gtk-close", Gtk.IconSize.Menu));
			closeButton.Relief = Gtk.ReliefStyle.None;
			box.PackEnd (closeButton, false, false, 0);
			closeButton.ShowAll ();
			
			label.Show ();
			box.Show ();
			alignment.Show ();

			closeButton.Clicked += closeButton_Clicked;
		}

		private void closeButton_Clicked (object o, EventArgs args)
		{
			if (CloseClicked != null)
				CloseClicked (o, args);
		}

		/*
		protected override void OnMapped ()
		{
			base.OnMapped ();
			base.Style = item.Style;
			label.Style = base.Style;
		}

		protected override void OnStyleSet (Gtk.Style style)
		{
			base.OnStyleSet (style);
			label.Style = style;
		}*/

		public bool ShowCloseButton {
			get {
				return closeButton.Visible;
			}
			set {
				closeButton.Visible = value;
			}
		}

		public string Text {
			get {
				return label.Text;
			}
			set {
				label.Markup = "<b>" + value + "</b>";
			}
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			if (base.IsDrawable) {
				Gdk.Color color = base.Style.Background (Gtk.StateType.Normal);
				Gdk.GC gc = GtkHelper.GetDarkenedGC (base.GdkWindow, color, 3);

                                base.GdkWindow.DrawLine (gc, 
						Allocation.X,
						Allocation.Y + Allocation.Height - 1,
						Allocation.X + Allocation.Width,
						Allocation.Y + Allocation.Height - 1);

			}
			return base.OnExposeEvent (evnt);
		}

	}
}
