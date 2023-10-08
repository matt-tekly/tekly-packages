using UnityEngine;
using UnityEngine.EventSystems;

namespace Tekly.Common.Cameras
{
	public class OrthoCameraBasic : MonoBehaviour
	{
		public Bounds? BoundsOverride { get; set; }
		public Bounds ActiveBounds => BoundsOverride ?? m_defaultBounds;
		public Vector3 MouseWorldPosition => m_camera.ScreenToWorldPoint(Input.mousePosition);
		public float Zoom => m_currentZoom;
		public Camera Camera => m_camera;

		[Header("Camera")]
		[SerializeField] private Camera m_camera;
		[SerializeField] private Transform m_cameraTransform;

		[Header("Zoom")] 
		[SerializeField] private float m_minZoom = 2;
		[SerializeField] private float m_maxZoom = 10;
		[SerializeField] private float m_zoomSpeed = .1f;
		[SerializeField] private float m_zoomTime = 100f;
		[SerializeField] private AnimationCurve m_zoomCurve;

		[Header("Input")] 
		[SerializeField] private int m_mouseButton = 2;

		[Header("Bounds")] 
		[SerializeField] private Bounds m_defaultBounds;
		[SerializeField] private bool m_useBounds = true;

		private float m_destinationZoom;
		private float m_currentZoom;

		private Vector3 m_dragStart;
		private bool m_dragging;

		private void Awake()
		{
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
				m_dragStart = MouseWorldPosition;
			}

			if (m_dragging && Input.GetMouseButton(m_mouseButton)) {
				var difference = m_dragStart - MouseWorldPosition;
				var position = m_cameraTransform.position;
				position += difference;

				if (m_useBounds) {
					m_cameraTransform.position = CameraUtils.ClampToBounds(m_camera, position, ActiveBounds);
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
			var oldMousePos = MouseWorldPosition;
			
			var delta = m_zoomSpeed * m_zoomCurve.Evaluate(m_currentZoom) * -Input.mouseScrollDelta.y;
			
			m_destinationZoom = Mathf.Clamp01(m_destinationZoom + delta);
			m_currentZoom = Mathf.SmoothStep(m_currentZoom, m_destinationZoom, Time.deltaTime * m_zoomTime);
			m_camera.orthographicSize = Mathf.Lerp(m_minZoom, m_maxZoom, m_currentZoom);

			var posDiff = oldMousePos - MouseWorldPosition;
			
			var camPos = m_cameraTransform.position;
			var targetPos = new Vector3(camPos.x + posDiff.x, camPos.y + posDiff.y, camPos.z);
			
			m_cameraTransform.position = targetPos;

			if (m_useBounds) {
				m_cameraTransform.position = CameraUtils.ClampToBounds(m_camera, m_cameraTransform.position, ActiveBounds);
			}
		}

		private static bool IsPointerOverUi()
		{
			var current = EventSystem.current;
			return current != null && current.IsPointerOverGameObject();
		}

#if UNITY_EDITOR
		private void Reset()
		{
			m_defaultBounds = new Bounds(Vector3.zero, new Vector3(5, 10, 0));
			m_zoomCurve = AnimationCurve.Constant(0, 1f, 1f);
		}

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
			var bounds = ActiveBounds;

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