using System.Collections.Generic;
using UnityEngine;

namespace Tekly.Tweenlines.Tracks
{
    /// <summary>
    /// Works out how far a target has to move along a direction to be fully outside a screen area, for
    /// tracks that move things in from or out to off screen. UI targets leave a RectTransform, 3D targets
    /// leave a camera's view. Targets are measured at the pose they have at the off screen moment (position,
    /// rotation and scale from the base tracks) instead of their current pose, so the result doesn't depend
    /// on what a previous animation left behind and a target turned by a rotation track still clears the edge.
    /// Its parents and the area are posed the same way, so a child of a panel that slides or turns measures
    /// against where the panel is at that moment.
    /// </summary>
    internal static class OffScreen
    {
        private const float MARGIN = 0.05f;

        private static readonly Vector3[] s_corners = new Vector3[4];
        private static readonly List<Renderer> s_renderers = new List<Renderer>();

        /// <summary>
        /// Offset along the direction, in the target's parent space, that puts the target fully outside the
        /// area with a small margin. Zero when there is no area to leave.
        /// </summary>
        /// <param name="mixer">The animation's mixer, used to pose the parents and the area</param>
        /// <param name="time">The off screen moment</param>
        /// <param name="target">The moving object</param>
        /// <param name="area">A RectTransform or Canvas for UI, a Camera for 3D. Null uses the root canvas or Main Camera</param>
        /// <param name="direction">Normalized direction in the target's parent space</param>
        /// <param name="position">The target's base local position at the off screen moment</param>
        /// <param name="rotation">The target's base local rotation at the off screen moment</param>
        /// <param name="scale">The target's base local scale at the off screen moment</param>
        public static Vector3 Vector(TweenMixer mixer, float time, Transform target, Component area, Vector3 direction, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var parentMatrix = LocalToWorldAt(mixer, target.parent, time);

            return target is RectTransform rectTransform
                ? OffCanvasVector(mixer, time, rectTransform, parentMatrix, AreaRect(target, area), direction, position, rotation, scale)
                : OffCameraVector(target, parentMatrix, AreaCamera(area), direction, position, rotation, scale);
        }

        /// <summary>
        /// The target's base local rotation at a time, from a rotation track when one drives it or the current
        /// rotation otherwise.
        /// </summary>
        public static Quaternion RotationAt(TweenMixer mixer, Transform target, float time)
        {
            return Quaternion.Euler(mixer.BaseAt(target, TweenChannels.LocalRotation, time));
        }

        /// <summary>
        /// A transform's local to world matrix built from each level's base pose at a time. Identity for null.
        /// </summary>
        private static Matrix4x4 LocalToWorldAt(TweenMixer mixer, Transform transform, float time)
        {
            var matrix = Matrix4x4.identity;

            while (transform != null) {
                var position = mixer.BaseAt(transform, TweenChannels.LocalPosition, time);

                // Anchored and local positions differ by a constant, so an anchored change moves it the same
                if (transform is RectTransform rectTransform) {
                    position += (Vector3) (mixer.BaseAt(rectTransform, TweenChannels.AnchoredPosition, time) - rectTransform.anchoredPosition);
                }

                var rotation = RotationAt(mixer, transform, time);
                var scale = mixer.BaseAt(transform, TweenChannels.LocalScale, time);

                matrix = Matrix4x4.TRS(position, rotation, scale) * matrix;
                transform = transform.parent;
            }

            return matrix;
        }

        /// <summary>
        /// The target's base position at a time in its parent's space, from the anchored slot when it has one or
        /// the position slot otherwise.
        /// </summary>
        public static Vector3 PositionAt(Transform target, ChannelSlot<Vector2> anchoredSlot, ChannelSlot<Vector3> positionSlot, float time)
        {
            if (anchoredSlot != null) {
                // Anchored and local positions differ by a constant, so convert through the current pair
                var anchored = anchoredSlot.SampleBase(time);
                var rectTransform = (RectTransform) target;
                return target.localPosition + (Vector3) (anchored - rectTransform.anchoredPosition);
            }

            return positionSlot.SampleBase(time);
        }

        /// <summary>
        /// The rect a UI target must leave: the area (a RectTransform, or a Canvas's rect) or the root canvas.
        /// </summary>
        public static RectTransform AreaRect(Transform target, Component area)
        {
            switch (area) {
                case RectTransform rectTransform:
                    return rectTransform;
                case Canvas areaCanvas:
                    return (RectTransform) areaCanvas.transform;
            }

            var canvas = target.GetComponentInParent<Canvas>();
            return canvas != null ? (RectTransform) canvas.rootCanvas.transform : null;
        }

