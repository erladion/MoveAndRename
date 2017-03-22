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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

namespace MoveAndRename
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
		SettingsWindow settings;
        public MainWindow()
        {
            InitializeComponent();
			settings = new SettingsWindow();
			refreshButton.VerticalAlignment = VerticalAlignment.Bottom;
			listBox.Height = this.Height - 150;
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
			settings.Show();		
		}

		private void refreshButton_Click(object sender, RoutedEventArgs e)
		{
			getNewSeries();
		}

		private List<string> getNewSeries()
		{
			List<string> res = new List<string>();
			StreamReader sr = new StreamReader("paths.txt");

			List<string> newDirectories = new List<string>();

			while (!sr.EndOfStream)
			{
				res.Add(sr.ReadLine());
			}
			for (int i = 0; i < res.Count; i++)
			{
				try
				{
					var directories = Directory.GetDirectories(res[i]);
					for (int j = 0; j < directories.Length; j++)
					{
						newDirectories.Add(directories[j]);
					}
				}
				catch (Exception)
				{
					continue;					
				}
				
			}
			for (int i = 0; i < newDirectories.Count; i++)
			{
				listBox.Items.Add(newDirectories[i]);
			}

			return res;
		}
	}
}
