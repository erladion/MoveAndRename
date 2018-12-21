using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoveAndRename
{
	class Movie
	{
		public string Name;
		public int Year;
		public string Extension { get; set; }
		public string CurrentPath { get; set; }
		public string SubtitlePath { get; set; }
		public bool GotSubtitle { get; set; }

		public Movie()
		{

		}

		public Movie(string name, int year)
		{
			this.Name = name;
			this.Year = year;
		}

		public Movie(string name, string year)
		{
			this.Name = name;
			this.Year = Convert.ToInt32(year);
		}

		public void PrintMovie()
		{
			Debug.WriteLine("---Printing movie info---");
			Debug.WriteLine(Name);
			Debug.WriteLine(Year);
			Debug.WriteLine(CurrentPath);
			Debug.WriteLine(Extension);
			Debug.WriteLine("Got subtitle: " + GotSubtitle);
			Debug.WriteLine("Subtitle path: " + SubtitlePath);
			Debug.WriteLine("---Done printing movie info---");
		}
	}
}
