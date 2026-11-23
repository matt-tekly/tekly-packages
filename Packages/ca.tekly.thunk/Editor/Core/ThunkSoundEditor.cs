using UnityEditor;
using UnityEngine;

namespace Tekly.Thunk
{
	/// <summary>
	/// The inspector every kind of sound shares: preview buttons at the top, then what is particular to the kind of
	/// sound, then the settings all sounds have, in groups that fold away. A kind of sound with its own inspector
	/// derives from this and draws its own content. One without gets its fields drawn as they are.
	/// </summary>
	[CustomEditor(typeof(ThunkSound), true)]
	[CanEditMultipleObjects]
	public class ThunkSoundEditor : Editor
	{
		private const string PREFS_PREFIX = "Tekly.Thunk.SoundEditor.";
		private const float BUTTON_WIDTH = 72f;

		private const int GROUP_OUTPUT = 0;
		private const int GROUP_FADES = 1;
		private const int GROUP_LIMITS = 2;
		private const int GROUP_SPATIAL = 3;

		private static readonly string[] s_groupNames = { "Output", "Fades", "Limits", "Spatial" };
		private static readonly bool[] s_groupDefaults = { true, true, false, false };
		private static readonly bool[] s_groupOpen = new bool[4];
		private static bool s_groupsLoaded;

		// The fields this inspector places itself, so they are left out when the rest are drawn as they are
		private static readonly string[] s_commonFields = {
			"m_Script", "m_mixerGroup", "m_volume", "m_pitch", "m_fadeIn", "m_fadeOut", "m_fadeEase", "m_priority",
			"m_playWhilePaused", "m_spatialTemplate", "m_maxInstances", "m_stealing", "m_cooldown"
		};

		/// <summary>
		/// Kept between presses so that exhaustive and non-repeating selection behave as they do at runtime.
		/// </summary>
		private protected ThunkSoundRecord Record => m_record;

		private protected ThunkClipSetLists ClipSets => m_clipSets ??= new ThunkClipSetLists(PlayClip);

		/// <summary>
		/// True for a sound that has an ending to preview, which adds a Release button.
		/// </summary>
		protected virtual bool CanRelease => false;

		private readonly ThunkSoundRecord m_record = new ThunkSoundRecord();
		private ThunkClipSetLists m_clipSets;

		private SerializedProperty m_mixerGroup;
		private SerializedProperty m_volume;
		private SerializedProperty m_pitch;
		private SerializedProperty m_fadeIn;
		private SerializedProperty m_fadeOut;
		private SerializedProperty m_fadeEase;
		private SerializedProperty m_priority;
		private SerializedProperty m_playWhilePaused;
		private SerializedProperty m_spatialTemplate;
		private SerializedProperty m_maxInstances;
		private SerializedProperty m_stealing;
		private SerializedProperty m_cooldown;

		public override bool RequiresConstantRepaint()
		{
			return Application.isPlaying || ThunkPreview.IsPlaying;
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			if (serializedObject.isEditingMultipleObjects) {
				EditorGUILayout.HelpBox("Preview and clips are hidden while several sounds are selected.", MessageType.None);
			} else {
				DrawPreviewBar((ThunkSound) target);
				EditorGUILayout.Space();
				DrawContent();
			}

			EditorGUILayout.Space();
			DrawCommon();

			serializedObject.ApplyModifiedProperties();
		}

		/// <summary>
		/// The reason a template would be a mistake to use, or null if it looks fine. A sound plays through copies
		/// of its template's whole GameObject, so a template has to be small and has to be an asset.
		/// </summary>
		internal static string GetTemplateProblem(AudioSource template)
		{
			if (template == null) {
				return null;
			}

			if (!EditorUtility.IsPersistent(template)) {
				return "This template is an object in a scene. Its whole GameObject is copied for every sound played through it, so use an AudioSource in a prefab of its own.";
			}

			var gameObject = template.gameObject;

			if (gameObject.transform.childCount > 0
				|| gameObject.TryGetComponent<Renderer>(out _)
				|| gameObject.TryGetComponent<Collider>(out _)
				|| gameObject.TryGetComponent<Animator>(out _)) {
				return "This template's GameObject has children or components that have nothing to do with audio. All of it is copied for every sound played through it. A template should be a prefab with only an AudioSource and its filters.";
			}

			return null;
		}

		internal static void DrawTemplateProblem(SerializedProperty template)
		{
			if (template.hasMultipleDifferentValues) {
				return;
			}

			var problem = GetTemplateProblem(template.objectReferenceValue as AudioSource);

			if (problem != null) {
				EditorGUILayout.HelpBox(problem, MessageType.Warning);
			}
		}

		/// <summary>
		/// Draws what is particular to this kind of sound.
		/// </summary>
		protected virtual void DrawContent()
		{
			DrawPropertiesExcluding(serializedObject, s_commonFields);
		}

		/// <summary>
		/// Why the sound can't be previewed, or null if it can.
		/// </summary>
		protected virtual string GetPreviewProblem()
		{
			return "This kind of sound has no preview.";
		}

		protected virtual void Preview() { }

		protected virtual void Release() { }

