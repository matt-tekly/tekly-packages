using System;
using UnityEngine;

namespace Tekly.Common.Timers
{
	[Serializable]
	public class Timer
	{
		[NonSerialized] private float m_scale = 1f;
		[SerializeField] private TimerRef m_parent;
		
		public virtual float Scale {
			get {
				if (m_parent == null) {
					return m_scale;
				}

				return m_parent.Value.Scale * m_scale;
			}
			set => m_scale = value;
		}

		public float DeltaTime => Time.deltaTime * Scale;
		public float FixedDeltaTime => Time.fixedDeltaTime * Scale;
	}
	
	[Serializable]
	public class UnityTimer : Timer
	{
		public override float Scale {
			get => Time.timeScale;
			set => Time.timeScale = value;
		}
	}
}