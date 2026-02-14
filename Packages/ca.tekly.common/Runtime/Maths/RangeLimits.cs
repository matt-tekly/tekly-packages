using System;
using UnityEngine;

namespace Tekly.Common.Maths
{
	[AttributeUsage(AttributeTargets.Field)]
	public sealed class RangeLimitsAttribute : PropertyAttribute
	{
		public float AllowedMin { get; }
		public float AllowedMax { get; }
		public bool DrawSlider { get; }

		public RangeLimitsAttribute(float allowedMin, float allowedMax, bool drawSlider = true)
		{
			AllowedMin = allowedMin;
			AllowedMax = allowedMax;
			DrawSlider = drawSlider;
		}
	}
}