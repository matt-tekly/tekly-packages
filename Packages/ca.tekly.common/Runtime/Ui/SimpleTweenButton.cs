using System;
using Tekly.Common.Utils.Tweening;
using UnityEngine;

namespace Tekly.Common.Ui
{
    public class SimpleButtonTweener : Tweener
    {
        private readonly SimpleTweenButton m_button;
        private readonly bool m_inverse;

        public SimpleButtonTweener(SimpleTweenButton button, bool inverse)
        {
            m_button = button;
            m_inverse = inverse;
        }

        public override void Tween(float ratio)
        {
            if (m_inverse) {
                m_button.PressedRatio = Mathf.Lerp(0, 1, 1 - ratio);
            } else {
                m_button.PressedRatio = Mathf.Lerp(0, 1, ratio);
            }
        }
    }

    public abstract class SimpleTweenButton : ButtonBase
    {
        [SerializeField] private TweenSettings m_upTween;
        [SerializeField] private TweenSettings m_downTween;
        [SerializeField] private Color m_disabledColor = Color.white;

        private TweenRunner m_tweenRunner;
        private SimpleButtonTweener m_upTweener;
        private SimpleButtonTweener m_downTweener;

        private float m_pressedRatio;

        public float PressedRatio {
            get => m_pressedRatio;
            set {
                if (!Mathf.Approximately(m_pressedRatio, value)) {
                    m_pressedRatio = value;
                    SetPressedRatio(m_pressedRatio);
                }
            }
        }

        protected SimpleTweenButton()
        {
            m_tweenRunner = new TweenRunner(this);
            m_upTweener = new SimpleButtonTweener(this, true);
            m_downTweener = new SimpleButtonTweener(this, false);
        }

        protected abstract void SetPressedRatio(float ratio);

        protected override void DoStateTransition(ButtonState state, bool instant)
        {
            switch (state) {
                case ButtonState.Up:
                    if (m_upTween.Duration > 0 && !instant) {
                        m_tweenRunner.StartTween(m_upTweener, m_upTween);
                    } else {
                        m_tweenRunner.StopTween();
                        PressedRatio = 0;
                    }

                    SetRendererColor(Color.white);
                    break;
                case ButtonState.Down:
                    if (m_downTween.Duration > 0 && !instant) {
                        m_tweenRunner.StartTween(m_downTweener, m_downTween);
                    } else {
                        m_tweenRunner.StopTween();
                        PressedRatio = 1;
                    }

                    SetRendererColor(Color.white);
                    break;
                case ButtonState.Disabled:
                    m_tweenRunner.StopTween();
                    PressedRatio = 1;

                    SetRendererColor(m_disabledColor);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        protected virtual void SetRendererColor(Color color) { }
    }
}