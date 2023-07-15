using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tekly.Favorites
{
	[Serializable]
	public class FavoriteCollection
	{
		public string Name = "Collection";
		[HideInInspector] public List<FavoriteAsset> Favorites = new List<FavoriteAsset>();

		public FavoriteCollection() { }

		public FavoriteCollection(string name)
		{
			Name = name;
		}
	}
}