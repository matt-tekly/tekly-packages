using System;
using Tekly.Webster.Routing;
using UnityEngine;

namespace Tekly.Webster.Routes
{
	[Serializable]
	public class PrefResult
	{
		public string Pref;
		public string Value;
	}

	[Serializable]
	public class HasKeyResult
	{
		public bool Exists;
		public string Pref;
	}

	[Route("/prefs")]
	public class PrefsRoutes
	{
		[Get("/string")]
		[Description("Returns the given PlayerPref")]
		public PrefResult GetStringPref(string pref)
		{
			var value = PlayerPrefs.GetString(pref);
			return new PrefResult {Pref = pref, Value = value};
		}

		[Put("/string")]
		[Description("Sets the given PlayerPref")]
		public void PutStringPref(string pref, string value)
		{
			PlayerPrefs.SetString(pref, value);
		}

		[Get("/int")]
		[Description("Returns the given PlayerPref")]
		public PrefResult GetIntPref(string pref)
		{
			var value = PlayerPrefs.GetInt(pref).ToString();
			return new PrefResult {Pref = pref, Value = value};
		}

		[Put("/int")]
		[Description("Sets the given PlayerPref")]
		public void PutIntPref(string pref, int value = 1)
		{
			PlayerPrefs.SetInt(pref, value);
		}

		[Get("/float")]
		[Description("Returns the given PlayerPref")]
		public PrefResult GetFloatPref(string pref)
		{
			var value = PlayerPrefs.GetFloat(pref).ToString();
			return new PrefResult {Pref = pref, Value = value};
		}

		[Put("/float")]
		[Description("Sets the given PlayerPref")]
		public void PutIntPref(string pref, float value = 1)
		{
			PlayerPrefs.SetFloat(pref, value);
		}

		[Get("/hasKey")]
		[Description("Returns the if the given key exists")]
		public HasKeyResult GetHasKey(string pref)
		{
			var value = PlayerPrefs.HasKey(pref);
			return new HasKeyResult {Pref = pref, Exists = value};
		}

		[Delete("/key")]
		[Description("Deletes the given key")]
		public void DeleteKey(string pref)
		{
			PlayerPrefs.DeleteKey(pref);
		}

		[Delete("/all")]
		[Description("Deletes all the PlayerPrefs")]
		public void DeleteAll()
		{
			PlayerPrefs.DeleteAll();
		}
	}
}