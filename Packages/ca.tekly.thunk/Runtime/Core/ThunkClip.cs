using UnityEngine;

namespace Tekly.Thunk
{
	/// <summary>
	/// A sound that plays one AudioClip picked from a set, once or looping.
	/// </summary>
	[CreateAssetMenu(fileName = "thunk_clip", menuName = "Tekly/Thunk/Clip")]
	public class ThunkClip : ThunkSound
	{
		public ThunkClipSet Clips => m_clips;
		public bool Loop => m_loop;
		public bool RandomizeStartTime => m_randomizeStartTime;

		[SerializeField] private ThunkClipSet m_clips;
		[SerializeField] private bool m_loop;

		[Tooltip("Starts at a random point in the clip. Useful for loops that can overlap themselves, like ambience.")]
		[SerializeField] private bool m_randomizeStartTime;

		internal override bool EndsOnItsOwn => !m_loop;

		internal override bool Begin(ThunkVoice voice, ThunkSoundRecord record)
		{
			var clip = record.Select(0, m_clips);

			if (clip == null) {
				return false;
			}

			var source = voice.AddSource(clip);
			source.Audio.loop = m_loop;

			if (m_randomizeStartTime) {
				// The int Range excludes the max, so this never seeks to the very end
				source.Audio.timeSamples = Random.Range(0, clip.samples);
			}

			source.Audio.Play();
			return true;
		}
	}
}
