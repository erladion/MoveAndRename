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
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace MoveAndRename
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
		public bool debug = true;
		SettingsWindow settings;
		public HashSet<string> excludeList = new HashSet<string>();
		public HashSet<string> includeList = new HashSet<string>();
		public HashSet<string> destinationList = new HashSet<string>();
		Settings setObj = new Settings();
		private string settingsFilePath = "settings.xml";
		
        public MainWindow()
        {
			if (debug)
			{
				AllocConsole();
			}			
			InitializeComponent();
			refreshButton.VerticalAlignment = VerticalAlignment.Bottom;
			listBox.Height = this.Height - 100;
			setObj.readSettingsFromXml(settingsFilePath);

			setObj.PropertyChanged += settingsChanged;
        }

		private void setTooltips()
		{
			ToolTip t = new ToolTip();
			t.IsOpen = true;
			t.StaysOpen = false;
			t.Content = "Search for new series episodes or movies in the directores given in settings.";
			refreshButton.ToolTip = t;
		}

		// Used to a attach a console window to the application, the console window is used when in debug mode.
		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool AllocConsole();

		private void settingsChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "settings")
			{
				setObj.writeSettingsToXml(settingsFilePath);			
			}
		}

		private void updateListbox(ListBox lb, HashSet<string> data)
		{
			lb.ItemsSource = null;
			lb.ItemsSource = data;
		}

		private void updateListbox(ListBox lb, List<string> data)
		{
			lb.ItemsSource = null;
			listBox.ItemsSource = data;
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
			setObj.readSettingsFromXml(settingsFilePath);
			settings = new SettingsWindow(setObj);
			settings.Show();			
		}

		private void refreshButton_Click(object sender, RoutedEventArgs e)
		{
			getNewSeries();
		}

		private List<string> getNewSeries()
		{
			List<string> res = new List<string>();

			List<string> newDirectories = new List<string>();
			HashSet<string> excludeList = setObj.ExcludeList;			

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
            return newDirectories;
		}        

		/// <summary>
		/// Converts a list of strings representing serieses to a list of series objects.
		/// See createSeries(string str) for implementation of one string to series object.
		/// </summary>
		/// <param name="stringList"></param>
		/// <returns></returns>
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


		/*
		*
		* TODO: Might need to change createSeries so that it regex matches the S??E?? and takes everything infront and use as name, 
		* might be faster than what is currently being done. What is currently being done might cause a problem if the series name actually contains a dot.
		*
		*/

		/// <summary>
		/// Converts a string representing a series to a series object.
		/// Example of a string is "SeriesName.S03E02"
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
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
		/// Takes in a Series object and returns possible matches from TheTVDB.
		/// </summary>
		/// <param name="series">
		/// A Series object to look for in the tvdb
		/// </param>
		private HashSet<Series> findMatch(TVDB obj, Series series)
		{
			HashSet<Series> hs = new HashSet<Series>(new SeriesComparer());

			var results = obj.Search(series.Name, 20);

			foreach (var ser in results)
			{
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

		/// <summary>
		/// Small object which is used when matching a series name with a directory.
		/// </summary>
		struct DestObj {
			public int count;
			public string str;
		}
		
		/// <summary>
		/// Goes through all the destinationlist subdirectories and checks if any of them match the series name, and returns the one with the best match.
		/// Where the best match is the one that contains the most amount of substrings from the series name.
		/// </summary>
		/// <param name="ser"></param>
		private void getDestinationPath(Series ser)
		{
			List<string> possibleDirectories = new List<string>();
			List<DestObj> de = new List<DestObj>();
			Console.WriteLine(setObj.DestinationList.Count);
			foreach (var item in setObj.DestinationList)
			{
				string[] subDirectories = Directory.GetDirectories(item);
				
				for (int i = 0; i < subDirectories.Length; i++)
				{
					List<string> subdirname = subDirectories[i].Split('\\').ToList();					
					string[] splitSer = ser.Name.ToLower().Split(' ');
					DestObj deo = new DestObj();
					deo.count = 0;
					deo.str = "";
					for (int j = 0; j < splitSer.Length; j++)
					{
						Console.WriteLine("---");
						Console.WriteLine("Currently checking if: " + subdirname.Last().ToLower() + " contains " + splitSer[j].ToLower());
						if (subdirname.Last().ToLower().Contains(splitSer[j].ToLower())) {
							Console.WriteLine("Yes it did");
							deo.count++;
							deo.str += splitSer[j] + " ";
						}
						Console.WriteLine("---");
					}
					if (!de.Contains(deo))
					{
						de.Add(deo);
					}					
				}				
			}

			de.Sort(new DestObjComparer());
			Console.WriteLine("Searched for: " + ser.Name);
			Console.WriteLine("Best match: " + de[0].str);
			Console.WriteLine("_________________________");
			foreach (var item in de)
			{
				Console.WriteLine("Matches: " + item.count.ToString() + " str " + item.str);
			}
			Console.WriteLine("_________________________");
		}

		/// <summary>
		/// Comparer class for DestObj
		/// </summary>
		private class DestObjComparer : IComparer<DestObj>
		{
			public int Compare(DestObj a, DestObj b)
			{
				if(a.count < b.count)
				{
					return 1;
				}
				else if(a.count > b.count)
				{
					return -1;
				}
				else
				{
					return 0;
				}
			}
		}

		/// <summary>
		/// Moves a series file to the specified path
		/// </summary>
		/// <param name="path">Destination path</param>
		/// <param name="ser">Series object for the file to be moved</param>
        private void moveFile(string path, Series ser) {
            String destination = "";
			var dirs = Directory.GetDirectories("");
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
			Series ser = new Series("A hidden rock", 1, 1);
			getDestinationPath(ser);
			/*
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
			*/
		}

		private void refreshButton_ToolTipOpening(object sender, ToolTipEventArgs e)
		{
			Button b = sender as Button;
			b.ToolTip = "Search for new series episodes or movies in the directores given in settings.";
		}

		private void button_ToolTipOpening(object sender, ToolTipEventArgs e)
		{
			
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
