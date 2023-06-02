using UnityEngine;

namespace Tekly.Common.Cameras
{
	public static class CameraUtils
	{
		public static Vector3 ClampToBounds(Camera camera, Vector3 position, Bounds bounds)
		{
			var min = bounds.min;
			var max = bounds.max;

			var orthographicSize = camera.orthographicSize;
			
			var cameraHeight = orthographicSize;
			var cameraWidth = orthographicSize * camera.aspect;

			var minX = min.x + cameraWidth;
			var maxX = max.x - cameraWidth;
			var minY = min.y + cameraHeight;
			var maxY = max.y - cameraHeight;

			var clampX = Mathf.Clamp(position.x, minX, maxX);
			var clampY = Mathf.Clamp(position.y, minY, maxY);

			return new Vector3(clampX, clampY, position.z);
		}
	}
}