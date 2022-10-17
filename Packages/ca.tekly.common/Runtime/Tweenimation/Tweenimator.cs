using System;
using System.Collections;
using System.Collections.Generic;
using Tekly.Common.Tweenimation.Tweens;
using UnityEngine;
using UnityEngine.Events;

namespace Tekly.Common.Tweenimation
{
    public class Tweenimator : MonoBehaviour
    {
        [SerializeField] private float m_delay;
        [SerializeField] private bool m_playOnEnable;
        [SerializeReference] private List<BaseTween> m_tweens = new List<BaseTween>();
        [SerializeField] private UnityEvent m_completed = new UnityEvent();

        public float TotalTime => m_delay + AnimationDuration;
        public bool IsPlaying => m_coroutine != null;
        
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
            Play(m_delay, reinitialize);
        }
        
        public void Play(float delay, bool reinitialize = false)
        {
            Kill();
            
            foreach (var basicTween in m_tweens) {
                basicTween.Play(reinitialize);
            }

            m_coroutine = StartCoroutine(Animate(delay));
        }
        
        public void Evaluate(float time)
        {
            foreach (var tween in m_tweens) {
                tween.Evaluate(time);
            }
        }

        private IEnumerator Animate(float delay)
        {
            if (delay > 0) {
                yield return new WaitForSeconds(delay);
            }

            var totalDuration = AnimationDuration;
            var timer = 0f;

            while (timer <= totalDuration) {
                timer += Time.deltaTime;

                Evaluate(timer);

                yield return null;
            }

            m_completed?.Invoke();
            m_coroutine = null;
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