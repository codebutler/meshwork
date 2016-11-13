//
// NewSearchPage.cs: 
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net
// 

using System;
using Meshwork.Client.GtkClient.Widgets;
using Gtk;

namespace Meshwork.Client.GtkClient.Pages
{
	
	public class NewSearchPage : Alignment, IPage
	{
		public event EventHandler UrgencyHintChanged;

		static NewSearchPage instance;

		public static NewSearchPage Instance {
			get {
				if (instance == null) {
					instance = new NewSearchPage();
				}
				return instance;
			}
		}
	
		Button          searchButton;
		VBox            mainVBox;
		FileSearchEntry searchEntry;

		public NewSearchPage () : base (0.5f, 0.5f, 0f, 0f)
		{
			base.SetPadding(36, 36, 36, 36);
			base.FocusGrabbed += base_FocusGrabbed;

			mainVBox = new VBox();

			Label label = new Label();
			label.Xalign = 0;
			label.Markup = "<span size=\"x-large\" weight=\"bold\">Search for files...</span>";
			mainVBox.PackStart(label, false, false, 0);
			label.Show();

			searchEntry = new FileSearchEntry();
			searchEntry.WidthRequest = 400;
			mainVBox.PackStart(searchEntry, false, false, 6);
			searchEntry.Show();
			
			searchButton = new Button("_Search");
			searchButton.Image = new Image(Stock.Find, IconSize.Button);
			searchButton.Clicked += searchButton_Clicked;
			searchButton.Show();

			HButtonBox buttonBox = new HButtonBox();
			buttonBox.Layout = ButtonBoxStyle.End;
			buttonBox.PackStart(searchButton, false, false, 0);
			mainVBox.PackStart(buttonBox, false, false, 0);
			buttonBox.Show();

			base.Add(mainVBox);
			mainVBox.Show();
		}

		public bool UrgencyHint {
			get {
				return false;
			}
		}

		private void searchButton_Clicked (object sender, EventArgs args)
		{
			searchEntry.Activate();
		}

		private void base_FocusGrabbed (object sender, EventArgs args)
		{
			searchEntry.HasFocus = true;
		}
	}
}
