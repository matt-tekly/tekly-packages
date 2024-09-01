using System.Threading.Tasks;
using Tekly.Common.Gui;
using Tekly.EditorUtils.Assets;
using Tekly.EditorUtils.Gui;
using UnityEditor;
using UnityEngine;

namespace Tekly.Sheets.Processing
{
	public class SheetFetcherWindow : EditorWindow
	{
		private Task m_activeTask;
		private GoogleSheetObject[] m_googleSheetObjects;
		private GUIContent m_linkIcon;
		private GUIContent m_downloadIcon;

		private readonly Color m_outerContainerColor = new Color(0.3f, 0.3f, 0.3f, 1);
		
		[MenuItem("Tools/Tekly/Sheets/Fetcher")]
		public static void Open()
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
			GUI.enabled = !(SheetFetcher.IsActive || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling);

			using (EditorGuiExt.LargeContainer(m_outerContainerColor)) {
				EditorGUILayout.LabelField("Data Fetch", EditorGuiStyles.Instance.Heading);

				foreach (var sheet in m_googleSheetObjects) {
					using (EditorGuiExt.SmallContainer()) {
						SheetGui(sheet);
					}
				}
				
				if (GUILayout.Button("Download All")) {
					SheetFetcher.DownloadAsync(m_googleSheetObjects);
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
				SheetFetcher.DownloadAsync(sheetObject);
			}

			EditorGUILayout.EndHorizontal();
		}
	}
}