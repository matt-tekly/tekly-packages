using UnityEngine;
using UnityEngine.Audio;

namespace Tekly.Lofi.Core
{
	[CreateAssetMenu(menuName = "Tekly/Lofi/Core", fileName = "lofi_core")]
	public class LofiCoreAssets : ScriptableObject
	{
		public AudioMixer Mixer => m_audioMixer;
		[SerializeField] private AudioMixer m_audioMixer;
	}
}