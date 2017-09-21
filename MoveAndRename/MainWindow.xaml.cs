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
		TVDB tvdb = new TVDB("0B4DFD2EB324D53C");
		private List<HashSet<Series>> seriesMatchesSet = new List<HashSet<Series>>();

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

		/*
		* Program structure order
		* Search for new series in "include"-directores ->
		* Search for the found series with TheTVDB and create series objects ->
		* Check if given series already have an exisiting folder in "destination"-directories ->
		* Move each series to the correct folder
		*/

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
			if(this.Height - 100 < 0)
			{
				listBox.Height = 0;
			}
			else
			{
				listBox.Height = this.Height - 100;
			}			
			listBox.Width = this.Width / 2 - 25;
			if (this.Height - 100 < 0)
			{
				listBox1.Height = 0;
			}
			else
			{
				listBox1.Height = this.Height - 100;
			}
			listBox1.Width = this.Width / 2 - 25;
			listBox1.Padding = new Thickness(0, 10, 15, 0);
			listBox1.HorizontalAlignment = HorizontalAlignment.Right;
			button.Margin = new Thickness(listBox.Width + 25, 0, 0, 10);
			moveButton.Margin = new Thickness(0, 0, 10, 10);
		}

		private void Settings_Click(object sender, RoutedEventArgs e)
        {
			setObj.readSettingsFromXml(settingsFilePath);
			settings = new SettingsWindow(setObj);
			settings.Show();
		}

		private List<string> cutPath(List<string> l)
		{
			List<string> res = new List<string>();
			foreach (var item in l)
			{
				foreach (var it in setObj.IncludeList)
				{
					if (item.Contains(it))
					{
						res.Add(item.Replace(it + "\\", ""));
					}
				}
			}
			return res;
		}

		private void refreshButton_Click(object sender, RoutedEventArgs e)
		{
			List<string> test = getNewSeries();
			test = cutPath(test);
			updateListbox(listBox, test);
		}

		private List<string> getFilesInDir(string dirPath)
		{
			Console.WriteLine(dirPath);
			string[] files = { };
			/*
			foreach (var inc in setObj.IncludeList)
			{
				files = Directory.GetFiles(inc + "\\"+ dirPath);
			}
			*/
			files = Directory.GetFiles(dirPath);
			List<string> fs = new List<string>();
			fs.AddRange(files.ToList<string>());
			//string[] files = Directory.GetFiles(dirPath);
			Console.WriteLine(files.Length);
			
			if(files.Length != 0)
			{
				List<string> foundFiles = new List<string>();
				for (int i = 0; i < files.Length; i++)
				{
					foreach (var item in Enum.GetValues(typeof(VideoExtensions)))
					{
						Console.WriteLine("Current item: " + item);
						Console.WriteLine(System.IO.Path.GetExtension(files[i]));
						if (System.IO.Path.GetExtension(files[i]) == "." + item.ToString())
						{
							Console.WriteLine("Add file based on PATH");
							foundFiles.Add(files[i]);
						}
						if (setObj.IncludeNfo)
						{
							Console.WriteLine("Include nfo: true");
							if (files[i].EndsWith(".nfo"))
							{
								foundFiles.Add(files[i]);
							}
						}
					}
				}
				Console.WriteLine("Found files size: " + foundFiles.Count);
				return foundFiles;
			}
			return new List<string>();
			
		}

		private List<string> getNewSeries()
		{
			//List<string> res = new List<string>();
			HashSet<string> includeList = setObj.IncludeList;

			List<string> newDirectories = new List<string>();
			HashSet<string> excludeList = setObj.ExcludeList;

			foreach (var item in includeList)
			{
				try
				{
					var directories = Directory.GetDirectories(item);
					for (int j = 0; j < directories.Length; j++)
					{
						if (excludeList.Contains(directories[j]))
						{
							continue;
						}
						newDirectories.Add(directories[j]);
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
			Console.WriteLine("Input string: " + str);
			Series ser;
			//string[] s = str.Split('.');
			string[] s = str.Split('\\');
			string[] fileN = s[s.Length - 1].Split('.');
			string pName = "";
			for (int i = 0; i < fileN.Length-1; i++)
			{
				pName += fileN[i];
			}
			string extension = fileN[fileN.Length - 1];
			string name = "";
			int season = 0;
			int episode = 0;

			bool gotName = false;
			// We split the name at every '.' and loop through it to decide what part it belongs to
			string[] s2 = s[s.Length - 2].Split('.');
			for (int i = 0; i < s2.Length; i++)
			{
				// If we get a regex match it means we got the name
				Match m = Regex.Match(s2[i], @"S([0-9]+)E([0-9]+)$", RegexOptions.IgnoreCase);
				Console.WriteLine("Name so far: " + name);

				if (m.Success)
				{
					gotName = true;
				}
				if (gotName)
				{
					Console.WriteLine("Got the name, printing rest: " + s2[i]);
					string[] t = s2[i].ToLower().Split('e');
					season = Convert.ToInt32(t[0].TrimStart('s'));
					episode = Convert.ToInt32(t[1]);
					Console.WriteLine("Season: " + t[0]);
					Console.WriteLine("Episode: " + t[1]);
					break;
				}
				else
				{
					name += s2[i] + " ";
				}
			}
			name.TrimEnd(' ');
			if(season == 0 || episode == 0)
			{
				return new Series();
			}
			else
			{
				string path = str;
				ser = new Series(name, season, episode,"", path, extension);
				ser.printSeries();
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

			var results = obj.Search(series.Name);

			foreach (var ser in results)
			{
				foreach (var episode in ser.Episodes)
				{
					if(episode.EpisodeNumber == series.Episode && episode.SeasonNumber == series.Season)
					{
						Series s = new Series(ser.Name, episode.EpisodeNumber, episode.SeasonNumber, episode.Title, series.CurrentPath, series.Extension);
						if (!hs.Contains(s))
						{
							hs.Add(s);
						}
					}
				}
			}

			foreach (var item in hs)
			{
				Console.WriteLine(item.Name);
			}
			return hs;
		}

		/*
		*	Episodes will follow the following naming structure
		*	SeriesTitle - seasonNumber x episodeNumber - episodeTitle . fileending
		*	Example:
		*	Taken - 1x02 - Random name.mp4
		*
		*/

		/// <summary>
		/// Small object which is used when matching a series name with a directory.
		/// </summary>
		struct DestObj {
			public int count;
			public string str;
			public string path;
		}

		/// <summary>
		/// Goes through all the destinationlist subdirectories and checks if any of them match the series name, and returns the one with the best match.
		/// Where the best match is the one that contains the most amount of substrings from the series name.
		/// </summary>
		/// <param name="ser"></param>
		private string getDestinationPath(Series ser)
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
							deo.path = subDirectories[i].ToString();
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

			Console.WriteLine("Returning: " + de[0].str + " Path: " + de[0].path);
			return de[0].path;
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

		private void moveFile(string from, string to)
		{
			bool fromOk = false;
			foreach (var item in setObj.IncludeList)
			{
				if (from.Contains(item))
				{
					fromOk = true;
					break;
				}
			}
			bool toOk = false;
			foreach (var item in setObj.DestinationList)
			{
				if (to.Contains(item))
				{
					toOk = true;
					break;
				}
			}
			if(fromOk && toOk)
			{
				File.Move(from, to);
			}
		}

		/// <summary>
		/// Returns all subtitle files in the specified folder, 
		/// the subtitles returned must have the file extension 
		/// .sub or .srt (specified in the enum SubtitleExtensions (more can be added if needed)).
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private List<string> findSubFiles(string path)
		{
			List<string> subFiles = new List<string>();
			foreach (var extension in Enum.GetValues(typeof(SubtitleExtensions)))
			{
				string[] files = Directory.GetFiles(path, "*." + extension, SearchOption.AllDirectories);
				subFiles.AddRange(files);
			}
			return subFiles;
		}

		/// <summary>
		/// Returns the first subtitle file which has the word eng in it, which probably means it is an english subtitle
		/// otherwise we return the first subtitle found.
		/// </summary>
		/// <param name="l"></param>
		/// <returns></returns>
		private string getBestSubFile(List<string> l)
		{
			string path = "";

			foreach (var sub in l)
			{
				Console.WriteLine("Current string: " + sub);
				Match m = Regex.Match(sub, @"eng", RegexOptions.IgnoreCase);
				Console.WriteLine("Current match: " + m.Value);
				if (m.Success)
				{
					path = sub;
					break;
				}
				else
				{
					if(l.Count > 0)
					{
						path = l[0];
					}					
				}
			}

			return path;
		}

		/// <summary>
		/// Moves a series file to the specified path
		/// </summary>
		/// <param name="path">Destination path</param>
		/// <param name="ser">Series object for the file to be moved</param>
        private void moveFile(HashSet<Series> serSet) {
			foreach (var ser in serSet)
			{
				string destinationPath = getDestinationPath(ser);
				ser.Name = ser.Name.Trim(' ');

				string printingEpisode = (ser.Episode < 10 ? "0" + ser.Episode.ToString() : ser.Episode.ToString());

				Console.WriteLine("Current path: " + ser.CurrentPath);
				Console.WriteLine("New Path: " + destinationPath + "\\" + ser.Name + " - " + ser.Season + "x" + printingEpisode + " - " + ser.Title + "." + ser.Extension);
				if(ser.CurrentPath != null && destinationPath+"//"+ser.Name+" - "+ser.Season+"x"+ser.Episode+" - "+ser.Title != null)
				{
					if (ser.Episode < 10)
					{
						File.Move(ser.CurrentPath, destinationPath + "//" + ser.Name + " - " + ser.Season + "x0" + ser.Episode + " - " + ser.Title + "." + ser.Extension);
					}
					else
					{
						File.Move(ser.CurrentPath, destinationPath + "//" + ser.Name + " - " + ser.Season + "x" + ser.Episode + " - " + ser.Title + "." + ser.Extension);
					}
					
				}
			}
		}

		// Remove the specified folder/file, but only if it is located in one of the folders specified in includeList
		// this makes sure so that we don't remove random stuff.
		private void removeFolder(string path)
		{
			try
			{
				foreach (var item in setObj.IncludeList)
				{
					if (path.Contains(item))
					{
						File.Delete(path);
					}
				}				
			}
			catch (Exception)
			{
				throw;
			}
		}

		private void showMatches(HashSet<Series> seriesSet)
		{
			string seasonNumber;
			string episodeNumber;
			foreach (var ser in seriesSet)
			{
				if(ser.Season < 10)
				{
					seasonNumber = "0" + ser.Season;
				}
				else
				{
					seasonNumber = ser.Season.ToString();
				}
				if(ser.Episode < 10)
				{
					episodeNumber = "0" + ser.Episode;
				}
				else
				{
					episodeNumber = ser.Episode.ToString();
				}
				listBox1.Items.Add(ser.Name + " " + "S" + seasonNumber + "E" + episodeNumber + " Name: " + ser.Title);
			}
		}

		private void showNewSeriesMatches()
		{
			List<string> ls = getNewSeries();

			List<string> filesInDir = new List<string>();
			for (int i = 0; i < ls.Count; i++)
			{
				filesInDir.AddRange(getFilesInDir(ls[i]));
			}

			updateListbox(listBox1, ls);
			List<Series> s = convertStringToSeries(filesInDir);

			List<HashSet<Series>> lh = new List<HashSet<Series>>();
			foreach (var item in s)
			{
				HashSet<Series> h = findMatch(tvdb, item);
				lh.Add(h);
			}

			foreach (var set in lh)
			{
				if (set.Count > 0)
				{
					Console.WriteLine("Before move file");
					foreach (var item in set)
					{
						item.printSeries();
					}
					showMatches(set);
					seriesMatchesSet.Add(set);
				}
			}
		}

		private void moveSeriesMatches()
		{
			foreach (var item in seriesMatchesSet)
			{
				moveFile(item);
			}			
		}

		private TVDB createTVDBObj()
		{
			var tvdb = new TVDB("0B4DFD2EB324D53C");
			return tvdb;
		}

		private void move_Click(object sender, RoutedEventArgs e)
		{
			moveSeriesMatches();
		}

		private void button_Click(object sender, RoutedEventArgs e)
		{

			//Series ser = new Series("A hidden rock", 1, 1);

			//getFilesInDir(str);


			//TVDB t = createTVDBObj();
			/*
			TVDB t = tvdb;

            List<string> ls = getNewSeries();

			List<string> filesInDir = new List<string>();
			for (int i = 0; i < ls.Count; i++)
			{
				Console.WriteLine(ls[i]);
				filesInDir.AddRange(getFilesInDir(ls[i]));
			}

			Console.WriteLine("Printing all files found in directories");
			Console.WriteLine(filesInDir.Count);
			foreach (var item in filesInDir)
			{
				Console.WriteLine(item);
			}
			Console.WriteLine("_______________________________________");

            updateListbox(listBox1,ls);
            List<Series> s = convertStringToSeries(filesInDir);

            List<HashSet<Series>> lh = new List<HashSet<Series>>();
            foreach (var item in s)
            {
                HashSet<Series> h = findMatch(t, item);
                lh.Add(h);
            }

            foreach (var set in lh)
            {
				if (set.Count > 0)
				{
					Console.WriteLine("Before move file");
					foreach(var item in set)
					{
						item.printSeries();
					}
					showMatches(set);
					moveFile(set);
				}
            }
			*/
			showNewSeriesMatches();
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
			if(a.Title == b.Title)
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
			return s.Title.GetHashCode();
		}
	}
}
