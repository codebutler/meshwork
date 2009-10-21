
using System;
using Gtk;
using FileFind.Meshwork;
using Hyena.Widgets;

namespace FileFind.Meshwork.GtkClient
{
	public class IndexingStatusWindow : GladeWindow
	{
		[Glade.Widget] Label indexingFileLabel;
		[Glade.Widget] Label hashingFileLabel;
		[Glade.Widget] Label hashingCountLabel;
		[Glade.Widget] Alignment indexingSpinnerAlignment;
		[Glade.Widget] Alignment hashingSpinnerAlignment;
		
		AnimatedImage indexingSpinner;
		AnimatedImage hashingSpinner;
		
		static IndexingStatusWindow s_Instance;
		
		public static IndexingStatusWindow Instance {
			get {
				if (s_Instance == null)
					s_Instance = new IndexingStatusWindow();
				return s_Instance;
			}
		}
		
		public IndexingStatusWindow () : base ("IndexingStatusWindow")
		{
			indexingSpinner = new Hyena.Widgets.AnimatedImage();
			indexingSpinner.SetSizeRequest(16, 16);
			indexingSpinner.Pixbuf = Gtk.IconTheme.Default.LoadIcon("process-working", 22, IconLookupFlags.NoSvg);
			indexingSpinner.FrameHeight = 22;
			indexingSpinner.FrameWidth = 22;
			indexingSpinner.Load();
			indexingSpinnerAlignment.Add(indexingSpinner);
			
			hashingSpinner = new Hyena.Widgets.AnimatedImage();
			hashingSpinner.SetSizeRequest(16, 16);
			hashingSpinner.Pixbuf = Gtk.IconTheme.Default.LoadIcon("process-working", 22, IconLookupFlags.NoSvg);
			hashingSpinner.FrameHeight = 22;
			hashingSpinner.FrameWidth = 22;
			hashingSpinner.Load();
			hashingSpinnerAlignment.Add(hashingSpinner);			
			
			Core.ShareBuilder.StartedIndexing += delegate {
				Application.Invoke(delegate {
					indexingFileLabel.Text = "(Starting...)";
					indexingSpinner.Show();
				});
			};			
			
			Core.ShareBuilder.IndexingFile += delegate (ShareBuilder builder, string filePath) {
				Application.Invoke(delegate {
					indexingFileLabel.Text = filePath;
					indexingSpinner.Show();
				});
			};
			
			Core.ShareBuilder.FinishedIndexing += delegate {
				Application.Invoke(delegate {
					indexingFileLabel.Text = "(Idle)";
					indexingSpinner.Hide();
				});
			};			
			
			Core.ShareBuilder.StoppedIndexing += delegate {
				Application.Invoke(delegate {
					indexingFileLabel.Text = "(Idle - last run aborted)";
					indexingSpinner.Hide();
				});
			};
			
			Core.ShareHasher.StartedHashingFile += delegate {
				UpdateShareHasherStatus();	
			};
			
			Core.ShareHasher.FinishedHashingFile += delegate {
				UpdateShareHasherStatus();				
			};
			
			Core.ShareHasher.QueueChanged += delegate {
				UpdateShareHasherStatus();
			};
		}
		
		protected override void window_DeleteEvent (object sender, DeleteEventArgs args)
		{
			args.RetVal = true;
			Window.Hide();
		}
		
		void HandleCloseButtonClicked (object o, EventArgs args)
		{
			Window.Hide();
		}
		
		void UpdateShareHasherStatus ()
		{
			Application.Invoke(delegate {
				if (Core.ShareHasher.FilesRemaining == 0 && Core.ShareHasher.CurrentFileCount == 0) {
					hashingFileLabel.Text = "(Idle)";
					hashingCountLabel.Text = "";
					hashingSpinner.Hide();
				} else if (Core.ShareHasher.FilesRemaining > 0 && Core.ShareHasher.CurrentFileCount == 0) {
					hashingFileLabel.Text = "(Starting...)";
					hashingCountLabel.Text = FileFind.Common.FormatNumber(Core.ShareHasher.FilesRemaining);
					hashingSpinner.Show();
				} else {
					hashingFileLabel.Text = Core.ShareHasher.CurrentFiles;;
					hashingCountLabel.Text = FileFind.Common.FormatNumber(Core.ShareHasher.FilesRemaining);
					hashingSpinner.Show();
				}
			});
		}
	}
}
