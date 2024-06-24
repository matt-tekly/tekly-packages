using UnityEngine;
using UnityEngine.Rendering;

namespace Tekly.TwoD
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(MeshFilter))]
	[RequireComponent(typeof(MeshRenderer))]
	[RequireComponent(typeof(SortingGroup))]
	public class SpriteShape : MonoBehaviour
	{
		[SerializeField] private MeshFilter m_meshFilter;
		[SerializeField] private MeshRenderer m_meshRenderer;
		
		[SerializeField] private Color m_color = Color.white;
		
		[Tooltip("How many squares the texture will be broken into")]
		[SerializeField] private int m_squares = 1;
		
		[Header("Corner Offsets")]
		[SerializeField] private Vector2 m_upperLeft;
		[SerializeField] private Vector2 m_upperRight;
		[SerializeField] private Vector2 m_lowerLeft;
		[SerializeField] private Vector2 m_lowerRight;
		
		private Mesh m_mesh;
		private bool m_dirty;

		private void Update()
		{
			if (m_dirty) {
				UpdateMesh();
			}
		}

		private void OnValidate()
		{
			m_dirty = true;
			m_squares = Mathf.Clamp(m_squares, 1, 100);

			m_meshFilter = GetComponent<MeshFilter>();
			m_meshRenderer = GetComponent<MeshRenderer>();
		}

		private void UpdateMesh()
		{
			m_dirty = false;
			
			if (m_mesh == null) {
				m_mesh = new Mesh { hideFlags = HideFlags.HideAndDontSave };
				m_mesh.MarkDynamic();
			}
			
			m_meshFilter.sharedMesh = m_mesh;

			var texture = m_meshRenderer.sharedMaterial.mainTexture;
			var width = texture.width;
			var height = texture.height;
			
			var bottomLeftCorner = new Vector3(0, 0) + (Vector3)m_lowerLeft;
			var bottomRightCorner = new Vector3(width, 0) + (Vector3)m_lowerRight;
			var topLeftCorner = new Vector3(0, height) + (Vector3)m_upperLeft;
			var topRightCorner = new Vector3(width, height) + (Vector3)m_upperRight;
			
			var newVertices = new Vector3[(m_squares + 1) * (m_squares + 1)];
			var colors = new Color[(m_squares + 1) * (m_squares + 1)];
			var uvs = new Vector2[newVertices.Length];
			var triangles = new int[m_squares * m_squares * 6];
			
			for (var y = 0; y <= m_squares; y++) {
				
				var yRatio = (float)y / m_squares;
				
				var left = Vector3.Lerp(bottomLeftCorner, topLeftCorner, yRatio);
				var right = Vector3.Lerp(bottomRightCorner, topRightCorner, yRatio);

				for (var x = 0; x <= m_squares; x++) {
					var xRatio = (float)x / m_squares;
					var index = y * (m_squares + 1) + x;
					newVertices[index] = Vector3.Lerp(left, right, xRatio);
					uvs[index] = new Vector2(xRatio, yRatio);
					colors[index] = m_color;
				}
			}
			
			var triangleIndex = 0;
			for (var y = 0; y < m_squares; y++) {
				for (var x = 0; x < m_squares; x++) {
					var vertexIndex = y * (m_squares + 1) + x;
					
					triangles[triangleIndex++] = vertexIndex;
					triangles[triangleIndex++] = vertexIndex + m_squares + 1;
					triangles[triangleIndex++] = vertexIndex + m_squares + 2;
					
					triangles[triangleIndex++] = vertexIndex;
					triangles[triangleIndex++] = vertexIndex + m_squares + 2;
					triangles[triangleIndex++] = vertexIndex + 1;
				}
			}

			m_mesh.vertices = newVertices;
			m_mesh.uv = uvs;
			m_mesh.colors = colors;
			m_mesh.triangles = triangles;
			m_mesh.RecalculateNormals();

			m_mesh.UploadMeshData(false);
		}
	}
}