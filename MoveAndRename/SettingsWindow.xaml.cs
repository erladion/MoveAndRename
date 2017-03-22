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

namespace MoveAndRename
{
	enum Settings
	{
		Paths
	}
	/// <summary>
	/// Interaction logic for SettingsWindow.xaml
	/// </summary>
	public partial class SettingsWindow : Window
	{
		public int settingsHeight;
		private ListBox lb;
		private bool listVsTree = false;

		public SettingsWindow()
		{
			settingsHeight = (int)this.Height;
			InitializeComponent();

			listView.Height = this.Height - 60;
			listView.Items.Add(Settings.Paths);

			treeView.Height = this.Height - 60;
			treeView.Items.Add(Settings.Paths);

			if (listVsTree == true)
			{
				listView.Visibility = Visibility.Hidden;
			}
			else
			{
				treeView.Visibility = Visibility.Visible;
			}
		}	

		private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			settingsHeight = (int)this.Height;
			listView.Height = settingsHeight - 60;
			Content.Height = settingsHeight - 60;			
		}		
		
		private void listView_ItemActivate(object sender, MouseButtonEventArgs e)
		{
			try
			{
				if (listView.SelectedItem.ToString() == Settings.Paths.ToString())
				{
					addPathsControls();
					showIncludePaths();					
				}				
			}
			catch (Exception)
			{
				return;
			}
			
		}
		
		private void addPathsControls()
		{
			lb = new ListBox();
			lb.Height = settingsHeight - 150;
			lb.Width = 250;
			lb.VerticalAlignment = VerticalAlignment.Top;
			Content.Children.Add(lb);

			Button add = new Button();
			add.Content = "Add";
			add.Height = 25;
			add.Width = 100;
			add.HorizontalAlignment = HorizontalAlignment.Left;
			add.VerticalAlignment = VerticalAlignment.Bottom;
			add.Margin = new Thickness(5, 0, 0, 5);

			add.Click += new RoutedEventHandler(add_Click);
			Content.Children.Add(add);

			Button save = new Button();
			save.Content = "Save";
			save.Height = 25;
			save.Width = 100;
			save.HorizontalAlignment = HorizontalAlignment.Right;
			save.VerticalAlignment = VerticalAlignment.Bottom;
			save.Margin = new Thickness(0, 0, 5, 5);


			save.Click += new RoutedEventHandler(save_Click);
			Content.Children.Add(save);
		}		

		private void createPathsFile()
		{
			FileStream fs;
			StreamWriter sw = new StreamWriter("paths.txt");

			if (!File.Exists("paths.txt"))
			{
				fs = File.Create("paths.txt");
			}
			else
			{
				foreach (var item in lb.Items)
				{
					MessageBox.Show(item.ToString());
					sw.WriteLine(item.ToString());
				}
			}
			sw.Close();
		}

		private void save_Click(object sender, EventArgs e)
		{
			createPathsFile();
		}

		private void add_Click(object sender, EventArgs e)
		{
			var dialog = new System.Windows.Forms.FolderBrowserDialog();
			System.Windows.Forms.DialogResult res = dialog.ShowDialog();
			string path = dialog.SelectedPath;
			lb.Items.Add(path);			
		}

		private void showIncludePaths()
		{
			StreamReader sr = new StreamReader("paths.txt");

			if (sr.EndOfStream)
			{
				return;
			}
			string str = sr.ReadLine();
			while (str != "Exclude" || !sr.EndOfStream)
			{
				lb.Items.Add(str);
				if (!sr.EndOfStream)
				{
					str = sr.ReadLine();
				}
				else
				{
					break;
				}
			}
		}

		private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			if(treeView.SelectedItem.ToString() == Settings.Paths.ToString())
			{
				addPathsControls();
				showIncludePaths();
			}
		}
	}
}
