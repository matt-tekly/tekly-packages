using System;
using System.Linq;
using System.Threading.Tasks;
using Tekly.EditorUtils.Console;
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
	
	public static class SheetFetcher
	{
		public static bool IsActive { get; private set; }
		
		public static async void DownloadAsync(GoogleSheetObject sheetObject)
		{
			Debug.Log("Downloading Sheet: " + sheetObject.name);
			
			try {
				IsActive = true;
				
				var sheetsApi = await sheetObject.CreateSheetsApiAsync();
				var operation = new DataFetchOperation(sheetObject, sheetsApi);

				await operation.Start();
				
				AssetDatabase.Refresh();
				Debug.Log("Downloading Complete: " + sheetObject.name);
			} catch (Exception e) {
				Debug.LogError("Downloading failed!");
				Debug.LogException(e);
			} finally {
				IsActive = false;
			}
		}
		
		public static async void DownloadAsync(GoogleSheetObject[] googleSheetObjects)
		{
			EditorConsole.Clear();
			
			Debug.Log("Downloading All Data");

			try {
				IsActive = true;
				await DownloadSheetsAsync(googleSheetObjects);
				Debug.Log("Downloading Complete");
			} catch (Exception e) {
				Debug.LogError("Downloading Failed!");
				Debug.LogException(e);
			} finally {
				IsActive = false;
			}
		}

		private static async Task DownloadSheetsAsync(GoogleSheetObject[] googleSheetObjects)
		{
			var tasks = googleSheetObjects.Select(async x => {
				var sheetsApi = await x.CreateSheetsApiAsync();
				var operation = new DataFetchOperation(x, sheetsApi);
				return operation.Start();
			});

			await Task.WhenAll(tasks);

			AssetDatabase.Refresh();
		}
	}
}