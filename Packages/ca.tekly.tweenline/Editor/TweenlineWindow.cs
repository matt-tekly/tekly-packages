using System;
using System.Collections.Generic;
using System.Linq;
using Tekly.Tweenlines.Tracks;
using UnityEditor;
using UnityEngine;

namespace Tekly.Tweenlines
{
    /// <summary>
    /// Timeline editor for the selected Tweenline. Scrubbing or pressing play in edit mode starts a preview.
    /// Locking keeps the shown Tweenline while the Hierarchy selection changes.
    /// </summary>
    public class TweenlineWindow : EditorWindow, IHasCustomMenu
    {
        private const float HEADER_WIDTH = 220;
        private const float RULER_HEIGHT = 22;
        private const float ROW_HEIGHT = 24;
        private const float TRACK_INDENT = 18;
        private const float DETAILS_HEIGHT = 190;
        private const float MIN_DETAILS_HEIGHT = 60;
        private const float MIN_BODY_HEIGHT = 80;
        private const float SPLITTER_GRAB = 6;
        private const float SCROLLBAR_WIDTH = 13;
        private const float EDGE_GRAB = 6;
        private const float MIN_CLIP_WIDTH = 6;
        private const float EDGE_SNAP_PIXELS = 8;
        private const float DRAG_THRESHOLD = 3;

        private enum Selected
        {
            None,
            Track,
            Clip,
            Marker
        }

        private enum DragMode
        {
            None,
            Scrub,
            MoveClip,
            ResizeClip,
            ResizeClipStart,
            MoveMarker,
            Pan,
            BoxSelect,
            MoveTrack,
            ScaleSelection
        }

        /// <summary>
        /// A timeline row. Tracks are shown grouped under their target object, which is display only: the
        /// animation still holds one flat list of tracks.
        /// </summary>
        private struct Row
        {
            public GameObject Object;
            public int Track;

            public bool IsHeader => Track < 0;
        }

        private readonly struct ClipRef : IEquatable<ClipRef>
        {
            public readonly int Track;
            public readonly int Clip;

            public ClipRef(int track, int clip)
            {
                Track = track;
                Clip = clip;
            }

            public bool Equals(ClipRef other) => Track == other.Track && Clip == other.Clip;
            public override bool Equals(object obj) => obj is ClipRef other && Equals(other);
            public override int GetHashCode() => Track * 397 ^ Clip;
        }

        [SerializeField] private Tweenline m_target;
        [SerializeField] private int m_animationIndex;
        [SerializeField] private float m_pixelsPerSecond = 200;
        [SerializeField] private float m_offset = -0.05f;
        [SerializeField] private float m_snap = 0.05f;
        [SerializeField] private bool m_snapEnabled = true;
        [SerializeField] private float m_detailsHeight = DETAILS_HEIGHT;
        [SerializeField] private float m_timeScale = 1;
        [SerializeField] private bool m_locked;

        private SerializedObject m_serializedObject;
        private readonly List<Row> m_rows = new List<Row>();
        private readonly List<GameObject> m_rowObjects = new List<GameObject>();
        private readonly HashSet<GameObject> m_collapsed = new HashSet<GameObject>();
        private Selected m_selected;
        private int m_selectedTrack = -1;
        private int m_selectedClip = -1;
        private int m_selectedMarker = -1;

        // Every selected clip, the primary m_selectedTrack and m_selectedClip included
        private readonly List<ClipRef> m_selectedClips = new List<ClipRef>();

        private DragMode m_dragMode;
        private float m_dragOffset;
        private Vector2 m_mouseDownPosition;
        private bool m_dragMoved;
        private bool m_reduceSelectionOnClick;
        private float m_dragEnd;
        private readonly List<float> m_dragStarts = new List<float>();
        private readonly List<float> m_snapTimes = new List<float>();
        private float m_snapGuide = float.NaN;
        private readonly List<ClipRef> m_boxBase = new List<ClipRef>();
        private int m_boxTrack = -1;
        private int m_trackDropRow = -1;
        private bool m_trackDropAfter;
        private Rect m_timelineRect;
        private readonly List<float> m_dragDurations = new List<float>();
        private float m_scalePivot;
        private float m_scaleEdge;
        private float m_panStartOffset;
        private int m_undoGroup;
        private float m_rowScroll;
        private Vector2 m_detailsScroll;
        private float m_runtimeScrubTime;
        private GUIStyle m_lockStyle;

        private static readonly Color s_clipColor = new Color(0.27f, 0.52f, 0.78f);
        private static readonly Color s_clipSelectedColor = new Color(0.45f, 0.7f, 0.95f);
        private static readonly Color s_clipDisabledColor = new Color(0.4f, 0.4f, 0.4f);
        private static readonly Color s_playheadColor = new Color(1f, 0.3f, 0.25f);
        private static readonly Color s_markerColor = new Color(1f, 0.8f, 0.2f);
        private static readonly Color s_rowColor = new Color(0, 0, 0, 0.12f);
        private static readonly Color s_objectRowColor = new Color(0, 0, 0, 0.28f);
        private static readonly Color s_summaryColor = new Color(0.27f, 0.52f, 0.78f, 0.5f);
        private static readonly Color s_rowSelectedColor = new Color(0.3f, 0.5f, 0.8f, 0.2f);
        private static readonly Color s_stepOnColor = new Color(0.3f, 0.65f, 0.35f);
        private static readonly Color s_stepOffColor = new Color(0.35f, 0.35f, 0.35f, 0.6f);
        private static readonly Color s_endColor = new Color(1, 1, 1, 0.25f);
        private static readonly Color s_snapGuideColor = new Color(1f, 0.85f, 0.3f, 0.8f);
        private static readonly Color s_boxFillColor = new Color(0.45f, 0.7f, 0.95f, 0.15f);
        private static readonly Color s_boxEdgeColor = new Color(0.45f, 0.7f, 0.95f, 0.8f);
        private static readonly Color s_dropLineColor = new Color(0.45f, 0.7f, 0.95f);
        private static readonly Color s_scaleHandleColor = new Color(0.45f, 0.7f, 0.95f, 0.6f);
        private static readonly float[] s_tickSteps = { 0.01f, 0.02f, 0.05f, 0.1f, 0.2f, 0.5f, 1f, 2f, 5f, 10f };

        // Real time is first, so a scale that is not in the list falls back to it
        private static readonly float[] s_timeScales = { 1f, 0.1f, 0.25f, 0.5f, 2f };
        private static readonly string[] s_timeScaleLabels = { "1x", "0.1x", "0.25x", "0.5x", "2x" };

        private TweenAnimation Animation {
            get {
                if (m_target == null || m_target.Animations.Count == 0) {
                    return null;
                }

                m_animationIndex = Mathf.Clamp(m_animationIndex, 0, m_target.Animations.Count - 1);
                return m_target.Animations[m_animationIndex];
            }
        }

        private bool IsPreviewing => TweenlinePreview.IsPreviewing(m_target);

        private bool IsPlaying => Application.isPlaying ? m_target.IsPlaying : IsPreviewing && TweenlinePreview.IsPlaying;

        private float PlayheadTime {
            get {
                if (Application.isPlaying) {
                    return m_target.IsPlaying && m_target.Current == Animation ? m_target.Time : m_runtimeScrubTime;
                }

                return IsPreviewing ? TweenlinePreview.Time : 0;
            }
        }

        [MenuItem("Window/Tekly/Tweenline")]
        public static void Open()
        {
            var window = GetWindow<TweenlineWindow>();
            window.titleContent = new GUIContent("Tweenline");
            window.Show();
        }

        public static void Open(Tweenline tweenline)
        {
            var window = GetWindow<TweenlineWindow>();
            window.titleContent = new GUIContent("Tweenline");
            window.SetTarget(tweenline);
            window.Show();
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            TweenlinePreview.Changed += Repaint;
            Undo.undoRedoPerformed += OnUndoRedo;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            TweenlinePreview.TimeScale = m_timeScale;
            OnSelectionChanged();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            TweenlinePreview.Changed -= Repaint;
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            if (IsPreviewing) {
                TweenlinePreview.Stop();
            }
        }

        private void Update()
        {
            if (Application.isPlaying && m_target != null && m_target.IsPlaying) {
                Repaint();
            }
        }

        /// <summary>
        /// Called by Unity to draw extra buttons in the window's title bar.
        /// </summary>
        private void ShowButton(Rect rect)
        {
            m_lockStyle ??= new GUIStyle("IN LockButton");

            var locked = GUI.Toggle(rect, m_locked, GUIContent.none, m_lockStyle);

            if (locked != m_locked) {
                SetLocked(locked);
            }
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Lock"), m_locked, () => SetLocked(!m_locked));
        }

        private void SetLocked(bool locked)
        {
            m_locked = locked;

            if (!m_locked) {
                OnSelectionChanged();
            }
        }

        private void OnSelectionChanged()
        {
            FollowSelection();

            // The Add Track button follows the Hierarchy selection
            Repaint();
        }

        /// <summary>
        /// Entering or leaving play mode and opening a scene replace the scene's objects, which leaves the
        /// window's target destroyed while the selection hasn't changed. Picks the target up again from the
        /// selection when that happens.
        /// </summary>
        private void OnHierarchyChanged()
        {
            if (m_target == null) {
                FollowSelection();
                Repaint();
            }
        }

