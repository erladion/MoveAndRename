using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MoveAndRename
{
	class Utility
	{
		// Will be a string[] for now since all we do with it is to index into it.
		public static string[] ParameterArray = { "Name", "Season", "Episode", "Title" };

		// Makes it so we can get the index for each of the parameters in the ParameterArray
		public static Dictionary<string, int> SeriesParameters	 = new Dictionary<string, int> {
			{ "Name",0 },
			{ "Season",1},
			{ "Episode",2},
			{ "Title",3} };
		

		public static void LogMessageToFile(string msg)
		{
			Debug.WriteLine("-----Writing message to log-----");
			string path = Environment.GetEnvironmentVariable("TEMP");
			if (!path.EndsWith("\\")) path += "\\";

			string application = "MoveAndRename";
			StreamWriter sw = File.AppendText(path + application + " - " + DateTime.Now.ToShortDateString());
			try
			{
				string logLine = String.Format("{0:G}: {1}.", DateTime.Now, msg);
				sw.WriteLine(logLine);
			}
			finally
			{
				sw.Close();
			}
		}

		/// <summary>
		/// Parses a user specified format for series formatting, returns a string that can be used in String.Format()
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static Tuple<List<int>, string> ParseSeriesFormat(string str)
		{
			Debug.WriteLine(String.Format("-----Currently in {0}-----", MethodBase.GetCurrentMethod().Name));
			if (str == "" || str == null)
			{
				return new Tuple<List<int>, string>(new List<int>(),"");
			}
			// Valid parameters:
			// Name (name of the series), Episode, Season, Title (name of the episode)
			string temp = SanitizeString(str);

			if (temp.Length == str.Length)
			{				
				int i = 0;

				if (!temp.Contains(ParameterArray[0]) && !temp.Contains(ParameterArray[3]))
				{
					return new Tuple<List<int>, string>(new List<int>(), "");
				}
				List<int> indexes = new List<int>();
				
				foreach (var item in ParameterArray)
				{
					string t2 = temp;
					temp = temp.Replace(item, "{" + i + "}");
					if(t2.Length > temp.Length)
					{
						indexes.Add(SeriesParameters[item]);
					}
					i++;
				}
				return new Tuple<List<int>, string>(indexes, temp);
			}
			else
			{
				return new Tuple<List<int>, string>(new List<int>(), "");
			}			
		}

		/// <summary>
		/// Creates n-grams from a string, n-grams are 
		/// </summary>
		/// <param name="str">Input string to create n-grams from</param>
		/// <param name="n">Size of the n-grams, have to be smaller than the length of the word or an empty list will be returned</param>
		/// <returns></returns>
		private static List<string> CreateNGrams(string str, int n)
		{
			if (n > str.Length)
			{
				return new List<string>();
			}

			List<string> nGrams = new List<string>();

			for (int i = 0; i < str.Length - n + 1; i++)
			{
				string s = str.Substring(i, n);
				nGrams.Add(s);
			}

			return nGrams;
		}

		private static double Magnitude(List<int> li)
		{
			double magnitude = 0;
			for (int i = 0; i < li.Count; i++)
			{
				magnitude += li[i] * li[i];
			}

			return Math.Sqrt(magnitude);
		}

		private static int CountStringOccurences(string s, string p)
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

		/// <summary>
		/// Calculates the cosine similarity between two strings. That is how similar they are to each other, will return a value between 0 and 1. Where 1 means the strings are equal. 
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="nGrams"></param>
		/// <returns></returns>
		public static double CosineSimilarity(string a, string b, int nGrams)
		{
			if(nGrams > a.Length || nGrams > b.Length)
			{
				return 0;
			}

			List<string> aNGrams = CreateNGrams(a, nGrams);
			List<string> bNGrams = CreateNGrams(b, nGrams);

			List<string> abGrams = aNGrams.Union(bNGrams).ToList();

			List<int> aFreq = Enumerable.Repeat(0, abGrams.Count).ToList();
			List<int> bFreq = Enumerable.Repeat(0, abGrams.Count).ToList();
			for (int i = 0; i < abGrams.Count; i++)
			{
				aFreq[i] = CountStringOccurences(a, abGrams[i]);
				bFreq[i] = CountStringOccurences(b, abGrams[i]);
			}

			double dotProduct = 0;
			for (int i = 0; i < aFreq.Count; i++)
			{
				dotProduct += (aFreq[i] * bFreq[i]);
			}

			return (dotProduct / (Magnitude(aFreq) * Magnitude(bFreq)));
		}

		/// <summary>
		/// Removes unwanted characters from a string, mainly to be used on strings for paths.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static string SanitizeString(string str)
		{
			if(str == null || str == "")
			{
				return "";
			}
			Debug.WriteLine(String.Format("-----Currently in {0}-----", MethodBase.GetCurrentMethod().Name));
			Debug.WriteLine("Input string: " + str);
			string retStr = "";

			List<char> unwantedChars = Path.GetInvalidPathChars().Union(Path.GetInvalidFileNameChars()).ToList();

			for (int i = 0; i < str.Length; i++)
			{
				int count = 0;
				for (int j = 0; j < unwantedChars.Count; j++)
				{
					if (str[i] == unwantedChars[j])
					{
						retStr += "";
						count++;
						break;
					}
				}
				if (count == 0)
				{
					retStr += str[i];
				}
				count = 0;
			}
			return retStr;
		}
	}
}
