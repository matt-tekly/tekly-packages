using UnityEngine;
using UnityEngine.Audio;

namespace Tekly.Lofi.Core
{
	[CreateAssetMenu(menuName = "Tekly/Lofi/Mixer Group")]
	public class LofiMixerGroup : ScriptableObject
	{
		public AudioMixerGroup MixerGroup => m_mixerGroup;
		public bool IgnoreListenerPause => m_ignoreListenerPause;
		
		[SerializeField] private AudioMixerGroup m_mixerGroup;
		[SerializeField] private bool m_ignoreListenerPause;
	}
}