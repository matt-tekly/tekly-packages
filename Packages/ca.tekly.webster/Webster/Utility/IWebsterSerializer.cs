//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

using UnityEngine;

namespace Tekly.Webster.Utility
{
	public interface IWebsterSerializer
	{
		string Serialize(object obj);
		T Deserialize<T>(string json);
	}

	public class UnityWebsterSerializer : IWebsterSerializer
	{
		public string Serialize(object obj)
		{
			return JsonUtility.ToJson(obj);
		}

		public T Deserialize<T>(string json)
		{
			return JsonUtility.FromJson<T>(json);
		}
	}
}