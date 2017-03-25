using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using Microsoft.Win32;
using System.Xml;
using System.Xml.Linq;

namespace MoveAndRename
{
	enum ESettings
	{
		Paths
	}
	enum Paths
	{
		Exclude,
		Include,
		Destinations
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

			settingsObj = settingsObject;		
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
		}		
		
		private void addPathsControls()
		{
			if(Content.Children.Count > 0)
			{
				Content.Children.RemoveAt(0);
			}

			lb = new ListBox();
			lb.Height = settingsHeight - 150;
			lb.Width = settingsWidth * 0.55;
			lb.VerticalAlignment = VerticalAlignment.Top;
			lb.HorizontalAlignment = HorizontalAlignment.Right;
			Content.Children.Insert(0, lb);

			Button add = new Button();
			add.Content = "Add";
			add.Height = 20;
			add.Width = 75;
			add.HorizontalAlignment = HorizontalAlignment.Left;
			add.VerticalAlignment = VerticalAlignment.Bottom;
			add.Margin = new Thickness(0, 0, 0, 5);

			add.Click += new RoutedEventHandler(add_Click);
			Content.Children.Add(add);
			
			Button remove = new Button();
			remove.Content = "Remove";
			remove.Height = 20;
			remove.Width = 75;
			remove.VerticalAlignment = VerticalAlignment.Bottom;
			remove.HorizontalAlignment = HorizontalAlignment.Left;
			remove.Margin = new Thickness(add.Width+5, 0, 0, 5);			

			remove.Click += new RoutedEventHandler(remove_Click);
			Content.Children.Add(remove);			
		}		

		private void remove_Click(object sender, EventArgs e)
		{
			Paths p = (Paths)treeView.SelectedItem;
			string path = lb.SelectedItem.ToString();

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
		}			
		
		private void updateListbox(ListBox lb, HashSet<string> data)
		{
			lb.ItemsSource = null;
			lb.ItemsSource = data;
		}
	}
}
