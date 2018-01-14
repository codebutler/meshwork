//
// NavigationBar.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2006-2008 Meshwork Authors
//

// TODO:
// - When a button becomes active, ensure that it's visible by adjusting the
//   HAdjustment Value.
// - If a button label is wider than my width, ellipse it.
// - Add left/right scrolly arrows like the GTK file chooser has.

using System;
using System.Collections;
using Gtk;
using Meshwork.Backend.Core;
using Meshwork.Backend.Feature.FileBrowsing.Filesystem;

namespace Meshwork.Client.GtkClient.Widgets
{
	public class NavigationBar : Layout
	{
		HBox mainHBox;

		Gdk.Pixbuf homeIcon;
		Gdk.Pixbuf networkIcon;

		public NavigationBar () : base (new Adjustment(0, 0, 0, 0, 0, 0), new Adjustment(0, 0, 0, 0, 0, 0))
		{
			mainHBox = new HBox();
			mainHBox.Spacing = 3;
			mainHBox.Show();
			base.Put(mainHBox, 0, 0);
			base.HeightRequest = 30;

			base.Hadjustment.StepIncrement = 10;

			mainHBox.SizeAllocated += mainHBox_SizeAllocated;

			base.ScrollEvent += base_ScrollEvent;
 	
			homeIcon = Gui.LoadIcon(16, "user-home");
 			networkIcon = Gui.LoadIcon(16, "stock_internet");
		}

		private ArrayList path = new ArrayList();
		
		public delegate void PathButtonClickedEventHandler (string path);
		public event PathButtonClickedEventHandler PathButtonClicked;
		
		public void SetLocation(string newPath)
		{
			if (string.IsNullOrEmpty(newPath)) {
				throw new ArgumentNullException("newPath");
			}

			foreach (NavigationBarEntry entry in (path.Clone() as ArrayList)) {
				entry.SetNotActive();
			}

			if (newPath.StartsWith("/") == false) {
				throw new Exception("Invalid path: " + newPath);
			}

			if (newPath.EndsWith("/") == false) {
				newPath = newPath + "/";
			}
			
			string[] pathParts = newPath.Substring(0, newPath.Length - 1).Split('/');
		
			string pathToButton = "/";

			for (int x = 0; x < pathParts.Length; x++) {
				string pathPart = PathUtil.Join("/", pathParts[x]);

				pathToButton = PathUtil.Join(pathToButton, pathPart);
				
				if (x < path.Count && !PathUtil.AreEqual((path[x] as NavigationBarEntry).Path, pathToButton)) {
					/* The path started changing here, let's remove everything after this point */
					for (int y = x; y < path.Count; y++) {
						NavigationBarEntry currentEntry = (NavigationBarEntry)path[y];
						mainHBox.Remove(currentEntry.Button);
					}
					path.RemoveRange(x, path.Count - x);
				}

				if (x >= path.Count || !PathUtil.AreEqual((path[x] as NavigationBarEntry).Path, pathToButton)) {
					/* Need to add new button */
					Image image = null;
					string text = pathParts[x];
					
					if (x > 0 && pathParts[1] == "local") {
						if (x == 1) {
							image = new Image(Gui.AvatarManager.GetMiniAvatar(Runtime.Core.MyNodeID));
							text = "My Shared Files";
						} 
					} else {
						if (x == 0) {
							image = new Image(homeIcon);
							text = "Home";
						} else if (x == 1) {
							image = new Image(networkIcon);
							Network network = Runtime.Core.GetNetwork(pathParts[x]);
							if (network != null) {
								text = network.NetworkName;
							}
						} else if (x == 2) {
							Network network = Runtime.Core.GetNetwork(pathParts[x-1]);
							string nodeID = "";
							if (network != null) {
								Node node = network.GetNode(pathParts[x]);
								text = node.NickName;
								nodeID = node.NodeID;
							}

							image = new Image(Gui.AvatarManager.GetMiniAvatar(nodeID));
						}
					}

					ToggleButton button = AddButton(text, image, false);
					path.Add(new NavigationBarEntry(button, pathToButton));
				}

				if (x < path.Count && PathUtil.AreEqual((path[x] as NavigationBarEntry).Path, newPath)) {
					/* This is where we are! */
					(path[x] as NavigationBarEntry).SetActive();
				} 	
			}

		}

