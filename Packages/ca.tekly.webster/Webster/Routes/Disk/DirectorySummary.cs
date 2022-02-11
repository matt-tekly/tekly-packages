//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

using System;
using System.IO;
using System.Linq;
using System.Text;
using Tekly.Webster.Utility;

namespace Tekly.Webster.Routes.Disk
{
	public class DirectoryResponse : IToJson
	{
		public string PersistentDataPath;
		public DirectorySummary Directory;

		public void ToJson(StringBuilder sb)
		{
			sb.Append("{");
			sb.Write("PersistentDataPath", PersistentDataPath, true);
			sb.Write("Directory", Directory, false);
			sb.Append("}");
		}
	}

	public class DiskEntry
	{
		public string Name;
		public string Path;
		public string Type;
	}

	public class DirectorySummary : DiskEntry, IToJson
	{
		public DirectorySummary[] Directories;
		public FileSummary[] Files;

		public DirectorySummary()
		{
			Type = "Directory";
		}

		public void ToJson(StringBuilder sb)
		{
			sb.Append("{");
			sb.Write("Name", Name, true);
			sb.Write("Path", Path, true);
			sb.Write("Directories", Directories, true);
			sb.Write("Files", Files, true);
			sb.Write("Type", Type, false);
			sb.Append("}");
		}
	}

	public class FileSummary : DiskEntry, IToJson
	{
		public DateTime CreationTime;
		public DateTime LastWriteTime;
		public long Size;

		public FileSummary()
		{
			Type = "File";
		}

		public void ToJson(StringBuilder sb)
		{
			sb.Append("{");
			sb.Write("Name", Name, true);
			sb.Write("Path", Path, true);
			sb.Write("Size", Size, true);
			sb.Write("CreationTime", CreationTime, true);
			sb.Write("LastWriteTime", LastWriteTime, true);
			sb.Write("Type", Type, false);
			sb.Append("}");
		}
	}

	public static class DirectorySummarizer
	{
		public static DirectorySummary Summarize(string path, string rootDirectory)
		{
			var di = new DirectoryInfo(path);
			return Summarize(di, rootDirectory);
		}

		public static DirectorySummary Summarize(DirectoryInfo directoryInfo, string rootDirectory)
		{
			var directorySummary = new DirectorySummary();
			directorySummary.Name = directoryInfo.Name;
			directorySummary.Path = directoryInfo.FullName.Replace("\\", "/").Replace(rootDirectory, "");

			var directoryInfos = directoryInfo.GetDirectories("*", SearchOption.TopDirectoryOnly);

			directorySummary.Directories = directoryInfos.Select(x => Summarize(x, rootDirectory)).ToArray();

			var fileInfos = directoryInfo.GetFiles("*", SearchOption.TopDirectoryOnly);

			directorySummary.Files = fileInfos.Select(fi => {
				var path = fi.FullName.Replace("\\", "/").Replace(rootDirectory, "");
				return new FileSummary {
					Name = fi.Name,
					Size = fi.Length,
					CreationTime = fi.CreationTime,
					LastWriteTime = fi.LastWriteTime,
					Path = path
				};
			}).ToArray();

			return directorySummary;
		}
	}
}