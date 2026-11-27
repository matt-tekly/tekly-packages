using UnityEngine;

namespace Tekly.Common.Ui.Styled
{
	/// <summary>
	/// C# copy of the shader's shape distance (StyledShapeDistance in StyledRectCore.cginc), used for hit tests.
	/// Keep the two in sync. The shader also divides a smoothed corner's distance by its slope so strokes keep
	/// their width. That never moves the edge, so it is left out here: away from the edge of a smoothed corner
	/// this distance is approximate.
	/// </summary>
	public static class StyledShapeField
	{
		/// <summary>Signed distance from a point to the shape's edge, negative inside.</summary>
		/// <param name="p">Point relative to the shape center.</param>
		/// <param name="radii">Top-left, top-right, bottom-right, bottom-left, already fitted to the size.</param>
		/// <param name="exponent">Corner exponent: 2 is a circular corner, 4 a squircle.</param>
		public static float Distance(Vector2 p, Vector2 halfSize, Vector4 radii, float exponent)
		{
			// Same corner split as the shader: the midpoint between neighbouring corners' circle centers.
			var splitY = p.x >= 0f ? (radii.z - radii.y) * 0.5f : (radii.w - radii.x) * 0.5f;
			var top = p.y >= splitY;
			var splitX = top ? (radii.x - radii.y) * 0.5f : (radii.w - radii.z) * 0.5f;
			var right = p.x >= splitX;

			var radius = top ? (right ? radii.y : radii.x) : (right ? radii.z : radii.w);
			var a = new Vector2((right ? p.x : -p.x) - halfSize.x + radius, (top ? p.y : -p.y) - halfSize.y + radius);

			var outside = Vector2.Max(a, Vector2.zero);
			var inside = Mathf.Min(Mathf.Max(a.x, a.y), 0f);

			if (radius > 0f && exponent > 2.001f && outside.x + outside.y > 0f) {
				var norm = Mathf.Pow(Mathf.Pow(outside.x, exponent) + Mathf.Pow(outside.y, exponent), 1f / exponent);
				return norm + inside - radius;
			}

			return outside.magnitude + inside - radius;
		}

		/// <summary>
		/// How far the shape's wobble pushes the edge out (positive) or pulls it in at a point. C# copy of
		/// StyledShapeWobbleAt in StyledRectCore.cginc, which StyledRect uses to place the vertices of a wobbly
		/// rect's edge strips. Keep the two in sync.
		/// </summary>
		/// <param name="p">Point relative to the shape center.</param>
		public static float Wobble(Vector2 p, float amount, float lobeLength, float seed)
		{
			const float TAU = Mathf.PI * 2f;
			var k = Mathf.PI / Mathf.Max(lobeLength, 1f);

			var wave0 = Mathf.Sin((p.x * 0.9397f + p.y * 0.3420f) * k + (Fraction(seed * 0.1373f) + 0.13f) * TAU);
			var wave1 = Mathf.Sin((p.x * 0.2588f + p.y * 0.9659f) * k * 1.37f + (Fraction(seed * 0.3719f) + 0.57f) * TAU);
			var wave2 = Mathf.Sin((p.x * -0.7071f + p.y * 0.7071f) * k * 2.23f + (Fraction(seed * 0.7151f) + 0.81f) * TAU);

			return amount * (wave0 + 0.7f * wave1 + 0.25f * wave2) * (1f / 1.95f);
		}

		// The shader's frac: always 0 to 1, for negative values too.
		private static float Fraction(float value)
		{
			return value - Mathf.Floor(value);
		}
	}
}
