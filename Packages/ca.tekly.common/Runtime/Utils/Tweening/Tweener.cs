using System;
using System.Collections;
using UnityEngine;

namespace Tekly.Common.Utils.Tweening
{
    [Serializable]
    public struct TweenSettings
    {
        public float Duration;
        public Ease Ease;
    }
	
    public abstract class Tweener
    {
        public abstract void Tween(float ratio);
        public virtual void Start(TweenSettings settings) {}
        public virtual void Finish() {}
    }

    public class TweenRunner
    {
        private MonoBehaviour m_container;
        private Coroutine m_coroutine;

        public bool Active { get; private set; }

        public TweenRunner(MonoBehaviour container)
        {
            m_container = container;
        }

        public void StartTween(Tweener tweener, TweenSettings settings)
        {
            StopTween();

            if (!m_container.gameObject.activeInHierarchy) {
                tweener.Tween(1.0f);
                return;
            }

            m_coroutine = m_container.StartCoroutine(Start(tweener, settings));
        }

        public void StopTween()
        {
            if (m_coroutine != null) {
                m_container.StopCoroutine(m_coroutine);
                m_coroutine = null;
            }

            Active = false;
        }

        private IEnumerator Start(Tweener tweener, TweenSettings settings)
        {
            Active = true;

            var elapsedTime = 0.0f;
            tweener.Start(settings);

            while (elapsedTime < settings.Duration) {
                elapsedTime += Time.deltaTime;

                var percentage = Mathf.Clamp01(elapsedTime / settings.Duration);
                var ratio = Easing.Evaluate(percentage, settings.Ease);
                tweener.Tween(ratio);
                yield return null;
            }

            tweener.Finish();

            Active = false;
        }
    }
}