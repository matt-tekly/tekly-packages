using System;
using Tekly.Logging.LogDestinations;
using UnityEngine;

namespace Tekly.Logging.Configurations
{
	public enum LogFileType
	{
		Flat,
		JsonNewlineDelimited
	}
	[CreateAssetMenu(menuName = "Tekly/Logger/File Destination", fileName = "FileLogDestinationConfig", order = 0)]
	public class FileLogDestinationConfig : LogDestinationConfig
	{
		public string FileName;
		public LogFileType Type;
		
		public TkLogLevel MinimumLogLevel = TkLogLevel.Debug;
		
		public override ILogDestination CreateInstance()
		{
			switch (Type) {
				case LogFileType.Flat:
					return new FlatFileLogDestination(name, FileName, MinimumLogLevel);
				case LogFileType.JsonNewlineDelimited:
					return new StructuredFileLogDestination(name, FileName, MinimumLogLevel);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}