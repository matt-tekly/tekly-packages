using System;
using System.Collections.Generic;

namespace Tekly.Favorites
{
    [Serializable]
    public class FavoriteCollection
    {
        public string Name = "Collection";
        public List<FavoriteAsset> Favorites = new List<FavoriteAsset>();

        public FavoriteCollection()
        {
        }

        public FavoriteCollection(string name)
        {
            Name = name;
        }
    }
}