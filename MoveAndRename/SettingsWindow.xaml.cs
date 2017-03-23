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
	enum Paths
	{
		Exclude,
		Include
	}
	/// <summary>
	/// Interaction logic for SettingsWindow.xaml
	/// </summary>
	public partial class SettingsWindow : Window
	{
		public int settingsHeight;
		private ListBox lb;
		private bool listVsTree = false;
		private List<string> excludeList = new List<string>();
		private List<string> includeList = new List<string>();

		public SettingsWindow()
		{
			settingsHeight = (int)this.Height;
			InitializeComponent();

			listView.Height = this.Height - 60;
			listView.Items.Add(Settings.Paths);

			treeView.Height = this.Height - 60;
			//treeView.Items.Add(Settings.Paths);

			TreeViewItem tvi = new TreeViewItem();
			tvi.Header = Settings.Paths;
			List<Paths> p = new List<Paths>();
			foreach (Paths item in Enum.GetValues(typeof(Paths)))
			{
				p.Add(item);
			}
			tvi.ItemsSource = p;

			treeView.Items.Add(tvi);
			

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
			StreamWriter sw;

			if (!File.Exists("paths.txt"))
			{
				fs = File.Create("paths.txt");
				fs.Close();
			}			

			sw = new StreamWriter("paths.txt");			
				
			sw.WriteLine("Include " + includeList.Count);
			foreach (var includeItem in includeList)
			{
				sw.WriteLine(includeItem);
			}
			
			sw.WriteLine("Exclude " + excludeList.Count);
			foreach (var excludeItem in excludeList)
			{
				sw.WriteLine(excludeItem);
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

			bool includeOrExclude = treeView.SelectedItem.ToString() == "Include" ? true : false;
			if (includeOrExclude)
			{
				includeList.Add(path);
				lb.ItemsSource = includeList;
			}
			else
			{
				excludeList.Add(path);
				lb.ItemsSource = excludeList;
			}
			createPathsFile();
			showPaths(includeOrExclude ? "Include" : "Exclude");			
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
				includeList.Add(str);
				if (!sr.EndOfStream)
				{
					str = sr.ReadLine();
				}
				else
				{
					break;
				}
			}
			sr.Close();
			lb.ItemsSource = includeList;
		}		

		private void showPaths(string includeOrExclude)
		{
			if (File.Exists("paths.txt"))
			{
				StreamReader sr = new StreamReader("paths.txt");

				if (includeOrExclude == "Include")
				{
					includeList = new List<string>();
					if (sr.EndOfStream)
					{
						return;
					}
					string str = sr.ReadLine();
					while (!str.Contains("Exclude") || !sr.EndOfStream)
					{
						if (str.Contains("Include"))
						{
							str = sr.ReadLine();
							if (str.Contains("Exclude"))
							{
								break;
							}
							includeList.Add(str);
						}
						if (!sr.EndOfStream)
						{
							if (str.Contains("Exclude"))
							{
								break;
							}
							str = sr.ReadLine();							
						}
						else
						{
							break;
						}
					}
					sr.Close();
					lb.ItemsSource = includeList;
				}
				else
				{
					excludeList = new List<string>();
					if (sr.EndOfStream)
					{
						return;
					}
					string[] s = sr.ReadLine().Split(' ');
					for (int i = 0; i < Convert.ToInt32(s[1]); i++)
					{
						sr.ReadLine();
						continue;
					}
					s = sr.ReadLine().Split(' ');
					for (int i = 0; i < Convert.ToInt32(s[1]); i++)
					{
						string st = sr.ReadLine();
						excludeList.Add(st);
					}
					lb.ItemsSource = excludeList;
					sr.Close();
				}
			}
			else
			{
				createPathsFile();
			}
		}

		private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			if(treeView.SelectedItem.ToString() == Paths.Include.ToString())
			{
				addPathsControls();
				//showIncludePaths();
				showPaths(Paths.Include.ToString());
			}
			else if(treeView.SelectedItem.ToString() == Paths.Exclude.ToString())
			{
				addPathsControls();
				showPaths(Paths.Exclude.ToString());
			}
		}
	}
}
