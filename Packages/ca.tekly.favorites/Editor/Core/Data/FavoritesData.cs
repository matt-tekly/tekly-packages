using System;
using System.Collections.Generic;
using System.Linq;
using Tekly.Favorites.Editor.Core.Settings;
using UnityEditor;
using UnityEngine;

namespace Tekly.Favorites
{
    public class FavoritesData : ScriptableObject
    {
        [SerializeField] private int m_activeCollectionIndex;
        [SerializeField] private int m_favoriteIndex;

        public List<FavoriteCollection> Collections = new List<FavoriteCollection>();
        public FavoriteCollection ActiveCollection => Collections[ActiveCollectionIndex];
        public FavoriteAsset ActiveFavorite => ActiveCollection.Favorites[FavoriteIndex];
        
        public event Action<FavoriteCollection> ActiveCollectionChanged;
        public event Action<int> FavoriteIndexChanged;
        public event Action FavoritesChanged;
        public event Action AssetRenamed;

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

        private static FavoritesData s_instance = null;

        public static FavoritesData Instance {
            get {
                if (s_instance == null) {
                    var tmp = Resources.FindObjectsOfTypeAll<FavoritesData>();

                    if (tmp.Length > 0) {
                        s_instance = tmp[0];
                    } else {
                        s_instance = CreateInstance<FavoritesData>();
                        var json = EditorPrefs.GetString(FavoritesSettings.FavoritesPrefsKey);
                        EditorJsonUtility.FromJsonOverwrite(json, s_instance);
                    }

                    s_instance.hideFlags = HideFlags.DontSave;
                }

                if (s_instance.Collections.Count == 0) {
                    s_instance.Collections.Add(new FavoriteCollection("General"));
                    s_instance.Collections.Add(new FavoriteCollection("Prefabs"));
                    s_instance.Collections.Add(new FavoriteCollection("Scenes"));
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

                ActiveCollectionChanged?.Invoke(ActiveCollection);
                FavoriteIndexChanged?.Invoke(m_favoriteIndex);
            }
        }

        public void SetActiveCollection(FavoriteCollection collection)
        {
            int index = Collections.IndexOf(collection);
            SetActiveCollection(index);
        }

        public void RenameActiveCollection(string newName)
        {
            Undo.RecordObject(this, "Modify favorites");
            ActiveCollection.Name = newName;
            Save();

            FavoritesChanged?.Invoke();
        }
        
        public void RenameCollection(string newName, FavoriteCollection collection)
        {
            Undo.RecordObject(this, "Modify favorites");
            collection.Name = newName;
            Save();

            FavoritesChanged?.Invoke();
        }

        public void AddNewCollection()
        {
            Undo.RecordObject(this, "Modify favorites");
            FavoriteCollection collection = new FavoriteCollection();
            collection.Name = "New Collection";

            Collections.Add(collection);

            m_activeCollectionIndex = Collections.Count - 1;
            Save();

            FavoritesChanged?.Invoke();
            ActiveCollectionChanged?.Invoke(ActiveCollection);
        }

        public void RemoveActiveCollection()
        {
            RemoveCollection(ActiveCollection);
        }

        public void RemoveCollection(int index)
        {
            RemoveCollection(Collections[index]);
        }

        public void RemoveCollection(FavoriteCollection collection)
        {
            if (Collections.Count == 1) {
                return;
            }

            Undo.RecordObject(this, "Modify favorites");
            int index = Collections.IndexOf(collection);
            
            Collections.RemoveAt(index);

            m_activeCollectionIndex = index - 1;
            Save();

            FavoritesChanged?.Invoke();
            ActiveCollectionChanged?.Invoke(ActiveCollection);
        }

        public void SetActiveFavorite(FavoriteAsset favorite)
        {
            var index = ActiveCollection.Favorites.IndexOf(favorite);
            SetFavoriteIndex(index);
        }

        public bool HandleShortcut(KeyCode keyCode, bool isModified)
        {
            if (keyCode < KeyCode.Alpha1 || keyCode > KeyCode.Alpha9) {
                return false;
            }

            int index = keyCode - KeyCode.Alpha1;

            if (isModified) {
                SetActiveCollection(index);
            } else {
                if (index < Instance.ActiveCollection.Favorites.Count) {
                    FavoriteAsset favoriteAsset = Instance.ActiveCollection.Favorites[index];

                    if (favoriteAsset.Asset == null) {
                        EditorApplication.Beep();
                        return false;
                    }

                    if (favoriteAsset.Asset is FavoriteActionAsset fa) {
                        fa.Activate();
                        return true;
                    }
                    
                    if (Selection.Contains(favoriteAsset.Asset.GetInstanceID())) {
                        AssetDatabase.OpenAsset(favoriteAsset.Asset);
                        
                        SetFavoriteIndex(index);
                        return true;
                    }

                    Selection.objects = new[] { favoriteAsset.Asset };
                    SetFavoriteIndex(index);
                }
            }

            return false;
        }

        private void SetFavoriteIndex(int index)
        {
            if (index == m_favoriteIndex) {
                return;
            }

            Undo.RecordObject(this, "Modify favorites");
            m_favoriteIndex = index;
            Save();

            Selection.objects = new[] { ActiveFavorite.Asset };

            FavoriteIndexChanged?.Invoke(index);
        }

        public void Save()
        {
            var json = EditorJsonUtility.ToJson(this);
            EditorPrefs.SetString(FavoritesSettings.FavoritesPrefsKey, json);
        }

        public void RemoveFavorite(FavoriteAsset favorite)
        {
            Undo.RecordObject(this, "Modify favorites");
            ActiveCollection.Favorites.Remove(favorite);
            Save();

            FavoritesChanged?.Invoke();
        }

        public void RemoveFavorite(int index)
        {
            Undo.RecordObject(this, "Modify favorites");
            ActiveCollection.Favorites.RemoveAt(index);
            m_favoriteIndex = Mathf.Clamp(index - 1, 0, ActiveCollection.Favorites.Count);
            Save();

            FavoriteIndexChanged?.Invoke(m_favoriteIndex);
            FavoritesChanged?.Invoke();
        }

        public void AddFavorites(UnityEngine.Object[] assets)
        {
            Undo.RecordObject(this, "Modify favorites");
            var objects = assets.Select(x => new FavoriteAsset { Asset = x });
            ActiveCollection.Favorites.AddRange(objects);
            m_favoriteIndex = ActiveCollection.Favorites.Count - 1;
            Save();

            FavoriteIndexChanged?.Invoke(m_favoriteIndex);
            FavoritesChanged?.Invoke();
        }

        public void AddFavorites(UnityEngine.Object[] assets, int index)
        {
            Undo.RecordObject(this, "Modify favorites");
            var objects = DragAndDrop.objectReferences.Select(x => new FavoriteAsset { Asset = x });
            ActiveCollection.Favorites.InsertRange(index, objects);

            Save();
            FavoritesChanged?.Invoke();
        }

        public void ReorderFavorite(FavoriteAsset fa, int index)
        {
            Undo.RecordObject(this, "Modify favorites");
            int oldIndex = ActiveCollection.Favorites.IndexOf(fa);
            ActiveCollection.Favorites.RemoveAt(oldIndex);
            ActiveCollection.Favorites.Insert(index, fa);
            m_favoriteIndex = index;
            Save();

            FavoriteIndexChanged?.Invoke(m_favoriteIndex);
        }

        public void ReorderCollection(FavoriteCollection collection, int index)
        {
            Undo.RecordObject(this, "Modify favorites");
            int oldIndex = Collections.IndexOf(collection);
            Collections.RemoveAt(oldIndex);
            Collections.Insert(index, collection);
            m_activeCollectionIndex = index;
            Save();

            ActiveCollectionChanged?.Invoke(ActiveCollection);
        }

        public void OnAssetRenamed()
        {
            AssetRenamed?.Invoke();
        }
    }
}