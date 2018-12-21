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
		Filetypes,
		ApiKeys,
		CustomFormat
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

	enum ApiKeys
	{
		TVDB,
		TMDB
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
			TreeViewItem tvi = new TreeViewItem
			{
				Header = ESettings.Paths
			};
			List<Paths> p = new List<Paths>();
			foreach (Paths item in Enum.GetValues(typeof(Paths)))
			{
				p.Add(item);
			}
			tvi.ItemsSource = p;
			treeView.Items.Add(tvi);

			TreeViewItem tvft = new TreeViewItem
			{
				Header = ESettings.Filetypes
			};
			List<FileTypes> ft = new List<FileTypes>();
			foreach (FileTypes item in Enum.GetValues(typeof(FileTypes)))
			{
				ft.Add(item);
			}
			tvft.ItemsSource = ft;
			treeView.Items.Add(tvft);

			List<ApiKeys> ak = new List<ApiKeys>();
			TreeViewItem tvak = new TreeViewItem
			{
				Header = ESettings.ApiKeys
			};
			foreach (ApiKeys item in Enum.GetValues(typeof(ApiKeys)))
			{
				ak.Add(item);
			}
			tvak.ItemsSource = ak;
			treeView.Items.Add(tvak);

			TreeViewItem tvc = new TreeViewItem
			{
				Header = ESettings.CustomFormat
			};
			tvc.ItemsSource = new List<ESettings>() { ESettings.CustomFormat };
			treeView.Items.Add(tvc);

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

		private Grid GetPathsControls()
		{
			Grid g = new Grid();

			lb = new ListBox
			{
				Height = settingsHeight - 150,
				Width = settingsWidth * 0.55,
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Right
			};
			g.Children.Add(lb);

			Button add = new Button
			{
				Content = "Add",
				Height = 20,
				Width = 75,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Bottom,
				Margin = new Thickness(0, 0, 0, 0)
			};

			add.Click += new RoutedEventHandler(Add_Click);
			g.Children.Add(add);

			Button remove = new Button
			{
				Content = "Remove",
				Height = 20,
				Width = 75,
				VerticalAlignment = VerticalAlignment.Bottom,
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness(add.Width + 5, 0, 0, 0)
			};
			remove.Click += new RoutedEventHandler(Remove_Click);
			g.Children.Add(remove);
			return g;
		}

		private Grid GetFileTypesControls(object sender)
		{
			Grid g = new Grid();
			Content.VerticalAlignment = VerticalAlignment.Top;
			TextBlock tb = new TextBlock();			
			tb.Text = "Check this to include " + sender.ToString() + " in the search space";
			tb.TextWrapping = TextWrapping.WrapWithOverflow;			
			tb.Measure(new Size());
			tb.Arrange(new Rect());			

			double tbH = tb.ActualHeight;
			CheckBox cb = new CheckBox
			{
				Margin = new Thickness(0, tbH + 20, 0, 0),
				HorizontalAlignment = HorizontalAlignment.Left,
				Name = sender.ToString(),
				IsChecked = settingsObj.IncludeSubtitle
			};
			cb.Unchecked += Cb_Unchecked;
			cb.Checked += Cb_Checked;

			g.Children.Add(tb);
			g.Children.Add(cb);

			return g;
		}

		private Grid GetAPIKeyControls(string s)
		{ 
			Grid g = new Grid();
			TextBlock t = new TextBlock
			{
				Margin = new Thickness(5, 5, 0, 0),
				Text = s + " key",
				VerticalAlignment = VerticalAlignment.Top
			};
			t.Measure(new Size());
			t.Arrange(new Rect());

			TextBox tb = new TextBox
			{
				Height = 20,
				Width = 150,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(5, 20, 0, 0),
				HorizontalAlignment = HorizontalAlignment.Left,
				Text = (s == "TVDB" ? settingsObj.TVDBKey : settingsObj.TMDBKey)
			};

			Button b = new Button
			{
				Width = 75,
				Height = 20,
				Margin = new Thickness(0, 20, 5, 0),
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Right,
				Content = "Update"
			};
			b.Click += (sender, EventArgs) => { Update_Click(sender, EventArgs, tb.Text, s); };

			g.Children.Add(t);
			g.Children.Add(tb);
			g.Children.Add(b);
			return g;
		}

		private Grid GetFormatControls()
		{
			Content.Children.Clear();

			Grid g = new Grid();
			TextBlock t = new TextBlock
			{
				Margin = new Thickness(5, 5, 0, 0),
				Text = "Series format",
				VerticalAlignment = VerticalAlignment.Top
			};
			t.Measure(new Size());
			t.Arrange(new Rect());

			TextBox tb = new TextBox
			{
				Height = 20,
				Width = 150,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(5, 20, 0, 0),
				HorizontalAlignment = HorizontalAlignment.Left,
				Text = ((settingsObj.CustomFormat == "") || (settingsObj.CustomFormat == null) ? "" : settingsObj.CustomFormat)
			};

			Button b = new Button
			{
				Width = 75,
				Height = 20,
				Margin = new Thickness(0, 20, 5, 0),
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Right,
				Content = "Set"
			};
			b.Click += (sender, EventArgs) => { SetFormat_Click(sender, EventArgs, tb.Text); };

			TextBlock workingKeyWords = new TextBlock
			{
				Margin = new Thickness(5, 30, 0, 0),
				Text = "Working keywords: Name (name of the series), Season, Episode (episode number), Title (name of the episode)",
				TextWrapping = TextWrapping.WrapWithOverflow,
				VerticalAlignment = VerticalAlignment.Top
			};
			workingKeyWords.Measure(new Size());
			workingKeyWords.Arrange(new Rect());

			TextBlock illegalCharacters = new TextBlock
			{
				Margin = new Thickness(5, 40, 0, 0),
				Text = @"Illegal characters (will be removed): <,>,\,/",
				TextWrapping = TextWrapping.WrapWithOverflow,
				VerticalAlignment = VerticalAlignment.Top				
			};
			illegalCharacters.Measure(new Size());
			illegalCharacters.Arrange(new Rect());

			g.Children.Add(t);
			g.Children.Add(tb);
			g.Children.Add(b);
			return g;
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

		private void UpdateControls(Grid g)
		{
			Content.Children.Clear();
			Content.Children.Add(g);
		}

		private void Update_Click(object sender, EventArgs e, string key, string type)
		{
			settingsObj.UpdateTVDBKey(key);
		}

		private void SetFormat_Click(object sender, EventArgs e, string format)
		{
			settingsObj.SetCustomFormat(Utility.ParseSeriesFormat(format).Item2);
		}

		private void Remove_Click(object sender, EventArgs e)
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
					UpdateListbox(lb, settingsObj.IncludeList);
					break;
				case Paths.Exclude:
					settingsObj.RemoveExclude(path);
					UpdateListbox(lb, settingsObj.ExcludeList);
					break;
				case Paths.Destinations:
					settingsObj.RemoveDestination(path);
					UpdateListbox(lb, settingsObj.DestinationList);
					break;
				default:
					break;
			}
		}

		private void Add_Click(object sender, EventArgs e)
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
					UpdateListbox(lb, settingsObj.IncludeList);
					break;
				case Paths.Exclude:
					settingsObj.AddExclude(path);					
					UpdateListbox(lb, settingsObj.ExcludeList);
					break;
				case Paths.Destinations:
					settingsObj.AddDestination(path);
					UpdateListbox(lb, settingsObj.DestinationList);
					break;
				default:
					break;
			}				
		}

		private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			switch (treeView.SelectedItem.ToString())
			{
				case nameof(Paths.Include):
					UpdateControls(GetPathsControls());
					UpdateListbox(lb, settingsObj.IncludeList);
					break;
				case nameof(Paths.Exclude):
					UpdateControls(GetPathsControls());
					UpdateListbox(lb, settingsObj.ExcludeList);
					break;
				case nameof(Paths.Destinations):
					UpdateControls(GetPathsControls());
					UpdateListbox(lb, settingsObj.DestinationList);
					break;
				case nameof(FileTypes.Episode):
					UpdateControls(GetFileTypesControls(treeView.SelectedItem));
					break;
				case nameof(FileTypes.Subtitle):
					UpdateControls(GetFileTypesControls(treeView.SelectedItem));
					break;
				case nameof(ApiKeys.TVDB):
					UpdateControls(GetAPIKeyControls("TVDB"));
					break;
				case nameof(ApiKeys.TMDB):
					UpdateControls(GetAPIKeyControls("MovieDB"));
					break;
				case nameof(ESettings.CustomFormat):
					UpdateControls(GetFormatControls());
					break;
				default:
					break;
			}
		}			
		
		private void UpdateListbox(ListBox lb, HashSet<string> data)
		{
			lb.ItemsSource = null;
			lb.ItemsSource = data;
		}
	}
}
