using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoveAndRename
{
	class Utility
	{
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

		public static double CosineSimilarity(string a, string b, int nGrams)
		{
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

			double magnitudeA = Magnitude(aFreq);
			double magnitudeB = Magnitude(bFreq);
			double ret = dotProduct / (magnitudeA * magnitudeB);

			return ret;
		}

		/// <summary>
		/// Removes unwanted characters from a string, mainly to be used on strings for paths
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static string SanitizeString(string str)
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
