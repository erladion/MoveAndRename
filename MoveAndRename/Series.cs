using System.Diagnostics;

namespace MoveAndRename
{
	public class Series
	{
		public string Name { get; set; }
		public int Season { get; set; }
		public int Episode { get; set; }
		public string Title { get; set; }
		public string CurrentPath { get; set; }
		public string Extension { get; set; }

		public Series()
		{
			this.Name = "";
		}

		public Series(string name, int season, int episode)
		{
			this.Name = name;
			this.Season = season;
			this.Episode = episode;
			this.Title = "";
		}

		public Series(string name, int season, int episode, string title)
		{
			this.Name = name;
			this.Season = season;
			this.Episode = episode;
			this.Title = title;
		}	
		
		public Series(string name, int season, int episode, string title, string path)
		{
			this.Name = name;
			this.Season = season;
			this.Episode = episode;
			this.Title = title;
			this.CurrentPath = path;
		}

		public Series(string name, int season, int episode, string title, string path, string extension)
		{
			this.Name = name;
			this.Season = season;
			this.Episode = episode;
			this.Title = title;
			this.CurrentPath = path;
			this.Extension = extension;
		}

		public void printSeries()
		{
			Debug.WriteLine("---Printing series info---");
			Debug.WriteLine(Name);
			Debug.WriteLine("Season: " + Season + " Episode: " + Episode);
			Debug.WriteLine(CurrentPath);
			Debug.WriteLine(Extension);
			Debug.WriteLine("---Done printing series info---");
		}
	}
}
