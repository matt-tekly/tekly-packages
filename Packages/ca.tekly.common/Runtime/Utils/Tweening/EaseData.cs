using System;
using UnityEngine;

namespace Tekly.Common.Utils.Tweening
{
    [Serializable]
    public struct EaseData
    {
        public bool UseCurve;
        public Ease Ease;
        public AnimationCurve Curve;

        public float Evaluate(float time)
        {
            return UseCurve ? Curve.Evaluate(time) : Easing.Evaluate(time, Ease);
        }
    }
}