        public static Camera AreaCamera(Component area)
        {
            return area is Camera camera ? camera : Camera.main;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Why the area won't work for the target, or null when it will.
        /// </summary>
        public static string GetAreaWarning(Transform target, Component area)
        {
            var isUi = target is RectTransform;

            if (isUi && area != null && !(area is RectTransform) && !(area is Canvas)) {
                return $"Off Screen Area [{area.GetType().Name}] should be a RectTransform or Canvas for UI";
            }

            if (!isUi && area != null && !(area is Camera)) {
                return $"Off Screen Area [{area.GetType().Name}] should be a Camera for 3D objects";
            }

            return null;
        }
#endif

        /// <summary>
        /// Distance along the direction that puts the element fully outside the area rect, with a small margin.
        /// </summary>
        private static Vector3 OffCanvasVector(TweenMixer mixer, float time, RectTransform target, Matrix4x4 parentMatrix, RectTransform area, Vector3 direction, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (area == null) {
                return Vector3.zero;
            }

            area.GetLocalCorners(s_corners);
            var areaToParent = parentMatrix.inverse * LocalToWorldAt(mixer, area, time);
            var areaMax = float.MinValue;
            foreach (var corner in s_corners) {
                areaMax = Mathf.Max(areaMax, Vector3.Dot(areaToParent.MultiplyPoint3x4(corner), direction));
            }

            var rect = target.rect;
            var elementMin = float.MaxValue;
            var elementMax = float.MinValue;

            for (var i = 0; i < 4; i++) {
                var local = new Vector3(i < 2 ? rect.xMin : rect.xMax, i % 2 == 0 ? rect.yMin : rect.yMax, 0);
                var projected = Vector3.Dot(position + rotation * Vector3.Scale(local, scale), direction);

                elementMin = Mathf.Min(elementMin, projected);
                elementMax = Mathf.Max(elementMax, projected);
            }

            var margin = (elementMax - elementMin) * MARGIN;
            var distance = areaMax - elementMin + margin;

            return distance > 0 ? direction * distance : Vector3.zero;
        }

        /// <summary>
        /// Distance along the direction that puts the object's renderers fully outside the camera's view.
        /// </summary>
        private static Vector3 OffCameraVector(Transform target, Matrix4x4 parentMatrix, Camera camera, Vector3 direction, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (camera == null) {
                return Vector3.zero;
            }

            var bounds = PoseBounds(target, parentMatrix, position, rotation, scale);
            var worldDirection = parentMatrix.MultiplyVector(direction);

            // Grow until hidden, then narrow down to the closest hidden distance
            var far = Mathf.Max(0.1f, bounds.extents.magnitude);
            var steps = 0;

            while (!IsHidden(camera, bounds, worldDirection, far)) {
                far *= 2;

                if (++steps > 30) {
                    return Vector3.zero;
                }
            }

            var near = 0f;
            for (var i = 0; i < 20; i++) {
                var middle = (near + far) * 0.5f;

                if (IsHidden(camera, bounds, worldDirection, middle)) {
                    far = middle;
                } else {
                    near = middle;
                }
            }

            return direction * (far * (1 + MARGIN));
        }

        /// <summary>
        /// World bounds of the target's renderers as if it had the given local pose.
        /// </summary>
        private static Bounds PoseBounds(Transform target, Matrix4x4 parentMatrix, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var targetMatrix = parentMatrix * Matrix4x4.TRS(position, rotation, scale);
            var bounds = new Bounds(targetMatrix.MultiplyPoint3x4(Vector3.zero), Vector3.zero);

            target.GetComponentsInChildren(s_renderers);

            foreach (var renderer in s_renderers) {
                var matrix = targetMatrix * RelativeMatrix(target, renderer.transform);
                var local = renderer.localBounds;
                var min = local.min;
                var max = local.max;

                for (var i = 0; i < 8; i++) {
                    var corner = new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z);
                    bounds.Encapsulate(matrix.MultiplyPoint3x4(corner));
                }
            }

            s_renderers.Clear();
            return bounds;
        }

        /// <summary>
        /// A child's transform relative to the target, built from local values so a zero target scale works.
        /// </summary>
        private static Matrix4x4 RelativeMatrix(Transform target, Transform child)
        {
            var matrix = Matrix4x4.identity;

            while (child != null && child != target) {
                matrix = Matrix4x4.TRS(child.localPosition, child.localRotation, child.localScale) * matrix;
                child = child.parent;
            }

            return matrix;
        }

        private static bool IsHidden(Camera camera, Bounds bounds, Vector3 worldDirection, float distance)
        {
            bounds.center += worldDirection * distance;
            return IsOutsideView(camera, bounds);
        }

        private static bool IsOutsideView(Camera camera, Bounds bounds)
        {
            var min = bounds.min;
            var max = bounds.max;
            var left = true;
            var right = true;
            var below = true;
            var above = true;
            var behind = true;

            for (var i = 0; i < 8; i++) {
                var corner = new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z);
                var viewport = camera.WorldToViewportPoint(corner);

                left &= viewport.x < 0;
                right &= viewport.x > 1;
                below &= viewport.y < 0;
                above &= viewport.y > 1;
                behind &= viewport.z < 0;
            }

            return left || right || below || above || behind;
        }
    }
}
