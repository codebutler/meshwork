using System;
using Meshwork.Client.GtkClient.Widgets;
using Gtk;
using Meshwork.Backend.Feature.FileIndexing;

namespace Meshwork.Client.GtkClient.Windows
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
			indexingSpinner = new AnimatedImage();
			indexingSpinner.SetSizeRequest(16, 16);
			indexingSpinner.Pixbuf = Gui.LoadIcon(22, "process-working");
			indexingSpinner.FrameHeight = 22;
			indexingSpinner.FrameWidth = 22;
			indexingSpinner.Load();
			indexingSpinnerAlignment.Add(indexingSpinner);
			
			hashingSpinner = new AnimatedImage();
			hashingSpinner.SetSizeRequest(16, 16);
			hashingSpinner.Pixbuf = Gui.LoadIcon(22, "process-working");
			hashingSpinner.FrameHeight = 22;
			hashingSpinner.FrameWidth = 22;
			hashingSpinner.Load();
			hashingSpinnerAlignment.Add(hashingSpinner);			
			
			Runtime.Core.ShareBuilder.StartedIndexing += delegate {
				Application.Invoke(delegate {
					indexingFileLabel.Text = "(Starting...)";
					indexingSpinner.Show();
				});
			};			
			
			Runtime.Core.ShareBuilder.IndexingFile += delegate (ShareBuilder builder, string filePath) {
				Application.Invoke(delegate {
					indexingFileLabel.Text = filePath;
					indexingSpinner.Show();
				});
			};
			
			Runtime.Core.ShareBuilder.FinishedIndexing += delegate {
				Application.Invoke(delegate {
					indexingFileLabel.Text = "(Idle)";
					indexingSpinner.Hide();
				});
			};			
			
			Runtime.Core.ShareBuilder.StoppedIndexing += delegate {
				Application.Invoke(delegate {
					indexingFileLabel.Text = "(Idle - last run aborted)";
					indexingSpinner.Hide();
				});
			};
			
			Runtime.Core.ShareHasher.StartedHashingFile += delegate {
				UpdateShareHasherStatus();	
			};
			
			Runtime.Core.ShareHasher.FinishedHashingFile += delegate {
				UpdateShareHasherStatus();				
			};
			
			Runtime.Core.ShareHasher.QueueChanged += delegate {
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
				if (Runtime.Core.ShareHasher.FilesRemaining == 0 && Runtime.Core.ShareHasher.CurrentFileCount == 0) {
					hashingFileLabel.Text = "(Idle)";
					hashingCountLabel.Text = "";
					hashingSpinner.Hide();
				} else if (Runtime.Core.ShareHasher.FilesRemaining > 0 && Runtime.Core.ShareHasher.CurrentFileCount == 0) {
					hashingFileLabel.Text = "(Starting...)";
					hashingCountLabel.Text = Common.Utils.FormatNumber(Runtime.Core.ShareHasher.FilesRemaining);
					hashingSpinner.Show();
				} else {
					hashingFileLabel.Text = Runtime.Core.ShareHasher.CurrentFiles;;
					hashingCountLabel.Text = Common.Utils.FormatNumber(Runtime.Core.ShareHasher.FilesRemaining);
					hashingSpinner.Show();
				}
			});
		}
	}
}
