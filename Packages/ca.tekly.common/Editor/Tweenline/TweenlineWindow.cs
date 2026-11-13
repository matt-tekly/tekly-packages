using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Tekly.Common.Tweenlines
{
    /// <summary>
    /// Timeline editor for the selected Tweenline. Scrubbing or pressing play in edit mode starts a preview.
    /// </summary>
    public class TweenlineWindow : EditorWindow
    {
        private const float HEADER_WIDTH = 220;
        private const float RULER_HEIGHT = 22;
        private const float ROW_HEIGHT = 24;
        private const float DETAILS_HEIGHT = 190;
        private const float SCROLLBAR_WIDTH = 13;
        private const float EDGE_GRAB = 6;
        private const float MIN_CLIP_WIDTH = 6;

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
            MoveMarker,
            Pan
        }

        [SerializeField] private Tweenline m_target;
        [SerializeField] private int m_animationIndex;
        [SerializeField] private float m_pixelsPerSecond = 200;
        [SerializeField] private float m_offset = -0.05f;
        [SerializeField] private float m_snap = 0.05f;

        private SerializedObject m_serializedObject;
        private Selected m_selected;
        private int m_selectedTrack = -1;
        private int m_selectedClip = -1;
        private int m_selectedMarker = -1;

        private DragMode m_dragMode;
        private float m_dragOffset;
        private float m_panStartOffset;
        private int m_undoGroup;
        private float m_rowScroll;
        private Vector2 m_detailsScroll;
        private float m_runtimeScrubTime;

        private static readonly Color s_clipColor = new Color(0.27f, 0.52f, 0.78f);
        private static readonly Color s_clipSelectedColor = new Color(0.45f, 0.7f, 0.95f);
        private static readonly Color s_clipDisabledColor = new Color(0.4f, 0.4f, 0.4f);
        private static readonly Color s_playheadColor = new Color(1f, 0.3f, 0.25f);
        private static readonly Color s_markerColor = new Color(1f, 0.8f, 0.2f);
        private static readonly Color s_rowColor = new Color(0, 0, 0, 0.12f);
        private static readonly Color s_rowSelectedColor = new Color(0.3f, 0.5f, 0.8f, 0.2f);
        private static readonly Color s_stepOnColor = new Color(0.3f, 0.65f, 0.35f);
        private static readonly Color s_stepOffColor = new Color(0.35f, 0.35f, 0.35f, 0.6f);
        private static readonly Color s_endColor = new Color(1, 1, 1, 0.25f);

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
            OnSelectionChanged();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            TweenlinePreview.Changed -= Repaint;
            Undo.undoRedoPerformed -= OnUndoRedo;

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

        private void OnSelectionChanged()
        {
            var gameObject = Selection.activeGameObject;
            var tweenline = gameObject != null ? gameObject.GetComponent<Tweenline>() : null;

            if (tweenline != null && tweenline != m_target) {
                SetTarget(tweenline);
            }
        }

        private void OnUndoRedo()
        {
            m_serializedObject?.Update();
            Repaint();
        }

        private void SetTarget(Tweenline tweenline)
        {
            if (tweenline == m_target) {
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
                DrawNoTarget();
                return;
            }

            if (m_serializedObject == null || m_serializedObject.targetObject != m_target) {
                m_serializedObject = new SerializedObject(m_target);
            }

            m_serializedObject.Update();
            ValidateSelection();

            EditorGUI.BeginChangeCheck();

            DrawToolbar();

            var top = EditorStyles.toolbar.fixedHeight;
            var body = new Rect(0, top, position.width, Mathf.Max(0, position.height - top - DETAILS_HEIGHT));
            var details = new Rect(0, body.yMax, position.width, DETAILS_HEIGHT);

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

            var names = m_target.Animations.Select(x => x.Name).ToArray();

            if (names.Length > 0) {
                var index = EditorGUILayout.Popup(m_animationIndex, names, EditorStyles.toolbarPopup, GUILayout.Width(120));

                if (index != m_animationIndex) {
                    SelectAnimation(index);
                }
            }

            if (GUILayout.Button(new GUIContent("+", "Add animation"), EditorStyles.toolbarButton, GUILayout.Width(22))) {
                AddAnimation();
            }

            if (Animation != null) {
                var wrap = AnimationProperty().FindPropertyRelative("m_wrap");
                EditorGUILayout.PropertyField(wrap, GUIContent.none, GUILayout.Width(80));
            }

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(Animation == null)) {
                DrawTransport();
            }

            GUILayout.Label("Snap", EditorStyles.miniLabel);
            m_snap = Mathf.Max(0, EditorGUILayout.FloatField(m_snap, EditorStyles.toolbarTextField, GUILayout.Width(40)));
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

            var playing = Application.isPlaying ? m_target.IsPlaying : IsPreviewing && TweenlinePreview.IsPlaying;
            var playIcon = EditorGUIUtility.TrIconContent(playing ? "PauseButton" : "PlayButton", playing ? "Pause" : "Play");

            if (GUILayout.Button(playIcon, EditorStyles.toolbarButton, GUILayout.Width(28))) {
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

            var time = EditorGUILayout.FloatField(PlayheadTime, EditorStyles.toolbarTextField, GUILayout.Width(50));

            if (!Mathf.Approximately(time, PlayheadTime)) {
                ScrubTo(time);
            }
        }

        private void DrawTimeline(Rect body)
        {
            var animation = Animation;
            var tracks = animation.Tracks;
            var contentHeight = tracks.Count * ROW_HEIGHT;
            var rowsHeight = body.height - RULER_HEIGHT;
            var hasScrollbar = contentHeight > rowsHeight;
            var width = body.width - (hasScrollbar ? SCROLLBAR_WIDTH : 0);

            var rulerRect = new Rect(HEADER_WIDTH, body.y, width - HEADER_WIDTH, RULER_HEIGHT);
            var rowsRect = new Rect(0, body.y + RULER_HEIGHT, width, rowsHeight);
            var timelineRect = new Rect(HEADER_WIDTH, rowsRect.y, width - HEADER_WIDTH, rowsHeight);

            m_rowScroll = hasScrollbar ? Mathf.Clamp(m_rowScroll, 0, contentHeight - rowsHeight) : 0;

            HandleInput(rulerRect, timelineRect);

            var headerRect = new Rect(0, body.y, HEADER_WIDTH, RULER_HEIGHT);
            EditorGUI.DrawRect(headerRect, new Color(0, 0, 0, 0.1f));

            if (GUI.Button(new Rect(4, body.y + 2, 90, RULER_HEIGHT - 4), "Add Track", EditorStyles.miniButton)) {
                TweenlineEditorUtils.ShowAddTrackMenu(m_target, animation, index => {
                    m_serializedObject.Update();
                    SelectTrack(index);
                });
            }

            GUI.Label(new Rect(98, body.y + 3, HEADER_WIDTH - 102, RULER_HEIGHT), $"{animation.Duration:0.###}s", EditorStyles.miniLabel);

            DrawRuler(rulerRect, animation);

            GUI.BeginGroup(rowsRect);

            for (var i = 0; i < tracks.Count; i++) {
                var rowRect = new Rect(0, i * ROW_HEIGHT - m_rowScroll, rowsRect.width, ROW_HEIGHT);

                if (rowRect.yMax < 0 || rowRect.y > rowsRect.height) {
                    continue;
                }

                DrawRow(rowRect, i, tracks[i]);
            }

            var endX = TimeToX(animation.Duration);
            if (endX > HEADER_WIDTH) {
                EditorGUI.DrawRect(new Rect(endX, 0, 1, rowsRect.height), s_endColor);
            }

            GUI.EndGroup();

            if (hasScrollbar) {
                var scrollRect = new Rect(width, rowsRect.y, SCROLLBAR_WIDTH, rowsHeight);
                m_rowScroll = GUI.VerticalScrollbar(scrollRect, m_rowScroll, rowsHeight, 0, contentHeight);
            }

            DrawPlayhead(new Rect(HEADER_WIDTH, body.y, rulerRect.width, body.height));
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

        private void DrawRow(Rect rowRect, int index, TweenTrack track)
        {
            var selectedRow = m_selectedTrack == index && m_selected != Selected.None && m_selected != Selected.Marker;
            EditorGUI.DrawRect(rowRect, selectedRow ? s_rowSelectedColor : index % 2 == 0 ? s_rowColor : Color.clear);

            if (track == null) {
                GUI.Label(rowRect, "Missing track type", EditorStyles.miniLabel);
                return;
            }

            var trackProperty = TrackProperty(index);
            var enabled = trackProperty.FindPropertyRelative("m_enabled");

            var toggleRect = new Rect(rowRect.x + 4, rowRect.y + 4, 16, 16);
            enabled.boolValue = EditorGUI.Toggle(toggleRect, enabled.boolValue);

            var targetName = track.TargetObject != null ? track.TargetObject.name : "None";
            var labelRect = new Rect(rowRect.x + 22, rowRect.y + 3, HEADER_WIDTH - 50, 18);
            var warning = track.GetWarning(m_target);
            var label = new GUIContent($"{(warning != null ? "⚠ " : string.Empty)}{TweenlineEditorUtils.TrackName(track.GetType())}  <{targetName}>", warning);

            if (GUI.Button(labelRect, label, EditorStyles.label)) {
                SelectTrack(index);
            }

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

                var selected = m_selected == Selected.Clip && m_selectedTrack == index && m_selectedClip == c;
                EditorGUI.DrawRect(clipRect, selected ? s_clipSelectedColor : clipColor);
                EditorGUI.DrawRect(new Rect(clipRect.xMax - 2, clipRect.y, 2, clipRect.height), new Color(1, 1, 1, 0.35f));

                var clip = clips.GetArrayElementAtIndex(c);
                var fromMode = (ClipFrom) clip.FindPropertyRelative("FromMode").enumValueIndex;
                var ease = clip.FindPropertyRelative("Ease");
                var easeLabel = ease.FindPropertyRelative("UseCurve").boolValue ? "Curve" : ease.FindPropertyRelative("Ease").enumDisplayNames[ease.FindPropertyRelative("Ease").enumValueIndex];
                var prefix = fromMode == ClipFrom.Previous ? "↳ " : fromMode == ClipFrom.Current ? "● " : string.Empty;

                GUI.Label(new Rect(clipRect.x + 3, clipRect.y, Mathf.Max(0, clipRect.width - 6), clipRect.height), prefix + easeLabel, EditorStyles.whiteMiniLabel);
            }

            GUI.EndClip();
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

                var selected = m_selected == Selected.Clip && m_selectedTrack == trackIndex && m_selectedClip == c;
                EditorGUI.DrawRect(handle, selected ? Color.white : new Color(0.85f, 0.85f, 0.85f));
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

        private void DrawDetails(Rect rect)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), new Color(0, 0, 0, 0.4f));

            GUILayout.BeginArea(new Rect(rect.x + 6, rect.y + 4, rect.width - 12, rect.height - 8));
            m_detailsScroll = EditorGUILayout.BeginScrollView(m_detailsScroll);

            if (Animation == null) {
                EditorGUILayout.LabelField("Add an animation to start", EditorStyles.centeredGreyMiniLabel);
            } else {
                switch (m_selected) {
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

            using (new EditorGUILayout.HorizontalScope()) {
                if (GUILayout.Button("Add Marker at Playhead")) {
                    AddMarker(PlayheadTime);
                }

                if (GUILayout.Button("Delete Animation")) {
                    DeleteAnimation();
                }
            }

            EditorGUILayout.HelpBox("Double click a track row to add a clip. Drag clips to move them and their right edge to resize. "
                + "Right click the ruler to add a marker. Scroll to zoom, middle drag to pan, hold Ctrl to drag without snapping.", MessageType.None);
        }

        private void DrawTrackDetails()
        {
            var track = Animation.Tracks[m_selectedTrack];
            var property = TrackProperty(m_selectedTrack);

            EditorGUILayout.LabelField(TweenlineEditorUtils.TrackName(track.GetType()), EditorStyles.boldLabel);
            DrawWarning(track);
            EditorGUILayout.PropertyField(property.FindPropertyRelative("m_target"));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("m_enabled"));

            using (new EditorGUILayout.HorizontalScope()) {
                if (GUILayout.Button("Add Clip at Playhead")) {
                    AddClip(m_selectedTrack, PlayheadTime);
                }

                if (GUILayout.Button("Remove Track")) {
                    RemoveTrack(m_selectedTrack);
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

            using (new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.PropertyField(clip.FindPropertyRelative("Start"));
                EditorGUILayout.PropertyField(clip.FindPropertyRelative("Duration"));
            }

            EditorGUILayout.PropertyField(clip.FindPropertyRelative("Ease"));
            EditorGUILayout.PropertyField(fromMode, new GUIContent("From"));

            if ((ClipFrom) fromMode.enumValueIndex == ClipFrom.Value) {
                EditorGUILayout.PropertyField(clip.FindPropertyRelative("From"), new GUIContent("From Value"));
            }

            EditorGUILayout.PropertyField(clip.FindPropertyRelative("To"));
            EditorGUILayout.PropertyField(clip.FindPropertyRelative("Space"));
            DrawClipButtons();
        }

        private void DrawWarning(TweenTrack track)
        {
            var warning = track.GetWarning(m_target);

            if (warning != null) {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }
        }

        private void DrawClipButtons()
        {
            using (new EditorGUILayout.HorizontalScope()) {
                if (GUILayout.Button("Duplicate")) {
                    DuplicateClip(m_selectedTrack, m_selectedClip);
                }

                if (GUILayout.Button("Delete")) {
                    DeleteClip(m_selectedTrack, m_selectedClip);
                }
            }
        }

        private void DrawMarkerDetails()
        {
            var marker = AnimationProperty().FindPropertyRelative("m_markers").GetArrayElementAtIndex(m_selectedMarker);

            EditorGUILayout.LabelField("Marker", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(marker.FindPropertyRelative("Time"));
            EditorGUILayout.PropertyField(marker.FindPropertyRelative("Name"));
            EditorGUILayout.PropertyField(marker.FindPropertyRelative("Event"));

            if (GUILayout.Button("Delete Marker")) {
                var markers = AnimationProperty().FindPropertyRelative("m_markers");
                markers.DeleteArrayElementAtIndex(m_selectedMarker);
                ClearSelection();
            }
        }

        private void HandleInput(Rect rulerRect, Rect timelineRect)
        {
            var e = Event.current;
            var controlId = GUIUtility.GetControlID(FocusType.Passive);
            var mouseTime = XToTime(e.mousePosition.x);
            var overRuler = rulerRect.Contains(e.mousePosition);
            var overTimeline = timelineRect.Contains(e.mousePosition);

            switch (e.GetTypeForControl(controlId)) {
                case EventType.MouseDown:
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
                        HandleDrag(e, mouseTime);
                        e.Use();
                    }

                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId) {
                        GUIUtility.hotControl = 0;

                        if (m_dragMode == DragMode.MoveClip || m_dragMode == DragMode.ResizeClip || m_dragMode == DragMode.MoveMarker) {
                            Undo.CollapseUndoOperations(m_undoGroup);
                        }

                        m_dragMode = DragMode.None;
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

        private void HandleRulerMouseDown(Event e, int controlId, float mouseTime)
        {
            var markers = Animation.Markers;

            for (var i = markers.Count - 1; i >= 0; i--) {
                if (Mathf.Abs(TimeToX(markers[i].Time) - e.mousePosition.x) <= 5) {
                    SelectMarker(i);

                    if (e.button == 1) {
                        var index = i;
                        var menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Delete Marker"), false, () => {
                            TweenlineEditorUtils.Modify(m_target, "Delete Marker", () => Animation.Markers.RemoveAt(index));
                            ClearSelection();
                        });
                        menu.ShowAsContext();
                    } else {
                        BeginDrag(controlId, DragMode.MoveMarker, markers[i].Time - mouseTime);
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
            var row = Mathf.FloorToInt((e.mousePosition.y - timelineRect.y + m_rowScroll) / ROW_HEIGHT);
            var tracks = Animation.Tracks;

            if (row < 0 || row >= tracks.Count || tracks[row] == null) {
                if (e.button == 0) {
                    ClearSelection();
                    e.Use();
                }

                return;
            }

            var track = tracks[row];
            var rowY = timelineRect.y + row * ROW_HEIGHT - m_rowScroll;

            for (var c = track.ClipCount - 1; c >= 0; c--) {
                var clipRect = ClipRect(track, c, rowY);

                if (!clipRect.Contains(e.mousePosition)) {
                    continue;
                }

                SelectClip(row, c);

                if (e.button == 1) {
                    ShowClipMenu(row, c);
                } else {
                    track.GetClipRange(c, out var start, out _);

                    if (!(track is IStepTrack) && e.mousePosition.x >= clipRect.xMax - EDGE_GRAB) {
                        BeginDrag(controlId, DragMode.ResizeClip, 0);
                    } else {
                        BeginDrag(controlId, DragMode.MoveClip, start - mouseTime);
                    }
                }

                e.Use();
                return;
            }

            var time = Snap(Mathf.Max(0, mouseTime), e);

            if (e.button == 1) {
                var trackIndex = row;
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Add Clip"), false, () => AddClip(trackIndex, time));
                menu.ShowAsContext();
            } else if (e.clickCount == 2) {
                AddClip(row, time);
            } else {
                SelectTrack(row);
            }

            e.Use();
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
                case DragMode.MoveClip: {
                    var start = ClipProperty(m_selectedTrack, m_selectedClip).FindPropertyRelative("Start");
                    start.floatValue = Mathf.Max(0, Snap(mouseTime + m_dragOffset, e));
                    ApplyChanges();
                    break;
                }
                case DragMode.ResizeClip: {
                    var clip = ClipProperty(m_selectedTrack, m_selectedClip);
                    var start = clip.FindPropertyRelative("Start").floatValue;
                    clip.FindPropertyRelative("Duration").floatValue = Mathf.Max(0, Snap(mouseTime, e) - start);
                    ApplyChanges();
                    break;
                }
                case DragMode.MoveMarker: {
                    var marker = AnimationProperty().FindPropertyRelative("m_markers").GetArrayElementAtIndex(m_selectedMarker);
                    marker.FindPropertyRelative("Time").floatValue = Mathf.Max(0, Snap(mouseTime + m_dragOffset, e));
                    ApplyChanges();
                    break;
                }
            }

            Repaint();
        }

        private void BeginDrag(int controlId, DragMode mode, float offset)
        {
            GUIUtility.hotControl = controlId;
            m_dragMode = mode;
            m_dragOffset = offset;

            Undo.IncrementCurrentGroup();
            m_undoGroup = Undo.GetCurrentGroup();
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

        private void DeleteAnimation()
        {
            var index = m_animationIndex;

            if (IsPreviewing) {
                TweenlinePreview.Stop();
            }

            TweenlineEditorUtils.Modify(m_target, "Delete Animation", () => m_target.Animations.RemoveAt(index));
            m_serializedObject.Update();
            SelectAnimation(Mathf.Max(0, index - 1));
            GUIUtility.ExitGUI();
        }

        private void AddClip(int trackIndex, float time)
        {
            var clipIndex = -1;
            TweenlineEditorUtils.Modify(m_target, "Add Clip", () => clipIndex = Animation.Tracks[trackIndex].AddClip(time));
            m_serializedObject.Update();
            SelectClip(trackIndex, clipIndex);
        }

        private void DuplicateClip(int trackIndex, int clipIndex)
        {
            var clips = TrackProperty(trackIndex).FindPropertyRelative("m_clips");
            clips.InsertArrayElementAtIndex(clipIndex);

            var copy = clips.GetArrayElementAtIndex(clipIndex + 1);
            var start = copy.FindPropertyRelative("Start");
            var duration = copy.FindPropertyRelative("Duration");
            start.floatValue += duration != null ? duration.floatValue : Mathf.Max(m_snap, 0.1f);

            ApplyChanges();
            SelectClip(trackIndex, clipIndex + 1);
        }

        private void DeleteClip(int trackIndex, int clipIndex)
        {
            TrackProperty(trackIndex).FindPropertyRelative("m_clips").DeleteArrayElementAtIndex(clipIndex);
            ApplyChanges();
            SelectTrack(trackIndex);
        }

        private void RemoveTrack(int trackIndex)
        {
            TweenlineEditorUtils.Modify(m_target, "Remove Track", () => Animation.Tracks.RemoveAt(trackIndex));
            m_serializedObject.Update();
            ClearSelection();
            GUIUtility.ExitGUI();
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
            menu.AddItem(new GUIContent("Remove Track"), false, () => {
                TweenlineEditorUtils.Modify(m_target, "Remove Track", () => Animation.Tracks.RemoveAt(trackIndex));
                m_serializedObject.Update();
                ClearSelection();
            });
            menu.ShowAsContext();
        }

        private void ShowClipMenu(int trackIndex, int clipIndex)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Duplicate"), false, () => {
                m_serializedObject.Update();
                DuplicateClip(trackIndex, clipIndex);
            });
            menu.AddItem(new GUIContent("Delete"), false, () => {
                m_serializedObject.Update();
                DeleteClip(trackIndex, clipIndex);
            });
            menu.ShowAsContext();
        }

        private void SelectTrack(int index)
        {
            m_selected = index >= 0 ? Selected.Track : Selected.None;
            m_selectedTrack = index;
            m_selectedClip = -1;
            m_selectedMarker = -1;
            GUI.FocusControl(null);
            Repaint();
        }

        private void SelectClip(int trackIndex, int clipIndex)
        {
            m_selected = clipIndex >= 0 ? Selected.Clip : Selected.None;
            m_selectedTrack = trackIndex;
            m_selectedClip = clipIndex;
            m_selectedMarker = -1;
            GUI.FocusControl(null);
            Repaint();
        }

        private void SelectMarker(int index)
        {
            m_selected = index >= 0 ? Selected.Marker : Selected.None;
            m_selectedTrack = -1;
            m_selectedClip = -1;
            m_selectedMarker = index;
            GUI.FocusControl(null);
            Repaint();
        }

        private void ClearSelection()
        {
            m_selected = Selected.None;
            m_selectedTrack = -1;
            m_selectedClip = -1;
            m_selectedMarker = -1;
            GUI.FocusControl(null);
        }

        private void ValidateSelection()
        {
            var animation = Animation;

            var valid = m_selected switch {
                Selected.Track => animation != null && m_selectedTrack >= 0 && m_selectedTrack < animation.Tracks.Count && animation.Tracks[m_selectedTrack] != null,
                Selected.Clip => animation != null && m_selectedTrack >= 0 && m_selectedTrack < animation.Tracks.Count && animation.Tracks[m_selectedTrack] != null
                    && m_selectedClip >= 0 && m_selectedClip < animation.Tracks[m_selectedTrack].ClipCount,
                Selected.Marker => animation != null && m_selectedMarker >= 0 && m_selectedMarker < animation.Markers.Count,
                _ => true
            };

            if (!valid) {
                ClearSelection();
            }
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

        private float Snap(float time, Event e)
        {
            if (m_snap <= 0 || e.control) {
                return time;
            }

            return Mathf.Round(time / m_snap) * m_snap;
        }

        private float TickStep()
        {
            var steps = new[] { 0.01f, 0.02f, 0.05f, 0.1f, 0.2f, 0.5f, 1f, 2f, 5f, 10f };

            foreach (var step in steps) {
                if (step * m_pixelsPerSecond >= 12) {
                    return step;
                }
            }

            return 10f;
        }
    }
}
