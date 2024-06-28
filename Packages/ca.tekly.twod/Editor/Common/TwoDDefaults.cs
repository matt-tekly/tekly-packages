using System.Collections.Generic;
using Tekly.TwoD.Cells;
using Tekly.TwoD.Tiles;
using UnityEditor;
using UnityEngine;

namespace Tekly.TwoD.Common
{
	[FilePath("ProjectSettings/TwoDDefaults.asset", FilePathAttribute.Location.ProjectFolder)]
	public class TwoDDefaults : ScriptableSingleton<TwoDDefaults>
	{
		public CellImporterSettings Cells;
		public TilesImporterSettings Tiles;

		public void Save()
		{
			Save(true);
		}
		
		protected void OnEnable()
		{
			hideFlags = HideFlags.None;
		}
	}
	
	public static class TwoDSettingsProvider
	{
		private static Editor s_settingsEditor;

		[SettingsProvider]
		public static SettingsProvider CreateMySettingsProvider()
		{
			var provider = new SettingsProvider("Project/TwoD", SettingsScope.Project)
			{
				label = "TwoD",
				guiHandler = _ =>
				{
					if (s_settingsEditor == null)
					{
						s_settingsEditor = Editor.CreateEditor(TwoDDefaults.instance);
					}
					
					s_settingsEditor.OnInspectorGUI();

					if (GUI.changed)
					{
						TwoDDefaults.instance.Save();
					}
				},

				keywords = new HashSet<string>(new[] { "TwoD", "Cell", "Cells", "Tile", "Tiles" })
			};

			return provider;
		}
	}
}