using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Common.Ui.Effects
{
	public static class GradientMeshUtils
	{
		private static readonly List<UIVertex> s_triangles = new List<UIVertex>();
		private static readonly List<UIVertex> s_workingList = new List<UIVertex>();
		
		public static void CutMesh(VertexHelper vh, float rotation, Rect rect, HashSet<float> cuts)
		{
			s_triangles.Clear();
			s_workingList.Clear();
			
			vh.GetUIVertexStream(s_triangles);
			vh.Clear();
			
			var direction = (Vector2Utils.UnitRotated(-rotation) / rect.size).Rotate90();

			foreach (var cut in cuts) {
				s_workingList.Clear();

				var point = GetCutOrigin(rotation, rect, cut);

				for (var i = 0; i < s_triangles.Count; i += 3) {
					CutTriangle(s_triangles, i, s_workingList, direction, point);
				}

				s_triangles.Clear();
				s_triangles.AddRange(s_workingList);
			}

			vh.AddUIVertexTriangleStream(s_triangles);
		}

		private static void CutTriangle(List<UIVertex> tris, int idx, List<UIVertex> outList, Vector2 cutDirection, Vector2 point)
		{
			var a = tris[idx];
			var b = tris[idx + 1];
			var c = tris[idx + 2];

			var ab = ProjectRayToLine(a.position, b.position, point, cutDirection);
			var bc = ProjectRayToLine(b.position, c.position, point, cutDirection);
			var ca = ProjectRayToLine(c.position, a.position, point, cutDirection);

			if (IsWithinLine(ab)) {
				if (IsWithinLine(bc)) {
					var pab = Lerp(a, b, ab);
					var pbc = Lerp(b, c, bc);
					Add(outList, a, pab, c, pab, pbc, c, pab, b, pbc);
				} else {
					var pab = Lerp(a, b, ab);
					var pca = Lerp(c, a, ca);
					Add(outList, c, pca, b, pca, pab, b, pca, a, pab);
				}
			} else if (IsWithinLine(bc)) {
				var pbc = Lerp(b, c, bc);
				var pca = Lerp(c, a, ca);
				Add(outList, b, pbc, a, pbc, pca, a, pbc, c, pca);
			} else {
				outList.Add(tris[idx]);
				outList.Add(tris[idx + 1]);
				outList.Add(tris[idx + 2]);
			}
		}
		
		private static UIVertex Lerp(UIVertex v1, UIVertex v2, float t)
		{
			return new UIVertex {
				position = Vector3.Lerp(v1.position, v2.position, t),
				color = Color.Lerp(v1.color, v2.color, t),
				uv0 = Vector2.Lerp(v1.uv0, v2.uv0, t),
				uv1 = Vector2.Lerp(v1.uv1, v2.uv1, t),
				uv2 = Vector2.Lerp(v1.uv2, v2.uv2, t),
				uv3 = Vector2.Lerp(v1.uv3, v2.uv3, t),
				
				// TODO: Are these necessary?
				normal = Vector3.Lerp(v1.normal, v2.normal, t),
				tangent = Vector4.Lerp(v1.tangent, v2.tangent, t)
			};
		}

		private static void Add(List<UIVertex> list, UIVertex a, UIVertex b, UIVertex c, UIVertex d, UIVertex e, UIVertex f, UIVertex g, UIVertex h, UIVertex i)
		{
			list.Add(a);
			list.Add(b);
			list.Add(c);
			list.Add(d);
			list.Add(e);
			list.Add(f);
			list.Add(g);
			list.Add(h);
			list.Add(i);
		}

		private static bool IsWithinLine(float f)
		{
			return f > 0 && f <= 1;
		}

		private static float ProjectRayToLine(Vector2 p1, Vector2 p2, Vector2 rayOrigin, Vector2 rayDirection)
		{
			var lineDir = p2 - p1;
			var det = lineDir.x * rayDirection.y - lineDir.y * rayDirection.x;
			
			// They don't intersect
			if (det == 0) {
				return -1;
			}
			 
			return ((rayOrigin.x - p1.x) * rayDirection.y - (rayOrigin.y - p1.y) * rayDirection.x) / det;
		}

		private static Vector2 GetCutOrigin(float rotation, Rect rect, float ratio)
		{
			var size = rect.size;
			var v = Vector2Utils.UnitRotated(-rotation) / size;
			
			Vector3 p1, p2;

			if (rotation % 180 < 90) {
				p1 = Vector3.Project(Vector2.Scale(size, new Vector2(-0.5f, -0.5f)), v);
				p2 = Vector3.Project(Vector2.Scale(size, new Vector2(0.5f, 0.5f)), v);
			} else {
				p1 = Vector3.Project(Vector2.Scale(size, new Vector2(-0.5f, 0.5f)), v);
				p2 = Vector3.Project(Vector2.Scale(size, new Vector2(0.5f, -0.5f)), v);
			}

			if (rotation % 360 >= 180) {
				return Vector2.Lerp(p2, p1, ratio) + rect.center;
			}

			return Vector2.Lerp(p1, p2, ratio) + rect.center;
		}
	}
}