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
using TVDBSharp;
using System.Text.RegularExpressions;

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
			refreshButton.VerticalAlignment = VerticalAlignment.Bottom;
			listBox.Height = this.Height - 100;
        }

		private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			listBox.Height = this.Height - 100;
			listBox.Width = this.Width / 2 - 25;
			listBox1.Height = this.Height - 100;
			listBox1.Width = this.Width / 2 - 25;
			listBox1.Padding = new Thickness(0, 10, 15, 0);
			listBox1.HorizontalAlignment = HorizontalAlignment.Right;
			button.Margin = new Thickness(listBox.Width + 25, 0, 0, 10);
		}

		private void Settings_Click(object sender, RoutedEventArgs e)
        {
			settings = new SettingsWindow();
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

			List<string> excludeList = new List<string>();
			

			while (!sr.EndOfStream)
			{
				string str = sr.ReadLine();
				string[] sp = str.Split(' ');
				for (int i = 0; i < Convert.ToInt32(sp[1]); i++)
				{
					res.Add(sr.ReadLine());
				}
				str = sr.ReadLine();
				string[] sp2 = str.Split(' ');
				for (int i = 0; i < Convert.ToInt32(sp2[1]); i++)
				{
					excludeList.Add(sr.ReadLine());
				}				
			}
			sr.Close();
			for (int i = 0; i < res.Count; i++)
			{
				try
				{
					var directories = Directory.GetDirectories(res[i]);
					for (int j = 0; j < directories.Length; j++)
					{
						if (excludeList.Contains(directories[j]))
						{
							continue;
						}						
						newDirectories.Add(directories[j].Replace(res[i] + "\\", ""));
					}
				}
				catch (Exception)
				{
					continue;					
				}
				
			}
            /*
			for (int i = 0; i < newDirectories.Count; i++)
			{
				listBox.Items.Add(newDirectories[i]);
			}
            */

            return newDirectories;
		}

        private void updateList(List<string> strList)
        {
            listBox.ItemsSource = strList;
        }

		private List<Series> convertStringToSeries(List<String> stringList)
		{
			List<Series> res = new List<Series>();

			foreach (var str in stringList)
			{
				Series s = createSeries(str);
				// If we get a series with name "" it means something went wrong in the parsing and we don't include that
				if(s.Name != "")
				{
					res.Add(s);
				}
			}

			return res;
		}

		private Series createSeries(string str)
		{
			Series ser;
			string[] s = str.Split('.');
			string name = "";
			int season = 0;
			int episode = 0;		
			
			bool gotName = false;
			// We split the name at every '.' and loop through it to decide what part it belongs to
			for (int i = 0; i < s.Length; i++)
			{
				// If we get a regex match it means we got the name
				Match m = Regex.Match(s[i], @"S([0-9]+)E([0-9]+)$", RegexOptions.IgnoreCase);
				
				if (m.Success)
				{
					gotName = true;
				}
				if (gotName)
				{
					string[] t = s[i].ToLower().Split('e');					
					season = Convert.ToInt32(t[0].TrimStart('s'));
					episode = Convert.ToInt32(t[1]);
				}
				else
				{
					name += s[i] + " ";
				}
			}
			name.TrimEnd(' ');
			if(season == 0 || episode == 0)
			{
				return new Series();
			}
			else
			{			
				ser = new Series(name, season, episode);
			}
			return ser;
		}

		/// <summary>
		/// Takes in a Series object and returns possible matches
		/// </summary>
		/// <param name="series">
		/// A Series object to look for in the tvdb
		/// </param>
		private HashSet<Series> findMatch(TVDB obj, Series series)
		{
			HashSet<Series> hs = new HashSet<Series>(new SeriesComparer());
			//List<Series> res = new List<Series>();

			var results = obj.Search(series.Name, 20);

			foreach (var ser in results)
			{
				//MessageBox.Show(ser.ImdbId);
				foreach (var episode in ser.Episodes)
				{
					if(episode.EpisodeNumber == series.Episode && episode.SeasonNumber == series.Season)
					{
						Series s = new Series(ser.Name, episode.EpisodeNumber, episode.SeasonNumber, episode.Title);
						if (!hs.Contains(s))
						{
							hs.Add(s);
						}
					}
				}				
			}
			return hs;
		}

        /*
         * TODO: Change settings file from .txt to .xml
         * 
         * 
         */

        private void moveFile(string path, Series ser) {

            String destination = "";
            File.Move(path, destination);
        }
        
		private void showMatches(HashSet<Series> seriesSet)
		{
			string seasonNumber;
			string episodeNumber;
			foreach (var ser in seriesSet)
			{
				seasonNumber = "";
				episodeNumber = "";
				if(ser.Season < 10)
				{
					seasonNumber += "0" + ser.Season;
				}
				if(ser.Episode < 10)
				{
					episodeNumber += "0" + ser.Episode;
				}
				listBox1.Items.Add(ser.Name + " " + "S" + seasonNumber + "E" + episodeNumber + " Name: " + ser.EpisodeName);
			}
		}

		private TVDB createTVDBObj()
		{
			var tvdb = new TVDB("0B4DFD2EB324D53C");
			return tvdb;
		}

		private void button_Click(object sender, RoutedEventArgs e)
		{
			TVDB t = createTVDBObj();

            List<string> ls = getNewSeries();
            updateList(ls);
            List<Series> s = convertStringToSeries(ls);

            List<HashSet<Series>> lh = new List<HashSet<Series>>();
            foreach (var item in s)
            {
                HashSet<Series> h = findMatch(t, item);
                lh.Add(h);
            }

            foreach (var set in lh)
            {
                showMatches(set);
            }
			//HashSet<Series> res = findMatch(t, new Series("Homeland", 6, 9));
			//showMatches(res);
		}		
	}

	class SeriesComparer : IEqualityComparer<Series>
	{
		public bool Equals(Series a, Series b)
		{
			if(a.EpisodeName == b.EpisodeName)
			{
				return true;
			}
			else
			{
				return false;
			}
		}
		public int GetHashCode(Series s)
		{
			return s.EpisodeName.GetHashCode();
		}
	}
}
