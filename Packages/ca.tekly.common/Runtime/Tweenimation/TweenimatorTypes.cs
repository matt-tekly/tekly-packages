using System;
using Tekly.Common.Utils.Tweening;
using UnityEngine;

namespace Tekly.Common.Tweenimation
{
    public enum OriginMode
    {
        Relative,
        Fixed
    }

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