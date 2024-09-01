using System;
using System.Linq;
using System.Threading.Tasks;
using Tekly.EditorUtils.Console;
using Tekly.Sheets.Core;
using UnityEditor;
using UnityEditor.Compilation;
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
	
	public static class SheetFetcher
	{
		public static bool IsActive => s_activeTask != null;
		
		private static Task s_activeTask;
		
		public static async void DownloadAsync(GoogleSheetObject sheetObject)
		{
			Debug.Log("Downloading Sheet: " + sheetObject.name);
			
			try {
				var credentialsPath = AssetDatabase.GetAssetPath(sheetObject.Credentials);
				
				var sheetsApi = new SheetsApi(credentialsPath);
				var operation = new DataFetchOperation(sheetObject, sheetsApi);

				s_activeTask = operation.Start();
				await s_activeTask;
				
				AssetDatabase.Refresh();
			} catch (Exception e) {
				Debug.LogException(e);
			} finally {
				s_activeTask = null;
			}
			
			Debug.Log("Downloading Complete: " + sheetObject.name);
		}
		
		public static async void DownloadAsync(GoogleSheetObject[] googleSheetObjects)
		{
			EditorConsole.Clear();
			
			Debug.Log("Downloading All Data");
			s_activeTask = DownloadSheetsAsync(googleSheetObjects);

			try {
				await s_activeTask;
			} catch (Exception e) {
				Debug.LogException(e);
			}

			Debug.Log("Downloading Complete");
			s_activeTask = null;
		}

		private static async Task DownloadSheetsAsync(GoogleSheetObject[] googleSheetObjects)
		{
			var tasks = googleSheetObjects.Select(x => {
				var credentialsPath = AssetDatabase.GetAssetPath(x.Credentials);
				var sheetsApi = new SheetsApi(credentialsPath);
				return new DataFetchOperation(x, sheetsApi);
			}).Select(x => x.Start());

			await Task.WhenAll(tasks);

			AssetDatabase.Refresh();
		}
	}
}