		private void base_ScrollEvent (object o, ScrollEventArgs args)
		{
			Adjustment adjustment = base.Hadjustment;

			if (args.Event.Direction == Gdk.ScrollDirection.Up) {
				adjustment.Value -= adjustment.StepIncrement;
			} else if (args.Event.Direction == Gdk.ScrollDirection.Down) {
				if (mainHBox.Allocation.Width - adjustment.Value > base.Allocation.Width) {

					// Don't overscroll.
					int upperValue = (mainHBox.Allocation.Width - base.Allocation.Width);
					if (adjustment.Value + adjustment.StepIncrement > upperValue) {
						adjustment.Value = upperValue;
					} else {
						adjustment.Value += adjustment.StepIncrement;
					}
				}
			}
			adjustment.Change();
		}

		private void mainHBox_SizeAllocated (object o, SizeAllocatedArgs args)
		{
			base.SetSize((uint)args.Allocation.Width, (uint)args.Allocation.Height);
		}
		
		private ToggleButton AddButton(string text, Image image, bool active)
		{

			Label label = new Label(text);
			label.UseMarkup = true;

			if (active) label.Markup = "<b>" + text.Replace("&","&amp;") + "</b>";
			
			ToggleButton newButton = new ToggleButton();

			if (image == null) {
				newButton.Add(label);
			} else {
				HBox hbox = new HBox();
				hbox.Spacing = 2;
				hbox.PackStart(image);
				hbox.PackEnd(label);
				newButton.Add(hbox);
			}

			newButton.CanFocus = false;
			newButton.FocusOnClick = false;
			newButton.Active = active;
			newButton.Released += on_button_clicked;
			mainHBox.PackStart(newButton, false, false, 0);
			newButton.ShowAll();

			base.Hadjustment.Value = base.Hadjustment.Upper;
			base.Hadjustment.Change();
		
			return newButton;
		}

		private void on_button_clicked (object o, EventArgs e)
		{
			ToggleButton clickedButton = (ToggleButton)o;
			if (clickedButton.InButton == true) {
				string clickedPath = GetPathFromButton(clickedButton);
				
				if (PathButtonClicked != null) 
					PathButtonClicked(clickedPath);
			}
		}

		private string GetPathFromButton(ToggleButton button)
		{
			foreach (NavigationBarEntry entry in path) {
				if (entry.Button == button) {
					return entry.Path;
				}
			}
			return null;
		}

		private class NavigationBarEntry
		{
			string path = string.Empty;
			ToggleButton button = null;
		
			public NavigationBarEntry(ToggleButton button, string path)
			{
				if (button == null) {
					throw new ArgumentNullException("button");
				}

				if (string.IsNullOrEmpty(path)) {
					throw new ArgumentNullException("path");
				}

				this.path = path;
				this.button = button;
			}
	
			public string Path {
				get {
					return path;
				}
			}

			public ToggleButton Button {
				get {
					return button;
				}
			}

			public void SetActive()
			{
				Button.Active = true;
				Label label = GetLabel();
				label.Markup = "<b>" + label.Text.Replace("&","&amp;") + "</b>";
			}
			
			public void SetNotActive()
			{
				Button.Active = false;
				Label label = GetLabel();
				label.Text = label.Text;
			}
			
			private Label GetLabel() {
				if (Button.Child is HBox) {
					foreach (Widget widget in (Button.Child as HBox).Children) {
						if (widget is Label)
							return (Label)widget;
					}
					return null;
				} else {
					return (Label)Button.Child;
				}
			}
		}
	}
}
