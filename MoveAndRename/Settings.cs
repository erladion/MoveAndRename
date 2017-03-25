using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.ComponentModel;

namespace MoveAndRename
{
	public class Settings : INotifyPropertyChanged
	{
		private HashSet<string> includeList;
		private HashSet<string> excludeList;
		private HashSet<string> destinationList;

		public event PropertyChangedEventHandler PropertyChanged;

		public Settings()
		{
			includeList = new HashSet<string>();
			excludeList = new HashSet<string>();
			destinationList = new HashSet<string>();
		}

		private void OnPropertyChanged(string property)
		{
			Console.WriteLine("Property changed!");
			if(PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(property));
			}
		}

		public void writeSettingsToXml(string filename)
		{
			Console.WriteLine("Writing settings to XML");
			XElement dests = new XElement(Paths.Destinations.ToString());
			dests.SetAttributeValue("count", destinationList.Count);
			foreach (var item in destinationList)
			{
				XElement p = new XElement("Path");
				p.SetAttributeValue("value", item);
				Console.WriteLine("Adding: " + item.ToString() + " to destination");
				dests.Add(p);
			}

			XElement include = new XElement(Paths.Include.ToString());
			include.SetAttributeValue("count", includeList.Count);
			foreach (var item in includeList)
			{
				XElement p = new XElement("Path");
				p.SetAttributeValue("value", item);
				Console.WriteLine("Adding: " + item.ToString() + " to include ");
				include.Add(p);
			}

			XElement exclude = new XElement(Paths.Exclude.ToString());
			exclude.SetAttributeValue("count", excludeList.Count);
			foreach (var item in excludeList)
			{
				XElement p = new XElement("Path");
				p.SetAttributeValue("value", item);
				Console.WriteLine("Adding: " + item.ToString() + " to exclude");
				exclude.Add(p);
			}
			
			XElement paths = new XElement(ESettings.Paths.ToString());
			paths.Add(include);
			paths.Add(exclude);
			paths.Add(dests);
			
			XElement settings = new XElement("Settings");
			settings.Add(paths);

			XDocument doc = new XDocument();
			doc.Add(settings);
			doc.Save(filename);
		}

		public void readSettingsFromXml(string filename)
		{
			Console.WriteLine("Reading settings from XML");
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
		}

		public void AddInclude(string str)
		{
			if (!includeList.Contains(str))
			{
				includeList.Add(str);
				OnPropertyChanged("settings");
			}
		}

		public void AddExclude(string str)
		{
			if (!excludeList.Contains(str))
			{
				excludeList.Add(str);
				OnPropertyChanged("settings");
			}
		}

		public void AddDestination(string str)
		{
			if (!destinationList.Contains(str))
			{
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
	}
}
