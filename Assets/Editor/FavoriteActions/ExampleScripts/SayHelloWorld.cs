using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Tekly.Favorites
{
	[UsedImplicitly]
	public class SayHelloWorld : IFavoriteActionScript
	{
		public void Activate()
		{
			Debug.Log("Hello World!");
		}
	}
}