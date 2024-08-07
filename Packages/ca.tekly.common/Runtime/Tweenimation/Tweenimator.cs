using System.Collections;
using System.Collections.Generic;
using Tekly.Common.Tweenimation.Tweens;
using UnityEngine;
using UnityEngine.Events;

namespace Tekly.Common.Tweenimation
{
    public class Tweenimator : MonoBehaviour
    {
        [SerializeField] private string m_name;
        [SerializeField] private bool m_playOnEnable = true;
        
        [SerializeField] private float m_delay;
        [SerializeField] private bool m_siblingIndexDelay;
        [SerializeField] private bool m_useUnscaledTime;
        [SerializeField] private int m_loops;
        
        [SerializeReference] private List<BaseTween> m_tweens = new List<BaseTween>();
        [SerializeField] private UnityEvent m_completed = new UnityEvent();

        public float TotalTime => m_delay + AnimationDuration;
        public bool IsPlaying => m_coroutine != null;
        public string Name => m_name;
        
        public float AnimationDuration {
            get {
                float time = 0;
                foreach (var tween in m_tweens) {
                    time = Mathf.Max(tween.TotalTime, time);
                }

                return time;
            }
        }
        
        private Coroutine m_coroutine;
        private int m_loopCount;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            foreach (var tween in m_tweens) {
                tween.Initialize(this);
            }
        }

        private void OnEnable()
        {
            if (m_playOnEnable) {
                Play();
            }
        }

        public void Play(bool reinitialize = false)
        {
            if (m_siblingIndexDelay) {
                Play(m_delay * transform.GetSiblingIndex(), reinitialize);
            } else {
                Play(m_delay, reinitialize);    
            }
        }
        
        public void Play(float delay, bool reinitialize = false)
        {
            Kill();

            m_loopCount = m_loops;
            PlayInternal(delay, reinitialize);
        }

        private void PlayInternal(float delay, bool reinitialize = false)
        {
            foreach (var basicTween in m_tweens) {
                basicTween.Play(reinitialize);
            }

            m_coroutine = StartCoroutine(Animate(delay));
        }

        public void Stop()
        {
            Kill();
        }
        
        public void Evaluate(float time)
        {
            foreach (var tween in m_tweens) {
                tween.Evaluate(time);
            }
        }

        private IEnumerator Animate(float delay)
        {
            while (delay > 0) {
                delay -= m_useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }

            var totalDuration = AnimationDuration;
            var timer = 0f;

            while (timer <= totalDuration) {
                timer += m_useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

                Evaluate(timer);

                yield return null;
            }

            m_loopCount--;
            
            if (m_loops == -1) {
                PlayInternal(0);
            } else {
                if (m_loopCount < 0) {
                    m_completed?.Invoke();
                    m_coroutine = null;                    
                } else {
                    PlayInternal(0);
                }
            }
        }

        private void Kill()
        {
            if (m_coroutine != null) {
                StopCoroutine(m_coroutine);
                m_coroutine = null;
            }
        }

        private void Reset()
        {
            if (m_tweens == null) {
                m_tweens = new List<BaseTween>();
            }

            if (m_tweens.Count == 0) {
                m_tweens.Add(new TranslationTween());
            }
        }

        private void OnValidate()
        {
            if (m_completed == null) {
                m_completed = new UnityEvent();
            }
        }
    }
}