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
	}
}
