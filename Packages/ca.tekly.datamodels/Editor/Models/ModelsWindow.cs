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
        public GUIContent Value;
        public string FullPath;
        public int Depth;
        public bool IsObject;
    }

    public class ModelsWindow : EditorWindow
    {
        private float INDENT = 18;
        private float HEIGHT = EditorGUIUtility.singleLineHeight;

        [SerializeField] private List<ObjectEntry> m_entries = new List<ObjectEntry>();
        [SerializeField] private List<ObjectEntry> m_visibleEntries = new List<ObjectEntry>();
        [SerializeField] private List<string> m_collapsedEntries = new List<string>();

        [SerializeField] private Vector2 m_scrollPos;
        [SerializeField] private string m_search;

        private SearchField m_searchField;

        [MenuItem("Tools/Tekly/DataModels", false, 1)]
        private static void OpenWindow()
        {
            GetWindow<ModelsWindow>("Models");
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += Repaint;
        }
        
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= Repaint;
        }

        public void OnGUI()
        {
            if (m_searchField == null) {
                m_searchField = new SearchField();
            }

            if (Application.isPlaying && !EditorApplication.isPaused) {
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

            m_search = m_searchField.OnGUI(new Rect(2, 2, width - 5, 20), m_search);
            m_scrollPos = GUI.BeginScrollView(new Rect(0, 22, width, height - 20), m_scrollPos, new Rect(0, 0, viewWidth, totalHeight));

            if (string.IsNullOrEmpty(m_search)) {
                var row = 0;
                for (var index = 0; index < m_visibleEntries.Count; index++) {
                    var objectEntry = m_visibleEntries[index];
                    DrawEntry(objectEntry, index, row, viewWidth);
                    row++;
                    
                    if (objectEntry.IsObject && !IsExpanded(objectEntry)) {
                        while (++index < m_visibleEntries.Count && m_visibleEntries[index].Depth > objectEntry.Depth) {
                            
                        }

                        index--;
                    }
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

        private bool IsExpanded(ObjectEntry entry)
        {
            return !m_collapsedEntries.Contains(entry.FullPath);
        }
        
        private void SetExpanded(ObjectEntry entry, bool expanded)
        {
            if (expanded) {
                m_collapsedEntries.Remove(entry.FullPath);
            } else {
                m_collapsedEntries.Add(entry.FullPath);
            }
        }

        private void DrawEntry(ObjectEntry objectEntry, int index, int row, float viewWidth)
        {
            viewWidth -= 4;
            var backgroundRect = new Rect(4, row * HEIGHT, viewWidth, HEIGHT);
            EditorGUI.DrawRect(backgroundRect, new Color(0, 0, 0, .5f * ((1 + objectEntry.Depth) / 8f)));

            var indent = objectEntry.Depth * INDENT + 4;
            
            var foldOutRect = new Rect(indent, row * HEIGHT, viewWidth - indent, HEIGHT);

            if (objectEntry.IsObject) {
                Undo.RecordObject(this, "Expand/Collapse");
                var isExpanded = IsExpanded(objectEntry);
                bool expanded = EditorGUI.Foldout(foldOutRect, isExpanded, objectEntry.Id);
            
                if (expanded != isExpanded) {
                    SetExpanded(objectEntry, expanded);
                    
                    if (Event.current.alt) {
                        SetExpanded(index, objectEntry.Depth, expanded);    
                    }
                }
            } else {
                foldOutRect.xMin += 14f;
                EditorGUI.LabelField(foldOutRect, objectEntry.Id);
            }
            
            if (objectEntry.Value != null) {
                var valueWidth = GUI.skin.label.CalcSize(objectEntry.Value).x;
                var valueRect = new Rect(foldOutRect.xMax - valueWidth, row * HEIGHT, valueWidth, HEIGHT);
                EditorGUI.LabelField(valueRect, objectEntry.Value);
            }
        }

        private void DrawEntrySearch(int index, float viewWidth)
        {
            var objectEntry = m_visibleEntries[index];

            var labelRect = new Rect(0, index * HEIGHT, viewWidth, HEIGHT);
            EditorGUI.DrawRect(labelRect, new Color(0, 0, 0, .2f * ((1 + index % 2) / 2f)));
            EditorGUI.LabelField(labelRect, objectEntry.FullPath);

            if (objectEntry.Value != null) {
                var valueWidth = GUI.skin.label.CalcSize(objectEntry.Value).x;
                var valueRect = new Rect(labelRect.xMax - valueWidth, index * HEIGHT, valueWidth, HEIGHT);
                EditorGUI.LabelField(valueRect, objectEntry.Value);
            }
        }
        
        private void SetExpanded(int index, int startDepth, bool expanded)
        {
            while (++index < m_visibleEntries.Count && m_visibleEntries[index].Depth > startDepth) {
                var entry = m_visibleEntries[index];
                if (entry.IsObject) {
                    SetExpanded(m_visibleEntries[index], expanded);    
                }
            }
        }

        private void CreateEntries(ObjectModel objectModel, List<ObjectEntry> entries, string parentKey, int depth = 0)
        {
            foreach (var modelReference in objectModel.Models) {
                var childModel = modelReference.Model;
                var fullPath = CombineKeys(parentKey, modelReference.Key);

                var entry = new ObjectEntry {
                    Id = new GUIContent(modelReference.Key, childModel.GetType().Name),
                    FullPath = fullPath,
                    Depth = depth,
                    IsObject = childModel is ObjectModel
                };
                
                entries.Add(entry);
                
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