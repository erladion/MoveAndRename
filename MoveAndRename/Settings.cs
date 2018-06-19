using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace MoveAndRename
{
	public class Settings : INotifyPropertyChanged
	{
		private HashSet<string> includeList;
		private HashSet<string> excludeList;
		private HashSet<string> destinationList;
		private bool includeNfo = false;
		private bool includeSubtitle = true;
		private HashSet<string> subtitleTypes;

		public event PropertyChangedEventHandler PropertyChanged;

		public Settings()
		{
			includeList = new HashSet<string>();
			excludeList = new HashSet<string>();
			destinationList = new HashSet<string>();
		}

		private void OnPropertyChanged(string property)
		{
			if(PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(property));
			}
		}

		public void writeFiletypesToXml(string filename)
		{
			Debug.WriteLine("Writing filetype settings to XML");
			XDocument doc = XDocument.Load("settings.xml");
			XElement ftypes = new XElement("Filetypes");
			XElement incNFO = new XElement("IncNFO");
			ftypes.Add(incNFO);
			incNFO.SetAttributeValue("include", true.ToString());
			XElement settings = doc.Element("Settings");
			settings.Add(ftypes);
			doc.Save(filename);
		}

		public void writeSettingsToXml(string filename)
		{
			Debug.WriteLine("Writing settings to XML");
			XElement dests = new XElement(Paths.Destinations.ToString());
			dests.SetAttributeValue("count", destinationList.Count);
			foreach (var item in destinationList)
			{
				XElement p = new XElement("Path");
				p.SetAttributeValue("value", item);
				Debug.WriteLine("Adding: " + item.ToString() + " to destination");
				dests.Add(p);
			}

			XElement include = new XElement(Paths.Include.ToString());
			include.SetAttributeValue("count", includeList.Count);
			foreach (var item in includeList)
			{
				XElement p = new XElement("Path");
				p.SetAttributeValue("value", item);
				Debug.WriteLine("Adding: " + item.ToString() + " to include ");
				include.Add(p);
			}

			XElement exclude = new XElement(Paths.Exclude.ToString());
			exclude.SetAttributeValue("count", excludeList.Count);
			foreach (var item in excludeList)
			{
				XElement p = new XElement("Path");
				p.SetAttributeValue("value", item);
				Debug.WriteLine("Adding: " + item.ToString() + " to exclude");
				exclude.Add(p);
			}
			
			XElement paths = new XElement(ESettings.Paths.ToString());
			paths.Add(include);
			paths.Add(exclude);
			paths.Add(dests);

            XElement filetypes = new XElement(ESettings.Filetypes.ToString());
            XElement subtitles = new XElement(FileTypes.Subtitle.ToString());
            subtitles.SetAttributeValue("value", includeSubtitle);

            filetypes.Add(subtitles);
			
			XElement settings = new XElement("Settings");
			settings.Add(paths);
            settings.Add(filetypes);

			XDocument doc = new XDocument();
			doc.Add(settings);
			doc.Save(filename);
		}

		public void readSettingsFromXml(string filename)
		{
			Debug.WriteLine("Reading settings from XML");
            try
            {
                XDocument doc = XDocument.Load("settings.xml");

                foreach (var path in doc.Descendants(Paths.Exclude.ToString()).Elements("Path"))
                {
                    string str = path.Attribute("value").Value;
                    AddExclude(str);
                }

                foreach (var path in doc.Descendants(Paths.Include.ToString()).Elements("Path"))
                {
                    string str = path.Attribute("value").Value;
                    AddInclude(str);
                }

                foreach (var path in doc.Descendants(Paths.Destinations.ToString()).Elements("Path"))
                {
                    string str = path.Attribute("value").Value;
                    AddDestination(str);
                }

                // TODO Make it so we read the settings about Subtitles etc here
            }
            catch (FileNotFoundException e)
            {
                Debug.WriteLine("Settings file not found");
                var f = File.Create("settings.xml");
                f.Close();
                writeSettingsToXml("settings.xml");
            }			
		}

		public void AddInclude(string str)
		{
			if (!includeList.Contains(str))
			{
				Debug.WriteLine("Includelist property changed!");
				includeList.Add(str);
				OnPropertyChanged("settings");
			}
		}

		public void AddExclude(string str)
		{
			if (!excludeList.Contains(str))
			{
				Debug.WriteLine("Excludelist property changed!");
				excludeList.Add(str);
				OnPropertyChanged("settings");
			}
		}

		public void AddDestination(string str)
		{
			if (!destinationList.Contains(str))
			{
				Debug.WriteLine("Destinationlist property changed!");
				destinationList.Add(str);
				OnPropertyChanged("settings");
			}
		}

		public void RemoveInclude(string str)
		{
			if (includeList.Contains(str))
			{
				includeList.Remove(str);
				OnPropertyChanged("settings");
			}
		}

		public void RemoveExclude(string str)
		{
			if (excludeList.Contains(str))
			{
				excludeList.Remove(str);
				OnPropertyChanged("settings");
			}
		}

		public void RemoveDestination(string str)
		{
			if (destinationList.Contains(str))
			{
				destinationList.Remove(str);
				OnPropertyChanged("settings");
			}
		}

		public void ChangeIncludeNfo()
		{
			if (includeNfo)
			{
				includeNfo = false;
			}
			else
			{
				includeNfo = true;
			}
			OnPropertyChanged("settings");
		}

		public void ChangeIncludeSub()
		{
			if (includeSubtitle)
			{
				includeSubtitle = false;
			}
			else
			{
				includeSubtitle = true;
			}
			OnPropertyChanged("settings");
		}

		public HashSet<string> IncludeList
		{
			get
			{
				return includeList;
			}			
		}

		public HashSet<string> ExcludeList
		{
			get
			{
				return excludeList;
			}
		}

		public HashSet<string> DestinationList
		{
			get
			{
				return destinationList;
			}
		}

		public bool IncludeNfo
		{
			get
			{
				return includeNfo;
			}
		}

		public bool IncludeSubtitle
		{
			get
			{
				return includeSubtitle;
			}
		}
	}
}
