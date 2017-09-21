using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoveAndRename
{
	class Series
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
			Console.WriteLine("---Printing series info---");
			Console.WriteLine(Name);
			Console.WriteLine("Season: " + Season + " Episode: " + Episode);
			Console.WriteLine(CurrentPath);
			Console.WriteLine(Extension);
			Console.WriteLine("---Done printing series info---");
		}
	}
}
