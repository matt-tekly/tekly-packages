using System;
using System.Linq;
using System.Threading.Tasks;
using Tekly.Common.Gui;
using Tekly.Common.Utils;
using Tekly.Sheets.Core;
using UnityEditor;
using UnityEngine;

namespace Tekly.Sheets.Processing
{
	public class DataFetchOperation
	{
		private readonly GoogleSheetObject m_googleSheetObject;
		private readonly SheetsApi m_sheetsApi;

		public DataFetchOperation(GoogleSheetObject googleSheetObject, SheetsApi sheetsApi)
		{
			m_googleSheetObject = googleSheetObject;
			m_sheetsApi = sheetsApi;
		}

		public async Task Start()
		{
			var sheets = await m_sheetsApi.GetAllSheets(m_googleSheetObject.SheetId);
			m_googleSheetObject.Processor.Process(m_googleSheetObject, sheets);
		}
	}

	public class SheetFetcherWindow : EditorWindow
	{
		private Task m_activeTask;
		private GoogleSheetObject[] m_googleSheetObjects;
		private GUIContent m_linkIcon;
		private GUIContent m_downloadIcon;

		private readonly Color m_outerContainerColor = new Color(0.3f, 0.3f, 0.3f, 1);
		
		[MenuItem("Tools/Tekly/Sheets/Fetcher")]
		private static void Init()
		{
			var window = CreateWindow<SheetFetcherWindow>("Sheet Fetcher");
			window.Show();
		}

		private void OnEnable()
		{
			m_googleSheetObjects = AssetDatabaseExt.FindAndLoad<GoogleSheetObject>();
			m_linkIcon = EditorGUIUtility.IconContent("d_Linked");
			m_linkIcon.tooltip = "Open in Google Sheets";
			
			m_downloadIcon = EditorGUIUtility.IconContent("Download-Available");
			m_downloadIcon.tooltip = "Download";
		}

		public void OnGUI()
		{
			GUI.enabled = m_activeTask == null && EditorApplication.isPlaying == false;

			using (EditorGuiExt.LargeContainer(m_outerContainerColor)) {
				EditorGUILayout.LabelField("Data Fetch", EditorGuiStyles.Instance.Heading);

				foreach (var sheet in m_googleSheetObjects) {
					using (EditorGuiExt.SmallContainer()) {
						SheetGui(sheet);
					}
				}
				
				if (GUILayout.Button("Download All")) {
					StartDownload();
				}

				GUI.enabled = true;
			}
		}

		private void SheetGui(GoogleSheetObject sheetObject)
		{
			EditorGUILayout.BeginHorizontal();
			if (EditorGUILayout.LinkButton(sheetObject.name)) {
				Selection.activeObject = sheetObject;
			}
			GUILayout.FlexibleSpace();
			
			if (GUILayout.Button(m_linkIcon)) {
				sheetObject.OpenSheet();
			}
			
			if (GUILayout.Button(m_downloadIcon)) {
				Download(sheetObject);
			}

			EditorGUILayout.EndHorizontal();
		}

		private async void Download(GoogleSheetObject sheetObject)
		{
			Debug.Log("Downloading Sheet");
			
			try {
				var credentialsPath = AssetDatabase.GetAssetPath(sheetObject.Credentials);
				
				var sheetsApi = new SheetsApi(credentialsPath);
				var operation = new DataFetchOperation(sheetObject, sheetsApi);

				m_activeTask = operation.Start();
				await m_activeTask;
				
				AssetDatabase.Refresh();
			} catch (Exception e) {
				Debug.LogException(e);
			} finally {
				m_activeTask = null;
			}
			
			Debug.Log("Downloading Complete");
		}

		private async void StartDownload()
		{
			ClearConsole.Go();

			Debug.Log("Downloading All Data");
			m_activeTask = Download();

			try {
				await m_activeTask;
			} catch (Exception e) {
				Debug.LogException(e);
			}

			Debug.Log("Downloading Complete");
			m_activeTask = null;
		}

		private async Task Download()
		{
			var tasks = m_googleSheetObjects.Select(x => {
				var credentialsPath = AssetDatabase.GetAssetPath(x.Credentials);
				var sheetsApi = new SheetsApi(credentialsPath);
				return new DataFetchOperation(x, sheetsApi);
			}).Select(x => x.Start());

			await Task.WhenAll(tasks);

			AssetDatabase.Refresh();
		}
	}
}