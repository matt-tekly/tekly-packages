//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

using System;
using System.Text;
using Tekly.Webster.Utility;
using UnityEngine;

namespace Tekly.Webster.Routes.GameObjects
{
	[Serializable]
	public class ComponentInfo : IToJson
	{
		public int InstanceId;
		public bool Enabled;
		public bool EnabledSelf;
		public bool CanBeDisabled;
		public string Type;

		private readonly string m_values;
		
		public ComponentInfo(Component component)
		{
			Type = component.GetType().Name;

			InstanceId = component.GetInstanceID();

			if (component is Behaviour) {
				var beh = component as Behaviour;
				Enabled = beh.isActiveAndEnabled;
				EnabledSelf = beh.enabled;
				CanBeDisabled = true;
			} else if (component is Renderer) {
				Enabled = true;
				EnabledSelf = true;
				CanBeDisabled = false;
			} else {
				Enabled = true;
				EnabledSelf = true;
				CanBeDisabled = false;
			}

			m_values = ConvertToValuesJson(component);
		}

		public void ToJson(StringBuilder sb)
		{
			sb.Append("{");
			sb.Write("InstanceId", InstanceId, true);
			sb.Write("Enabled", Enabled, true);
			sb.Write("EnabledSelf", EnabledSelf, true);
			sb.Write("CanBeDisabled", CanBeDisabled, true);
			sb.Write("Type", Type, true);
			sb.Append("\"Values\":");
			sb.Append(m_values);
			sb.Append("}");
		}

		private static string ConvertToValuesJson(Component component)
		{
			try {
				if (component is RectTransform) {
					var transform = component as RectTransform;
					var rect = transform.rect;

					var sb = new StringBuilder();
					sb.Append("{");
					sb.Write("X", rect.x, true);
					sb.Write("Y", rect.y, true);
					sb.Write("Width", rect.width, true);
					sb.Write("Height", rect.height, false);
					sb.Append("}");

					return sb.ToString();
				}

				if (component is Transform) {
					var transform = component as Transform;
					var sb = new StringBuilder();
					sb.Append("{");
					sb.Write("Position", transform.localPosition, true);
					sb.Write("Rotation", transform.localEulerAngles, true);
					sb.Write("Scale", transform.localScale, false);
					sb.Append("}");

					return sb.ToString();
				}

				if (component is Camera) {
					var camera = component as Camera;
					var sb = new StringBuilder();
					sb.Append("{");
					sb.Write("Depth", camera.depth, true);
					sb.Write("ClearFlags", camera.clearFlags.ToString(), true);
					sb.Write("CullingMask", LayerMaskToString(camera.cullingMask), false);
					sb.Append("}");

					return sb.ToString();
				}

				if (component is MonoBehaviour) {
					return JsonUtility.ToJson(component);
				}
			} catch {
				// ignored
			}

			return "{}";
		}

		private static string LayerMaskToString(int mask)
		{
			if (mask == -1) {
				return "Everything";
			}

			var sb = new StringBuilder();
			for (var i = 0; i < 32; i++) {
				if ((mask & i) != 0) {
					if (sb.Length > 0) {
						sb.Append("|");
					}

					sb.Append(LayerMask.LayerToName(i));
				}
			}

			return sb.ToString();
		}
	}
}