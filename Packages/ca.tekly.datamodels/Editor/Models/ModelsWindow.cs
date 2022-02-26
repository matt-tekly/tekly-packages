using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Tekly.DataModels.Models
{
    [Serializable]
    public class ObjectEntry
    {
        public GUIContent Id;
        public string Type;
        public GUIContent Value;
        
        public int Depth;
    }
    
    public class ModelsWindow : EditorWindow
    {
        private float INDENT = 18;
        private float HEIGHT = EditorGUIUtility.singleLineHeight;

        private Vector2 m_scrollPos;
        
        [SerializeField] private List<ObjectEntry> m_entries = new List<ObjectEntry>();
        
        [MenuItem("Tools/Tekly/DataModels", false, 1)]
        private static void OpenWindow()
        {
            GetWindow<ModelsWindow>("Models");
        }

        public void OnGUI()
        {
            if (Application.isPlaying) {
                m_entries.Clear();
                CreateEntries(ObjectModel.Instance, m_entries);
            }
            
            var width = position.width;
            var height = position.height;

            var totalHeight = m_entries.Count * HEIGHT;
            var viewWidth = width;
            
            if (totalHeight > height) {
                viewWidth -= 15;
            }
            
            m_scrollPos = GUI.BeginScrollView(new Rect(0,0, width, height), m_scrollPos, new Rect(0, 0, viewWidth, totalHeight));
            
            for (var index = 0; index < m_entries.Count; index++) {
                var objectEntry = m_entries[index];
                var indent = objectEntry.Depth * INDENT;
                var labelRect = new Rect(indent, index * HEIGHT, viewWidth - indent, HEIGHT);
                EditorGUI.LabelField(labelRect, objectEntry.Id);

                if (objectEntry.Value != null) {
                    var valueWidth = GUI.skin.label.CalcSize(objectEntry.Value).x;
                    var valueRect = new Rect(labelRect.xMax - valueWidth, index * HEIGHT, valueWidth, HEIGHT);
                    EditorGUI.LabelField(valueRect, objectEntry.Value);
                }
            }
            
            GUI.EndScrollView();
        }

        private void CreateEntries(ObjectModel objectModel, List<ObjectEntry> entries, int depth = 0)
        {
            foreach (var modelReference in objectModel.Models) {
                var childModel = modelReference.Model;
                var entry = new ObjectEntry {
                    Id = new GUIContent(modelReference.Key, childModel.GetType().ToString()),
                    Type = childModel.GetType().ToString(),
                    Depth = depth
                };

                entries.Add(entry);
                
                switch (childModel) {
                    case IValueModel childValueModel:
                        entry.Value = new GUIContent(childValueModel.ToDisplayString());
                        break;
                    case ObjectModel childObjectModel:
                        CreateEntries(childObjectModel, entries, depth + 1);
                        break;
                    default:
                        entry.Value = new GUIContent("UNKNOWN TYPE");
                        break;
                }
            }
        }
    }
}