        private void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode || change == PlayModeStateChange.EnteredEditMode) {
                OnHierarchyChanged();
            }
        }

        /// <summary>
        /// Shows the Tweenline on the selected object. An object without one shows the nearest Tweenline above
        /// it, unless it is already inside the shown Tweenline's hierarchy, where it picks the Add Track
        /// target instead. A locked window keeps its Tweenline, so any selection only picks the Add Track target.
        /// </summary>
        private void FollowSelection()
        {
            var gameObject = Selection.activeGameObject;

            if (gameObject == null || m_locked && m_target != null) {
                return;
            }

            var tweenline = gameObject.GetComponent<Tweenline>();

            if (tweenline == null) {
                if (m_target != null && IsInHierarchy(gameObject)) {
                    return;
                }

                tweenline = gameObject.GetComponentInParent<Tweenline>(true);
            }

            if (tweenline != null) {
                SetTarget(tweenline);
            }
        }

        /// <summary>
        /// The object Add Track adds to: a child of the Tweenline picked in the Hierarchy, else the object of the
        /// track or clip selected in the window, else the Tweenline's own object. Objects outside the Tweenline's
        /// hierarchy are never picked automatically.
        /// </summary>
        private GameObject AddTrackTarget()
        {
            var selected = Selection.activeGameObject;

            if (selected != null && selected != m_target.gameObject && IsInHierarchy(selected)) {
                return selected;
            }

            if ((m_selected == Selected.Track || m_selected == Selected.Clip) && m_selectedTrack >= 0 && m_selectedTrack < Animation.Tracks.Count) {
                var trackObject = TargetGameObject(Animation.Tracks[m_selectedTrack]);

                if (trackObject != null) {
                    return trackObject;
                }
            }

            return m_target.gameObject;
        }

        private bool IsInHierarchy(GameObject gameObject)
        {
            return gameObject.transform.IsChildOf(m_target.transform);
        }

        /// <summary>
        /// The track's own warning, else a warning when it targets an object outside the Tweenline's hierarchy.
        /// </summary>
        private string TrackWarning(TweenTrack track)
        {
            var warning = track.GetWarning(m_target);

            if (warning != null) {
                return warning;
            }

            var gameObject = TargetGameObject(track);

            if (gameObject != null && !IsInHierarchy(gameObject)) {
                return $"Targets [{gameObject.name}], which is outside this Tweenline's hierarchy. Prefabs lose the reference, the object can be destroyed while the Tweenline still uses it, and another Tweenline animating it will fight this one.";
            }

            return null;
        }

        /// <summary>
        /// Undo can destroy the target, for example undoing adding the Tweenline or creating its object, and a
        /// SerializedObject of a destroyed object throws on Update. OnGUI makes a new one once there's a target.
        /// </summary>
        private void OnUndoRedo()
        {
            if (m_serializedObject != null && m_serializedObject.targetObject == null) {
                m_serializedObject = null;
            }

            m_serializedObject?.Update();
            Repaint();
        }

        private void SetTarget(Tweenline tweenline)
        {
            // Leaving play mode recreates the scene with the same instance ids, and Unity's == compares ids, so the
            // destroyed target would equal the restored one and the window would never pick it up again
            if (ReferenceEquals(tweenline, m_target)) {
                return;
            }

            if (TweenlinePreview.IsActive) {
                TweenlinePreview.Stop();
            }

            m_target = tweenline;
            m_animationIndex = 0;
            m_serializedObject = null;
            ClearSelection();
            Repaint();
        }

        private void OnGUI()
        {
            if (m_target == null) {
                FollowSelection();
            }

            if (m_target == null) {
                DrawNoTarget();
                return;
            }

            if (m_serializedObject == null || m_serializedObject.targetObject != m_target) {
                m_serializedObject = new SerializedObject(m_target);
            }

            m_serializedObject.Update();
            ValidateSelection();
            HandleKeyboard();

            EditorGUI.BeginChangeCheck();

            DrawToolbar();

            var top = EditorStyles.toolbar.fixedHeight;
            var maxDetailsHeight = Mathf.Max(MIN_DETAILS_HEIGHT, position.height - top - MIN_BODY_HEIGHT);
            m_detailsHeight = Mathf.Clamp(m_detailsHeight, MIN_DETAILS_HEIGHT, maxDetailsHeight);

            var body = new Rect(0, top, position.width, Mathf.Max(0, position.height - top - m_detailsHeight));
            var details = new Rect(0, body.yMax, position.width, m_detailsHeight);

            // Handled before the timeline so the timeline doesn't take drags that start on the divider
            HandleDetailsSplitter(new Rect(0, details.y - SPLITTER_GRAB * 0.5f, position.width, SPLITTER_GRAB), maxDetailsHeight);

            if (Animation != null) {
                DrawTimeline(body);
            } else {
                GUI.Label(body, "No animations. Use + in the toolbar to add one.", EditorStyles.centeredGreyMiniLabel);
            }

            DrawDetails(details);

            if (EditorGUI.EndChangeCheck()) {
                ApplyChanges();
            }
        }

        private void ApplyChanges()
        {
            m_serializedObject.ApplyModifiedProperties();
            TweenlineEditorUtils.RefreshPreview(m_target);
        }

        private void DrawNoTarget()
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label("Select a GameObject with a Tweenline", EditorStyles.centeredGreyMiniLabel);

            var gameObject = Selection.activeGameObject;

            if (gameObject != null) {
                using (new EditorGUILayout.HorizontalScope()) {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button($"Add Tweenline to {gameObject.name}", GUILayout.Width(240))) {
                        SetTarget(Undo.AddComponent<Tweenline>(gameObject));
                    }

                    GUILayout.FlexibleSpace();
                }
            }

            GUILayout.FlexibleSpace();
        }

        private void DrawToolbar()
        {
            using var scope = new EditorGUILayout.HorizontalScope(EditorStyles.toolbar);

            if (GUILayout.Button(m_target.name, EditorStyles.toolbarButton, GUILayout.MaxWidth(140))) {
                EditorGUIUtility.PingObject(m_target);
            }

            var current = Animation;
            var animationLabel = new GUIContent(current != null ? current.Name : "No Animation", "Switch, add or delete animations");

            if (EditorGUILayout.DropdownButton(animationLabel, FocusType.Passive, EditorStyles.toolbarDropDown, GUILayout.Width(120))) {
                ShowAnimationMenu();
            }

            if (GUILayout.Button(new GUIContent("+", "Add animation"), EditorStyles.toolbarButton, GUILayout.Width(22))) {
                AddAnimation();
            }

            if (Animation != null) {
                var wrap = AnimationProperty().FindPropertyRelative("m_wrap");
                EditorGUILayout.PropertyField(wrap, GUIContent.none, GUILayout.Width(80));
            }

            using (new EditorGUI.DisabledScope(Animation == null)) {
                if (GUILayout.Button(new GUIContent("Frame All", "Fit the whole animation in view. F frames the selection"), EditorStyles.toolbarButton, GUILayout.Width(64))) {
                    FrameAll();
                }
            }

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(Animation == null)) {
                DrawTransport();
            }

            var snapLabel = new GUIContent("Snap", "Snap drags to the grid, clip edges, markers and the playhead. Hold Ctrl while dragging to skip snapping");
            m_snapEnabled = GUILayout.Toggle(m_snapEnabled, snapLabel, EditorStyles.toolbarButton, GUILayout.Width(40));

            using (new EditorGUI.DisabledScope(!m_snapEnabled)) {
                m_snap = Mathf.Max(0, EditorGUILayout.FloatField(m_snap, EditorStyles.toolbarTextField, GUILayout.Width(40)));
            }
        }

        private void DrawTransport()
        {
            if (!Application.isPlaying) {
                var previewing = IsPreviewing;
                var preview = GUILayout.Toggle(previewing, "Preview", EditorStyles.toolbarButton, GUILayout.Width(60));

                if (preview != previewing) {
                    if (preview) {
                        TweenlinePreview.Start(m_target, m_animationIndex);
                    } else {
                        TweenlinePreview.Stop();
                    }
                }
            }

            if (GUILayout.Button(EditorGUIUtility.TrIconContent("Animation.FirstKey", "Restart"), EditorStyles.toolbarButton, GUILayout.Width(28))) {
                if (Application.isPlaying) {
                    m_target.Play(Animation, false);
                } else {
                    EnsurePreview();
                    TweenlinePreview.Restart();
                }
            }

            var playing = IsPlaying;
            var playIcon = EditorGUIUtility.TrIconContent(playing ? "PauseButton" : "PlayButton", playing ? "Pause" : "Play");

            if (GUILayout.Button(playIcon, EditorStyles.toolbarButton, GUILayout.Width(28))) {
                TogglePlay();
            }

            var time = EditorGUILayout.FloatField(PlayheadTime, EditorStyles.toolbarTextField, GUILayout.Width(50));

            if (!Mathf.Approximately(time, PlayheadTime)) {
                ScrubTo(time);
            }

            // Play mode runs on the game's own clock, so the scale only applies to the edit mode preview
            using (new EditorGUI.DisabledScope(Application.isPlaying)) {
                var scaleIndex = Mathf.Max(0, System.Array.IndexOf(s_timeScales, m_timeScale));
                var newIndex = EditorGUILayout.Popup(scaleIndex, s_timeScaleLabels, EditorStyles.toolbarPopup, GUILayout.Width(52));

                if (newIndex != scaleIndex || !Mathf.Approximately(TweenlinePreview.TimeScale, m_timeScale)) {
                    m_timeScale = s_timeScales[newIndex];
                    TweenlinePreview.TimeScale = m_timeScale;
                }
            }
        }

        private void TogglePlay()
        {
            var playing = IsPlaying;

            if (Application.isPlaying) {
                if (playing) {
                    m_target.Stop();
                } else {
                    m_target.Play(Animation, false);
                }
            } else if (playing) {
                TweenlinePreview.Pause();
            } else {
                EnsurePreview();
                TweenlinePreview.Play();
            }
        }

        private void DrawTimeline(Rect body)
        {
            var animation = Animation;
            var tracks = animation.Tracks;
            BuildRows(tracks);

            var contentHeight = m_rows.Count * ROW_HEIGHT;
            var rowsHeight = body.height - RULER_HEIGHT;
            var hasScrollbar = contentHeight > rowsHeight;
            var width = body.width - (hasScrollbar ? SCROLLBAR_WIDTH : 0);

            var rulerRect = new Rect(HEADER_WIDTH, body.y, width - HEADER_WIDTH, RULER_HEIGHT);
            var rowsRect = new Rect(0, body.y + RULER_HEIGHT, width, rowsHeight);
            var timelineRect = new Rect(HEADER_WIDTH, rowsRect.y, width - HEADER_WIDTH, rowsHeight);

            m_rowScroll = hasScrollbar ? Mathf.Clamp(m_rowScroll, 0, contentHeight - rowsHeight) : 0;
            m_timelineRect = timelineRect;

            HandleInput(rulerRect, timelineRect, new Rect(0, rowsRect.y, HEADER_WIDTH, rowsHeight));
            HandleDragAndDrop(new Rect(0, body.y, width, body.height));

            var headerRect = new Rect(0, body.y, HEADER_WIDTH, RULER_HEIGHT);
            EditorGUI.DrawRect(headerRect, new Color(0, 0, 0, 0.1f));

            var addTarget = AddTrackTarget();
            var addLabel = new GUIContent($"Add Track to {addTarget.name}", "Adds to the child of this Tweenline selected in the Hierarchy, else the selected track's object, else this Tweenline's object. Drag an object from the Hierarchy onto the timeline to add a track for it.");

            if (GUI.Button(new Rect(4, body.y + 2, HEADER_WIDTH - 60, RULER_HEIGHT - 4), addLabel, EditorStyles.miniButton)) {
                ShowAddTrackMenu(addTarget);
            }

            GUI.Label(new Rect(HEADER_WIDTH - 52, body.y + 3, 50, RULER_HEIGHT), $"{animation.Duration:0.##}s", EditorStyles.miniLabel);

            DrawRuler(rulerRect, animation);

            GUI.BeginGroup(rowsRect);

            for (var i = 0; i < m_rows.Count; i++) {
                var rowRect = new Rect(0, i * ROW_HEIGHT - m_rowScroll, rowsRect.width, ROW_HEIGHT);

                if (rowRect.yMax < 0 || rowRect.y > rowsRect.height) {
                    continue;
                }

                var row = m_rows[i];

                if (row.IsHeader) {
                    DrawObjectRow(rowRect, row.Object, tracks);
                } else {
                    DrawRow(rowRect, row.Track, tracks[row.Track]);
                }
            }

            var endX = TimeToX(animation.Duration);
            if (endX > HEADER_WIDTH) {
                EditorGUI.DrawRect(new Rect(endX, 0, 1, rowsRect.height), s_endColor);
            }

            GUI.EndGroup();

            DrawSelectionFrame(timelineRect);

            if (hasScrollbar) {
                var scrollRect = new Rect(width, rowsRect.y, SCROLLBAR_WIDTH, rowsHeight);
                m_rowScroll = GUI.VerticalScrollbar(scrollRect, m_rowScroll, rowsHeight, 0, contentHeight);
            }

            DrawPlayhead(new Rect(HEADER_WIDTH, body.y, rulerRect.width, body.height));
            DrawDragOverlays(rowsRect, timelineRect);
        }

        /// <summary>
        /// The box selection, the snap guide, the dragged clip's times and where a dragged track will land.
        /// </summary>
        private void DrawDragOverlays(Rect rowsRect, Rect timelineRect)
        {
            if (Event.current.type != EventType.Repaint || m_dragMode == DragMode.None) {
                return;
            }

            switch (m_dragMode) {
                case DragMode.BoxSelect when m_dragMoved: {
                    var box = BoxRect(Event.current.mousePosition);
                    EditorGUI.DrawRect(box, s_boxFillColor);
                    DrawOutline(box, s_boxEdgeColor);
                    break;
                }
                case DragMode.MoveTrack when m_trackDropRow >= 0: {
                    var y = rowsRect.y + (m_trackDropRow + (m_trackDropAfter ? 1 : 0)) * ROW_HEIGHT - m_rowScroll;
                    EditorGUI.DrawRect(new Rect(TRACK_INDENT, y - 1, rowsRect.width - TRACK_INDENT, 2), s_dropLineColor);
                    break;
                }
                case DragMode.MoveClip when m_dragMoved:
                case DragMode.ResizeClip:
                case DragMode.ResizeClipStart:
                case DragMode.MoveMarker:
                case DragMode.ScaleSelection:
                    if (!float.IsNaN(m_snapGuide)) {
                        var x = TimeToX(m_snapGuide);

                        if (x >= timelineRect.x && x <= timelineRect.xMax) {
                            EditorGUI.DrawRect(new Rect(x, timelineRect.y - RULER_HEIGHT, 1, timelineRect.height + RULER_HEIGHT), s_snapGuideColor);
                        }
                    }

                    DrawDragReadout();
                    break;
            }
        }

        private void DrawDragReadout()
        {
            string text;

            if (m_dragMode == DragMode.MoveMarker) {
                text = $"{Animation.Markers[m_selectedMarker].Time:0.###}s";
            } else if (m_dragMode == DragMode.ScaleSelection) {
                GetSelectionRange(out var start, out var end);
                text = $"{start:0.###}s to {end:0.###}s ({end - start:0.###}s, {(end - start) / Mathf.Abs(m_scaleEdge - m_scalePivot) * 100:0}%)";
            } else {
                var track = Animation.Tracks[m_selectedTrack];
                track.GetClipRange(m_selectedClip, out var start, out var duration);
                text = track is IStepTrack ? $"{start:0.###}s" : $"{start:0.###}s to {start + duration:0.###}s ({duration:0.###}s)";
            }

            var content = new GUIContent(text);
            var size = EditorStyles.helpBox.CalcSize(content);
            var mouse = Event.current.mousePosition;
            var rect = new Rect(mouse.x + 14, mouse.y - size.y - 6, size.x, size.y);
            rect.x = Mathf.Min(rect.x, position.width - size.x - 4);
            rect.y = Mathf.Max(rect.y, 2);

            GUI.Label(rect, content, EditorStyles.helpBox);
        }

        private static void DrawOutline(Rect rect, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), color);
        }

        private void DrawRuler(Rect rect, TweenAnimation animation)
        {
            EditorGUI.DrawRect(rect, new Color(0, 0, 0, 0.2f));

            var step = TickStep();
            var first = Mathf.Ceil(XToTime(rect.x) / step) * step;
            var last = XToTime(rect.xMax);

            GUI.BeginClip(rect);

            for (var time = first; time <= last; time += step) {
                var x = TimeToX(time) - rect.x;
                var major = Mathf.Abs(time / (step * 5) - Mathf.Round(time / (step * 5))) < 0.001f;

                EditorGUI.DrawRect(new Rect(x, major ? 8 : 14, 1, rect.height), new Color(1, 1, 1, major ? 0.4f : 0.2f));

                if (major) {
                    GUI.Label(new Rect(x + 2, 0, 60, 14), time.ToString("0.##"), EditorStyles.miniLabel);
                }
            }

            for (var i = 0; i < animation.Markers.Count; i++) {
                var x = TimeToX(animation.Markers[i].Time) - rect.x;
                var selected = m_selected == Selected.Marker && m_selectedMarker == i;
                var markerRect = new Rect(x - 4, rect.height - 10, 8, 8);

                EditorGUI.DrawRect(markerRect, selected ? Color.white : s_markerColor);
            }

            GUI.EndClip();
        }

        /// <summary>
        /// Groups tracks under the object they animate, objects in the order their first track appears.
        /// Tracks with no target or a missing type go under an empty object row.
        /// </summary>
        private void BuildRows(List<TweenTrack> tracks)
        {
            m_rows.Clear();
            m_rowObjects.Clear();

            foreach (var track in tracks) {
                var gameObject = TargetGameObject(track);

                if (!m_rowObjects.Contains(gameObject)) {
                    m_rowObjects.Add(gameObject);
                }
            }

            foreach (var gameObject in m_rowObjects) {
                m_rows.Add(new Row { Object = gameObject, Track = -1 });

                if (m_collapsed.Contains(gameObject)) {
                    continue;
                }

                for (var t = 0; t < tracks.Count; t++) {
                    if (TargetGameObject(tracks[t]) == gameObject) {
                        m_rows.Add(new Row { Object = gameObject, Track = t });
                    }
                }
            }
        }

        private static GameObject TargetGameObject(TweenTrack track)
        {
            // A destroyed target still matches its type, so it is checked against Unity's null too.
            return track?.TargetObject switch {
                GameObject gameObject when gameObject != null => gameObject,
                Component component when component != null => component.gameObject,
                _ => null
            };
        }

        private void DrawObjectRow(Rect rowRect, GameObject gameObject, List<TweenTrack> tracks)
        {
            EditorGUI.DrawRect(rowRect, s_objectRowColor);

            var collapsed = gameObject != null && m_collapsed.Contains(gameObject);
            var foldoutRect = new Rect(rowRect.x + 4, rowRect.y + 3, 14, 18);

            if (gameObject != null && GUI.Button(foldoutRect, collapsed ? "\u25b6" : "\u25bc", EditorStyles.miniLabel)) {
                if (collapsed) {
                    m_collapsed.Remove(gameObject);
                } else {
                    m_collapsed.Add(gameObject);
                }
            }

            var name = gameObject != null ? gameObject.name : "No target";
            var labelRect = new Rect(rowRect.x + 20, rowRect.y + 3, HEADER_WIDTH - 50, 18);

            if (GUI.Button(labelRect, name, EditorStyles.boldLabel) && gameObject != null) {
                EditorGUIUtility.PingObject(gameObject);
            }

            if (gameObject != null) {
                var addRect = new Rect(HEADER_WIDTH - 26, rowRect.y + 3, 22, 18);

                if (GUI.Button(addRect, new GUIContent("+", $"Add a track for [{gameObject.name}]"), EditorStyles.miniButton)) {
                    ShowAddTrackMenu(gameObject);
                }
            }

            EditorGUI.DrawRect(new Rect(HEADER_WIDTH - 1, rowRect.y, 1, rowRect.height), new Color(0, 0, 0, 0.3f));

            if (!collapsed) {
                return;
            }

            // A collapsed object still shows a thin bar for each of its clips
            GUI.BeginClip(new Rect(HEADER_WIDTH, rowRect.y, rowRect.width - HEADER_WIDTH, rowRect.height));

            foreach (var track in tracks) {
                if (TargetGameObject(track) != gameObject) {
                    continue;
                }

                for (var c = 0; c < track.ClipCount; c++) {
                    var clipRect = ClipRect(track, c, 0);
                    clipRect.x -= HEADER_WIDTH;
                    clipRect.y = ROW_HEIGHT * 0.5f - 2;
                    clipRect.height = 4;

                    EditorGUI.DrawRect(clipRect, s_summaryColor);
                }
            }

            GUI.EndClip();
        }

        /// <summary>
        /// A track whose class was deleted or renamed. Its data is kept by Unity until removed here.
        /// </summary>
        private void DrawMissingRow(Rect rowRect)
        {
            var typeNames = TweenlineEditorUtils.MissingTypeNames(m_target);
            var label = new GUIContent($"⚠ Missing track type [{typeNames}]. Use Remove All below", "The class for this track no longer exists");

            GUI.Label(new Rect(rowRect.x + TRACK_INDENT + 6, rowRect.y + 3, rowRect.width - TRACK_INDENT - 12, 18), label, EditorStyles.miniBoldLabel);
        }

        private void DrawRow(Rect rowRect, int index, TweenTrack track)
        {
            var selectedRow = m_selectedTrack == index && m_selected != Selected.None && m_selected != Selected.Marker;
            EditorGUI.DrawRect(rowRect, selectedRow ? s_rowSelectedColor : index % 2 == 0 ? s_rowColor : Color.clear);

            if (track == null) {
                DrawMissingRow(rowRect);
                return;
            }

            var trackProperty = TrackProperty(index);
            var enabled = trackProperty.FindPropertyRelative("m_enabled");

            var toggleRect = new Rect(rowRect.x + TRACK_INDENT, rowRect.y + 4, 16, 16);
            enabled.boolValue = EditorGUI.Toggle(toggleRect, enabled.boolValue);

            var labelRect = new Rect(rowRect.x + TRACK_INDENT + 18, rowRect.y + 3, HEADER_WIDTH - TRACK_INDENT - 46, 18);
            var warning = TrackWarning(track);
            var label = new GUIContent($"{(warning != null ? "⚠ " : string.Empty)}{TweenlineEditorUtils.TrackName(track.GetType())}", warning);

            // Clicking selects and dragging reorders, both handled in HandleInput
            GUI.Label(labelRect, label, EditorStyles.label);

            var menuRect = new Rect(HEADER_WIDTH - 24, rowRect.y + 3, 20, 18);
            if (EditorGUI.DropdownButton(menuRect, GUIContent.none, FocusType.Passive, EditorStyles.toolbarDropDown)) {
                ShowTrackMenu(index);
            }

            EditorGUI.DrawRect(new Rect(HEADER_WIDTH - 1, rowRect.y, 1, rowRect.height), new Color(0, 0, 0, 0.3f));

            var clips = trackProperty.FindPropertyRelative("m_clips");
            var clipColor = track.Enabled && track.TargetObject != null ? s_clipColor : s_clipDisabledColor;

            GUI.BeginClip(new Rect(HEADER_WIDTH, rowRect.y, rowRect.width - HEADER_WIDTH, rowRect.height));

            if (track is IStepTrack stepTrack) {
                DrawStepClips(index, track, stepTrack, rowRect.width - HEADER_WIDTH);
                GUI.EndClip();
                return;
            }

            for (var c = 0; c < track.ClipCount; c++) {
                var clipRect = ClipRect(track, c, 0);
                clipRect.x -= HEADER_WIDTH;

                EditorGUI.DrawRect(clipRect, IsClipSelected(index, c) ? s_clipSelectedColor : clipColor);
                EditorGUI.DrawRect(new Rect(clipRect.xMax - 2, clipRect.y, 2, clipRect.height), new Color(1, 1, 1, 0.35f));
                EditorGUIUtility.AddCursorRect(new Rect(clipRect.xMax - EDGE_GRAB, clipRect.y, EDGE_GRAB, clipRect.height), MouseCursor.ResizeHorizontal);

                if (CanGrabStart(clipRect)) {
                    EditorGUI.DrawRect(new Rect(clipRect.x, clipRect.y, 2, clipRect.height), new Color(1, 1, 1, 0.2f));
                    EditorGUIUtility.AddCursorRect(new Rect(clipRect.x, clipRect.y, EDGE_GRAB, clipRect.height), MouseCursor.ResizeHorizontal);
                }

                var clip = clips.GetArrayElementAtIndex(c);
                var clipLabelRect = new Rect(clipRect.x + 3, clipRect.y, Mathf.Max(0, clipRect.width - 6), clipRect.height);
                var fromModeProperty = clip.FindPropertyRelative("FromMode");

                // Clips that aren't from/to tweens label with their hop count, slide mode or nothing
                if (fromModeProperty == null) {
                    GUI.Label(clipLabelRect, CustomClipLabel(clip), EditorStyles.whiteMiniLabel);
                    continue;
                }

                var fromMode = (ClipFrom) fromModeProperty.intValue;
                var ease = clip.FindPropertyRelative("Ease");
                var easeLabel = TweenEaseDrawer.Label(ease);
                var prefix = fromMode == ClipFrom.Previous ? "↳ " : fromMode == ClipFrom.Current ? "● " : string.Empty;

                GUI.Label(clipLabelRect, prefix + easeLabel, EditorStyles.whiteMiniLabel);
            }

            GUI.EndClip();
        }

        private static string CustomClipLabel(SerializedProperty clip)
        {
            var hops = clip.FindPropertyRelative("Hops");

            if (hops != null) {
                return $"{hops.intValue} hops";
            }

            var slideMode = clip.FindPropertyRelative("Mode");
            var direction = clip.FindPropertyRelative("Direction");

            if (slideMode != null && direction != null) {
                var preposition = slideMode.intValue == (int) SlideMode.In ? "from" : "to";
                return $"{slideMode.enumDisplayNames[slideMode.enumValueIndex]} {preposition} {direction.enumDisplayNames[direction.enumValueIndex]}";
            }

            // A clip with only a mode, like a wipe's Cover or Uncover
            if (slideMode != null && slideMode.propertyType == SerializedPropertyType.Enum) {
                return slideMode.enumDisplayNames[slideMode.enumValueIndex];
            }

            return string.Empty;
        }

        /// <summary>
        /// Step clips hold their value until the next clip, so each is drawn as a segment running to the next
        /// switch (or the edge of the view) with a handle at its start.
        /// </summary>
        private void DrawStepClips(int trackIndex, TweenTrack track, IStepTrack stepTrack, float width)
        {
            var active = track.Enabled && track.TargetObject != null;

            for (var c = 0; c < track.ClipCount; c++) {
                track.GetClipRange(c, out var start, out _);

                var next = float.MaxValue;
                for (var other = 0; other < track.ClipCount; other++) {
                    track.GetClipRange(other, out var otherStart, out _);

                    if (otherStart > start && otherStart < next) {
                        next = otherStart;
                    }
                }

                var x = TimeToX(start) - HEADER_WIDTH;
                var xEnd = next < float.MaxValue ? TimeToX(next) - HEADER_WIDTH : width;
                var on = stepTrack.GetClipValue(c);
                var color = !active ? s_clipDisabledColor : on ? s_stepOnColor : s_stepOffColor;

                EditorGUI.DrawRect(new Rect(x, 7, Mathf.Max(0, xEnd - x), ROW_HEIGHT - 14), color);
                GUI.Label(new Rect(x + 6, 3, Mathf.Max(0, xEnd - x - 8), ROW_HEIGHT - 6), on ? "On" : "Off", EditorStyles.whiteMiniLabel);
            }

            for (var c = 0; c < track.ClipCount; c++) {
                var handle = ClipRect(track, c, 0);
                handle.x -= HEADER_WIDTH;

                EditorGUI.DrawRect(handle, IsClipSelected(trackIndex, c) ? Color.white : new Color(0.85f, 0.85f, 0.85f));
            }
        }

        private void DrawPlayhead(Rect rect)
        {
            if (!IsPreviewing && !Application.isPlaying) {
                return;
            }

            var x = TimeToX(PlayheadTime);

            if (x >= rect.x && x <= rect.xMax) {
                EditorGUI.DrawRect(new Rect(x, rect.y, 1, rect.height), s_playheadColor);
                EditorGUI.DrawRect(new Rect(x - 4, rect.y, 9, 6), s_playheadColor);
            }
        }

        /// <summary>
        /// Drags the divider above the details pane to resize it. The height is kept with the window.
        /// </summary>
        private void HandleDetailsSplitter(Rect rect, float maxHeight)
        {
            var id = GUIUtility.GetControlID(FocusType.Passive);
            var e = Event.current;

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeVertical, id);

            switch (e.GetTypeForControl(id)) {
                case EventType.MouseDown when e.button == 0 && rect.Contains(e.mousePosition):
                    GUIUtility.hotControl = id;
                    e.Use();
                    break;
                case EventType.MouseDrag when GUIUtility.hotControl == id:
                    m_detailsHeight = Mathf.Clamp(position.height - e.mousePosition.y, MIN_DETAILS_HEIGHT, maxHeight);
                    e.Use();
                    Repaint();
                    break;
                case EventType.MouseUp when GUIUtility.hotControl == id:
                    GUIUtility.hotControl = 0;
                    e.Use();
                    break;
            }
        }

        private void DrawDetails(Rect rect)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), new Color(0, 0, 0, 0.4f));

            GUILayout.BeginArea(new Rect(rect.x + 6, rect.y + 4, rect.width - 12, rect.height - 8));
            m_detailsScroll = EditorGUILayout.BeginScrollView(m_detailsScroll);

            if (TweenlineEditorUtils.HasMissingTypes(m_target)) {
                using (new EditorGUILayout.HorizontalScope()) {
                    EditorGUILayout.HelpBox($"Some tracks use classes that no longer exist [{TweenlineEditorUtils.MissingTypeNames(m_target)}]", MessageType.Warning);

                    if (GUILayout.Button("Remove All", GUILayout.Width(80), GUILayout.Height(38))) {
                        TweenlineEditorUtils.RemoveAllMissingTypes(m_target);
                        m_serializedObject.Update();
                        ClearSelection();
                        GUIUtility.ExitGUI();
                    }
                }
            }

            if (Animation == null) {
                EditorGUILayout.LabelField("Add an animation to start", EditorStyles.centeredGreyMiniLabel);
            } else {
                switch (m_selected) {
                    case Selected.Clip when m_selectedClips.Count > 1:
                        DrawMultiClipDetails();
                        break;
                    case Selected.Clip:
                        DrawClipDetails();
                        break;
                    case Selected.Track:
                        DrawTrackDetails();
                        break;
                    case Selected.Marker:
                        DrawMarkerDetails();
                        break;
                    default:
                        DrawAnimationDetails();
                        break;
                }
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawAnimationDetails()
        {
            EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);

            var animation = AnimationProperty();
            EditorGUILayout.PropertyField(animation.FindPropertyRelative("m_name"));
            EditorGUILayout.PropertyField(animation.FindPropertyRelative("m_wrap"));

            if (GUILayout.Button("Add Marker at Playhead")) {
                AddMarker(PlayheadTime);
            }

            EditorGUILayout.HelpBox("Double click a track row to add a clip. Drag clips to move them and either edge to resize. "
                + "Shift click or drag a box to select several clips, and drag the handles beside them to stretch them together.Drag a track's name to reorder it. Right click the ruler to add a marker. "
                + "Scroll to zoom, middle drag to pan, hold Ctrl to drag without snapping.\n"
                + "Keys: Delete, Ctrl+D duplicate, Ctrl+A select all, Space play, F frame, Home and End jump, arrows nudge, Esc deselect.", MessageType.None);
        }

        private void DrawMultiClipDetails()
        {
            EditorGUILayout.LabelField($"{m_selectedClips.Count} clips selected", EditorStyles.boldLabel);
            GetSelectionRange(out var start, out var end);
            EditorGUILayout.LabelField("Range", $"{start:0.###}s to {end:0.###}s");
            DrawClipButtons();
        }

        private void DrawTrackDetails()
        {
            var track = Animation.Tracks[m_selectedTrack];
            var property = TrackProperty(m_selectedTrack);

            EditorGUILayout.LabelField(TweenlineEditorUtils.TrackName(track.GetType()), EditorStyles.boldLabel);
            DrawWarning(track);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("m_target"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("m_enabled"));

            // Any other track settings, like a bounce's direction
            var end = property.GetEndProperty();
            var child = property.Copy();

            for (var enter = true; child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end); enter = false) {
                if (child.name != "m_target" && child.name != "m_enabled" && child.name != "m_clips") {
                    EditorGUILayout.PropertyField(child, true);
                }
            }

            using (new EditorGUILayout.HorizontalScope()) {
                if (GUILayout.Button("Add Clip at Playhead")) {
                    AddClip(m_selectedTrack, PlayheadTime);
                }

                if (GUILayout.Button("Remove Track")) {
                    RemoveTrack(m_selectedTrack);
                    GUIUtility.ExitGUI();
                }
            }
        }

        private void DrawClipDetails()
        {
            var track = Animation.Tracks[m_selectedTrack];
            var clip = TrackProperty(m_selectedTrack).FindPropertyRelative("m_clips").GetArrayElementAtIndex(m_selectedClip);

            EditorGUILayout.LabelField($"{TweenlineEditorUtils.TrackName(track.GetType())} clip {m_selectedClip + 1}", EditorStyles.boldLabel);
            DrawWarning(track);

            if (track is IStepTrack) {
                EditorGUILayout.PropertyField(clip.FindPropertyRelative("Start"));
                EditorGUILayout.PropertyField(clip.FindPropertyRelative("Value"));
                DrawClipButtons();
                return;
            }

            var fromMode = clip.FindPropertyRelative("FromMode");

            // Clips that aren't from/to tweens, like bounces, show all their fields
            if (fromMode == null) {
                var end = clip.GetEndProperty();
                var child = clip.Copy();

                for (var enter = true; child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end); enter = false) {
                    EditorGUILayout.PropertyField(child, true);
                }

                DrawClipButtons();
                return;
            }

            using (new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.PropertyField(clip.FindPropertyRelative("Start"));
                EditorGUILayout.PropertyField(clip.FindPropertyRelative("Duration"));
            }

            EditorGUILayout.PropertyField(clip.FindPropertyRelative("Ease"));
            EditorGUILayout.PropertyField(fromMode, new GUIContent("From"));

            if ((ClipFrom) fromMode.intValue == ClipFrom.Value) {
                EditorGUILayout.PropertyField(clip.FindPropertyRelative("From"), new GUIContent("From Value"));
            }

            EditorGUILayout.PropertyField(clip.FindPropertyRelative("To"));
            EditorGUILayout.PropertyField(clip.FindPropertyRelative("Space"));
            DrawClipButtons();
        }

        private void DrawWarning(TweenTrack track)
        {
            var warning = TrackWarning(track);

            if (warning != null) {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }
        }

        private void DrawClipButtons()
        {
            using (new EditorGUILayout.HorizontalScope()) {
                if (GUILayout.Button("Duplicate")) {
                    DuplicateSelectedClips();
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button("Delete")) {
                    DeleteSelectedClips();
                    GUIUtility.ExitGUI();
                }
            }
        }

        private void DrawMarkerDetails()
        {
            var marker = AnimationProperty().FindPropertyRelative("m_markers").GetArrayElementAtIndex(m_selectedMarker);

            EditorGUILayout.LabelField("Marker", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(marker.FindPropertyRelative("Time"));
            EditorGUILayout.PropertyField(marker.FindPropertyRelative("Name"));
            EditorGUILayout.PropertyField(marker.FindPropertyRelative("Fire"));
            EditorGUILayout.PropertyField(marker.FindPropertyRelative("Event"));

            if (GUILayout.Button("Delete Marker")) {
                var markers = AnimationProperty().FindPropertyRelative("m_markers");
                markers.DeleteArrayElementAtIndex(m_selectedMarker);
                ClearSelection();
            }
        }

        private void ShowAddTrackMenu(GameObject target)
        {
            m_collapsed.Remove(target);

            TweenlineEditorUtils.ShowAddTrackMenu(m_target, Animation, target, index => {
                m_serializedObject.Update();
                SelectTrack(index);
            });
        }

        /// <summary>
        /// Dropping a scene object from the Hierarchy opens the Add Track menu for it. Dragging leaves the
        /// Hierarchy selection alone, so this works for children that have their own Tweenline.
        /// </summary>
        private void HandleDragAndDrop(Rect rect)
        {
            var e = Event.current;

            if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform || !rect.Contains(e.mousePosition)) {
                return;
            }

            var target = DraggedSceneObject();

            if (target == null) {
                return;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Link;

            if (e.type == EventType.DragPerform) {
                DragAndDrop.AcceptDrag();
                ShowAddTrackMenu(target);
            }

            e.Use();
        }

        private static GameObject DraggedSceneObject()
        {
            foreach (var dragged in DragAndDrop.objectReferences) {
                var gameObject = dragged switch {
                    GameObject go => go,
                    Component component => component.gameObject,
                    _ => null
                };

                if (gameObject != null && !EditorUtility.IsPersistent(gameObject)) {
                    return gameObject;
                }
            }

            return null;
        }

        private void HandleInput(Rect rulerRect, Rect timelineRect, Rect headerRect)
        {
            var e = Event.current;
            var controlId = GUIUtility.GetControlID(FocusType.Passive);
            var mouseTime = XToTime(e.mousePosition.x);
            var overRuler = rulerRect.Contains(e.mousePosition);
            var overTimeline = timelineRect.Contains(e.mousePosition);

            switch (e.GetTypeForControl(controlId)) {
                case EventType.MouseDown:
                    if (headerRect.Contains(e.mousePosition)) {
                        HandleHeaderMouseDown(e, controlId, timelineRect);
                        return;
                    }

                    if (!overRuler && !overTimeline) {
                        return;
                    }

                    if (e.button == 2 || e.button == 0 && e.alt) {
                        BeginDrag(controlId, DragMode.Pan, e.mousePosition.x);
                        m_panStartOffset = m_offset;
                        e.Use();
                        return;
                    }

                    if (overRuler) {
                        HandleRulerMouseDown(e, controlId, mouseTime);
                    } else {
                        HandleRowsMouseDown(e, controlId, mouseTime, timelineRect);
                    }

                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId) {
                        if (!m_dragMoved && (e.mousePosition - m_mouseDownPosition).sqrMagnitude >= DRAG_THRESHOLD * DRAG_THRESHOLD) {
                            m_dragMoved = true;
                        }

                        HandleDrag(e, mouseTime);
                        e.Use();
                    }

                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId) {
                        GUIUtility.hotControl = 0;
                        EndDrag(e);
                        e.Use();
                    }

                    break;
                case EventType.ScrollWheel:
                    if (overRuler || overTimeline) {
                        var anchor = mouseTime;
                        var zoom = e.delta.y > 0 ? 0.9f : 1.1f;
                        m_pixelsPerSecond = Mathf.Clamp(m_pixelsPerSecond * zoom, 20, 4000);
                        m_offset = anchor - (e.mousePosition.x - HEADER_WIDTH) / m_pixelsPerSecond;
                        e.Use();
                        Repaint();
                    }

                    break;
            }
        }

        private void EndDrag(Event e)
        {
            switch (m_dragMode) {
                case DragMode.MoveClip:
                    // A click on a clip that was already part of a group selects only that clip
                    if (!m_dragMoved && m_reduceSelectionOnClick) {
                        SelectClip(m_selectedTrack, m_selectedClip);
                    }

                    Undo.CollapseUndoOperations(m_undoGroup);
                    break;
                case DragMode.ResizeClip:
                case DragMode.ResizeClipStart:
                case DragMode.MoveMarker:
                case DragMode.ScaleSelection:
                    Undo.CollapseUndoOperations(m_undoGroup);
                    break;
                case DragMode.BoxSelect:
                    // A click without a drag keeps the old behaviour of selecting the row's track
                    if (!m_dragMoved && !e.shift) {
                        if (m_boxTrack >= 0) {
                            SelectTrack(m_boxTrack);
                        } else {
                            ClearSelection();
                        }
                    }

                    break;
                case DragMode.MoveTrack:
                    if (m_dragMoved && m_trackDropRow >= 0) {
                        MoveTrackTo(m_selectedTrack, m_rows[m_trackDropRow].Track, m_trackDropAfter);
                    }

                    break;
            }

            m_dragMode = DragMode.None;
            m_reduceSelectionOnClick = false;
            m_snapGuide = float.NaN;
            m_trackDropRow = -1;
            Repaint();
        }

        /// <summary>
        /// Clicking a track's name selects it and dragging it reorders it among the tracks of the same object.
        /// The toggle and menu button in the header are left to their own controls.
        /// </summary>
        private void HandleHeaderMouseDown(Event e, int controlId, Rect timelineRect)
        {
            var rowIndex = RowAt(e.mousePosition.y, timelineRect);

            if (rowIndex < 0 || m_rows[rowIndex].IsHeader || Animation.Tracks[m_rows[rowIndex].Track] == null) {
                return;
            }

            var x = e.mousePosition.x;

            if (x < TRACK_INDENT + 18 || x > HEADER_WIDTH - 24) {
                return;
            }

            var trackIndex = m_rows[rowIndex].Track;
            SelectTrack(trackIndex);

            if (e.button == 1) {
                ShowTrackMenu(trackIndex);
            } else if (e.button == 0) {
                BeginDrag(controlId, DragMode.MoveTrack, 0);
            }

            e.Use();
        }

        private void HandleRulerMouseDown(Event e, int controlId, float mouseTime)
        {
            var markers = Animation.Markers;

            for (var i = markers.Count - 1; i >= 0; i--) {
                if (Mathf.Abs(TimeToX(markers[i].Time) - e.mousePosition.x) <= 5) {
                    SelectMarker(i);

                    if (e.button == 1) {
                        var index = i;
                        var menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Delete Marker"), false, () => DeleteMarker(index));
                        menu.ShowAsContext();
                    } else {
                        BeginDrag(controlId, DragMode.MoveMarker, markers[i].Time - mouseTime);
                        CollectSnapTimes(false, i);
                    }

                    e.Use();
                    return;
                }
            }

            if (e.button == 1) {
                var time = Snap(mouseTime, e);
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Add Marker"), false, () => AddMarker(time));
                menu.ShowAsContext();
                e.Use();
                return;
            }

            BeginDrag(controlId, DragMode.Scrub, 0);
            ScrubTo(mouseTime);
            e.Use();
        }

        private void HandleRowsMouseDown(Event e, int controlId, float mouseTime, Rect timelineRect)
        {
            // The group handles sit just outside the selection, over empty space or other clips, so they go first
            if (e.button == 0 && TryBeginScaleSelection(e, controlId, timelineRect)) {
                e.Use();
                return;
            }

            var rowIndex = RowAt(e.mousePosition.y, timelineRect);
            var tracks = Animation.Tracks;

            if (rowIndex < 0 || m_rows[rowIndex].IsHeader || tracks[m_rows[rowIndex].Track] == null) {
                if (e.button == 0) {
                    BeginBoxSelect(controlId, e, -1);
                    e.Use();
                }

                return;
            }

            var row = m_rows[rowIndex].Track;
            var track = tracks[row];
            var rowY = timelineRect.y + rowIndex * ROW_HEIGHT - m_rowScroll;

            for (var c = track.ClipCount - 1; c >= 0; c--) {
                var clipRect = ClipRect(track, c, rowY);

                if (!clipRect.Contains(e.mousePosition)) {
                    continue;
                }

                HandleClipMouseDown(e, controlId, mouseTime, row, c, clipRect);
                e.Use();
                return;
            }

            var time = Snap(Mathf.Max(0, mouseTime), e);

            if (e.button == 1) {
                var trackIndex = row;
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Add Clip"), false, () => AddClip(trackIndex, time));
                menu.ShowAsContext();
            } else if (e.button == 0 && e.clickCount == 2) {
                AddClip(row, time);
            } else if (e.button == 0) {
                BeginBoxSelect(controlId, e, row);
            }

            e.Use();
        }

        private void HandleClipMouseDown(Event e, int controlId, float mouseTime, int track, int clip, Rect clipRect)
        {
            var alreadySelected = IsClipSelected(track, clip);

            if (e.button == 1) {
                if (!alreadySelected) {
                    SelectClip(track, clip);
                }

                ShowClipMenu();
                return;
            }

            if (e.button != 0) {
                return;
            }

            if (e.shift) {
                ToggleClip(track, clip);
                return;
            }

            var isStep = Animation.Tracks[track] is IStepTrack;
            var x = e.mousePosition.x;

            if (!isStep && x >= clipRect.xMax - EDGE_GRAB) {
                SelectClip(track, clip);
                BeginDrag(controlId, DragMode.ResizeClip, 0);
                CollectSnapTimes(true, -1);
                return;
            }

            if (!isStep && x <= clipRect.x + EDGE_GRAB && CanGrabStart(clipRect)) {
                SelectClip(track, clip);
                Animation.Tracks[track].GetClipRange(clip, out var start, out var duration);
                m_dragEnd = start + duration;
                BeginDrag(controlId, DragMode.ResizeClipStart, 0);
                CollectSnapTimes(true, -1);
                return;
            }

            // Grabbing a clip of a group keeps the group so it moves together, and a click without a drag
            // narrows the selection down to the clip
            if (alreadySelected) {
                SetPrimaryClip(track, clip);
                m_reduceSelectionOnClick = m_selectedClips.Count > 1;
            } else {
                SelectClip(track, clip);
            }

            m_dragStarts.Clear();

            foreach (var selected in m_selectedClips) {
                Animation.Tracks[selected.Track].GetClipRange(selected.Clip, out var selectedStart, out _);
                m_dragStarts.Add(selectedStart);
            }

            Animation.Tracks[track].GetClipRange(clip, out var grabbedStart, out _);
            BeginDrag(controlId, DragMode.MoveClip, grabbedStart - mouseTime);
            CollectSnapTimes(true, -1);
        }

        private void BeginBoxSelect(int controlId, Event e, int track)
        {
            m_boxTrack = track;
            m_boxBase.Clear();

            if (e.shift && m_selected == Selected.Clip) {
                m_boxBase.AddRange(m_selectedClips);
            }

            BeginDrag(controlId, DragMode.BoxSelect, 0);
        }

        private void HandleDrag(Event e, float mouseTime)
        {
            switch (m_dragMode) {
                case DragMode.Scrub:
                    ScrubTo(mouseTime);
                    break;
                case DragMode.Pan:
                    m_offset = m_panStartOffset - (e.mousePosition.x - m_dragOffset) / m_pixelsPerSecond;
                    break;
                case DragMode.MoveClip when m_dragMoved:
                    DragSelectedClips(e, mouseTime);
                    break;
                case DragMode.ResizeClip: {
                    var clip = ClipProperty(m_selectedTrack, m_selectedClip);
                    var start = clip.FindPropertyRelative("Start").floatValue;
                    clip.FindPropertyRelative("Duration").floatValue = Mathf.Max(0, SnapTime(mouseTime, e) - start);
                    ApplyChanges();
                    break;
                }
                case DragMode.ScaleSelection:
                    ScaleSelectedClips(e, mouseTime);
                    break;
                case DragMode.ResizeClipStart: {
                    var clip = ClipProperty(m_selectedTrack, m_selectedClip);
                    var start = Mathf.Clamp(SnapTime(mouseTime, e), 0, m_dragEnd);
                    clip.FindPropertyRelative("Start").floatValue = start;
                    clip.FindPropertyRelative("Duration").floatValue = m_dragEnd - start;
                    ApplyChanges();
                    break;
                }
                case DragMode.MoveMarker: {
                    var marker = AnimationProperty().FindPropertyRelative("m_markers").GetArrayElementAtIndex(m_selectedMarker);
                    marker.FindPropertyRelative("Time").floatValue = Mathf.Max(0, SnapTime(mouseTime + m_dragOffset, e));
                    ApplyChanges();
                    break;
                }
                case DragMode.BoxSelect when m_dragMoved:
                    UpdateBoxSelection(e.mousePosition);
                    break;
                case DragMode.MoveTrack when m_dragMoved:
                    UpdateTrackDrop(e.mousePosition.y);
                    break;
            }

            Repaint();
        }

        /// <summary>
        /// Moves every selected clip by how far the grabbed clip moved. The grabbed clip snaps by either edge,
        /// and the group stops at zero.
        /// </summary>
        private void DragSelectedClips(Event e, float mouseTime)
        {
            var grabbed = m_selectedClips.IndexOf(new ClipRef(m_selectedTrack, m_selectedClip));
            Animation.Tracks[m_selectedTrack].GetClipRange(m_selectedClip, out _, out var duration);

            var grabbedStart = m_dragStarts[grabbed];
            var delta = SnapClipStart(mouseTime + m_dragOffset, duration, e) - grabbedStart;
            var earliest = float.MaxValue;

            foreach (var start in m_dragStarts) {
                earliest = Mathf.Min(earliest, start);
            }

            delta = Mathf.Max(delta, -earliest);

            for (var i = 0; i < m_selectedClips.Count; i++) {
                var selected = m_selectedClips[i];
                ClipProperty(selected.Track, selected.Clip).FindPropertyRelative("Start").floatValue = m_dragStarts[i] + delta;
            }

            ApplyChanges();
        }

        /// <summary>
        /// The frame around a group of selected clips: their time range across the visible rows that hold them.
        /// Only shown for more than one clip over a range longer than zero.
        /// </summary>
        private bool TryGetSelectionFrame(Rect timelineRect, out Rect frame)
        {
            frame = default;

            if (m_selected != Selected.Clip || m_selectedClips.Count < 2) {
                return false;
            }

            GetSelectionRange(out var start, out var end);

            if (end - start <= 0) {
                return false;
            }

            var firstRow = -1;
            var lastRow = -1;

            for (var i = 0; i < m_rows.Count; i++) {
                var row = m_rows[i];

                if (row.IsHeader || !HasSelectedClip(row.Track)) {
                    continue;
                }

                if (firstRow < 0) {
                    firstRow = i;
                }

                lastRow = i;
            }

            if (firstRow < 0) {
                return false;
            }

            var top = timelineRect.y + firstRow * ROW_HEIGHT - m_rowScroll + 1;
            var bottom = timelineRect.y + (lastRow + 1) * ROW_HEIGHT - m_rowScroll - 1;
            frame = Rect.MinMaxRect(TimeToX(start), top, TimeToX(end), bottom);
            return true;
        }

        private bool HasSelectedClip(int trackIndex)
        {
            foreach (var selected in m_selectedClips) {
                if (selected.Track == trackIndex) {
                    return true;
                }
            }

            return false;
        }

        private static Rect LeftScaleHandle(Rect frame)
        {
            return new Rect(frame.x - EDGE_GRAB - 2, frame.y, EDGE_GRAB, frame.height);
        }

        private static Rect RightScaleHandle(Rect frame)
        {
            return new Rect(frame.xMax + 2, frame.y, EDGE_GRAB, frame.height);
        }

        private void DrawSelectionFrame(Rect timelineRect)
        {
            if (!TryGetSelectionFrame(timelineRect, out var frame)) {
                return;
            }

            GUI.BeginClip(timelineRect);
            frame.position -= timelineRect.position;

            DrawOutline(frame, s_boxEdgeColor);

            var left = LeftScaleHandle(frame);
            var right = RightScaleHandle(frame);

            EditorGUI.DrawRect(left, s_scaleHandleColor);
            EditorGUI.DrawRect(right, s_scaleHandleColor);
            EditorGUIUtility.AddCursorRect(left, MouseCursor.ResizeHorizontal);
            EditorGUIUtility.AddCursorRect(right, MouseCursor.ResizeHorizontal);

            GUI.EndClip();
        }

        private bool TryBeginScaleSelection(Event e, int controlId, Rect timelineRect)
        {
            if (!TryGetSelectionFrame(timelineRect, out var frame)) {
                return false;
            }

            var fromRight = RightScaleHandle(frame).Contains(e.mousePosition);

            if (!fromRight && !LeftScaleHandle(frame).Contains(e.mousePosition)) {
                return false;
            }

            GetSelectionRange(out var start, out var end);
            m_scalePivot = fromRight ? start : end;
            m_scaleEdge = fromRight ? end : start;

            m_dragStarts.Clear();
            m_dragDurations.Clear();

            foreach (var selected in m_selectedClips) {
                Animation.Tracks[selected.Track].GetClipRange(selected.Clip, out var clipStart, out var duration);
                m_dragStarts.Add(clipStart);
                m_dragDurations.Add(duration);
            }

            BeginDrag(controlId, DragMode.ScaleSelection, 0);
            CollectSnapTimes(true, -1);
            return true;
        }

        /// <summary>
        /// Stretches the selected clips around the opposite edge of the group, scaling each clip's start and
        /// duration so their timing stays in proportion. Step clips have no duration and only move.
        /// </summary>
        private void ScaleSelectedClips(Event e, float mouseTime)
        {
            const float MIN_SPAN = 0.01f;

            var edge = SnapTime(mouseTime, e);
            edge = m_scaleEdge > m_scalePivot ? Mathf.Max(edge, m_scalePivot + MIN_SPAN) : Mathf.Clamp(edge, 0, m_scalePivot - MIN_SPAN);

            var scale = (edge - m_scalePivot) / (m_scaleEdge - m_scalePivot);

            for (var i = 0; i < m_selectedClips.Count; i++) {
                var selected = m_selectedClips[i];
                var clip = ClipProperty(selected.Track, selected.Clip);

                clip.FindPropertyRelative("Start").floatValue = m_scalePivot + (m_dragStarts[i] - m_scalePivot) * scale;

                var duration = clip.FindPropertyRelative("Duration");

                if (duration != null) {
                    duration.floatValue = m_dragDurations[i] * scale;
                }
            }

            ApplyChanges();
        }

        private Rect BoxRect(Vector2 mousePosition)
        {
            return Rect.MinMaxRect(
                Mathf.Min(m_mouseDownPosition.x, mousePosition.x), Mathf.Min(m_mouseDownPosition.y, mousePosition.y),
                Mathf.Max(m_mouseDownPosition.x, mousePosition.x), Mathf.Max(m_mouseDownPosition.y, mousePosition.y));
        }

        private void UpdateBoxSelection(Vector2 mousePosition)
        {
            var box = BoxRect(mousePosition);
            var tracks = Animation.Tracks;

            m_selectedClips.Clear();
            m_selectedClips.AddRange(m_boxBase);

            for (var i = 0; i < m_rows.Count; i++) {
                var row = m_rows[i];

                if (row.IsHeader || tracks[row.Track] == null) {
                    continue;
                }

                var track = tracks[row.Track];
                var rowY = m_timelineRect.y + i * ROW_HEIGHT - m_rowScroll;

                for (var c = 0; c < track.ClipCount; c++) {
                    var clip = new ClipRef(row.Track, c);

                    if (ClipRect(track, c, rowY).Overlaps(box) && !m_selectedClips.Contains(clip)) {
                        m_selectedClips.Add(clip);
                    }
                }
            }

            if (m_selectedClips.Count == 0) {
                m_selected = Selected.None;
                m_selectedTrack = -1;
                m_selectedClip = -1;
            } else {
                var primary = m_selectedClips[m_selectedClips.Count - 1];
                m_selected = Selected.Clip;
                m_selectedTrack = primary.Track;
                m_selectedClip = primary.Clip;
            }

            m_selectedMarker = -1;
        }

        /// <summary>
        /// Finds where a dragged track would land. Only rows of the same object take it, since tracks are shown
        /// grouped under their object and a move across objects would not show.
        /// </summary>
        private void UpdateTrackDrop(float mouseY)
        {
            m_trackDropRow = -1;

            var rowIndex = RowAt(mouseY, m_timelineRect);

            if (rowIndex < 0 || m_rows[rowIndex].IsHeader) {
                return;
            }

            var tracks = Animation.Tracks;
            var target = m_rows[rowIndex].Track;

            if (TargetGameObject(tracks[target]) != TargetGameObject(tracks[m_selectedTrack])) {
                return;
            }

            var rowTop = m_timelineRect.y + rowIndex * ROW_HEIGHT - m_rowScroll;
            m_trackDropRow = rowIndex;
            m_trackDropAfter = mouseY > rowTop + ROW_HEIGHT * 0.5f;
        }

        private int RowAt(float y, Rect timelineRect)
        {
            var rowIndex = Mathf.FloorToInt((y - timelineRect.y + m_rowScroll) / ROW_HEIGHT);
            return rowIndex >= 0 && rowIndex < m_rows.Count ? rowIndex : -1;
        }

        private static bool CanGrabStart(Rect clipRect)
        {
            // Narrow clips keep their whole body for moving and only resize from the right
            return clipRect.width >= EDGE_GRAB * 3;
        }

        private void BeginDrag(int controlId, DragMode mode, float offset)
        {
            GUIUtility.hotControl = controlId;
            m_dragMode = mode;
            m_dragOffset = offset;
            m_mouseDownPosition = Event.current.mousePosition;
            m_dragMoved = false;
            m_snapGuide = float.NaN;

            Undo.IncrementCurrentGroup();
            m_undoGroup = Undo.GetCurrentGroup();
        }

        /// <summary>
        /// Gathers the times a drag can snap to: zero, clip edges, markers and the playhead, leaving out the
        /// selected clips or the given marker that are being dragged.
        /// </summary>
        private void CollectSnapTimes(bool skipSelectedClips, int skipMarker)
        {
            m_snapTimes.Clear();
            m_snapTimes.Add(0);

            var animation = Animation;

            for (var t = 0; t < animation.Tracks.Count; t++) {
                var track = animation.Tracks[t];

                if (track == null) {
                    continue;
                }

                for (var c = 0; c < track.ClipCount; c++) {
                    if (skipSelectedClips && IsClipSelected(t, c)) {
                        continue;
                    }

                    track.GetClipRange(c, out var start, out var duration);
                    m_snapTimes.Add(start);

                    if (duration > 0) {
                        m_snapTimes.Add(start + duration);
                    }
                }
            }

            for (var i = 0; i < animation.Markers.Count; i++) {
                if (i != skipMarker) {
                    m_snapTimes.Add(animation.Markers[i].Time);
                }
            }

            if (IsPreviewing || Application.isPlaying) {
                m_snapTimes.Add(PlayheadTime);
            }
        }

        private bool SnapActive(Event e)
        {
            return m_snapEnabled && !e.control;
        }

        /// <summary>
        /// Grid snapping for clicks, like adding a clip or marker.
        /// </summary>
        private float Snap(float time, Event e)
        {
            return SnapActive(e) ? GridSnap(time) : time;
        }

        private float GridSnap(float time)
        {
            return m_snap > 0 ? Mathf.Round(time / m_snap) * m_snap : time;
        }

        /// <summary>
        /// Snaps a dragged time to a nearby edge, marker or the playhead, else to the grid.
        /// </summary>
        private float SnapTime(float time, Event e)
        {
            m_snapGuide = float.NaN;

            if (!SnapActive(e)) {
                return time;
            }

            if (TrySnapToEdge(time, out var snapped, out _)) {
                m_snapGuide = snapped;
                return snapped;
            }

            return GridSnap(time);
        }

        /// <summary>
        /// Snaps a moved clip by whichever of its edges is closer to something, else its start to the grid.
        /// </summary>
        private float SnapClipStart(float start, float duration, Event e)
        {
            m_snapGuide = float.NaN;

            if (!SnapActive(e)) {
                return start;
            }

            var startHit = TrySnapToEdge(start, out var snappedStart, out var startDistance);
            var endDistance = float.MaxValue;
            var snappedEnd = 0f;
            var endHit = duration > 0 && TrySnapToEdge(start + duration, out snappedEnd, out endDistance);

            if (startHit && (!endHit || startDistance <= endDistance)) {
                m_snapGuide = snappedStart;
                return snappedStart;
            }

            if (endHit) {
                m_snapGuide = snappedEnd;
                return snappedEnd - duration;
            }

            return GridSnap(start);
        }

        private bool TrySnapToEdge(float time, out float snapped, out float distance)
        {
            snapped = time;
            distance = EDGE_SNAP_PIXELS / m_pixelsPerSecond;

            var found = false;

            foreach (var candidate in m_snapTimes) {
                var candidateDistance = Mathf.Abs(candidate - time);

                if (candidateDistance <= distance) {
                    distance = candidateDistance;
                    snapped = candidate;
                    found = true;
                }
            }

            return found;
        }

        /// <summary>
        /// Delete, duplicate, select all and frame come through Unity's commands so the Edit menu and its
        /// shortcuts work. The other keys have no command and are read directly.
        /// </summary>
        private void HandleKeyboard()
        {
            var e = Event.current;

            // A text field being edited keeps its own select all, delete and arrow keys
            if (Animation == null || EditorGUIUtility.editingTextField) {
                return;
            }

            switch (e.type) {
                case EventType.ValidateCommand:
                    if (CanRunCommand(e.commandName)) {
                        e.Use();
                    }

                    break;
                case EventType.ExecuteCommand:
                    if (CanRunCommand(e.commandName)) {
                        RunCommand(e.commandName);
                        e.Use();
                        GUIUtility.ExitGUI();
                    }

                    break;
                case EventType.KeyDown:
                    if (GUIUtility.hotControl != 0 || e.control || e.command || e.alt) {
                        return;
                    }

                    if (HandleKey(e.keyCode)) {
                        e.Use();
                        GUIUtility.ExitGUI();
                    }

                    break;
            }
        }

        private bool CanRunCommand(string command)
        {
            switch (command) {
                case "Delete":
                case "SoftDelete":
                    return m_selected != Selected.None;
                case "Duplicate":
                    return m_selected == Selected.Clip;
                case "SelectAll":
                case "FrameSelected":
                    return true;
                default:
                    return false;
            }
        }

        private void RunCommand(string command)
        {
            switch (command) {
                case "Delete":
                case "SoftDelete":
                    DeleteSelection();
                    break;
                case "Duplicate":
                    DuplicateSelectedClips();
                    break;
                case "SelectAll":
                    SelectAllClips();
                    break;
                case "FrameSelected":
                    FrameSelection();
                    break;
            }
        }

        private bool HandleKey(KeyCode key)
        {
            switch (key) {
                case KeyCode.Delete:
                case KeyCode.Backspace:
                    if (m_selected == Selected.None) {
                        return false;
                    }

                    DeleteSelection();
                    return true;
                case KeyCode.Space:
                    TogglePlay();
                    return true;
                case KeyCode.F:
                    FrameSelection();
                    return true;
                case KeyCode.Home:
                    ScrubTo(0);
                    return true;
                case KeyCode.End:
                    ScrubTo(Animation.Duration);
                    return true;
                case KeyCode.LeftArrow:
                    Nudge(-1);
                    return true;
                case KeyCode.RightArrow:
                    Nudge(1);
                    return true;
                case KeyCode.Escape:
                    if (m_selected == Selected.None) {
                        return false;
                    }

                    ClearSelection();
                    Repaint();
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Moves the selected clips or marker by one snap step, or steps the playhead when nothing is selected.
        /// </summary>
        private void Nudge(int direction)
        {
            var step = direction * (m_snap > 0 ? m_snap : 0.01f);

            switch (m_selected) {
                case Selected.Clip: {
                    GetSelectionRange(out var start, out _);
                    step = Mathf.Max(step, -start);

                    foreach (var selected in m_selectedClips) {
                        ClipProperty(selected.Track, selected.Clip).FindPropertyRelative("Start").floatValue += step;
                    }

                    ApplyChanges();
                    break;
                }
                case Selected.Marker: {
                    var time = AnimationProperty().FindPropertyRelative("m_markers").GetArrayElementAtIndex(m_selectedMarker).FindPropertyRelative("Time");
                    time.floatValue = Mathf.Max(0, time.floatValue + step);
                    ApplyChanges();
                    break;
                }
                default:
                    ScrubTo(PlayheadTime + step);
                    break;
            }

            Repaint();
        }

        private void FrameAll()
        {
            FrameRange(0, Animation.Duration);
        }

        private void FrameSelection()
        {
            switch (m_selected) {
                case Selected.Clip: {
                    GetSelectionRange(out var start, out var end);
                    FrameRange(start, end);
                    break;
                }
                case Selected.Marker: {
                    var time = Animation.Markers[m_selectedMarker].Time;
                    FrameRange(time - 0.5f, time + 0.5f);
                    break;
                }
                default:
                    FrameAll();
                    break;
            }
        }

        private void FrameRange(float start, float end)
        {
            var width = Mathf.Max(50, position.width - HEADER_WIDTH - SCROLLBAR_WIDTH);
            var span = Mathf.Max(end - start, 0.1f);

            m_pixelsPerSecond = Mathf.Clamp(width * 0.9f / span, 20, 4000);
            m_offset = start - (width / m_pixelsPerSecond - span) * 0.5f;
            Repaint();
        }

        private void GetSelectionRange(out float start, out float end)
        {
            start = float.MaxValue;
            end = float.MinValue;

            foreach (var selected in m_selectedClips) {
                Animation.Tracks[selected.Track].GetClipRange(selected.Clip, out var clipStart, out var duration);
                start = Mathf.Min(start, clipStart);
                end = Mathf.Max(end, clipStart + duration);
            }

            if (m_selectedClips.Count == 0) {
                start = end = 0;
            }
        }

        private void ScrubTo(float time)
        {
            var animation = Animation;
            time = Mathf.Clamp(time, 0, animation.Duration);

            if (Application.isPlaying) {
                m_target.Stop();
                m_target.Prepare(animation, false);
                m_target.Evaluate(animation, time);
                m_runtimeScrubTime = time;
            } else {
                EnsurePreview();
                TweenlinePreview.Pause();
                TweenlinePreview.SetTime(time);
            }

            Repaint();
        }

        private void EnsurePreview()
        {
            if (!IsPreviewing) {
                TweenlinePreview.Start(m_target, m_animationIndex);
            } else if (TweenlinePreview.AnimationIndex != m_animationIndex) {
                TweenlinePreview.SetAnimation(m_animationIndex);
            }
        }

        private void SelectAnimation(int index)
        {
            m_animationIndex = index;
            ClearSelection();

            if (IsPreviewing) {
                TweenlinePreview.SetAnimation(index);
            }
        }

        private void AddAnimation()
        {
            var index = m_target.Animations.Count;
            var name = index == 0 ? "Show" : $"Animation {index + 1}";

            TweenlineEditorUtils.Modify(m_target, "Add Animation", () => m_target.Animations.Add(new TweenAnimation(name)));
            m_serializedObject.Update();
            SelectAnimation(index);
        }

        private void ShowAnimationMenu()
        {
            var menu = new GenericMenu();
            var animations = m_target.Animations;

            for (var i = 0; i < animations.Count; i++) {
                var index = i;
                menu.AddItem(new GUIContent(animations[i].Name), i == m_animationIndex, () => SelectAnimation(index));
            }

            if (animations.Count > 0) {
                menu.AddSeparator(string.Empty);
            }

            menu.AddItem(new GUIContent("New Animation"), false, AddAnimation);

            if (Animation != null) {
                var index = m_animationIndex;
                menu.AddItem(new GUIContent($"Delete {Animation.Name}"), false, () => DeleteAnimation(index));
            }

            menu.ShowAsContext();
        }

        private void DeleteAnimation(int index)
        {
            TweenlineEditorUtils.DeleteAnimation(m_target, index);
            m_serializedObject.Update();
            SelectAnimation(index < m_animationIndex || index == m_target.Animations.Count ? Mathf.Max(0, m_animationIndex - 1) : m_animationIndex);
            Repaint();
        }

        private void AddClip(int trackIndex, float time)
        {
            var clipIndex = -1;
            TweenlineEditorUtils.Modify(m_target, "Add Clip", () => clipIndex = Animation.Tracks[trackIndex].AddClip(time));
            m_serializedObject.Update();
            SelectClip(trackIndex, clipIndex);
        }

        /// <summary>
        /// Copies the selected clips and places the copies right after the selection as one block, then
        /// selects the copies. Copies go to the end of each clip list so the other clip indices stay put.
        /// </summary>
        private void DuplicateSelectedClips()
        {
            GetSelectionRange(out var start, out var end);
            var offset = end - start > 0 ? end - start : Mathf.Max(m_snap, 0.1f);
            var copies = new List<ClipRef>(m_selectedClips.Count);

            foreach (var selected in m_selectedClips) {
                var clips = TrackProperty(selected.Track).FindPropertyRelative("m_clips");
                clips.InsertArrayElementAtIndex(selected.Clip);
                clips.MoveArrayElement(selected.Clip + 1, clips.arraySize - 1);

                var copyIndex = clips.arraySize - 1;
                clips.GetArrayElementAtIndex(copyIndex).FindPropertyRelative("Start").floatValue += offset;
                copies.Add(new ClipRef(selected.Track, copyIndex));
            }

            ApplyChanges();

            m_selectedClips.Clear();
            m_selectedClips.AddRange(copies);
            SetPrimaryClip(copies[copies.Count - 1].Track, copies[copies.Count - 1].Clip);
            Repaint();
        }

        private void DeleteSelectedClips()
        {
            // Highest index first per track so the earlier indices stay valid
            m_selectedClips.Sort((a, b) => a.Track != b.Track ? a.Track.CompareTo(b.Track) : b.Clip.CompareTo(a.Clip));

            foreach (var selected in m_selectedClips) {
                TrackProperty(selected.Track).FindPropertyRelative("m_clips").DeleteArrayElementAtIndex(selected.Clip);
            }

            ApplyChanges();
            ClearSelection();
            Repaint();
        }

        private void DeleteSelection()
        {
            switch (m_selected) {
                case Selected.Clip:
                    DeleteSelectedClips();
                    break;
                case Selected.Track:
                    RemoveTrack(m_selectedTrack);
                    break;
                case Selected.Marker:
                    DeleteMarker(m_selectedMarker);
                    break;
            }
        }

        private void DeleteMarker(int index)
        {
            TweenlineEditorUtils.Modify(m_target, "Delete Marker", () => Animation.Markers.RemoveAt(index));
            m_serializedObject.Update();
            ClearSelection();
            Repaint();
        }

        private void RemoveTrack(int trackIndex)
        {
            TweenlineEditorUtils.Modify(m_target, "Remove Track", () => Animation.Tracks.RemoveAt(trackIndex));
            m_serializedObject.Update();
            ClearSelection();
        }

        /// <summary>
        /// Moves a track to just before or after another one in the animation's track list.
        /// </summary>
        private void MoveTrackTo(int trackIndex, int targetIndex, bool after)
        {
            var insert = after ? targetIndex + 1 : targetIndex;

            if (insert == trackIndex || insert == trackIndex + 1) {
                return;
            }

            if (insert > trackIndex) {
                insert--;
            }

            var tracks = Animation.Tracks;

            TweenlineEditorUtils.Modify(m_target, "Move Track", () => {
                var track = tracks[trackIndex];
                tracks.RemoveAt(trackIndex);
                tracks.Insert(insert, track);
            });

            m_serializedObject.Update();
            SelectTrack(insert);
        }

        private void MoveTrack(int trackIndex, int direction)
        {
            var tracks = Animation.Tracks;
            var other = trackIndex + direction;

            if (other < 0 || other >= tracks.Count) {
                return;
            }

            TweenlineEditorUtils.Modify(m_target, "Move Track", () => (tracks[trackIndex], tracks[other]) = (tracks[other], tracks[trackIndex]));
            m_serializedObject.Update();
            SelectTrack(other);
        }

        private void AddMarker(float time)
        {
            var index = -1;

            TweenlineEditorUtils.Modify(m_target, "Add Marker", () => {
                Animation.Markers.Add(new TweenMarker { Time = time, Name = "Marker" });
                index = Animation.Markers.Count - 1;
            });

            m_serializedObject.Update();
            SelectMarker(index);
        }

        private void ShowTrackMenu(int trackIndex)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Add Clip at Playhead"), false, () => AddClip(trackIndex, PlayheadTime));
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Move Up"), false, () => MoveTrack(trackIndex, -1));
            menu.AddItem(new GUIContent("Move Down"), false, () => MoveTrack(trackIndex, 1));
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Remove Track"), false, () => RemoveTrack(trackIndex));
            menu.ShowAsContext();
        }

        private void ShowClipMenu()
        {
            var label = m_selectedClips.Count > 1 ? $" {m_selectedClips.Count} Clips" : string.Empty;
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent($"Duplicate{label}"), false, () => {
                m_serializedObject.Update();
                DuplicateSelectedClips();
            });
            menu.AddItem(new GUIContent($"Delete{label}"), false, () => {
                m_serializedObject.Update();
                DeleteSelectedClips();
            });
            menu.ShowAsContext();
        }

        private void SelectTrack(int index)
        {
            m_selected = index >= 0 ? Selected.Track : Selected.None;
            m_selectedTrack = index;
            m_selectedClip = -1;
            m_selectedMarker = -1;
            m_selectedClips.Clear();
            GUI.FocusControl(null);
            Repaint();
        }

        private void SelectClip(int trackIndex, int clipIndex)
        {
            m_selectedClips.Clear();

            if (clipIndex >= 0) {
                m_selectedClips.Add(new ClipRef(trackIndex, clipIndex));
            }

            m_selected = clipIndex >= 0 ? Selected.Clip : Selected.None;
            m_selectedTrack = trackIndex;
            m_selectedClip = clipIndex;
            m_selectedMarker = -1;
            GUI.FocusControl(null);
            Repaint();
        }

        /// <summary>
        /// Makes a clip that is already in the selection the one the details pane shows.
        /// </summary>
        private void SetPrimaryClip(int trackIndex, int clipIndex)
        {
            m_selected = Selected.Clip;
            m_selectedTrack = trackIndex;
            m_selectedClip = clipIndex;
            m_selectedMarker = -1;
            GUI.FocusControl(null);
        }

        private void ToggleClip(int trackIndex, int clipIndex)
        {
            if (m_selected != Selected.Clip) {
                SelectClip(trackIndex, clipIndex);
                return;
            }

            var clip = new ClipRef(trackIndex, clipIndex);

            if (m_selectedClips.Remove(clip)) {
                if (m_selectedClips.Count == 0) {
                    ClearSelection();
                } else {
                    var last = m_selectedClips[m_selectedClips.Count - 1];
                    SetPrimaryClip(last.Track, last.Clip);
                }
            } else {
                m_selectedClips.Add(clip);
                SetPrimaryClip(trackIndex, clipIndex);
            }

            Repaint();
        }

        private void SelectAllClips()
        {
            m_selectedClips.Clear();

            var tracks = Animation.Tracks;

            for (var t = 0; t < tracks.Count; t++) {
                if (tracks[t] == null) {
                    continue;
                }

                for (var c = 0; c < tracks[t].ClipCount; c++) {
                    m_selectedClips.Add(new ClipRef(t, c));
                }
            }

            if (m_selectedClips.Count == 0) {
                ClearSelection();
            } else {
                SetPrimaryClip(m_selectedClips[0].Track, m_selectedClips[0].Clip);
            }

            Repaint();
        }

        private bool IsClipSelected(int trackIndex, int clipIndex)
        {
            return m_selected == Selected.Clip && m_selectedClips.Contains(new ClipRef(trackIndex, clipIndex));
        }

        private void SelectMarker(int index)
        {
            m_selected = index >= 0 ? Selected.Marker : Selected.None;
            m_selectedTrack = -1;
            m_selectedClip = -1;
            m_selectedMarker = index;
            m_selectedClips.Clear();
            GUI.FocusControl(null);
            Repaint();
        }

        private void ClearSelection()
        {
            m_selected = Selected.None;
            m_selectedTrack = -1;
            m_selectedClip = -1;
            m_selectedMarker = -1;
            m_selectedClips.Clear();
            GUI.FocusControl(null);
        }

        /// <summary>
        /// Drops selected items that no longer exist, like after an undo removed a clip.
        /// </summary>
        private void ValidateSelection()
        {
            var animation = Animation;

            if (m_selected == Selected.Clip && animation != null) {
                m_selectedClips.RemoveAll(clip => !IsValidClip(animation, clip.Track, clip.Clip));
            }

            var valid = m_selected switch {
                Selected.Track => animation != null && IsValidTrack(animation, m_selectedTrack),
                Selected.Clip => animation != null && IsValidClip(animation, m_selectedTrack, m_selectedClip),
                Selected.Marker => animation != null && m_selectedMarker >= 0 && m_selectedMarker < animation.Markers.Count,
                _ => true
            };

            if (!valid) {
                ClearSelection();
            }
        }

        private static bool IsValidTrack(TweenAnimation animation, int trackIndex)
        {
            return trackIndex >= 0 && trackIndex < animation.Tracks.Count && animation.Tracks[trackIndex] != null;
        }

        private static bool IsValidClip(TweenAnimation animation, int trackIndex, int clipIndex)
        {
            return IsValidTrack(animation, trackIndex) && clipIndex >= 0 && clipIndex < animation.Tracks[trackIndex].ClipCount;
        }

        private SerializedProperty AnimationProperty()
        {
            return m_serializedObject.FindProperty("m_animations").GetArrayElementAtIndex(m_animationIndex);
        }

        private SerializedProperty TrackProperty(int trackIndex)
        {
            return AnimationProperty().FindPropertyRelative("m_tracks").GetArrayElementAtIndex(trackIndex);
        }

        private SerializedProperty ClipProperty(int trackIndex, int clipIndex)
        {
            return TrackProperty(trackIndex).FindPropertyRelative("m_clips").GetArrayElementAtIndex(clipIndex);
        }

        private Rect ClipRect(TweenTrack track, int clipIndex, float rowY)
        {
            track.GetClipRange(clipIndex, out var start, out var duration);

            if (track is IStepTrack) {
                return new Rect(TimeToX(start) - 3, rowY + 3, 7, ROW_HEIGHT - 6);
            }

            var x = TimeToX(start);
            var width = Mathf.Max(MIN_CLIP_WIDTH, duration * m_pixelsPerSecond);

            return new Rect(x, rowY + 3, width, ROW_HEIGHT - 6);
        }

        private float TimeToX(float time)
        {
            return HEADER_WIDTH + (time - m_offset) * m_pixelsPerSecond;
        }

        private float XToTime(float x)
        {
            return (x - HEADER_WIDTH) / m_pixelsPerSecond + m_offset;
        }

        private float TickStep()
        {
            foreach (var step in s_tickSteps) {
                if (step * m_pixelsPerSecond >= 12) {
                    return step;
                }
            }

            return 10f;
        }
    }
}
