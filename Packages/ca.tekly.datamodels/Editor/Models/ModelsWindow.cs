using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Tekly.DataModels.Models
{
    [Serializable]
    public class ObjectEntry
    {
        public GUIContent Id;
        public string Type;
        public GUIContent Value;
        public string FullPath;
        public int Depth;
    }
    
    public class ModelsWindow : EditorWindow
    {
        private float INDENT = 18;
        private float HEIGHT = EditorGUIUtility.singleLineHeight;

        [SerializeField] private List<ObjectEntry> m_entries = new List<ObjectEntry>();
        [SerializeField] private List<ObjectEntry> m_visibleEntries = new List<ObjectEntry>();
        
        [SerializeField] private Vector2 m_scrollPos;
        [SerializeField] private string m_search;
        
        private SearchField m_searchField;
        
        [MenuItem("Tools/Tekly/DataModels", false, 1)]
        private static void OpenWindow()
        {
            GetWindow<ModelsWindow>("Models");
        }
        
        public void OnGUI()
        {
            if (m_searchField == null) {
                m_searchField = new SearchField();
            }
            
            if (Application.isPlaying) {
                m_entries.Clear();
                CreateEntries(RootModel.Instance, m_entries, null);
            }
            
            m_visibleEntries.Clear();
            
            if (string.IsNullOrEmpty(m_search)) {
                m_visibleEntries.AddRange(m_entries);
            } else {
                var search = m_search.Split(" ");
                foreach (var entry in m_entries) {
                    if (MatchesSearch(search, entry.FullPath)) {
                        m_visibleEntries.Add(entry);
                    }
                }
            }
            
            var width = position.width;
            var height = position.height;

            var totalHeight = m_visibleEntries.Count * HEIGHT;
            var viewWidth = width;
            
            if (totalHeight > height) {
                viewWidth -= 15;
            }
            
            m_search = m_searchField.OnGUI(new Rect(2, 2, width-5, 20), m_search);
            
            m_scrollPos = GUI.BeginScrollView(new Rect(0,22, width, height - 20), m_scrollPos, new Rect(0, 0, viewWidth, totalHeight));

            if (string.IsNullOrEmpty(m_search)) {
                for (var index = 0; index < m_visibleEntries.Count; index++) {
                    DrawEntry(index, viewWidth);
                }    
            } else {
                for (var index = 0; index < m_visibleEntries.Count; index++) {
                    DrawEntrySearch(index, viewWidth);
                }
            }
            
            
            GUI.EndScrollView();
        }

        private bool MatchesSearch(string[] search, string fullPath)
        {
            if (search.Length == 1) {
                return fullPath.Contains(search[0]);
            }

            foreach (var entry in search) {
                if (!fullPath.Contains(entry)) {
                    return false;
                }
            }

            return true;
        }

        private void DrawEntry(int index, float viewWidth)
        {
            var objectEntry = m_visibleEntries[index];
            var indent = objectEntry.Depth * INDENT;
            var labelRect = new Rect(indent, index * HEIGHT, viewWidth - indent, HEIGHT);
            EditorGUI.LabelField(labelRect, objectEntry.Id);

            if (objectEntry.Value != null) {
                var valueWidth = GUI.skin.label.CalcSize(objectEntry.Value).x;
                var valueRect = new Rect(labelRect.xMax - valueWidth, index * HEIGHT, valueWidth, HEIGHT);
                EditorGUI.LabelField(valueRect, objectEntry.Value);
            }
        }
        
        private void DrawEntrySearch(int index, float viewWidth)
        {
            var objectEntry = m_visibleEntries[index];
            
            var labelRect = new Rect(0, index * HEIGHT, viewWidth, HEIGHT);
            EditorGUI.LabelField(labelRect, objectEntry.FullPath);

            if (objectEntry.Value != null) {
                var valueWidth = GUI.skin.label.CalcSize(objectEntry.Value).x;
                var valueRect = new Rect(labelRect.xMax - valueWidth, index * HEIGHT, valueWidth, HEIGHT);
                EditorGUI.LabelField(valueRect, objectEntry.Value);
            }
        }

        private void CreateEntries(ObjectModel objectModel, List<ObjectEntry> entries, string parentKey, int depth = 0)
        {
            foreach (var modelReference in objectModel.Models) {
                var childModel = modelReference.Model;
                var fullPath = CombineKeys(parentKey, modelReference.Key);
                
                var entry = new ObjectEntry {
                    Id = new GUIContent(modelReference.Key, childModel.GetType().ToString()),
                    Type = childModel.GetType().ToString(),
                    FullPath = fullPath,
                    Depth = depth
                };

                if (string.IsNullOrEmpty(m_search) || fullPath.Contains(m_search)) {
                    entries.Add(entry);
                }
                
                switch (childModel) {
                    case IValueModel childValueModel:
                        entry.Value = new GUIContent(childValueModel.ToDisplayString());
                        break;
                    case ObjectModel childObjectModel:
                        CreateEntries(childObjectModel, entries, fullPath, depth + 1);
                        break;
                    default:
                        entry.Value = new GUIContent("UNKNOWN TYPE");
                        break;
                }
            }
        }

        private string CombineKeys(string parent, string child)
        {
            if (string.IsNullOrEmpty(parent)) {
                return child;
            }

            return parent + "." + child;
        }
    }
}