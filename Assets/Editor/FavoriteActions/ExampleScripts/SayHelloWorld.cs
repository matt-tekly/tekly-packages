using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Tekly.Favorites
{
	[UsedImplicitly]
	[Serializable]
	public class SayHelloWorld : IFavoriteActionScript
	{
		public void Activate()
		{
			Debug.Log("Hello World!");
		}
	}
}