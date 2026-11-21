#if TWEENLINE_UGUI
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Tweenlines
{
    /// <summary>
    /// Colors combine per component, so a layer multiplies as a tint and adds as a shift.
    /// </summary>
    public abstract class ColorChannel : TweenChannel<Color>
    {
        public override Color One => Color.white;
        public override Color Zero => Color.clear;
        public override Color Multiply(Color a, Color b) => a * b;
        public override Color Add(Color a, Color b) => a + b;
    }

    /// <summary>
    /// A Graphic's color, like an Image's or Text's tint and alpha.
    /// </summary>
    public sealed class GraphicColorChannel : ColorChannel
    {
        public override string Name => "color";
        public override Color Read(Object target) => ((Graphic) target).color;
        public override void Write(Object target, Color value) => ((Graphic) target).color = value;

#if UNITY_EDITOR
        public override void CollectDrivenProperties(Object target, List<DrivenProperty> properties)
        {
            properties.Add(new DrivenProperty(target, "m_Color.r"));
            properties.Add(new DrivenProperty(target, "m_Color.g"));
            properties.Add(new DrivenProperty(target, "m_Color.b"));
            properties.Add(new DrivenProperty(target, "m_Color.a"));
        }
#endif
    }

    public sealed class ImageFillAmountChannel : FloatChannel
    {
        public override string Name => "fillAmount";
        public override float Read(Object target) => ((Image) target).fillAmount;
        public override void Write(Object target, float value) => ((Image) target).fillAmount = value;

#if UNITY_EDITOR
        public override void CollectDrivenProperties(Object target, List<DrivenProperty> properties)
        {
            properties.Add(new DrivenProperty(target, "m_FillAmount"));
        }
#endif
    }
}
#endif
