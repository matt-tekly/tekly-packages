using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util;
using Tekly.Sheets.Dynamics;
using Tekly.Sheets.Processing;
using UnityEngine;
using System.Threading;
using Google.Apis.Util.Store;
using UnityEditor;

namespace Tekly.Sheets.Core
{
	[Serializable]
	public class SheetMetaData
	{
		public string SheetName;
		public List<string> Sheets;
	}

	public class Sheet
	{
		public readonly string Name;
		public readonly IList<IList<object>> Values;

		public Sheet(ValueRange valueRange)
		{
			Name = valueRange.Range.Substring(0, valueRange.Range.IndexOf('!'));
			Values = valueRange.Values;
		}
	}

	public class SheetsApi
	{
		private static readonly string[] Scopes = {
			SheetsService.Scope.SpreadsheetsReadonly
		};
		
		private static readonly string[] k_Scopes = {
			SheetsService.Scope.Spreadsheets
		};

		private readonly SheetsService m_sheetsService;

		public SheetsApi(string credentialsFile, GoogleAuthenticationType authType, string applicationName)
		{
			if (authType == GoogleAuthenticationType.ServiceAccount)
			{
				try
				{
					string file = Path.GetFullPath(credentialsFile);
					var credential = GoogleCredential.FromFile(file).CreateScoped(Scopes);

					m_sheetsService = new SheetsService(new BaseClientService.Initializer
					{
						HttpClientInitializer = credential
					});
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
					throw;
				}
			}
			else if (authType == GoogleAuthenticationType.OAuth)
			{
				try
				{
					var userCredentials = AuthorizeOAuth(credentialsFile, applicationName);

					m_sheetsService = new SheetsService(new BaseClientService.Initializer
					{
						HttpClientInitializer = userCredentials,
						ApplicationName = applicationName
					});
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
					throw;
				}
			}
		}

		private static UserCredential AuthorizeOAuth(string credentialsFile, string applicationName)
		{
			string file = Path.GetFullPath(credentialsFile);
			var secrets = LoadSecrets(File.ReadAllText(file));
			
			// Auto cancel after 60 secs
			var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
			var dataStore = new FileDataStore($"Library/Google/{applicationName}", true);
			var connectTask = GoogleWebAuthorizationBroker.AuthorizeAsync(secrets, k_Scopes, secrets.ClientId, cts.Token, dataStore);

			while (!connectTask.IsCompleted)
				Thread.Sleep(100);

			if (connectTask.Status == TaskStatus.Faulted)
			{
				throw new Exception($"Failed to connect to Google Sheets.\n{connectTask.Exception}");
			}
			return connectTask.Result;
		}
		
		private static ClientSecrets LoadSecrets(string credentials)
		{
			if (string.IsNullOrEmpty(credentials))
				throw new ArgumentException(nameof(credentials));

			using var stream = new MemoryStream(System.Text.Encoding.ASCII.GetBytes(credentials));
			var gcs = GoogleClientSecrets.FromStream(stream);
			
			return gcs.Secrets;
		}
		
		public async Task<IList<Sheet>> GetAllSheets(string spreadSheetId)
		{
			try {
				var metaData = await ListSheets(spreadSheetId);
				return await GetSheets(spreadSheetId, metaData.Sheets);
			} catch (Exception ex) {
				Debug.LogException(ex);
			}

			return null;
		}

		public Task GetSheetAndCreateJson(string spreadSheetId, string sheet, string outDir)
		{
			return GetSheetsAndCreateJson(spreadSheetId, new[] {
				sheet
			}, outDir);
		}

		public async Task GetSheetsAndCreateJson(string spreadSheetId, IList<string> sheets, string outDir)
		{
			var result = await GetSheets(spreadSheetId, sheets);
			await WriteSheetsToJson(outDir, result);
		}

		public static Task WriteSheetsToJson(string outDir, IList<Sheet> result)
		{
			return Task.Run(() => {
				Directory.CreateDirectory(outDir);

				foreach (var s in result) {
					if (s.Name.Contains("//")) {
						continue;
					}

					var sheetResult = SheetParser.ParseRows(s.Values, s.Name);
					var container = new Dynamic(DynamicType.Object);
					container["Data"] = sheetResult.Dynamic;

					var str = container.ToJson();

					var fileName = $"{s.Name}.json";
					File.WriteAllText(Path.Combine(outDir, fileName), str);
				}
			});
		}

		public async Task<SheetMetaData> ListSheets(string spreadSheetId)
		{
			var request = m_sheetsService.Spreadsheets.Get(spreadSheetId);
			var result = await request.ExecuteAsync();

			var sheetMetaData = new SheetMetaData();
			sheetMetaData.SheetName = result.Properties.Title;
			sheetMetaData.Sheets = result.Sheets
				.Where(s => !SheetParser.IsCommentName(s.Properties.Title))
				.Select(s => s.Properties.Title)
				.ToList();

			return sheetMetaData;
		}

		public async Task<IList<Sheet>> GetSheets(string spreadSheetId, IList<string> sheets)
		{
			var request = m_sheetsService.Spreadsheets.Values.BatchGet(spreadSheetId);
			request.ValueRenderOption = SpreadsheetsResource.ValuesResource.BatchGetRequest.ValueRenderOptionEnum
				.UNFORMATTEDVALUE;
			
			// Wrapping the sheet name in quotes ensures it references the sheet and not some other named object in the sheet
			request.Ranges = new Repeatable<string>(sheets.Select(x => $"'{x}'"));

			var result = await request.ExecuteAsync();

			return result.ValueRanges.Select(s => new Sheet(s)).ToList();
		}
	}
}