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

        /// <summary>
        /// Picks up an edit: refreshes the edit mode preview, or in play mode resolves the playing animation
        /// again so added, moved or changed clips take effect straight away.
        /// </summary>
        public static void RefreshPreview(Tweenline tweenline)
        {
            if (Application.isPlaying) {
                if (tweenline.Current != null) {
                    tweenline.Prepare(tweenline.Current, false);
                }

                return;
            }

            if (TweenlinePreview.IsPreviewing(tweenline)) {
                TweenlinePreview.Refresh();
            }
        }

        /// <summary>
        /// True when a track references a class that no longer exists, like a deleted track type.
        /// </summary>
        public static bool HasMissingTypes(Tweenline tweenline)
        {
            return SerializationUtility.HasManagedReferencesWithMissingTypes(tweenline);
        }

        /// <summary>
        /// The deleted class names the Tweenline still references. Unity can't say which list entry each one
        /// belongs to, since a missing entry reports a null reference id, so these are per Tweenline.
        /// </summary>
        public static string MissingTypeNames(Tweenline tweenline)
        {
            var names = SerializationUtility.GetManagedReferencesWithMissingTypes(tweenline)
                .Select(missing => missing.className)
                .Distinct();

            return string.Join(", ", names);
        }

        /// <summary>
        /// Removes every track whose class no longer exists, along with Unity's stored data for it. Data for a
        /// missing type can't be used, so there is no per entry removal.
        /// </summary>
        public static void RemoveAllMissingTypes(Tweenline tweenline)
        {
            Undo.RecordObject(tweenline, "Remove Missing Types");
            SerializationUtility.ClearAllManagedReferencesWithMissingTypes(tweenline);

            foreach (var animation in tweenline.Animations) {
                animation.Tracks.RemoveAll(track => track == null);
            }

            PrefabUtility.RecordPrefabInstancePropertyModifications(tweenline);
            EditorUtility.SetDirty(tweenline);
            RefreshPreview(tweenline);
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
                    return (type, targetType, available: IsAvailable(tweenline.gameObject, targetType), depth: Depth(targetType));
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
                var addComponent = !entry.available && CanAddComponent(entry.targetType) ? entry.targetType : null;
                var label = addComponent != null ? $"{TrackName(type)} (adds {addComponent.Name})" : TrackName(type);

                menu.AddItem(new GUIContent(label), false, () => {
                    var index = -1;
                    var undoGroup = Undo.GetCurrentGroup();

                    if (addComponent != null) {
                        Undo.AddComponent(tweenline.gameObject, addComponent);
                    }

                    Modify(tweenline, "Add Track", () => {
                        var track = (TweenTrack) Activator.CreateInstance(type);
                        track.Bind(tweenline.gameObject);
                        track.AddClip(0);

                        animation.Tracks.Add(track);
                        index = animation.Tracks.Count - 1;
                    });

                    Undo.CollapseUndoOperations(undoGroup);
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

        /// <summary>
        /// Whether a missing target component can be added for the user, like a CanvasGroup for a fade.
        /// </summary>
        private static bool CanAddComponent(Type targetType)
        {
            return typeof(Component).IsAssignableFrom(targetType)
                && !targetType.IsAbstract
                && targetType != typeof(Component)
                && !typeof(Transform).IsAssignableFrom(targetType);
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
