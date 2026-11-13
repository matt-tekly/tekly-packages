using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Common.Tweenlines
{
    public static class TweenlineEditorUtils
    {
        /// <summary>
        /// Changes a Tweenline directly with undo and prefab override support, then refreshes the preview.
        /// </summary>
        public static void Modify(Tweenline tweenline, string undoName, Action action)
        {
            Undo.RecordObject(tweenline, undoName);
            action();
            PrefabUtility.RecordPrefabInstancePropertyModifications(tweenline);
            EditorUtility.SetDirty(tweenline);

            RefreshPreview(tweenline);
        }

        public static void RefreshPreview(Tweenline tweenline)
        {
            if (TweenlinePreview.IsPreviewing(tweenline)) {
                TweenlinePreview.Refresh();
            }
        }

        public static string TrackName(Type trackType)
        {
            var name = trackType.Name;

            if (name.EndsWith("Track")) {
                name = name.Substring(0, name.Length - "Track".Length);
            }

            return ObjectNames.NicifyVariableName(name);
        }

        /// <summary>
        /// Shows every concrete track type. Types whose target exists on the GameObject come first, most
        /// specific target type first, so a RectTransform lists Anchored Position above Position.
        /// </summary>
        public static void ShowAddTrackMenu(Tweenline tweenline, TweenAnimation animation, Action<int> added)
        {
            var entries = TypeCache.GetTypesDerivedFrom<TweenTrack>()
                .Where(type => !type.IsAbstract && !type.IsGenericTypeDefinition && type.GetConstructor(Type.EmptyTypes) != null)
                .Select(type => {
                    var targetType = ((TweenTrack) Activator.CreateInstance(type)).TargetType;
                    return (type, available: IsAvailable(tweenline.gameObject, targetType), depth: Depth(targetType));
                })
                .OrderByDescending(entry => entry.available)
                .ThenByDescending(entry => entry.depth)
                .ThenBy(entry => entry.type.Name)
                .ToArray();

            var menu = new GenericMenu();
            var separated = false;

            foreach (var entry in entries) {
                if (!entry.available && !separated && menu.GetItemCount() > 0) {
                    menu.AddSeparator(string.Empty);
                    separated = true;
                }

                var type = entry.type;

                menu.AddItem(new GUIContent(TrackName(type)), false, () => {
                    var index = -1;

                    Modify(tweenline, "Add Track", () => {
                        var track = (TweenTrack) Activator.CreateInstance(type);
                        track.Bind(tweenline.gameObject);
                        track.AddClip(0);

                        animation.Tracks.Add(track);
                        index = animation.Tracks.Count - 1;
                    });

                    added?.Invoke(index);
                });
            }

            menu.ShowAsContext();
        }

        private static bool IsAvailable(GameObject gameObject, Type targetType)
        {
            if (targetType == typeof(GameObject)) {
                return true;
            }

            return typeof(Component).IsAssignableFrom(targetType) && gameObject.GetComponent(targetType) != null;
        }

        private static int Depth(Type type)
        {
            var depth = 0;

            while (type != null && type != typeof(Object)) {
                depth++;
                type = type.BaseType;
            }

            return depth;
        }
    }
}
