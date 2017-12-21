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
		private Tuple<List<string>, List<string>> newSeriesFF;

		private KodiControl kc = new KodiControl("192.168.0.101", "8080");

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

			Debug.WriteLine(System.Environment.GetEnvironmentVariable("TEMP"));

			refreshButton.VerticalAlignment = VerticalAlignment.Bottom;
			listBox.Height = this.Height - 100;
			setObj.readSettingsFromXml(settingsFilePath);

			setObj.PropertyChanged += settingsChanged;

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
							newSeriesFF = getNewSeries();
							Debug.WriteLine("1");
							showNewSeriesMatches();
							Debug.WriteLine("2");
							moveSeriesMatches();
							Debug.WriteLine("3");
							updateListbox(listBox1, new List<string>());
							updateListbox(listBox, new List<string>());
							updateKodiLibrary();
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
						LogMessageToFile(e.ToString());
					}					
				}
			}

			kc.UpdateLibrary();
		}

		public string GetTempPath()
		{
			string path = System.Environment.GetEnvironmentVariable("TEMP");
			if (!path.EndsWith("\\")) path += "\\";
			return path;
		}

		public void LogMessageToFile(string msg)
		{
			System.IO.StreamWriter sw = System.IO.File.AppendText(
				GetTempPath() + "My Log File.txt");
			try
			{
				string logLine = System.String.Format(
					"{0:G}: {1}.", System.DateTime.Now, msg);
				sw.WriteLine(logLine);
			}
			finally
			{
				sw.Close();
			}
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
			newSeriesFF = getNewSeries();
			List<string> test = newSeriesFF.Item1;
			test = cutPath(test);
			updateListbox(listBox, test);
		}

		private List<string> getFilesInDir(string dirPath)
		{
			Debug.WriteLine("-----Currently in getFilesInDir-----");
			Debug.WriteLine(dirPath);
			string[] files = { };

			if (Directory.Exists(dirPath))
			{
				files = Directory.GetFiles(dirPath);
			}
			List<string> fs = new List<string>();
			fs.AddRange(files);
			Debug.WriteLine(files.Length);

			if (files.Length != 0)
			{
				List<string> foundFiles = new List<string>();
				for (int i = 0; i < files.Length; i++)
				{
					foreach (var item in Enum.GetValues(typeof(VideoExtensions)))
					{
						Debug.WriteLine("Current item: " + item);
						Debug.WriteLine(System.IO.Path.GetExtension(files[i]));
						if (System.IO.Path.GetExtension(files[i]) == "." + item.ToString())
						{
							Debug.WriteLine("Add file based on PATH");
							foundFiles.Add(files[i]);
						}
						if (setObj.IncludeNfo)
						{
							Debug.WriteLine("Include nfo: true");
							if (files[i].EndsWith(".nfo"))
							{
								foundFiles.Add(files[i]);
							}
						}
					}
				}
				Debug.WriteLine("Found files size: " + foundFiles.Count);
				return foundFiles;
			}
			return new List<string>();

		}

		private Tuple<List<string>, List<string>> getNewSeries()
		{
			Debug.WriteLine("-----Currently in getNewSeries-----");
			//List<string> res = new List<string>();
			HashSet<string> includeList = setObj.IncludeList;

			List<string> newDirectories = new List<string>();
			List<string> newFiles = new List<string>();
			HashSet<string> excludeList = setObj.ExcludeList;

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
						if (excludeList.Contains(directories[j]))
						{
							continue;
						}
						newDirectories.Add(directories[j]);
					}
				}
				catch (Exception e)
				{
					LogMessageToFile(e.ToString());
					continue;
				}
			}
			return Tuple.Create(newDirectories, newFiles);
		}

		/// <summary>
		/// Converts a list of strings representing serieses to a list of series objects.
		/// See createSeries(string str) for implementation of one string to series object.
		/// </summary>
		/// <param name="stringList"></param>
		/// <returns></returns>
		private List<Series> convertStringToSeries(List<String> stringList)
		{
			Debug.WriteLine("-----Currently in convertStringToSeries-----");
			List<Series> res = new List<Series>();

			foreach (var str in stringList)
			{
				Series s = createSeries(str);
				// If we get a series with name "" it means something went wrong in the parsing and we don't include that
				if (s.Name != "")
				{
					res.Add(s);
				}
			}
			return res;
		}

		private List<Series> convertFolderToSeries(List<String> stringList)
		{
			List<Series> res = new List<Series>();

			return res;
		}

		private List<Series> convertFileToSeries(List<String> stringList)
		{
			List<Series> res = new List<Series>();
			foreach (var item in stringList)
			{
				res.Add(createSeriesFromFile(item));
			}
			return res; 
		}

		private Series createSeriesFromFile(string s)
		{
			string path = s.Substring(0, s.Length - 4);
			string ext = s.Substring(s.Length - 3, 3);
			string[] splitStr = s.Split('\\');
			string[] splitN = splitStr[splitStr.Length-1].Split('.');
			string name = "";
			string season = "";
			string episode = "";
			for (int i = 0; i < splitN.Length-1; i++)
			{
				Match m = Regex.Match(splitN[i], @"S([0-9]+)E([0-9]+)$", RegexOptions.IgnoreCase);
				if (m.Success)
				{
					string[] u = splitN[i].ToLower().Split('e');
					season = u[0].TrimStart('s');
					episode = u[1];
					break;
				}
				name += splitN[i] + " ";
			}
			name.TrimEnd(' ');
			return new Series(name, Convert.ToInt32(season), Convert.ToInt32(episode), "", path, ext);
		}

        private Series regexMatch(string s)
        {
            string[] m = Regex.Split(s, @"S([0-9]+)E([0-9]+)", RegexOptions.IgnoreCase);
            //Match m = Regex.Match(s, @"S([0-9]+)E([0-9]+)", RegexOptions.IgnoreCase);
            Debug.WriteLine("In regex match");            
            Debug.WriteLine(m.Length);
            for (int i = 0; i < m.Length; i++)
            {
                Debug.WriteLine("Part: " + i);
                Debug.WriteLine("Content: " + m[i]);
                Debug.WriteLine("-----------------");
            }

            return new Series(m[0], Convert.ToInt32(m[1]), Convert.ToInt32(m[2]));
        }

        /// <summary>
        /// Removes unwanted characters from a string, mainly to be used on strings for paths
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private string sanitizeString(string str)
        {
            Debug.WriteLine("Input string: " + str);
            string retStr = "";

            // Hopefully this covers all needed cases         
            List<char> unwantedChars = Path.GetInvalidPathChars().ToList();
            char[] f = Path.GetInvalidFileNameChars();            
            foreach (var item in f)
            {
                if (!unwantedChars.Contains(item))
                {
                    unwantedChars.Add(item);
                }
            }

            for (int i = 0; i < str.Length; i++)
            {
                int count = 0;
                for (int j = 0; j < unwantedChars.Count; j++)
                {
                    if(str[i] == unwantedChars[j])
                    {
                        retStr += "";
                        count++;
                        break;
                    }
                }
                if(count == 0)
                {
                    retStr += str[i];
                }
                count = 0;
            }

            return retStr;
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
			Debug.WriteLine("-----Currently in createSeries-----");
			Debug.WriteLine("Input string: " + str);
			Series ser;
			//string[] s = str.Split('.');
			string[] s = str.Split('\\');
			string[] fileN = s[s.Length - 1].Split('.');
			string pName = "";
			for (int i = 0; i < fileN.Length - 1; i++)
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
			name.TrimEnd(' ');
			if (season == 0 || episode == 0)
			{
				return new Series();
			}
			else
			{
				string path = str;
				Debug.WriteLine("Season: " + season);
				Debug.WriteLine("Episode: " + episode);
				ser = new Series(name, season, episode, "", path, extension);
				//ser.printSeries();
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
			Debug.WriteLine("-----Currently in findMatch-----");
			HashSet<Series> hs = new HashSet<Series>(new SeriesComparer());

			var results = obj.Search(series.Name);

			foreach (var ser in results)
			{
				foreach (var episode in ser.Episodes)
				{
					if (episode.EpisodeNumber == series.Episode && episode.SeasonNumber == series.Season)
					{
						Series s = new Series(ser.Name, episode.SeasonNumber, episode.EpisodeNumber, episode.Title, series.CurrentPath, series.Extension);
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
		*	SeriesTitle - seasonNumber x episodeNumber - episodeTitle . fileending
		*	Example:
		*	Taken - 1x02 - Random name.mp4
		*
		*/

		/// <summary>
		/// Small object which is used when matching a series name with a directory.
		/// </summary>
		struct DestObj
		{
			public int count;
			public string str;
			public string path;
		}

        private string BinarySearch(List<string> list, string str)
        {
            int l = 0;
            int r = list.Count - 1;
            // If we are search for a string in an empty list we terminate
            if (l > r)
            {
                return "";
            }

            list.Sort();
            for (int i = 0; i < list.Count; i++)
            {
                list[i] = list[i].ToLower();
                string[] splLis = list[i].Split(' ');

                string tempStr = "";
                for (int j = 0; j < splLis.Length; j++)
                {
                    if(splLis[j] != "the")
                    {
                        if(j == splLis.Length - 1)
                        {
                            tempStr += splLis[i];
                        }
                        else
                        {
                            tempStr += splLis[i] + " ";
                        }
                    }
                }             
            }

            int m = (l + r) / 2;
            
            return "";
        }

        private int levenshtein(string a, string b)
        {
            int n = a.Length;
            int m = b.Length;
            int[,] d = new int[n + 1, m + 1];

            if(n == 0)
            {
                return m;
            }

            if(m == 0)
            {
                return n;
            }

            for (int i = 0; i <= n; d[i,0] = i++)
            {

            }
            for (int j = 0; j <= m; d[0,j] = j++)
            {

            }

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (b[j - 1] == a[i - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }

        private Tuple<string, double> findBestStringMatch(List<string> list, string str)
        {
            list.Sort();
            List<LevenstheinObj> impList = new List<LevenstheinObj>();
            for (int i = 0; i < list.Count; i++)
            {
                LevenstheinObj temp = new LevenstheinObj();
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
             
            foreach (var item in impList)
            {
                foreach (var it in setObj.DestinationList)
                {
                    //Debug.WriteLine("Checking if: " + item.GetChanged());
                    //Debug.WriteLine("contains: " + it.ToLower());
                    //Debug.WriteLine("Original string: " + item.GetOriginal());
                    if (item.GetChanged().Contains(it.ToLower()))
                    {
                        //var levenshteinCost = levenshtein(item.GetChanged().Replace(it.ToLower(), ""), str.ToLower());
                        var cosineCost = cosineSimiliarity(item.GetChanged().Replace(it.ToLower(), ""), str.ToLower(), 2);
                        //Debug.WriteLine("Levenshtein: " + levenshteinCost);
                        //Debug.WriteLine("Cosine: " + cosineCost);
                        item.SetCost(cosineCost);
                    }
                }
                
            }

            impList.Sort(new LevenstheinObjComparer());

            /*
            foreach (var item in impList)
            {
                Debug.WriteLine("-------------------------------");
                Debug.WriteLine("Current string before change: " + item.GetOriginal());
                Debug.WriteLine("Current string after change: " + item.GetChanged());
                Debug.WriteLine("Cost: " + item.GetCost());
                Debug.WriteLine("-------------------------------");
            }
            */

            return new Tuple<string, double>(impList[0].GetOriginal(), impList[0].GetCost());
            //return impList[0].GetOriginal();
        }

        private class LevenstheinObj
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

        class LevenstheinObjComparer : IComparer<LevenstheinObj>
        {
            public int Compare(LevenstheinObj a, LevenstheinObj b)
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

        private List<string> createNGrams(string str, int n)
        {
            if (n > str.Length)
            {
                return new List<string>();
            }

            List<string> nGrams = new List<string>();

            for (int i = 0; i < str.Length-n+1; i++)
            {
                string s = str.Substring(i, n);
                nGrams.Add(s);
            }

            return nGrams;
        }

        private double cosineSimiliarity(string a, string b, int nGrams)
        {
            List<string> aNGrams = createNGrams(a, nGrams);
            List<string> bNGrams = createNGrams(b, nGrams);           

            List<string> abGrams = aNGrams.Union(bNGrams).ToList();

            List<int> aFreq = Enumerable.Repeat(0, abGrams.Count).ToList();
            List<int> bFreq = Enumerable.Repeat(0, abGrams.Count).ToList();
            for (int i = 0; i < abGrams.Count; i++)
            {
                aFreq[i] = countStringOccurences(a, abGrams[i]);
                bFreq[i] = countStringOccurences(b, abGrams[i]);
            }
           
            /*
            for (int i = 0; i < abGrams.Count; i++)
            {
                Debug.WriteLine("Word: " + abGrams[i] + " A count: " + aFreq[i] + " B count: " + bFreq[i]);
            }
            */

            double dotProduct = 0;
            for (int i = 0; i < aFreq.Count; i++)
            {
                dotProduct += (aFreq[i] * bFreq[i]);
            }
            //Debug.WriteLine("Dotproduct: " + dotProduct);

            double magnitudeA = magnitude(aFreq);
            //Debug.WriteLine("Magnitued for A: " + magnitudeA);
            double magnitudeB = magnitude(bFreq);
            //Debug.WriteLine("Magnitued for B: " + magnitudeB);

            double ret = dotProduct / (magnitudeA * magnitudeB);
            //Debug.WriteLine("Answer: " + ret);

            return ret;
        }

        private double magnitude(List<int> li)
        {
            double magnitudeA = 0;
            for (int i = 0; i < li.Count; i++)
            {
                magnitudeA += li[i] * li[i];
            }

            return Math.Sqrt(magnitudeA);
        }

        private int countStringOccurences(string s, string p)
        {
            int count = 0;
            int i = 0;
            while ((i = s.IndexOf(p, i)) != -1)
            {
                i += p.Length;
                count++;
            }
            return count;
        }


        private void testFunc(string test)
        {
            foreach (var item in setObj.DestinationList)
            {
                string[] subDirs = Directory.GetDirectories(item);

                findBestStringMatch(subDirs.ToList(), test);
            }
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
			Debug.WriteLine(setObj.DestinationList.Count);
            string bestMatch = "";

            foreach (var item in setObj.DestinationList)
			{
				string[] subDirectories = Directory.GetDirectories(item);

                // TODO CHECK THIS
                Tuple <string, double> bestMatchTuple = findBestStringMatch(subDirectories.ToList(), ser.Name);

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

                /*
				for (int i = 0; i < subDirectories.Length; i++)
				{
					List<string> subdirname = subDirectories[i].Split('\\').ToList();
					string[] splitSer = ser.Name.ToLower().Split(' ');
					DestObj deo = new DestObj();
					deo.count = 0;
					deo.str = "";
					for (int j = 0; j < splitSer.Length; j++)
					{
						Debug.WriteLine("---");
						Debug.WriteLine("Currently checking if: " + subdirname.Last().ToLower() + " contains " + splitSer[j].ToLower());
						if (subdirname.Last().ToLower().Contains(splitSer[j].ToLower()))
						{
							Debug.WriteLine("Yes it did");
							deo.count++;
							deo.str += splitSer[j] + " ";
							deo.path = subDirectories[i].ToString();
						}
						Debug.WriteLine("---");
					}
					if (!de.Contains(deo))
					{
						de.Add(deo);
					}
				}
                */
			}
            /*
			de.Sort(new DestObjComparer());
			Debug.WriteLine("Searched for: " + ser.Name);
			Debug.WriteLine("Best match: " + de[0].str);
			Debug.WriteLine("_________________________");
			foreach (var item in de)
			{
				Debug.WriteLine("Matches: " + item.count.ToString() + " str " + item.str);
			}
			Debug.WriteLine("_________________________");

			Debug.WriteLine("Returning: " + de[0].str + " Path: " + de[0].path);
			if(de[0].path == null)
			{
				return "";
			}
            
			return de[0].path;
            */
            return bestMatch;
		}

		/// <summary>
		/// Comparer class for DestObj
		/// </summary>
		private class DestObjComparer : IComparer<DestObj>
		{
			public int Compare(DestObj a, DestObj b)
			{
				if (a.count < b.count)
				{
					return 1;
				}
				else if (a.count > b.count)
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
		/// Moves a file from "from" to "to" with some additional checks to make sure we are moving the correct file
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <returns></returns>
		private bool moveFile(string from, string to)
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
			if (fromOk && toOk)
			{
				DirectoryInfo f = new FileInfo(to).Directory;
				if (Directory.Exists(f.FullName)){
					File.Move(from, to);
				}
				else
				{
					new FileInfo(to).Directory.Create();
					File.Move(from, to);					
				}
				return true;

			}
			return false;
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

		/// <summary>
		/// Moves a series file to the specified path
		/// </summary>
		/// <param name="path">Destination path</param>
		/// <param name="ser">Series object for the file to be moved</param>
		private void moveFile(HashSet<Series> serSet)
		{
			foreach (var ser in serSet)
			{
				string destinationPath = getDestinationPath(ser);
				if(destinationPath.Length == 0)
				{
                    Debug.WriteLine("No folder exists for the series, so we create one");
					// We got no current folder for the series, so we create one.
					// TODO: Write code to create the correct folder
					if(setObj.DestinationList.Count > 1)
					{
						ChoiceWindow cw = new ChoiceWindow(setObj.DestinationList);
						cw.ShowDialog();
						destinationPath = cw.choice;
					}
					else
					{
						destinationPath = setObj.DestinationList.First();
					}
                    ser.Name = sanitizeString(ser.Name);
                    destinationPath += '\\' + ser.Name;
                    Debug.WriteLine("Folder name after creation: " + destinationPath);
				}
				Debug.WriteLine("Current destination path: " + destinationPath);
				destinationPath += '\\' + "Season " + ser.Season;
				Debug.WriteLine("Current destination path: " + destinationPath);
				ser.Name = ser.Name.Trim(' ');

				string printingEpisode = (ser.Episode < 10 ? "0" + ser.Episode.ToString() : ser.Episode.ToString());

                ser.Name = sanitizeString(ser.Name);
                ser.Title = sanitizeString(ser.Title);

				if (ser.Episode < 10)
				{
					destinationPath += '\\' + ser.Name + " - " + ser.Season + "x0" + ser.Episode + " - " + ser.Title + "." + ser.Extension;
				}
				else
				{
					destinationPath += '\\' + ser.Name + " - " + ser.Season + "x" + ser.Episode + " - " + ser.Title + "." + ser.Extension;
				}


				Debug.WriteLine("Current path: " + ser.CurrentPath);
				Debug.WriteLine("Destination path: " + destinationPath);
				if (ser.CurrentPath != null && destinationPath + "//" + ser.Name + " - " + ser.Season + "x" + ser.Episode + " - " + ser.Title != null)
				{
					bool movedFile = false;
					string sp = ser.CurrentPath;
					string ext = "." + ser.Extension;
					if (ser.CurrentPath.Contains(ext))
					{
						movedFile = moveFile(ser.CurrentPath, destinationPath);
					}
					else
					{
						movedFile = moveFile(ser.CurrentPath + "." + ser.Extension, destinationPath);
					}					
					// If the setting to include subtitles is set, we first get the subtitle which is in the appropriate language (english)
					// then we use the path where we are sending our "series" file and we simply send the sub there as well, with the same name
					// so it gets associated with the "series".
					/*
					if (setObj.IncludeSubtitle)
					{
						List<string> subFiles = findSubFiles(System.IO.Path.GetDirectoryName(ser.CurrentPath));
						if (subFiles.Count > 0)
						{
							string sub = getBestSubFile(subFiles);

							string s = System.IO.Path.GetFileNameWithoutExtension(destinationPath);
							string fileExt = System.IO.Path.GetExtension(sub);

							moveFile(sub, s + "." + fileExt);
						}
					}
					*/
					if (movedFile)
					{
						if(sp.Contains(ext))
						{
							removeFolder(System.IO.Path.GetDirectoryName(sp));
						}
						else
						{
							removeFolder(sp);
						}						
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
						Debug.WriteLine("Currently checking: " + path);
						Debug.WriteLine("Against: " + item);
						Debug.WriteLine(Directory.Exists(path));
						if (Directory.Exists(path))
						{
							Debug.WriteLine("Before deleting folder");
							
							string[] files = Directory.GetFiles(path);
							string[] dirs = Directory.GetDirectories(path);

							foreach (var file in files)
							{
								File.SetAttributes(file, FileAttributes.Normal);
								File.Delete(file);
							}

							foreach (var dir in dirs)
							{
								removeFolder(dir);
							}
							Directory.Delete(path,false);
						}
						// Should probably never happen, as we move the series/movie file.
						else if(File.Exists(path))
						{
							File.SetAttributes(path, FileAttributes.Normal);
							File.Delete(path);
						}
																		
					}
				}
			}
			catch (Exception e)
			{
				LogMessageToFile(e.ToString());
				throw;
			}
		}

		private void showMatches(HashSet<Series> seriesSet)
		{
			string seasonNumber;
			string episodeNumber;
			foreach (var ser in seriesSet)
			{
				if (ser.Season < 10)
				{
					seasonNumber = "0" + ser.Season;
				}
				else
				{
					seasonNumber = ser.Season.ToString();
				}
				if (ser.Episode < 10)
				{
					episodeNumber = "0" + ser.Episode;
				}
				else
				{
					episodeNumber = ser.Episode.ToString();
				}
				Debug.WriteLine("Showing currently printed information in right listbox");
				Debug.WriteLine(seasonNumber);
				Debug.WriteLine(episodeNumber);
				listBox1.Items.Add(ser.Name + " " + "S" + seasonNumber + "E" + episodeNumber + " Name: " + ser.Title);
			}
		}

		private void updateKodiLibrary()
		{
			KodiControl kc = new KodiControl("192.168.0.101", "8080");
			kc.UpdateLibrary();
		}

		private void showNewSeriesMatches()
		{
			//Tuple<List<string>, List<string>> t = getNewSeries();

			List<string> ls = newSeriesFF.Item1;
			List<string> filesInDir = new List<string>();
			for (int i = 0; i < ls.Count; i++)
			{
				filesInDir.AddRange(getFilesInDir(ls[i]));
			}

			ls.AddRange(newSeriesFF.Item2);

			updateListbox(listBox1, cutPath(ls));
			List<Series> s = convertStringToSeries(filesInDir);
			List<Series> u = convertFileToSeries(newSeriesFF.Item2);
			Debug.WriteLine(u.Count);

			List<HashSet<Series>> lh = new List<HashSet<Series>>();
			foreach (var item in s)
			{
				HashSet<Series> h = findMatch(tvdb, item);
				lh.Add(h);
			}

			foreach (var item in u)
			{
				HashSet<Series> h = findMatch(tvdb, item);
				lh.Add(h);
			}

			foreach (var set in lh)
			{
                Debug.WriteLine("Set count: " + set.Count);
				if (set.Count > 0)
				{
                    if(set.Count > 1)
                    {
                        string cho = ChoiceWindow.GetChoice(set);
                        Debug.WriteLine(cho);

                        foreach (var item in set)
                        {
                            Debug.WriteLine("Comparing: " + item.Name + " with " + cho);
                            if(item.Name == cho)
                            {
                                HashSet <Series> ne = new HashSet<Series>();
                                ne.Add(item);
                                seriesMatchesSet.Add(ne);
                                break;
                            }
                        }                           
                    }
                    else
                    {
                        seriesMatchesSet.Add(set);
                    }
					Debug.WriteLine("Before move file");
					foreach (var item in set)
					{
						item.printSeries();
					}
					showMatches(set);
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
			updateListbox(listBox1, new List<string>());
			updateListbox(listBox, new List<string>());
			updateKodiLibrary();
		}

		private void button_Click(object sender, RoutedEventArgs e)
		{
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

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string a = sanitizeString("");

            List<string> l = new List<string>();
            l.Add("Greys.Anatomy.S14E08.720p.HDTV.x264-KILLERS[rarbg]");
            l.Add("Scandal.S06E15.WEBRip.x264-RARBG");
            l.Add("The.Blacklist.S05E08.WEBRip.x264-RARBG");

            foreach (var item in l)
            {
                var t = regexMatch(item);
                t.printSeries();
            }

            double d = cosineSimiliarity("Julie loves me more than Linda loves me", "Jane likes me more than Julie loves me", 2);
            
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
