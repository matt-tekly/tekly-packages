using System;
using Tekly.Common.Maths;
using UnityEngine;

namespace Tekly.Lofi.Core
{
	[Serializable]
	public struct ClipRef
	{
		public AudioClip Clip;
	}
	
	[CreateAssetMenu(menuName = "Tekly/Lofi/Clip")]
	public class LofiClipDefinition : ScriptableObject
	{
		public ClipRef[] Clips;

		public FloatRange Volume;
		public FloatRange Pitch;
		public LofiMixerGroup MixerGroup;
		
		public float MinTimeBetweenPlays;

		public bool Loop;
		
		public LofiClip CreateClip()
		{
			return new LofiClip(this);
		}
		
		public void Reset()
		{
			Volume = new FloatRange(1, 1);
			Pitch = new FloatRange(1, 1);
		}
	}	
}