using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

namespace Tekly.Common.Tweenlines
{
    /// <summary>
    /// Previews a Tweenline in edit mode. Driven properties are registered with AnimationMode so stopping
    /// the preview restores them and the scene is never dirtied.
    /// </summary>
    public static class TweenlinePreview
    {
        public static event Action Changed;

        public static Tweenline Target { get; private set; }
        public static int AnimationIndex { get; private set; }
        public static float Time { get; private set; }
        public static bool IsPlaying { get; private set; }
        public static bool IsActive => s_driver != null && AnimationMode.InAnimationMode(s_driver);

        public static TweenAnimation Animation {
            get {
                if (Target == null || AnimationIndex < 0 || AnimationIndex >= Target.Animations.Count) {
                    return null;
                }

                return Target.Animations[AnimationIndex];
            }
        }

        private static AnimationModeDriver s_driver;
        private static readonly List<DrivenProperty> s_registered = new List<DrivenProperty>();
        private static readonly List<DrivenProperty> s_scratch = new List<DrivenProperty>();
        private static readonly List<(EditorCurveBinding, PropertyModification)> s_modifications = new List<(EditorCurveBinding, PropertyModification)>();
        private static double s_lastTick;
        private static float s_direction = 1;

        public static bool IsPreviewing(Tweenline tweenline)
        {
            return IsActive && Target == tweenline;
        }

        public static void Start(Tweenline target, int animationIndex)
        {
            if (Application.isPlaying || target == null) {
                return;
            }

            if (IsActive) {
                Stop();
            }

            Target = target;
            AnimationIndex = animationIndex;
            s_direction = 1;

            if (s_driver == null) {
                s_driver = ScriptableObject.CreateInstance<AnimationModeDriver>();
                s_driver.hideFlags = HideFlags.HideAndDontSave;
                s_driver.name = "TweenlinePreview";
            }

            AnimationMode.StartAnimationMode(s_driver);
            Register();

            Target.CaptureOrigins();

            var animation = Animation;
            if (animation != null) {
                Time = Mathf.Clamp(Time, 0, animation.Duration);
                Target.Prepare(animation, true);
            }

            Subscribe();
            Sample();
        }

        public static void Stop()
        {
            Unsubscribe();
            IsPlaying = false;

            if (s_driver != null && AnimationMode.InAnimationMode(s_driver)) {
                AnimationMode.StopAnimationMode(s_driver);
            }

            s_registered.Clear();
            s_modifications.Clear();
            Target = null;

            RepaintViews();
            Changed?.Invoke();
        }

        /// <summary>
        /// Switches the previewed animation. Clips using ClipFrom.Current start from the pose the previous
        /// animation left, the same as interrupting it at runtime.
        /// </summary>
        public static void SetAnimation(int index)
        {
            AnimationIndex = index;

            if (!IsActive) {
                return;
            }

            Time = 0;
            s_direction = 1;

            var animation = Animation;
            if (animation != null) {
                Target.Prepare(animation, true);
            }

            Sample();
        }

        public static void SetTime(float time)
        {
            var animation = Animation;
            Time = animation != null ? Mathf.Clamp(time, 0, animation.Duration) : 0;
            Sample();
        }

        public static void Play()
        {
            var animation = Animation;
            if (!IsActive || animation == null) {
                return;
            }

            if (animation.Wrap == TweenWrap.Once && Time >= animation.Duration) {
                Time = 0;
            }

            IsPlaying = true;
            s_lastTick = EditorApplication.timeSinceStartup;
            Changed?.Invoke();
        }

        public static void Pause()
        {
            IsPlaying = false;
            Changed?.Invoke();
        }

        public static void Restart()
        {
            s_direction = 1;
            SetTime(0);
            Play();
        }

        /// <summary>
        /// Call after the Tweenline changes. Registers properties again if the driven set changed, then
        /// resolves and samples the animation at the current time.
        /// </summary>
        public static void Refresh()
        {
            if (!IsActive) {
                return;
            }

            if (Target == null) {
                Stop();
                return;
            }

            if (DrivenPropertiesChanged()) {
                var target = Target;
                var index = AnimationIndex;
                var time = Time;
                var playing = IsPlaying;

                Stop();
                Time = time;
                Start(target, index);

                if (playing) {
                    Play();
                }

                return;
            }

            var animation = Animation;
            if (animation != null) {
                Target.Prepare(animation, false);
                Time = Mathf.Clamp(Time, 0, animation.Duration);
            }

            Sample();
        }

