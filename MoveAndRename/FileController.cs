using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoveAndRename
{
	class FileController
	{
		Settings setObj;

		public FileController(Settings sObj)
		{
			this.setObj = sObj;
		}

		public void LogMessageToFile(string msg)
		{
			Debug.WriteLine("-----Writing message to log-----");
			string path = System.Environment.GetEnvironmentVariable("TEMP");
			if (!path.EndsWith("\\")) path += "\\";

			System.IO.StreamWriter sw = System.IO.File.AppendText(path + "My Log File.txt");
			try
			{
				string logLine = System.String.Format("{0:G}: {1}.", System.DateTime.Now, msg);
				sw.WriteLine(logLine);
			}
			finally
			{
				sw.Close();
			}
		}

		public List<string> getFilesInDir(string dirPath)
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
							foundFiles.Add(files[i]);
						}
						if (setObj.IncludeNfo)
						{
							if (files[i].EndsWith(".nfo"))
							{
								foundFiles.Add(files[i]);
							}
						}
					}
				}
				Debug.WriteLine("Files found: " + foundFiles.Count);
				return foundFiles;
			}
			return new List<string>();
		}

		public void removeFolder(string path)
		{
			Debug.WriteLine("-----Currently in removeFolder-----");
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
							Directory.Delete(path, false);
						}
						// Should probably never happen, as we move the series/movie file.
						else if (File.Exists(path))
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

		/// <summary>
		/// Moves a file from "from" to "to" with some additional checks to make sure we are moving the correct file
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <returns></returns>
		public bool moveFile(string from, string to)
		{
			Debug.WriteLine("-----Currently in moveFile-----");
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
				if (Directory.Exists(f.FullName))
				{
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
		/// Moves a series file to the specified path
		/// </summary>
		/// <param name="path">Destination path</param>
		/// <param name="ser">Series object for the file to be moved</param>
		public void moveFile(HashSet<Series> serSet)
		{
			Debug.WriteLine("-----Currently in moveFile(HashSet<Series>)-----");
			foreach (var ser in serSet)
			{
				Debug.WriteLine("Current path: " + ser.CurrentPath);
				Debug.WriteLine("Destination path: " + ser.DestinationPath);
				if (ser.CurrentPath != null && ser.DestinationPath != null)
				{
					bool movedFile = false;
					string sp = ser.CurrentPath;
					string ext = "." + ser.Extension;
					if (ser.CurrentPath.Contains(ext))
					{
						movedFile = moveFile(ser.CurrentPath, ser.DestinationPath);
					}
					else
					{
						movedFile = moveFile(ser.CurrentPath + "." + ser.Extension, ser.DestinationPath);
					}

					// If the setting to include subtitles is set, we first get the subtitle which is in the appropriate language (english)
					// then we use the path where we are sending our "series" file and we simply send the sub there as well, with the same name
					// so it gets associated with the "series".

					if (setObj.IncludeSubtitle)
					{
						if (ser.GotSubtitle)
						{
							moveFile(ser.SubtitlePath, System.IO.Path.GetFileNameWithoutExtension(ser.DestinationPath) + System.IO.Path.GetExtension(ser.SubtitlePath));
						}
					}

					if (movedFile)
					{
						if (sp.Contains(ext))
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
	}
}
