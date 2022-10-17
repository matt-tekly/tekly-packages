using System;
using UnityEngine;

namespace Tekly.Common.Tweenimation.Tweens
{
    [Serializable]
    public class CanvasGroupTween : BasicTween
    {
        [SerializeField] private CanvasGroup m_canvasGroup;
        
        protected override void OnEvaluate(float time)
        {
            throw new NotImplementedException();
        }
    }
}