using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;

namespace MoveAndRename
{
	enum ESettings
	{
		Paths,
		Filetypes
	}
	enum Paths
	{
		Exclude,
		Include,
		Destinations
	}
	enum FileTypes
	{
		Episode,
		Subtitle
	}

	enum VideoExtensions
	{
		mkv,
		mp4,
		avi
	}

	enum SubtitleExtensions
	{
		sub,
		srt
	}
	/// <summary>
	/// Interaction logic for SettingsWindow.xaml
	/// </summary>
	public partial class SettingsWindow : Window
	{
		private bool debug = true;
		public double settingsHeight;
		public double settingsWidth;
		private ListBox lb = new ListBox();
		Settings settingsObj;

		public SettingsWindow(Settings settingsObject)
		{			
			settingsHeight = this.Height;
			settingsWidth = this.Width;
			InitializeComponent();			

			treeView.Height = this.Height - 60;
			TreeViewItem tvi = new TreeViewItem();
			tvi.Header = ESettings.Paths;
			List<Paths> p = new List<Paths>();
			foreach (Paths item in Enum.GetValues(typeof(Paths)))
			{
				p.Add(item);
			}
			tvi.ItemsSource = p;
			treeView.Items.Add(tvi);

			TreeViewItem tvft = new TreeViewItem();
			tvft.Header = ESettings.Filetypes;
			List<FileTypes> ft = new List<FileTypes>();
			foreach (FileTypes item in Enum.GetValues(typeof(FileTypes)))
			{
				ft.Add(item);
			}
			tvft.ItemsSource = ft;
			treeView.Items.Add(tvft);


			settingsObj = settingsObject;
			this.MinHeight = 400;
			this.MinWidth = 450;	
		}	

		private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			settingsHeight = (int)this.Height;
			settingsWidth = (int)this.Width;
			Content.Width = settingsWidth * 0.55;
			Content.Height = settingsHeight - 60;
			treeView.Width = settingsWidth * 0.3;
			treeView.Height = settingsHeight - 60;
			lb.Width = settingsWidth * 0.55;
			lb.Height = settingsHeight * 0.63;
		}

		private void addPathsControls()
		{
			Content.Children.Clear();

			lb = new ListBox();
			lb.Height = settingsHeight - 150;
			lb.Width = settingsWidth * 0.55;
			lb.VerticalAlignment = VerticalAlignment.Top;
			lb.HorizontalAlignment = HorizontalAlignment.Right;
			Content.Children.Add(lb);

			Button add = new Button();
			add.Content = "Add";
			add.Height = 20;
			add.Width = 75;
			add.HorizontalAlignment = HorizontalAlignment.Left;
			add.VerticalAlignment = VerticalAlignment.Bottom;
			add.Margin = new Thickness(0, 0, 0, 0);

			add.Click += new RoutedEventHandler(add_Click);
			Content.Children.Add(add);
			
			Button remove = new Button();
			remove.Content = "Remove";
			remove.Height = 20;
			remove.Width = 75;
			remove.VerticalAlignment = VerticalAlignment.Bottom;
			remove.HorizontalAlignment = HorizontalAlignment.Left;
			remove.Margin = new Thickness(add.Width+5, 0, 0, 0);			

			remove.Click += new RoutedEventHandler(remove_Click);
			Content.Children.Add(remove);			
		}

		private void addFileTypesControls(object sender)
		{
			Content.Children.Clear();
			Content.VerticalAlignment = VerticalAlignment.Top;
			TextBlock tb = new TextBlock();
			tb.Measure(new Size());
			tb.Arrange(new Rect());
			tb.Text = "Check this to include " + sender.ToString() + " in the search space";
			tb.TextWrapping = TextWrapping.WrapWithOverflow;
			Content.Children.Add(tb);

			CheckBox cb = new CheckBox();
			double tbH = tb.ActualHeight;
			cb.Margin = new Thickness(0, tbH + 20, 0, 0);
			cb.HorizontalAlignment = HorizontalAlignment.Left;
			cb.Unchecked += Cb_Unchecked;
			cb.Checked += Cb_Checked;
            cb.IsChecked = settingsObj.IncludeSubtitle;
            cb.Name = sender.ToString();
			Content.Children.Add(cb);
		}

		private void Cb_Unchecked(object sender, RoutedEventArgs e)
		{
			Debug.WriteLine("Uncked checkbox for episode");
            if (((CheckBox)sender).Name.ToString() == FileTypes.Subtitle.ToString())
            {
                ((CheckBox)sender).IsChecked = false;
                settingsObj.ChangeIncludeSub();
            }
        }

		private void Cb_Checked(object sender, RoutedEventArgs e)
		{
			Debug.WriteLine("Checked checkbox for episode");
            if(((CheckBox)sender).Name.ToString() == FileTypes.Subtitle.ToString())
            {
                ((CheckBox)sender).IsChecked = true;
                settingsObj.ChangeIncludeSub();
            }
		}

		private void remove_Click(object sender, EventArgs e)
		{
			Paths p = (Paths)treeView.SelectedItem;
			string path = "";
			try
			{
				path = lb.SelectedItem.ToString();
			}
			catch (Exception)
			{
				path = "";
			}

			switch (p)
			{
				case Paths.Include:
					settingsObj.RemoveInclude(path);
					updateListbox(lb, settingsObj.IncludeList);
					break;
				case Paths.Exclude:
					settingsObj.RemoveExclude(path);
					updateListbox(lb, settingsObj.ExcludeList);
					break;
				case Paths.Destinations:
					settingsObj.RemoveDestination(path);
					updateListbox(lb, settingsObj.DestinationList);
					break;
				default:
					break;
			}
		}

		private void add_Click(object sender, EventArgs e)
		{
			// Opens a dialog window for the user to be able to choose folders, the selected folders path is saved in path and added to the correct set of data.
			var dialog = new System.Windows.Forms.FolderBrowserDialog();
			System.Windows.Forms.DialogResult res = dialog.ShowDialog();
			string path = dialog.SelectedPath;

			// Depending on what item is active in the treeview we update the listbox to use different sets of data.
			Paths p = (Paths)treeView.SelectedItem;
			switch (p)
			{
				case Paths.Include:					
					settingsObj.AddInclude(path);					
					updateListbox(lb, settingsObj.IncludeList);
					break;
				case Paths.Exclude:
					settingsObj.AddExclude(path);					
					updateListbox(lb, settingsObj.ExcludeList);
					break;
				case Paths.Destinations:
					settingsObj.AddDestination(path);
					updateListbox(lb, settingsObj.DestinationList);
					break;
				default:
					break;
			}				
		}

		private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			if (treeView.SelectedItem.ToString() == Paths.Include.ToString())
			{
				addPathsControls();
				updateListbox(lb, settingsObj.IncludeList);
			}
			else if (treeView.SelectedItem.ToString() == Paths.Exclude.ToString())
			{
				addPathsControls();
				updateListbox(lb, settingsObj.ExcludeList);
			}
			else if(treeView.SelectedItem.ToString() == Paths.Destinations.ToString())
			{
				addPathsControls();
				updateListbox(lb, settingsObj.DestinationList);
			}
			else if(treeView.SelectedItem.ToString() == FileTypes.Episode.ToString())
			{
				addFileTypesControls(treeView.SelectedItem);
			}
            else if (treeView.SelectedItem.ToString() == FileTypes.Subtitle.ToString())
            {
                addFileTypesControls(treeView.SelectedItem);
            }
		}			
		
		private void updateListbox(ListBox lb, HashSet<string> data)
		{
			lb.ItemsSource = null;
			lb.ItemsSource = data;
		}
	}
}
