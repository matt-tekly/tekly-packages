using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
using Tekly.Sheets.Core;
using UnityEditor;
using UnityEngine;

namespace Tekly.Sheets.Processing
{
	public enum GoogleAuthenticationType
	{
		ServiceAccount,
		OAuth
	}

	[CreateAssetMenu(menuName = "Tekly/Sheets/Google Sheet")]
	public class GoogleSheetObject : ScriptableObject
	{
		private const string URL = "https://docs.google.com/spreadsheets/d/{0}";

		private static readonly string[] Scopes = {
			SheetsService.Scope.SpreadsheetsReadonly
		};
		
		private const int AUTHORIZATION_TIMEOUT_SECONDS = 60;

		public string SheetId;
		public GoogleSheetProcessor Processor;
		public GoogleAuthenticationType Authentication;
		public TextAsset Credentials;

		[Tooltip("Sets the ApplicationName for your requests to Google. This is optional.")]
		public string GoogleApplicationName;

		public void OpenSheet()
		{
			Application.OpenURL(string.Format(URL, SheetId));
		}

		public async Task<SheetsApi> CreateSheetsApiAsync()
		{
			var sheetsService = await CreateSheetsServiceAsync();
			return new SheetsApi(sheetsService);
		}

		private async Task<SheetsService> CreateSheetsServiceAsync()
		{
			switch (Authentication) {
				case GoogleAuthenticationType.ServiceAccount: {
					var file = Path.GetFullPath(AssetDatabase.GetAssetPath(Credentials));
					var credential = GoogleCredential.FromFile(file).CreateScoped(Scopes);

					return new SheetsService(new BaseClientService.Initializer {
						HttpClientInitializer = credential,
						ApplicationName = GoogleApplicationName
					});
				}
				case GoogleAuthenticationType.OAuth: {
					var file = Path.GetFullPath(AssetDatabase.GetAssetPath(Credentials));
					var userCredentials = await AuthorizeOAuthAsync(file, GoogleApplicationName);

					return new SheetsService(new BaseClientService.Initializer {
						HttpClientInitializer = userCredentials,
						ApplicationName = GoogleApplicationName
					});
				}
				default:
					throw new ArgumentOutOfRangeException(nameof(Authentication), Authentication, null);
			}
		}

		private static async Task<UserCredential> AuthorizeOAuthAsync(string credentialsFile, string applicationName)
		{
			var secrets = LoadSecrets(Path.GetFullPath(credentialsFile));

			// Auto cancel after 60 secs
			var cts = new CancellationTokenSource(TimeSpan.FromSeconds(AUTHORIZATION_TIMEOUT_SECONDS));
			var dataStore = new FileDataStore($"Library/Google/{applicationName}", true);
			var connectTask = GoogleWebAuthorizationBroker.AuthorizeAsync(secrets, Scopes, secrets.ClientId, cts.Token, dataStore);
			
			var progressId = Progress.Start("Google Sheets Authorization", "Waiting for Google Authorization in browser", Progress.Options.Indefinite | Progress.Options.Managed);

			try {
				Progress.RegisterCancelCallback(progressId, () => {
					cts.Cancel();
					return true;
				});

				var timeElapsed = 0f;

				while (!connectTask.IsCompleted) {
					await Task.Delay(100, cts.Token);

					timeElapsed += 100;
					Progress.Report(progressId, timeElapsed / (AUTHORIZATION_TIMEOUT_SECONDS * 1000f));
				}
			} catch (TaskCanceledException) {
				throw new Exception("Failed to authorize Google Sheets. Timeout Exceeded.");
			} finally {
				Progress.Finish(progressId);
			}

			return connectTask.Result;
		}

		private static ClientSecrets LoadSecrets(string credentialsFilePath)
		{
			if (string.IsNullOrEmpty(credentialsFilePath)) {
				throw new ArgumentException(nameof(credentialsFilePath));
			}

			var gcs = GoogleClientSecrets.FromFile(credentialsFilePath);

			return gcs.Secrets;
		}
	}
}