        private static void Register()
        {
            s_registered.Clear();
            s_modifications.Clear();
            CollectDrivenProperties(s_registered);

            // Original values are read before anything is written so AnimationMode restores to them
            foreach (var property in s_registered) {
                var modification = new PropertyModification {
                    target = property.Target,
                    propertyPath = property.PropertyPath,
                    value = ReadValue(property)
                };

                var binding = EditorCurveBinding.FloatCurve(string.Empty, property.Target.GetType(), property.PropertyPath);
                s_modifications.Add((binding, modification));
            }

            AnimationMode.BeginSampling();
            AddModifications();
            AnimationMode.EndSampling();
        }

        /// <summary>
        /// EndSampling forgets every property that was not added during that sampling pass, so this runs
        /// inside every pass. Without it the properties stop being previewed and are never restored.
        /// </summary>
        private static void AddModifications()
        {
            foreach (var (binding, modification) in s_modifications) {
                AnimationMode.AddPropertyModification(binding, modification, true);
            }
        }

        private static void CollectDrivenProperties(List<DrivenProperty> properties)
        {
            foreach (var animation in Target.Animations) {
                foreach (var track in animation.Tracks) {
                    if (track != null && track.Previewable) {
                        track.CollectDrivenProperties(properties);
                    }
                }
            }
        }

        private static bool DrivenPropertiesChanged()
        {
            s_scratch.Clear();
            CollectDrivenProperties(s_scratch);

            if (s_scratch.Count != s_registered.Count) {
                return true;
            }

            for (var i = 0; i < s_scratch.Count; i++) {
                if (s_scratch[i].Target != s_registered[i].Target || s_scratch[i].PropertyPath != s_registered[i].PropertyPath) {
                    return true;
                }
            }

            return false;
        }

        private static string ReadValue(DrivenProperty property)
        {
            using var serializedObject = new SerializedObject(property.Target);
            var serializedProperty = serializedObject.FindProperty(property.PropertyPath);

            if (serializedProperty == null) {
                return string.Empty;
            }

            return serializedProperty.propertyType switch {
                SerializedPropertyType.Float => serializedProperty.floatValue.ToString(CultureInfo.InvariantCulture),
                SerializedPropertyType.Boolean => serializedProperty.boolValue ? "1" : "0",
                SerializedPropertyType.Integer => serializedProperty.intValue.ToString(CultureInfo.InvariantCulture),
                _ => string.Empty
            };
        }

        private static void Sample()
        {
            var animation = Animation;

            if (IsActive && animation != null) {
                AnimationMode.BeginSampling();
                AddModifications();
                SampleTracks(animation);
                AnimationMode.EndSampling();
            }

            RepaintViews();
            Changed?.Invoke();
        }

        private static void SampleTracks(TweenAnimation animation)
        {
            foreach (var track in animation.Tracks) {
                if (track != null && track.Previewable) {
                    track.Evaluate(Time);
                }
            }
        }

        private static void Tick()
        {
            if (Target == null) {
                Stop();
                return;
            }

            if (!IsPlaying) {
                return;
            }

            var now = EditorApplication.timeSinceStartup;
            var deltaTime = (float) (now - s_lastTick);
            s_lastTick = now;

            var animation = Animation;
            if (animation == null) {
                Pause();
                return;
            }

            var duration = animation.Duration;
            var time = Time + deltaTime * s_direction;

            if (duration <= 0) {
                Time = 0;
                IsPlaying = false;
            } else if (s_direction > 0 && time >= duration || s_direction < 0 && time <= 0) {
                var edge = s_direction > 0 ? duration : 0;
                var overshoot = Mathf.Abs(time - edge) % duration;

                switch (animation.Wrap) {
                    case TweenWrap.Once:
                        Time = edge;
                        IsPlaying = false;
                        break;
                    case TweenWrap.Loop:
                        Time = s_direction > 0 ? overshoot : duration - overshoot;
                        break;
                    case TweenWrap.PingPong:
                        s_direction = -s_direction;
                        Time = edge + overshoot * s_direction;
                        break;
                }
            } else {
                Time = time;
            }

            Sample();
        }

        private static void RepaintViews()
        {
            EditorApplication.QueuePlayerLoopUpdate();
            InternalEditorUtility.RepaintAllViews();
        }

        private static void Subscribe()
        {
            Unsubscribe();
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
            EditorSceneManager.sceneSaving += OnSceneSaving;
            PrefabStage.prefabStageClosing += OnPrefabStageClosing;
            Undo.undoRedoPerformed += Refresh;
        }

        private static void Unsubscribe()
        {
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= Stop;
            EditorSceneManager.sceneSaving -= OnSceneSaving;
            PrefabStage.prefabStageClosing -= OnPrefabStageClosing;
            Undo.undoRedoPerformed -= Refresh;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode) {
                Stop();
            }
        }

        private static void OnSceneSaving(UnityEngine.SceneManagement.Scene scene, string path)
        {
            Stop();
        }

        private static void OnPrefabStageClosing(PrefabStage stage)
        {
            Stop();
        }
    }
}
