using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;
using System.IO;
using Shell32;

namespace MoveAndRename
{
	class MovieUtility
	{
		public static HashSet<Movie> FindMovieMatches(TMDbClient cli, Movie mov)
		{
			HashSet<Movie> res = new HashSet<Movie>();

			SearchContainer<SearchMovie> results = cli.SearchMovieAsync(mov.Name).Result;

			foreach (SearchMovie item in results.Results)
			{
				if(item.Title.Contains(mov.Name) && item.ReleaseDate.Value.Year == mov.Year)
				{
					Debug.WriteLine("Got it!");
					Debug.WriteLine(item.Title);
				}
			}

			return res;
		}

		public static Movie ConvertFileToMovie(string path)
		{




			return null;
		}

		public static Dictionary<int, KeyValuePair<string, string>> GetFileProps(string filename)
		{
			Shell shl = new Shell();
			Folder fldr = shl.NameSpace(Path.GetDirectoryName(filename));
			FolderItem itm = fldr.ParseName(Path.GetFileName(filename));
			Dictionary<int, KeyValuePair<string, string>> fileProps = new Dictionary<int, KeyValuePair<string, string>>();
			for (int i = 0; i < 100; i++)
			{
				string propValue = fldr.GetDetailsOf(itm, i);
				if (propValue != "")
				{
					fileProps.Add(i, new KeyValuePair<string, string>(fldr.GetDetailsOf(null, i), propValue));
				}
			}
			return fileProps;
		}
	}
}
