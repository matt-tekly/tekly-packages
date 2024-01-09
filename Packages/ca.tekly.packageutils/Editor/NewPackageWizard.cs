using System;
using System.IO;
using System.Text;
using ImageMagick;
using UnityEditor;
using UnityEngine;

namespace Tekly.PackageUtils
{
	[Serializable]
	public class NewPackageConfig
	{
		public string Name;
		public string NameSpace;
	}

	public class NewPackageWizard : ScriptableWizard
	{
		[SerializeField] private string m_name;
		[SerializeField] private string m_displayName;
		[SerializeField] private string m_codeName;
		[SerializeField] private string m_company;
		
		[SerializeField] private bool m_runtime;
		[SerializeField] private bool m_editor;


		private static string KEY = "Tekly.NewPackageWizard";
		
		[MenuItem("Tools/Tekly/New Package")]
		private static void CreateWizard()
		{
			var wizard = DisplayWizard<NewPackageWizard>("Create Package", "Create");
			
			if (EditorPrefs.HasKey(KEY)) {
				var json = EditorPrefs.GetString(KEY);
				EditorJsonUtility.FromJsonOverwrite(json, wizard);
			}
		}

		protected void OnDisable()
		{
			var json = EditorJsonUtility.ToJson(this);
			EditorPrefs.SetString(KEY, json);
		}

		protected override bool DrawWizardGUI()
		{
			var changed = base.DrawWizardGUI();

			if (GUILayout.Button("Do It")) {
				OnWizardCreate();
			}
			
			return changed;
		}
		private void OnWizardCreate()
		{
			Debug.Log("Create Package " + m_name);

			var directory = Path.Combine("Packages", m_name);
			Directory.CreateDirectory(directory);

			CreatePackageJson(directory);
			CreateRuntimeAssembly(directory);
			CreateEditorAssembly(directory);
			
			AssetDatabase.Refresh();
		}

		private void CreatePackageJson(string directory)
		{
			var package = new JsonPackage {
				name = m_name,
				displayName = m_displayName
			};

			WriteToJson(Path.Combine(directory, "package.json"), package);
		}
		
		public void WriteToJson(string path, object obj)
		{
			File.WriteAllText(path, JsonUtility.ToJson(obj), Encoding.UTF8);
		}

		private void CreateRuntimeAssembly(string directory)
		{
			if (!m_runtime) {
				return;
			}

			var runtimeDirectory = Path.Combine(directory, "Runtime");
			Directory.CreateDirectory(runtimeDirectory);

			var assemblyName = $"{m_company}.{m_codeName}.Runtime";
			var assemblyPath = Path.Combine(runtimeDirectory, assemblyName + ".asmdef");
			var assembly = new JsonAssemblyDefinition {
				name = assemblyName,
				rootNamespace = $"{m_company}.{m_codeName}",
				autoReferenced = true
			};

			WriteToJson(assemblyPath, assembly);
		}
		
		private void CreateEditorAssembly(string directory)
		{
			if (!m_runtime) {
				return;
			}

			var runtimeDirectory = Path.Combine(directory, "Editor");
			Directory.CreateDirectory(runtimeDirectory);

			var assemblyName = $"{m_company}.{m_codeName}.Editor";
			var assemblyPath = Path.Combine(runtimeDirectory, assemblyName + ".asmdef");
			var assembly = new JsonAssemblyDefinition {
				name = assemblyName,
				rootNamespace = $"{m_company}.{m_codeName}",
				includePlatforms = new[] {"Editor"}
			};

			WriteToJson(assemblyPath, assembly);
		}
	}

	[Serializable]
	public sealed class JsonPackage
	{
		public string name;
		public string version = "0.0.1";
		public string displayName;
	}

	[Serializable]
	public sealed class JsonAssemblyDefinition
	{
		public string name = string.Empty;
		public string rootNamespace = string.Empty;
		public string[] references = Array.Empty<string>();
		public string[] includePlatforms = Array.Empty<string>();
		public string[] excludePlatforms = Array.Empty<string>();
		public bool allowUnsafeCode;
		public bool overrideReferences;
		public string[] precompiledReferences = Array.Empty<string>();
		public bool autoReferenced;
		public string[] defineConstraints = Array.Empty<string>();
		public string[] versionDefines = Array.Empty<string>();
		public bool noEngineReferences;
	}
}