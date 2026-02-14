using Tekly.Common.Maths;
using Tekly.Common.Utils;
using UnityEngine;
using UnityEngine.Audio;

namespace Tekly.Thunk.Core
{
	public enum ThunkClipCapacityBehaviour
	{
		Unbound,
		ReplaceOldest,
		IgnoreNew
	}
	
	[CreateAssetMenu(fileName = "thunk_clip", menuName = "Tekly/Thunk/Clip")]
	public class ThunkClip : UniqueIdScriptableObject
	{
		public RandomSelectMode RandomMode;
		public AudioClip[] Clips;
		public AudioMixerGroup MixerGroup;
		
		[RangeLimits(0f, 1f)]
		public FloatRange Volume;
		
		[RangeLimits(0f, 3f)]
		public FloatRange Pitch;
		
		public bool Loop;
		public float MinimumTimeBetweenPlays;
		public int InstanceCapacity;
		
		public ThunkClipCapacityBehaviour CapacityBehaviour;

		public ThunkClipState State => Thunk.Instance.ClipStateManager.GetOrCreate(this);

		public virtual ThunkClipState CreateState()
		{
			return new ThunkClipState(this);
		}
		
		private void OnDisable()
		{
			Unregister();
		}

		private void OnDestroy()
		{
			Unregister();
		}

		private void Unregister()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying) {
				return;
			}
#endif
			Thunk.Instance.ClipStateManager.Unregister(this);
		}

		
#if UNITY_EDITOR
		public void Reset()
		{
			Volume = new FloatRange(1, 1);
			Pitch = new FloatRange(1, 1);
			Loop = false;
			MinimumTimeBetweenPlays = 0;
			InstanceCapacity = 0;
			CapacityBehaviour = ThunkClipCapacityBehaviour.Unbound;
		}
#endif
	}
}