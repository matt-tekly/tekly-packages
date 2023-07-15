using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Favorites
{
	public class FavoritesData : ScriptableObject
	{
		[HideInInspector] [SerializeField] private int m_activeCollectionIndex;
		[HideInInspector] [SerializeField] private int m_favoriteIndex;

		public List<FavoriteCollection> Collections = new List<FavoriteCollection>();
		public FavoriteCollection ActiveCollection => Collections[ActiveCollectionIndex];
		public FavoriteAsset ActiveFavorite => ActiveCollection.Favorites[FavoriteIndex];
		
		public int FavoriteIndex {
			get {
				if (m_favoriteIndex >= ActiveCollection.Favorites.Count) {
					m_favoriteIndex = -1;
				}

				return m_favoriteIndex;
			}
		}

		public int ActiveCollectionIndex {
			get {
				if (m_activeCollectionIndex >= Collections.Count || m_activeCollectionIndex < 0) {
					m_activeCollectionIndex = 0;
				}

				return m_activeCollectionIndex;
			}
		}

		private static FavoritesData s_instance ;
		public const string FAVORITES_SAVE_FILE = "UserSettings/TeklyFavorites.asset";

		public static FavoritesData Instance {
			get {
				if (s_instance == null) {
					var tmp = Resources.FindObjectsOfTypeAll<FavoritesData>();

					if (tmp.Length > 0) {
						s_instance = tmp[0];
					} else {
						s_instance = CreateInstance<FavoritesData>();
						if (File.Exists(FAVORITES_SAVE_FILE)) {
							EditorJsonUtility.FromJsonOverwrite(File.ReadAllText(FAVORITES_SAVE_FILE), s_instance);
						}
					}

					s_instance.hideFlags = HideFlags.DontSave;
				}

				if (s_instance.Collections.Count == 0) {
					s_instance.Collections.Add(new FavoriteCollection("General"));
				}

				return s_instance;
			}
		}

		public void SetActiveCollection(int index)
		{
			if (m_activeCollectionIndex != index && index < Collections.Count) {
				Undo.RecordObject(this, "Modify favorites");
				m_activeCollectionIndex = index;
				m_favoriteIndex = ActiveCollection.Favorites.Count > 0 ? 0 : -1;
				Save();
			}
		}

		public void SetActiveCollection(FavoriteCollection collection)
		{
			int index = Collections.IndexOf(collection);
			SetActiveCollection(index);
		}

		public void RenameActiveCollection(string newName)
		{
			RenameCollection(newName, ActiveCollection);
		}

		public void RenameCollection(string newName, FavoriteCollection collection)
		{
			if (newName == collection.Name) {
				return;
			}
			
			Undo.RecordObject(this, "Rename favorites collection");
			collection.Name = newName;
			Save();
		}

		public void AddNewCollection()
		{
			Undo.RecordObject(this, "Add new favorites collection");
			FavoriteCollection collection = new FavoriteCollection();
			collection.Name = "New Collection";

			Collections.Add(collection);

			m_activeCollectionIndex = Collections.Count - 1;
			Save();
		}
		
		public void RemoveCollection(FavoriteCollection collection)
		{
			if (Collections.Count == 1) {
				return;
			}

			Undo.RecordObject(this, "Remove favorites collection");
			int index = Collections.IndexOf(collection);

			Collections.RemoveAt(index);

			m_activeCollectionIndex = index - 1;
			Save();
		}

		public void SetActiveFavorite(FavoriteAsset favorite)
		{
			var index = ActiveCollection.Favorites.IndexOf(favorite);
			SetFavoriteIndex(index);
		}

		public bool HandleShortcut(KeyCode keyCode, bool isModified, Event evt)
		{
			if (keyCode < KeyCode.Alpha1 || keyCode > KeyCode.Alpha9) {
				return false;
			}
			
			var index = keyCode - KeyCode.Alpha1;

			if (isModified) {
				SetActiveCollection(index);
				evt.Use();
			} else {
				if (index >= Instance.ActiveCollection.Favorites.Count) {
					return false;
				}

				var favoriteAsset = Instance.ActiveCollection.Favorites[index];

				if (favoriteAsset.Asset == null) {
					EditorApplication.Beep();
					return false;
				}

				if (favoriteAsset.Asset is FavoriteActionAsset fa) {
					fa.Activate();
					return true;
				}

				if (m_favoriteIndex == index) {
					AssetDatabase.OpenAsset(favoriteAsset.Asset);
					return true;
				}

				SetFavoriteIndex(index);
				return false;
			}

			return false;
		}

		public void SetFavoriteIndex(int index)
		{
			if (index == m_favoriteIndex) {
				return;
			}

			Undo.RecordObject(this, "Modify favorites");
			m_favoriteIndex = index;
			ActiveFavorite.TryToUpdateIcon();
			Save();

			Selection.objects = new[] { ActiveFavorite.Asset };
		}

		public void Save()
		{
			var json = EditorJsonUtility.ToJson(this);
			File.WriteAllText(FAVORITES_SAVE_FILE, json);
		}

		public void RemoveFavorite(FavoriteAsset favorite)
		{
			Undo.RecordObject(this, "Modify favorites");
			ActiveCollection.Favorites.Remove(favorite);
			Save();
		}
		
		public void AddFavoritesToEnd(Object[] assets)
		{
			Undo.RecordObject(this, "Modify favorites");
			var objects = assets.Select(x => new FavoriteAsset { Asset = x });
			ActiveCollection.Favorites.AddRange(objects);
			m_favoriteIndex = ActiveCollection.Favorites.Count - 1;
			
			Save();
		}
		
		public void AddFavorites(Object[] assets, int index)
		{
			Undo.RecordObject(this, "Modify favorites");
			var objects = assets.Select(x => new FavoriteAsset { Asset = x });
			ActiveCollection.Favorites.InsertRange(index, objects);

			Save();
		}

		public void OrderFavoriteToEnd(FavoriteAsset fa)
		{
			Undo.RecordObject(this, "Modify favorites");
			ActiveCollection.Favorites.Remove(fa);
			ActiveCollection.Favorites.Add(fa);
			m_favoriteIndex = ActiveCollection.Favorites.Count - 1;
			Save();
		}
		
		public void ReorderFavorite(FavoriteAsset fa, int index)
		{
			Undo.RecordObject(this, "Modify favorites");
			int oldIndex = ActiveCollection.Favorites.IndexOf(fa);
			ActiveCollection.Favorites.RemoveAt(oldIndex);
			ActiveCollection.Favorites.Insert(index, fa);
			m_favoriteIndex = index;
			Save();
		}

		public void ReorderCollection(FavoriteCollection collection, int index)
		{
			Undo.RecordObject(this, "Modify favorites");
			int oldIndex = Collections.IndexOf(collection);
			Collections.RemoveAt(oldIndex);
			Collections.Insert(index, collection);
			m_activeCollectionIndex = index;
			Save();
		}
		
		public void OrderCollectionToEnd(FavoriteCollection collection)
		{
			Undo.RecordObject(this, "Modify favorites");
			Collections.Remove(collection);
			Collections.Add(collection);
			m_activeCollectionIndex = Collections.Count - 1;
			Save();
		}

		public void TryToUpdateIcons()
		{
			foreach (var favorite in ActiveCollection.Favorites) {
				favorite.TryToUpdateIcon();
			}
		}
	}
}