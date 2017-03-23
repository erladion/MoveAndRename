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
		public string EpisodeName { get; set; }

		public Series()
		{
			this.Name = "";
		}

		public Series(string name, int season, int episode)
		{
			this.Name = name;
			this.Season = season;
			this.Episode = episode;
			this.EpisodeName = "";
		}

		public Series(string name, int season, int episode, string episodeName)
		{
			this.Name = name;
			this.Season = season;
			this.Episode = episode;
			this.EpisodeName = episodeName;
		}		
	}
}
