using System.Collections.Generic;
using NUnit.Framework;
using Tekly.Common.Utils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Thunk.Tests
{
	public abstract class ThunkTestBase
	{
		internal ThunkRuntime Runtime => m_runtime;

		private readonly List<Object> m_objects = new List<Object>();
		private ThunkRuntime m_runtime;

		[SetUp]
		public void SetUpThunk()
		{
			m_runtime = new ThunkRuntime();
		}

		[TearDown]
		public void TearDownThunk()
		{
			// The runtime goes first, while the emitters and templates its voices refer to still exist
			m_runtime.Dispose();
			m_runtime = null;

			for (var index = 0; index < m_objects.Count; index++) {
				if (m_objects[index] != null) {
					Object.DestroyImmediate(m_objects[index]);
				}
			}

			m_objects.Clear();
		}

		protected AudioClip[] CreateClips(int count)
		{
			var clips = new AudioClip[count];

			for (var index = 0; index < count; index++) {
				clips[index] = AudioClip.Create("test", 4410, 1, 44100, false);
				m_objects.Add(clips[index]);
			}

			return clips;
		}

		protected static ThunkClipSet CreateSet(AudioClip[] clips, RandomSelectMode mode)
		{
			return new ThunkClipSet { Clips = clips, Mode = mode };
		}

		protected ThunkEmitter CreateEmitter()
		{
			var emitter = new GameObject("emitter").AddComponent<ThunkEmitter>();
			m_objects.Add(emitter.gameObject);

			return emitter;
		}

		protected ThunkClip CreateSound(int clipCount = 1, int maxInstances = 0, ThunkStealing stealing = ThunkStealing.None, float cooldown = 0f, ulong uniqueId = 0UL, bool loop = true)
		{
			var sound = ScriptableObject.CreateInstance<ThunkClip>();
			m_objects.Add(sound);

			var clips = CreateClips(clipCount);
			var serialized = new SerializedObject(sound);
			var clipsProperty = serialized.FindProperty("m_clips.Clips");

			clipsProperty.arraySize = clipCount;

			for (var index = 0; index < clipCount; index++) {
				clipsProperty.GetArrayElementAtIndex(index).objectReferenceValue = clips[index];
			}

			serialized.FindProperty("m_clips.Mode").enumValueIndex = (int) RandomSelectMode.Random;
			serialized.FindProperty("m_loop").boolValue = loop;
			serialized.FindProperty("m_maxInstances").intValue = maxInstances;
			serialized.FindProperty("m_stealing").enumValueIndex = (int) stealing;
			serialized.FindProperty("m_cooldown").floatValue = cooldown;
			serialized.FindProperty("m_uniqueId").ulongValue = uniqueId;
			serialized.ApplyModifiedPropertiesWithoutUndo();

			return sound;
		}

		protected ThunkTrack CreateTrack(bool resumeHeld = true, float duckLevel = 0.3f, float duckFadeDown = 0f, float duckFadeBack = 0f)
		{
			var track = ScriptableObject.CreateInstance<ThunkTrack>();
			m_objects.Add(track);

			var serialized = new SerializedObject(track);
			serialized.FindProperty("m_crossfade").floatValue = 0f;
			serialized.FindProperty("m_resumeHeld").boolValue = resumeHeld;
			serialized.FindProperty("m_duckLevel").floatValue = duckLevel;
			serialized.FindProperty("m_duckFadeDown").floatValue = duckFadeDown;
			serialized.FindProperty("m_duckFadeBack").floatValue = duckFadeBack;
			serialized.ApplyModifiedPropertiesWithoutUndo();

			return track;
		}

		protected AudioSource CreateTemplate(float spatialBlend = 1f, float minDistance = 1f, float maxDistance = 50f, AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic)
		{
			var gameObject = new GameObject("template");
			m_objects.Add(gameObject);

			var template = gameObject.AddComponent<AudioSource>();
			template.playOnAwake = false;
			template.spatialBlend = spatialBlend;
			template.minDistance = minDistance;
			template.maxDistance = maxDistance;
			template.rolloffMode = rolloffMode;

			return template;
		}

		protected static void SetSoundTemplate(ThunkSound sound, AudioSource template)
		{
			var serialized = new SerializedObject(sound);
			serialized.FindProperty("m_spatialTemplate").objectReferenceValue = template;
			serialized.ApplyModifiedPropertiesWithoutUndo();
		}

		protected AudioClip CreateClip(int samples)
		{
			var clip = AudioClip.Create("test", samples, 1, 44100, false);
			m_objects.Add(clip);

			return clip;
		}

		protected ThunkChain CreateChain(int startClips = 0, int loopClips = 1, int endClips = 0, float endCrossfade = 0.05f, float fadeOut = 0f, int maxInstances = 0, float pitch = 1f, int startSamples = 44100)
		{
			var chain = ScriptableObject.CreateInstance<ThunkChain>();
			m_objects.Add(chain);

			var serialized = new SerializedObject(chain);

			WriteClips(serialized.FindProperty("m_start.Clips"), startClips, startSamples);
			WriteClips(serialized.FindProperty("m_loop.Clips"), loopClips, 4410);
			WriteClips(serialized.FindProperty("m_end.Clips"), endClips, 4410);

			serialized.FindProperty("m_endCrossfade").floatValue = endCrossfade;
			serialized.FindProperty("m_fadeOut").floatValue = fadeOut;
			serialized.FindProperty("m_maxInstances").intValue = maxInstances;
			serialized.FindProperty("m_cooldown").floatValue = 0f;
			serialized.FindProperty("m_pitch.Min").floatValue = pitch;
			serialized.FindProperty("m_pitch.Max").floatValue = pitch;
			serialized.ApplyModifiedPropertiesWithoutUndo();

			return chain;
		}

		private void WriteClips(SerializedProperty property, int count, int samples)
		{
			property.arraySize = count;

			for (var index = 0; index < count; index++) {
				property.GetArrayElementAtIndex(index).objectReferenceValue = CreateClip(samples);
			}
		}

		protected static TimelineEntryDescription Entry(float delay = 0f, int clips = 1, float volumeOffset = 0f, float semitones = 0f)
		{
			return new TimelineEntryDescription { Delay = delay, ClipCount = clips, VolumeOffset = volumeOffset, Semitones = semitones };
		}

		protected ThunkTimeline CreateTimeline(TimelineEntryDescription[] entries, float fadeOut = 0f, float pitch = 1f, int maxInstances = 0)
		{
			var timeline = ScriptableObject.CreateInstance<ThunkTimeline>();
			m_objects.Add(timeline);

			var serialized = new SerializedObject(timeline);
			var entriesProperty = serialized.FindProperty("m_entries");
			var count = entries != null ? entries.Length : 0;

			entriesProperty.arraySize = count;

			for (var index = 0; index < count; index++) {
				var element = entriesProperty.GetArrayElementAtIndex(index);
				var clips = element.FindPropertyRelative("Clips").FindPropertyRelative("Clips");

				element.FindPropertyRelative("Delay").floatValue = entries[index].Delay;
				element.FindPropertyRelative("VolumeOffset").floatValue = entries[index].VolumeOffset;
				element.FindPropertyRelative("Semitones").floatValue = entries[index].Semitones;

				WriteClips(clips, entries[index].ClipCount, 44100);
			}

			serialized.FindProperty("m_fadeOut").floatValue = fadeOut;
			serialized.FindProperty("m_maxInstances").intValue = maxInstances;
			serialized.FindProperty("m_cooldown").floatValue = 0f;
			serialized.FindProperty("m_pitch.Min").floatValue = pitch;
			serialized.FindProperty("m_pitch.Max").floatValue = pitch;
			serialized.ApplyModifiedPropertiesWithoutUndo();

			return timeline;
		}

		internal ThunkVoice GetVoice(ThunkHandle handle)
		{
			Assert.IsTrue(m_runtime.TryGetVoice(handle, out var voice), "the handle should resolve");
			return voice;
		}

		protected bool Resolves(ThunkHandle handle)
		{
			return m_runtime.TryGetVoice(handle, out _);
		}
	}

	public struct TimelineEntryDescription
	{
		public float Delay;
		public int ClipCount;
		public float VolumeOffset;
		public float Semitones;
	}
}
