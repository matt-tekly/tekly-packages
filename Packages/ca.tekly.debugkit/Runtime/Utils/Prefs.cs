using UnityEngine;

namespace Tekly.DebugKit.Utils
{
	public class BoolPref
	{
		public bool Value
		{
			get => m_value;
			set
			{
				if (m_value != value)
				{
					m_value = value;
					PlayerPrefs.SetInt(m_key, m_value ? 1 : 0);
				}
			}
		}
        
		private readonly string m_key;
		private bool m_value;
        
		public BoolPref(string key, bool defaultValue = false)
		{
			m_key = key;
			m_value = PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;
		}
	}
	
	public class StringPref
	{
		public string Value
		{
			get => m_value;
			set
			{
				if (m_value != value)
				{
					m_value = value;
					PlayerPrefs.SetString(m_key, m_value);
				}
			}
		}
        
		private readonly string m_key;
		private string m_value;
        
		public StringPref(string key, string defaultValue)
		{
			m_key = key;
			m_value = PlayerPrefs.GetString(key, defaultValue);
		}
	}
}