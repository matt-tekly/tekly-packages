using System;
using Tekly.Common.Timers;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Tekly.TwoD.Cells
{
	public abstract class CellAnimator : MonoBehaviour
	{
		[SerializeField] private CellSprite m_sprite;
		[SerializeField] private string m_animName;
		
		[SerializeField] private bool m_loop = true;
		[SerializeField] private bool m_playOnEnable = true;
		[SerializeField] private float m_speed = 1f;
		[SerializeField] private bool m_destroyOnStop;
		[SerializeField] private bool m_randomizeTime;
		[SerializeField] private TimerRef m_timer;

		private CellAnimation m_animation;
		private bool m_isPlaying;
		private int m_frame = -1;
		private float m_time;
		
		public event Action<CellAnimator, CellFrameEvt> AnimationEvent;

		public CellSprite Sprite {
			get => m_sprite;
			set {
				m_sprite = value;
				m_animation = null;
				SetAnimation(m_animName);
			}
		}
		
		public float Speed {
			get => m_speed;
			set => m_speed = value;
		}

		public bool IsPlaying => m_isPlaying;

		public abstract bool Visible { get; set; }
		public abstract Color Color { get; set; }
		protected abstract Sprite RenderedSprite { get; set; }
		
		public void SetAnimation(string animName)
		{
			if ((m_animation != null && m_animation.name == animName) || m_sprite == null) {
				return;
			}
			
			m_animName = animName;
			m_animation = m_sprite.Get(animName);
			m_time = 0;
			m_frame = -1;
		}

		public void Play()
		{
			m_isPlaying = true;
		}

		public void RandomizeTime()
		{
			m_time = Random.Range(0, m_animation.Duration);
		}
		
		private void OnEnable()
		{
			SetAnimation(m_animName);	
			
			if (m_playOnEnable) {
				m_time = 0;
				m_frame = 0;
				m_isPlaying = true;

				if (m_randomizeTime) {
					RandomizeTime();
					m_frame = CalculateFrame();
				}

				if (m_animation != null) {
					RenderedSprite = m_animation.Frames[m_frame].Sprite;	
				}
			}
		}

		private void Update()
		{
			if (!m_isPlaying || m_animation == null) {
				return;
			}

			if (m_timer == null) {
				m_time += Time.deltaTime * m_speed;
			} else {
				m_time += m_timer.DeltaTime * m_speed;	
			}
			
			FrameUpdate();
		}

		public void FrameUpdate()
		{
			var prevFrame = m_frame;
			m_frame = CalculateFrame();

			if (prevFrame != m_frame) {
				var frame = m_animation.Frames[m_frame];
				if (frame.Event != null) {
					AnimationEvent?.Invoke(this, frame.Event);	
				}

				RenderedSprite = m_animation.Frames[m_frame].Sprite;
			}
		}

		private int CalculateFrame()
		{
			var totalTime = 0f;

			for (var index = 0; index < m_animation.Frames.Length; index++) {
				var frame = m_animation.Frames[index];
				totalTime += frame.Duration;

				if (m_time < totalTime) {
					return index;
				}
			}

			if (!m_loop) {
				Stop();
				return m_frame;
			}

			m_time = 0;
			return 0;
		}

		private void Stop()
		{
			m_isPlaying = false;
			AnimationEvent?.Invoke(this, new StopEvt());

			if (m_destroyOnStop) {
				Destroy(gameObject);
			}
		}
		
#if UNITY_EDITOR
		protected virtual void OnValidate()
		{
			if (Application.isPlaying || m_sprite == null) {
				return;
			}
			
	
			RenderedSprite = m_sprite.Icon;
			m_animation = m_sprite.Get("icon");
		}
#endif
		public void SetSprite(Sprite sprite)
		{
			RenderedSprite = sprite;
		}
	}
}