		protected virtual void OnEnable()
		{
			m_mixerGroup = serializedObject.FindProperty("m_mixerGroup");
			m_volume = serializedObject.FindProperty("m_volume");
			m_pitch = serializedObject.FindProperty("m_pitch");
			m_fadeIn = serializedObject.FindProperty("m_fadeIn");
			m_fadeOut = serializedObject.FindProperty("m_fadeOut");
			m_fadeEase = serializedObject.FindProperty("m_fadeEase");
			m_priority = serializedObject.FindProperty("m_priority");
			m_playWhilePaused = serializedObject.FindProperty("m_playWhilePaused");
			m_spatialTemplate = serializedObject.FindProperty("m_spatialTemplate");
			m_maxInstances = serializedObject.FindProperty("m_maxInstances");
			m_stealing = serializedObject.FindProperty("m_stealing");
			m_cooldown = serializedObject.FindProperty("m_cooldown");

			if (!s_groupsLoaded) {
				s_groupsLoaded = true;

				for (var i = 0; i < s_groupOpen.Length; i++) {
					s_groupOpen[i] = EditorPrefs.GetBool(PREFS_PREFIX + s_groupNames[i], s_groupDefaults[i]);
				}
			}
		}

		protected virtual void OnDisable()
		{
			ThunkPreview.Stop();
		}

		private void DrawPreviewBar(ThunkSound sound)
		{
			var problem = GetPreviewProblem();

			using (new EditorGUILayout.HorizontalScope()) {
				using (new EditorGUI.DisabledScope(problem != null)) {
					if (GUILayout.Button("Preview", GUILayout.Width(BUTTON_WIDTH))) {
						Preview();
					}

					if (CanRelease && GUILayout.Button("Release", GUILayout.Width(BUTTON_WIDTH))) {
						Release();
					}
				}

				if (GUILayout.Button("Stop", GUILayout.Width(BUTTON_WIDTH))) {
					ThunkPreview.Stop();
				}

				GUILayout.FlexibleSpace();

				var runtime = Application.isPlaying ? ThunkRuntime.Instance : null;

				if (runtime != null) {
					var live = runtime.GetRecord(sound).LiveCount;
					var text = sound.MaxInstances > 0 ? $"Playing: {live} of {sound.MaxInstances}" : $"Playing: {live}";

					GUILayout.Label(text, EditorStyles.miniLabel);
				}
			}

			if (problem != null) {
				EditorGUILayout.HelpBox(problem, MessageType.Warning);
			}
		}

		private void DrawCommon()
		{
			if (BeginGroup(GROUP_OUTPUT, null)) {
				EditorGUILayout.PropertyField(m_mixerGroup);
				EditorGUILayout.PropertyField(m_volume);
				EditorGUILayout.PropertyField(m_pitch);
				EditorGUILayout.PropertyField(m_priority);
				EditorGUILayout.PropertyField(m_playWhilePaused);
			}

			EndGroup();

			if (BeginGroup(GROUP_FADES, GetFadesSummary())) {
				EditorGUILayout.PropertyField(m_fadeIn);
				EditorGUILayout.PropertyField(m_fadeOut);
				EditorGUILayout.PropertyField(m_fadeEase);
			}

			EndGroup();

			if (BeginGroup(GROUP_LIMITS, GetLimitsSummary())) {
				EditorGUILayout.PropertyField(m_maxInstances);

				// Stealing only happens once the sound is at its max, and 0 means it has none
				using (new EditorGUI.DisabledScope(!m_maxInstances.hasMultipleDifferentValues && m_maxInstances.intValue == 0)) {
					EditorGUILayout.PropertyField(m_stealing);
				}

				EditorGUILayout.PropertyField(m_cooldown);
			}

			EndGroup();

			if (BeginGroup(GROUP_SPATIAL, GetSpatialSummary())) {
				EditorGUILayout.PropertyField(m_spatialTemplate);
				DrawTemplateProblem(m_spatialTemplate);
			}

			EndGroup();
		}

		/// <summary>
		/// Starts a group that folds away. The summary is shown next to the title while the group is closed, so a
		/// setting that matters is not hidden by it.
		/// </summary>
		private static bool BeginGroup(int group, string summary)
		{
			var wasOpen = s_groupOpen[group];
			var title = wasOpen || string.IsNullOrEmpty(summary) ? s_groupNames[group] : s_groupNames[group] + "  (" + summary + ")";
			var open = EditorGUILayout.BeginFoldoutHeaderGroup(wasOpen, title);

			if (open != wasOpen) {
				s_groupOpen[group] = open;
				EditorPrefs.SetBool(PREFS_PREFIX + s_groupNames[group], open);
			}

			EditorGUI.indentLevel++;
			return open;
		}

		private static void EndGroup()
		{
			EditorGUI.indentLevel--;
			EditorGUILayout.EndFoldoutHeaderGroup();
		}

		private string GetFadesSummary()
		{
			if (m_fadeIn.hasMultipleDifferentValues || m_fadeOut.hasMultipleDifferentValues) {
				return null;
			}

			if (m_fadeIn.floatValue <= 0f && m_fadeOut.floatValue <= 0f) {
				return "none";
			}

			return $"in {m_fadeIn.floatValue:0.##}s, out {m_fadeOut.floatValue:0.##}s";
		}

		private string GetLimitsSummary()
		{
			if (m_maxInstances.hasMultipleDifferentValues || m_cooldown.hasMultipleDifferentValues) {
				return null;
			}

			var max = m_maxInstances.intValue > 0 ? "max " + m_maxInstances.intValue : "no max";
			return $"{max}, cooldown {m_cooldown.floatValue:0.##}s";
		}

		private string GetSpatialSummary()
		{
			if (m_spatialTemplate.hasMultipleDifferentValues) {
				return null;
			}

			var template = m_spatialTemplate.objectReferenceValue;
			return template != null ? template.name : "no template";
		}

		private void PlayClip(AudioClip clip)
		{
			if (clip == null) {
				return;
			}

			var sound = (ThunkSound) target;
			ThunkPreview.Play(clip, sound.Volume.Get(), sound.Pitch.Get(), false, sound.MixerGroup);
		}
	}
}
