using UnityEngine;
using UnityEngine.EventSystems;

namespace Tekly.Common.Cameras
{
	public class Basic2dCamera : MonoBehaviour
	{
		public Bounds Bounds { get; set; }

		[SerializeField] private Camera m_camera;
		[SerializeField] private Transform m_cameraTransform;

		[Header("Zoom")]
		[SerializeField] private float m_minZoom = 2;
		[SerializeField] private float m_maxZoom = 5;
		[SerializeField] private float m_zoomSpeed = 1;
		[SerializeField] private float m_zoomTime = 0.1f;
		
		[Header("Input")]
		[SerializeField] private int m_mouseButton;

		[Header("Bounds")]
		[SerializeField] private Bounds m_defaultBounds;
		[SerializeField] private bool m_useBounds = true;

		private float m_destinationZoom;
		private float m_currentZoom;

		private Vector3 m_dragStart;
		private bool m_dragging;

		private void Awake()
		{
			Bounds = m_defaultBounds;
			AdjustToCameraProperties();
		}

		private void OnEnable()
		{
			AdjustToCameraProperties();
		}

		private void AdjustToCameraProperties()
		{
			m_destinationZoom = Mathf.InverseLerp(m_minZoom, m_maxZoom, m_camera.orthographicSize);
			m_currentZoom = m_destinationZoom;
		}

		private void Update()
		{
			UpdateDrag();
			UpdateZoom();
		}
		
		private void UpdateDrag()
		{
			if (!IsPointerOverUi() && Input.GetMouseButtonDown(m_mouseButton)) {
				m_dragging = true;
				m_dragStart = m_camera.ScreenToWorldPoint(Input.mousePosition);
			}

			if (m_dragging && Input.GetMouseButton(m_mouseButton)) {
				var difference = m_dragStart - m_camera.ScreenToWorldPoint(Input.mousePosition);
				var position = m_cameraTransform.position;
				position += difference;

				if (m_useBounds) {
					m_cameraTransform.position = CameraUtils.ClampToBounds(m_camera, position, Bounds);
				} else {
					m_cameraTransform.position = position;
				}
			}

			if (!Input.GetMouseButton(m_mouseButton)) {
				m_dragging = false;
			}
		}

		private void UpdateZoom()
		{
			var delta = m_zoomSpeed * -Input.mouseScrollDelta.y;
			m_destinationZoom = Mathf.Clamp01(m_destinationZoom + delta);

			m_currentZoom = Mathf.SmoothStep(m_currentZoom, m_destinationZoom, Time.deltaTime * m_zoomTime);
				
			m_camera.orthographicSize = Mathf.Lerp(m_minZoom, m_maxZoom, m_currentZoom);
			
			if (m_useBounds) {
				m_cameraTransform.position = CameraUtils.ClampToBounds(m_camera, m_cameraTransform.position, Bounds);
			}
		}
		
		private static bool IsPointerOverUi()
		{
			var current = EventSystem.current;
			return current != null && current.IsPointerOverGameObject();
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (m_camera == null) {
				m_camera = GetComponent<Camera>();
			}

			if (m_cameraTransform == null && m_camera != null) {
				m_cameraTransform = m_camera.transform;
			}
		}

		private void OnDrawGizmos()
		{
			var bounds = Application.isPlaying ? Bounds : m_defaultBounds;

			var min = bounds.min;
			var max = bounds.max;

			Gizmos.color = Color.yellow;

			Gizmos.DrawLine(min, new Vector3(min.x, max.y));
			Gizmos.DrawLine(min, new Vector3(max.x, min.y));

			Gizmos.DrawLine(max, new Vector3(min.x, max.y));
			Gizmos.DrawLine(max, new Vector3(max.x, min.y));
		}
#endif
	}
}