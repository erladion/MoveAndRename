using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using TVDBSharp;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using TMDbLib.Client;

namespace MoveAndRename
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public bool debug = true;
		private string settingsFilePath = "settings.xml";
		SettingsWindow settings;
		Settings setObj = new Settings();
		public HashSet<string> excludeList = new HashSet<string>();
		public HashSet<string> includeList = new HashSet<string>();
		public HashSet<string> destinationList = new HashSet<string>();
		private List<HashSet<Series>> seriesMatchesSet = new List<HashSet<Series>>();
		private Tuple<List<string>, List<string>> newSeriesDirectoriesOrFiles;
		private FileController fc;
		private TVDB tvdb;
		private TMDbClient tmdb;

		public MainWindow()
		{
			// If debug is set to true, we first allocate a console,
			// then we create a listener for Debug.WriteLine so that our debug messages gets printed in the console.
			if (debug)
			{
				AllocConsole();
				TextWriterTraceListener writer = new TextWriterTraceListener(System.Console.Out);
				Debug.Listeners.Add(writer);
			}
			InitializeComponent();

			Debug.WriteLine(Environment.GetEnvironmentVariable("TEMP"));

			refreshButton.VerticalAlignment = VerticalAlignment.Bottom;
			listBox.Height = this.Height - 100;
			setObj.ReadSettingsFromXml(settingsFilePath);

			setObj.PropertyChanged += SettingsChanged;

			fc = new FileController(setObj);
			tvdb = CreateTVDBObj();

			string[] cmdLine = Environment.GetCommandLineArgs();
			if (cmdLine.Length > 0)
			{
				foreach (var item in cmdLine)
				{
					Debug.WriteLine(item);
					try
					{
						if (item == "-r")
						{
							Debug.WriteLine("We are here");
							// Implement here so that a search is done whenever the argument -r is passed via cmd
							// Can be used with programs that can run other programs whenever a task is finished
							newSeriesDirectoriesOrFiles = GetNewSeries();
							Debug.WriteLine("1");
							ShowNewSeriesMatches();
							Debug.WriteLine("2");
							MoveSeriesMatches();
							Debug.WriteLine("3");
							UpdateListbox(listBox1, new List<string>());
							UpdateListbox(listBox, new List<string>());
							//updateKodiLibrary();
							Debug.WriteLine("4");
							Application.Current.Shutdown();
						}
						else
						{
							continue;
						}
					}
					catch (Exception e)
					{
						Utility.LogMessageToFile(e.ToString());
					}					
				}
			}
		}

		private void SetTooltips()
		{
			ToolTip t = new ToolTip
			{
				IsOpen = true,
				StaysOpen = false,
				Content = "Search for new series episodes or movies in the directores given in settings."
			};
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

		private TMDbClient CreateTMDbObj()
		{
			return new TMDbClient(setObj.TMDBKey);
		}

		private TVDB CreateTVDBObj()
		{			
			return new TVDB(setObj.TVDBKey);
		}

		private void SettingsChanged(object sender, PropertyChangedEventArgs e)
		{
			Debug.WriteLine(new StackTrace().GetFrame(1).GetMethod().Name);
			if (e.PropertyName == "settings")
			{
				setObj.WriteSettingsToXml(settingsFilePath);
			}
		}

		private void UpdateListbox(ListBox lb, HashSet<string> data)
		{
			lb.ItemsSource = null;
			lb.ItemsSource = data;
		}

		private void UpdateListbox(ListBox lb, List<string> data)
		{
			lb.ItemsSource = null;
			listBox.ItemsSource = data;
		}

		private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (this.Height - 100 < 0)
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
			setObj.ReadSettingsFromXml(settingsFilePath);
			settings = new SettingsWindow(setObj);
			settings.Show();
		}

		private void RefreshButton_Click(object sender, RoutedEventArgs e)
		{
			newSeriesDirectoriesOrFiles = GetNewSeries();
			List<string> newSeries = newSeriesDirectoriesOrFiles.Item1;
			newSeries = CutPath(newSeries);
			UpdateListbox(listBox, newSeries);
		}

		private void Move_Click(object sender, RoutedEventArgs e)
		{
			MoveSeriesMatches();
			UpdateListbox(listBox1, new List<string>());
			UpdateListbox(listBox, new List<string>());
			//updateKodiLibrary();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			ShowNewSeriesMatches();
		}

		private void RefreshButton_ToolTipOpening(object sender, ToolTipEventArgs e)
		{
			Button b = sender as Button;
			b.ToolTip = "Search for new series episodes or movies in the directores given in settings.";
		}

		private void Button_ToolTipOpening(object sender, ToolTipEventArgs e)
		{

		}

		private List<string> CutPath(List<string> l)
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

		private Tuple<List<string>, List<string>> GetNewSeries()
		{
			Debug.WriteLine(String.Format("-----Currently in {0}-----", MethodBase.GetCurrentMethod().Name));
			HashSet<string> includeList = setObj.IncludeList;

			List<string> newDirectories = new List<string>();
			List<string> newFiles = new List<string>();
			HashSet<string> excludeList = setObj.ExcludeList;
			HashSet<string> destList = setObj.DestinationList;

			foreach (var item in includeList)
			{
				try
				{
					string[] t = Directory.GetFiles(item);
					newFiles.AddRange(t);
					Debug.WriteLine("Writing found files");
					foreach (var i in newFiles)
					{
						Debug.WriteLine(i);
					}
					var directories = Directory.GetDirectories(item);
					for (int j = 0; j < directories.Length; j++)
					{
						if (excludeList.Contains(directories[j]) || destList.Contains(directories[j]))
						{
							continue;
						}
						newDirectories.Add(directories[j]);
					}
				}
				catch (Exception e)
				{
					Utility.LogMessageToFile(e.ToString());
					continue;
				}
			}
			return Tuple.Create(newDirectories, newFiles);
		}

		/// <summary>
		/// Converts a list of strings representing serieses to a list of series objects.
		/// See CreateSeries(string str) for implementation of one string to series object.
		/// </summary>
		/// <param name="stringList"></param>
		/// <returns></returns>
		private List<Series> ConvertStringToSeries(List<String> stringList)
		{
			Debug.WriteLine(String.Format("-----Currently in {0}-----", MethodBase.GetCurrentMethod().Name));
			List<Series> res = new List<Series>();

			foreach (var str in stringList)
			{
				Series s = CreateSeries(str);
				// If we get a series with name "" it means something went wrong in the parsing and we don't include that
				if (s.Name != "")
				{
					res.Add(s);
				}
			}
			return res;
		}

		private List<Series> ConvertFolderToSeries(List<String> stringList)
		{
			List<Series> res = new List<Series>();

			foreach (var item in stringList)
			{



			}

			return res;
		}

		private List<Series> ConvertFileToSeries(List<String> stringList)
		{
			List<Series> res = new List<Series>();
			foreach (var item in stringList)
			{
				res.Add(CreateSeriesFromFile(item));
			}
			return res; 
		}

		private Series CreateSeriesFromFile(string s)
		{
			string[] splitStr = s.Split('\\');
			string[] splitN = splitStr.Last().Split('.');
			string ext = splitN.Last();
			string path = s.Substring(0, s.Length - ext.Length + 1);
			string name = "";
			string season = "";
			string episode = "";
			foreach (var currentPart in splitN)
			{
				Match m = Regex.Match(currentPart, @"S([0-9]+)E([0-9]+)$", RegexOptions.IgnoreCase);
				if (m.Success)
				{
					// We split at e, so we will get sXX and YY
					// so we know index 0 will have sXX and index 1 YY.
					string[] u = currentPart.ToLower().Split('e');
					season = u[0].TrimStart('s');
					episode = u[1];
					break;
				}
				name += currentPart + " ";
			}
			name.TrimEnd(' ');
			return new Series(name, Convert.ToInt32(season), Convert.ToInt32(episode), "", path, ext);
		}       

		private Series CreateSeriesTest(string str)
		{
			Debug.WriteLine(String.Format("-----Currently in {0}-----", MethodBase.GetCurrentMethod().Name));

			string[] s = str.Split('\\');

			string extension = str.Split('.').Last();
			for (int i = s.Length-1; i >= 0; i--)
			{
				string[] t = Regex.Split(s[i], @"S([0-9]+)E([0-9]+)");
				if(t.Length == 1)
				{
					continue;
				}
				else
				{
					if(((string[])Enum.GetValues(typeof(VideoExtensions))).ToList().Contains(extension))
					{
						Series se = new Series(t[0].TrimEnd('.'), t[1], t[2], "", str, extension);
						return se;
					}					
				}
			}
			return new Series();
		}

		private Movie CreateMovie(string str)
		{
			Debug.WriteLine(String.Format("-----Currently in {0}-----", MethodBase.GetCurrentMethod().Name));
			Debug.WriteLine("Input string: " + str);
			string[] s = str.Split('\\');

			string ext = str.Split('.').Last();
			Debug.WriteLine("Extension: " + ext);

			string name = "";
			string year = "";
			bool gotYear = false;
			foreach (var part in s.Last().Split('.').Reverse())
			{
				Debug.WriteLine("Current part of the string: " + part);
				if (!gotYear)
				{
					Match t = Regex.Match(part, @"^\d{4}$");

					// Since we are looping backwards we know that we will get the year of the movie first, then the name.
					if (t.Success)
					{
						Debug.WriteLine("Succes!, we got the year");
						gotYear = true;
						year = t.Value;
					}
					else
					{
						continue;
					}
				}
				else
				{
					name = name.Insert(0, part + " ");
				}
			}
			name = name.TrimEnd(' ');
			Debug.WriteLine("Gotten name: " + name);

			if(gotYear != false)
			{
				return new Movie(name, year)
				{
					CurrentPath = str,
					Extension = ext
				};
			}

			return new Movie();
		}

		/*
		*
		* TODO: Might need to change CreateSeries so that it regex matches the S??E?? and takes everything infront and use as name,
		* might be faster than what is currently being done. What is currently being done might cause a problem if the series name actually contains a dot.
		*
		*/

		/// <summary>
		/// Converts a string representing a series to a series object.
		/// Example of a string is "SeriesName.S03E02"
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		private Series CreateSeries(string str)
		{
			Debug.WriteLine(String.Format("-----Currently in {0}-----", MethodBase.GetCurrentMethod().Name));
			Debug.WriteLine("Input string: " + str);
			Series ser;
			string[] s = str.Split('\\');			
			string extension = s.Last().Split('.').Last();
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
				Debug.WriteLine("Name so far: " + name);

				if (m.Success)
				{
					gotName = true;
				}
				if (gotName)
				{
					Debug.WriteLine("Got the name, printing rest: " + s2[i]);
					string[] t = s2[i].ToLower().Split('e');
					season = Convert.ToInt32(t[0].TrimStart('s'));
					episode = Convert.ToInt32(t[1]);
					Debug.WriteLine("Season: " + season);
					Debug.WriteLine("Episode: " + episode);
					break;
				}
				else
				{
					name += s2[i] + " ";
				}
			}
			if (season == 0 || episode == 0)
			{
				return new Series();
			}
			else
			{
				string path = str;
				Debug.WriteLine("Season: " + season);
				Debug.WriteLine("Episode: " + episode);
				ser = new Series(name.TrimEnd(' '), season, episode, "", path, extension);
			}
			return ser;
		}

		/// <summary>
		/// Takes in a Series object and returns possible matches from TheTVDB.
		/// </summary>
		/// <param name="series">
		/// A Series object to look for in the tvdb
		/// </param>
		private HashSet<Series> FindMatch(TVDB obj, Series series)
		{
			Debug.WriteLine(String.Format("-----Currently in {0}-----", MethodBase.GetCurrentMethod().Name));
			HashSet<Series> hs = new HashSet<Series>(new SeriesComparer());

			var results = obj.Search(series.Name);

			if(results.Count == 0)
			{
				// If we get no match (probably because of weird additions to the name, we try to match it with currently exisiting folders and use the one with highest score)
				List<string> dirs = new List<string>();
				foreach (var item in setObj.DestinationList)
				{
					dirs.AddRange(Directory.GetDirectories(item));
				}
				Tuple<string, double> res = FindBestStringMatch(dirs, series.Name);

				Debug.WriteLine("Got no match with tvdb");
				Debug.WriteLine("Best matching folder already existing");
				Debug.WriteLine(res.Item1);
				Debug.WriteLine(res.Item2);
				results = obj.Search(res.Item1.Split('\\').Last());
			}

			foreach (var ser in results)
			{
				foreach (var episode in ser.Episodes)
				{
					if (episode.EpisodeNumber == series.Episode && episode.SeasonNumber == series.Season)
					{
						Series s = new Series(ser.Name, episode.SeasonNumber, episode.EpisodeNumber, episode.Title, series.CurrentPath, series.Extension)
						{
							GotSubtitle = series.GotSubtitle,
							SubtitlePath = series.SubtitlePath,
							DestinationPath = series.DestinationPath
						};
						if (!hs.Contains(s))
						{
							hs.Add(s);
						}
					}
				}
			}

			foreach (var item in hs)
			{
				Debug.WriteLine(item.Name);
			}
			return hs;
		}

		/*
		*	Episodes will follow the following naming structure
		*	SeriesTitle - seasonNumber x episodeNumber - episodeTitle.fileending
		*	Example:
		*	Taken - 1x02 - Random name.mp4
		*/

        private Tuple<string, double> FindBestStringMatch(List<string> list, string str)
        {
			Debug.WriteLine(String.Format("-----Currently in {0}-----", MethodBase.GetCurrentMethod().Name));
			list.Sort();
            List<StringCostObj> impList = new List<StringCostObj>();
            for (int i = 0; i < list.Count; i++)
            {
                StringCostObj temp = new StringCostObj();
                temp.SetOriginal(list[i]);
                list[i] = list[i].ToLower();
                string[] splLis = list[i].Split(' ');

                string tempStr = "";
                for (int j = 0; j < splLis.Length; j++)
                {
                    if (splLis[j] != "the")
                    {
                        if (j == splLis.Length - 1)
                        {
                            tempStr += splLis[j];
                        }
                        else
                        {
                            tempStr += splLis[j] + " ";
                        }
                    }
                }
                temp.SetChanged(tempStr);
                impList.Add(temp);
            }
             
            foreach (var path in impList)
            {
                foreach (var destPath in setObj.DestinationList)
                {
                    if (path.GetChanged().Contains(destPath.ToLower()))
                    {
                        var cosineCost = Utility.CosineSimilarity(path.GetChanged().Replace(destPath.ToLower(), ""), str.ToLower(), 2);
						path.SetCost(cosineCost);
                    }
                }
                
            }

            impList.Sort(new StringCostObjComparer());
			
            return new Tuple<string, double>(impList[0].GetOriginal(), impList[0].GetCost());
        }

        private class StringCostObj
        {
            private string original;
            private string changed;
            private double cost;

            public void SetOriginal(string a)
            {
                this.original = a;
            }
            public void SetChanged(string a)
            {
                this.changed = a;
            }
            public void SetCost(double a)
            {
                this.cost = a;
            }

            public string GetOriginal()
            {
                return this.original;
            }
            public string GetChanged()
            {
                return this.changed;
            }
            public double GetCost()
            {
                return this.cost;
            }
        }

        class StringCostObjComparer : IComparer<StringCostObj>
        {
            public int Compare(StringCostObj a, StringCostObj b)
            {
                if (a.GetCost() == b.GetCost())
                {
                    return 0;
                }
                else if (a.GetCost() > b.GetCost())
                {
                    return -1;
                }
                else
                {
                    return 1;
                }                
            }
        }                

        /// <summary>
        /// Goes through all the destinationlist subdirectories and checks if any of them match the series name, and returns the one with the best match.
        /// Where the best match is the one that contains the most amount of substrings from the series name.
        /// </summary>
        /// <param name="ser"></param>
        private string GetDestinationPath(Series ser)
		{
			List<string> possibleDirectories = new List<string>();
			Debug.WriteLine(setObj.DestinationList.Count);
            string bestMatch = "";

			foreach (var item in setObj.DestinationList)
			{
				string[] subDirectories = Directory.GetDirectories(item);
				
				if (subDirectories.Length == 0)
				{
					break;
				}
				// TODO CHECK THIS
				Tuple<string, double> bestMatchTuple = FindBestStringMatch(subDirectories.ToList(), ser.Name);

				Debug.WriteLine("Best string found: " + bestMatchTuple.Item1);
				Debug.WriteLine("Cosine value: " + bestMatchTuple.Item2);

				if (bestMatchTuple.Item2 < 0.8)
				{
					bestMatch = "";
				}
				else
				{
					bestMatch = bestMatchTuple.Item1;
				}
			}             
            return bestMatch;
		}

		/// <summary>
		/// Returns all subtitle files in the specified folder, 
		/// the subtitles returned must have the file extension 
		/// .sub or .srt (specified in the enum SubtitleExtensions (more can be added if needed)).
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private List<string> FindSubFiles(string path)
		{
			Debug.WriteLine("-------------Searching for sub files-------------");
			List<string> subFiles = new List<string>();
			int subsFound = 0;
			foreach (var extension in Enum.GetValues(typeof(SubtitleExtensions)))
			{
				string[] files = Directory.GetFiles(path, "*." + extension, SearchOption.AllDirectories);
				subFiles.AddRange(files);
				subsFound += files.Length;
			}
			Debug.WriteLine("Found " + subsFound + " sub files");
			Debug.WriteLine("-------------Done searching for sub files-------------");

			return subFiles;
		}

		/// <summary>
		/// Returns the first subtitle file which has the word eng in it, which probably means it is an english subtitle
		/// otherwise we return the first subtitle found.
		/// </summary>
		/// <param name="l"></param>
		/// <returns></returns>
		private string GetBestSubFile(List<string> l)
		{
			string path = "";

			foreach (var sub in l)
			{
				Debug.WriteLine("Current string: " + sub);
				Match m = Regex.Match(sub, @"eng", RegexOptions.IgnoreCase);
				Debug.WriteLine("Current match: " + m.Value);
				if (m.Success)
				{
					path = sub;
					break;
				}
				else
				{
					if (l.Count > 0)
					{
						path = l[0];
					}
				}
			}
			return path;
		}

		private class Language
		{
			public string Value { get; set; }
			private Language(string value) { Value = value; }

			public Language()
			{
			}

			public static Language English { get { return new Language("eng"); } }

			public static string Swedish => "swe";
		}

		private string GetBestMatchingSubFile(List<string> l, string mainLang)
		{
			Language lan = new Language();

			PropertyInfo[] properties = typeof(Language).GetProperties();
			foreach (PropertyInfo property in properties)
			{
				Debug.WriteLine(property.Name);
			}

			return "";
		}

		private void ShowMatches(HashSet<Series> seriesSet)
		{
			string seasonNumber;
			string episodeNumber;
			foreach (var ser in seriesSet)
			{
				seasonNumber = (ser.Season < 10 ? "0" + ser.Season : ser.Season.ToString());
				episodeNumber = (ser.Season < 10 ? "0" + ser.Episode : ser.Episode.ToString());

				TreeView root = new TreeView();
				TreeViewItem tvi = new TreeViewItem
				{
					Header = ser.Name + " " + "S" + seasonNumber + "E" + episodeNumber + " Name: " + ser.Title
				};
				tvi.Items.Add(new TreeViewItem().Header = ("Current path: " + ser.CurrentPath));
				tvi.Items.Add(new TreeViewItem().Header = ("Destination path: " + ser.DestinationPath));
				tvi.Items.Add(new TreeViewItem().Header = ("Got subtitle: " + (ser.GotSubtitle ? "Yes" : "No")));
				if (ser.GotSubtitle)
				{
					tvi.Items.Add(new TreeViewItem().Header = ("Subtitle path: " + ser.SubtitlePath));
				}
				root.Items.Add(tvi);

				listBox1.Items.Add(root);
			}
		}
        /*
		private void updateKodiLibrary()
		{
			KodiControl kc = new KodiControl("192.168.0.101", "8080");
			kc.UpdateLibrary();
		}
        */
		private void ShowNewSeriesMatches()
		{
			List<string> ls = newSeriesDirectoriesOrFiles.Item1;
			List<string> filesInDir = new List<string>();
			for (int i = 0; i < ls.Count; i++)
			{
				filesInDir.AddRange(fc.GetFilesInDir(ls[i]));
			}

			ls.AddRange(newSeriesDirectoriesOrFiles.Item2);

			UpdateListbox(listBox1, CutPath(ls));
			// TODO
			//
			// Fix here so that we don't search for sub files for the individual series files.
			//

			List<Series> s = ConvertStringToSeries(filesInDir);
			List<Series> u = ConvertFileToSeries(newSeriesDirectoriesOrFiles.Item2);
			Debug.WriteLine(u.Count);

			s.AddRange(u);

			// Test to add subtitle path to the series object
			foreach (var ser in s)
			{
				ser.PrintSeries();

				// Count how many chars there are after the last "\"
				int charsToRemove = 0;
				for (int i = ser.CurrentPath.Length - 1 ; i >= 0; i--)
				{
					if (ser.CurrentPath[i] == '\\')
					{
						charsToRemove++;
						break;
					}
					charsToRemove++;
				}
				
				int count = 0;
				foreach (var item in setObj.IncludeList)
				{
					if (Directory.GetParent(ser.CurrentPath).ToString() == item)
					{
						count++;
					}
				}
				List<string> l = new List<string>();
				// Cut our current path string at the end, removing as many chars as we just counted
				string subStartPath = ser.CurrentPath.Substring(0, ser.CurrentPath.Length - charsToRemove);

				if (count == 0)
				{
					// Search for sub files in the folder
					l = FindSubFiles(subStartPath);
				}
				
				// Set the correct fields if we got a subtitle
				if(l.Count > 0)
				{
					ser.GotSubtitle = true;
					// If we happen to get more than 1 result from the "FindSubFiles" method 
					// we get the one we consider the "best" where "best" means it either contains "eng" or we get the first one
					ser.SubtitlePath = GetBestSubFile(l);
				}

				foreach (var i in l)
				{
					Debug.WriteLine(i);
				}

				ser.PrintSeries();
			}

			List<HashSet<Series>> lh = new List<HashSet<Series>>();
			foreach (var item in s)
			{
				HashSet<Series> h = FindMatch(tvdb, item);
				lh.Add(h);
			}
			
			foreach (var set in lh)
			{
                Debug.WriteLine("Set count: " + set.Count);
				if (set.Count > 0)
				{
                    if(set.Count > 1)
                    {
                        string choice = ChoiceWindow.GetChoice(set);
                        Debug.WriteLine(choice);

                        foreach (var item in set)
                        {
                            Debug.WriteLine("Comparing: " + item.Name + " with " + choice);
                            if(item.Name == choice)
                            {
								HashSet <Series> ne = new HashSet<Series>();
								SetSeriesDestinationPath(item);
								ne.Add(item);
                                seriesMatchesSet.Add(ne);
                                break;
                            }
                        }                           
                    }
                    else
                    {
						HashSet<Series> updatedSet = new HashSet<Series>();
						foreach (var item in set)
						{
							SetSeriesDestinationPath(item);
							updatedSet.Add(item);
						}
                        seriesMatchesSet.Add(updatedSet);
                    }
					Debug.WriteLine("Before move file");
					foreach (var item in set)
					{
						item.PrintSeries();
					}
					ShowMatches(set);
				}                
			}
		}

		private void SetSeriesDestinationPath(Series s)
		{
			Debug.WriteLine("------Before getting destination path------");
			string destinationPath = GetDestinationPath(s);
			// We got no current folder for the series, so we create one.
			if (destinationPath.Length == 0)
			{
				Debug.WriteLine("No folder exists for the series, so we create one");
				if (setObj.DestinationList.Count > 1)
				{
					ChoiceWindow cw = new ChoiceWindow(setObj.DestinationList);
					cw.ShowDialog();
					destinationPath = cw.choice;
				}
				else
				{
					destinationPath = setObj.DestinationList.First();
				}
				s.Name = Utility.SanitizeString(s.Name);
				destinationPath += '\\' + s.Name;
				Debug.WriteLine("Folder name after creation: " + destinationPath);
			}
			Debug.WriteLine("Current destination path: " + destinationPath);
			destinationPath += "\\Season " + s.Season;
			Debug.WriteLine("Current destination path: " + destinationPath);

			s.Name = Utility.SanitizeString(s.Name.Trim(' '));
			s.Title = Utility.SanitizeString(s.Title);

			Debug.WriteLine("Series title: " + s.Title);

			Tuple<List<int>, string> format = Utility.ParseSeriesFormat(setObj.CustomFormat);
			if ((setObj.CustomFormat != "" || setObj.CustomFormat != null) && format.Item2 != "") {
				string fm = "\\"+format.Item2;
				destinationPath += String.Format(fm, Utility.SeriesParameters);
				destinationPath += String.Format(fm, s.Name, s.Season, s.Episode, s.Title);
			}
			else
			{
				destinationPath += String.Format("\\{0} - {1}{2} - {3}.{4}", s.Name, s.Season, (s.Episode < 10 ? "x0" : "x") + s.Episode, s.Title, s.Extension);
			}

			//destinationPath += '\\' + s.Name + " - " + s.Season + (s.Episode < 10 ? "x0" : "x") + s.Episode + " - " + s.Title + "." + s.Extension;

			s.DestinationPath = destinationPath;
		}

		private void MoveSeriesMatches()
		{
			foreach (var item in seriesMatchesSet)
			{
				fc.MoveFile(item);
			}
		}		

		private void Test_Click(object sender, RoutedEventArgs e)
		{
			Utility.ParseSeriesFormat("Name - Episode . Season Title - Name - Name.txt");

			Debug.WriteLine("------Testing------");

			string str1 = "Q:\\TestFiles\\Quantico.S01E02.randomstuff\\Quantico.S01E02.randomstuff.mp4";
			string str2 = "Q:\\TestFiles\\The.Good.Doctor.s01e04.test\\The.Good.Doctor.s01e04.test.mkv";
			string str3 = "Q:\\TestFiles\\GoodMovie.2015.1080p.randomstuff\\GoodMovie.2015.1080p.randomstuff.mp4";

			Debug.WriteLine(String.Format("Current string: {0}", str2));

			Series s = CreateSeries(str2);
			string str = GetDestinationPath(s);
			s.PrintSeries();
			Debug.WriteLine(str);

			Tuple<List<int>, string> parsedFormat = Utility.ParseSeriesFormat("Name . Season - Episode");
			Debug.WriteLine(parsedFormat.Item2);

			tmdb = CreateTMDbObj();
			HashSet<Movie> t;

			t = MovieUtility.FindMovieMatches(tmdb, new Movie("Godzilla", 2014));

			/*
			Dictionary<int, KeyValuePair<string, string>> test = MovieUtility.GetFileProps(@"2.Guns.2013.720p.BluRay.H264.AAC - RARBG.mp4");

			foreach (var item in test)
			{
				Debug.WriteLine(item.Value);
			}
			*/
			CreateMovie(str3).PrintMovie(); ;
		}
	}

	class SeriesComparer : IEqualityComparer<Series>
	{
		public bool Equals(Series a, Series b)
		{
			if (a.Title == b.Title)
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
