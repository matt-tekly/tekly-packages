using UnityEngine;

namespace Tekly.Sheets.Processing
{
	public enum GoogleAuthenticationType
	{
		OAuth,
		ServiceAccount
	}
	
	[CreateAssetMenu(menuName = "Tekly/Sheets/Google Sheet")]
	public class GoogleSheetObject : ScriptableObject
	{
		private const string URL = "https://docs.google.com/spreadsheets/d/{0}";
		
		public string SheetId;
		public GoogleSheetProcessor Processor;
		public GoogleAuthenticationType Authentication;
		public string GoogleApplicationName;
		public TextAsset Credentials;

		public void OpenSheet()
		{
			Application.OpenURL(string.Format(URL, SheetId));
		}
	}
}