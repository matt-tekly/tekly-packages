using System.Collections.Generic;
using Tekly.Leaf;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Tekly.DevBoard
{
	/// <summary>
	/// Shows the cursor Leaf asks for (<see cref="LeafCore.Cursor"/>), for projects that don't draw their own. On
	/// WebGL it sets the browser's own cursor on the canvas; elsewhere it uses the cursor textures from the
	/// DevBoardAssets added so far, and the default cursor for any it has no texture for. Off unless DevBoardInit's
	/// Apply Leaf Cursor is on: a game with its own cursor listens to LeafCore.Cursor itself instead.
	/// </summary>
	internal sealed class DevBoardCursor
	{
		private readonly Dictionary<LeafCursor, CursorTexture> m_textures = new();
		private bool m_enabled;

		public bool Enabled {
			get => m_enabled;
			set {
				if (m_enabled == value) {
					return;
				}

				m_enabled = value;

				if (value) {
					LeafCore.Instance.Cursor.Changed += Apply;
					Apply(LeafCore.Instance.Cursor.Current);
				} else {
					LeafCore.Instance.Cursor.Changed -= Apply;
					Apply(LeafCursor.None);
				}
			}
		}

		public void AddTextures(CursorTexture[] textures)
		{
			if (textures == null) {
				return;
			}

			foreach (var texture in textures) {
				if (texture != null && texture.Texture != null) {
					m_textures[texture.Cursor] = texture;
				}
			}
		}

		private void Apply(LeafCursor cursor)
		{
#if UNITY_WEBGL && !UNITY_EDITOR
			DevBoard_SetCursor(CssName(cursor));
#else
			if (cursor != LeafCursor.None && m_textures.TryGetValue(cursor, out var texture)) {
				Cursor.SetCursor(texture.Texture, texture.Hotspot, CursorMode.Auto);
			} else {
				Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
			}
#endif
		}

#if UNITY_WEBGL && !UNITY_EDITOR
		[DllImport("__Internal")]
		private static extern void DevBoard_SetCursor(string cursor);

		/// <summary>
		/// The CSS cursor for each of Leaf's, so the browser shows its own.
		/// </summary>
		private static string CssName(LeafCursor cursor)
		{
			switch (cursor) {
				case LeafCursor.Pointer:
					return "pointer";
				case LeafCursor.Text:
					return "text";
				case LeafCursor.Grab:
					return "grab";
				case LeafCursor.ResizeHorizontal:
					return "ew-resize";
				case LeafCursor.ResizeVertical:
					return "ns-resize";
				default:
					return "";
			}
		}
#endif
	}
}
