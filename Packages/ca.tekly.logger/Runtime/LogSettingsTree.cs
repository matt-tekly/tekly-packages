using System;
using System.Collections.Generic;
using Tekly.Logging.Configurations;

namespace Tekly.Logging
{
	public class LogSettingsTree
	{
		internal class LevelNode
		{
			public string Name;
			public List<LevelNode> Children = new List<LevelNode>();
			public LoggerSettings Settings;
		}

		private LevelNode m_rootNode = new LevelNode();

		public void Initialize(TkLogLevel defaultLevel, LogDestinationConfig defaultDestination, LoggerConfig[] configs)
		{
			Initialize(defaultLevel, TkLogger.GetDestination(defaultDestination), configs);
		}
		
		public void Initialize(TkLogLevel defaultLevel, ILogDestination defaultDestination, LoggerConfig[] configs)
		{
			m_rootNode = new LevelNode();
			m_rootNode.Settings = new LoggerSettings(defaultLevel, defaultDestination);
			
			foreach (var config in configs) {
				var loggerDestination = defaultDestination;
				
				if (config.DestinationOverride != null) {
					loggerDestination = TkLogger.GetDestination(config.DestinationOverride);
				}
				
				AddLevel(config.Logger, new LoggerSettings(config.Level, loggerDestination));
			}
		}

		public LoggerSettings GetSettings(string fullClassName)
		{
			var lastSettings = m_rootNode.Settings;

			var lastNode = m_rootNode;
			var names = fullClassName.Split('.');

			foreach (var name in names) {
				if (TryGet(lastNode.Children, name, out var newNode)) {
					if (newNode.Settings != null) {
						lastSettings = newNode.Settings;
					}

					lastNode = newNode;
				} else {
					break;
				}
			}

			return lastSettings;
		}

		private void AddLevel(string fullClassName, LoggerSettings settings)
		{
			var names = fullClassName.Split('.');

			var lastNode = m_rootNode;

			foreach (var name in names) {
				lastNode = GetOrCreate(lastNode.Children, name);
			}

			lastNode.Settings = settings;
		}

		private LevelNode GetOrCreate(List<LevelNode> nodes, string name)
		{
			foreach (var node in nodes) {
				if (string.Equals(node.Name, name, StringComparison.OrdinalIgnoreCase)) {
					return node;
				}
			}

			var newNode = new LevelNode {
				Name = name
			};

			nodes.Add(newNode);

			return newNode;
		}

		private bool TryGet(List<LevelNode> nodes, string name, out LevelNode targetNode)
		{
			foreach (var node in nodes) {
				if (string.Equals(node.Name, name, StringComparison.OrdinalIgnoreCase)) {
					targetNode = node;
					return true;
				}
			}

			targetNode = null;
			return false;
		}

		public void Clear()
		{
			m_rootNode = new LevelNode();
		}
	}
}