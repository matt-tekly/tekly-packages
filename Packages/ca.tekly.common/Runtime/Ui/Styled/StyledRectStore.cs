using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using GpuBlendMode = UnityEngine.Rendering.BlendMode;

namespace Tekly.Common.Ui.Styled
{
	/// <summary>
	/// Holds every StyledRect's values in one float texture that the shader reads by address. Changing a value
	/// is a write into an array, with no mesh rebuild, and at most one texture upload per frame, after the
	/// canvas rebuild. Every rect shares one material so they batch.
	///
	/// An address is a texel index counted row by row. The width never changes, so addresses survive growth.
	/// Nothing here exists until the first StyledRect is enabled.
	///
	/// There is one store for edit mode and play mode alike, since graphics draw in both. It is not a Singleton
	/// because that only exists in play mode. It needs no reset between play sessions: every rect returns its
	/// texels when it is disabled, and the store starts over once the last one has.
	/// </summary>
	public class StyledRectStore : IDisposable
	{
		public const int WIDTH = 256;

		private const int START_HEIGHT = 16;
		private const string SHADER_RESOURCE = "Shaders/StyledRect";
		private const string EXTENDED_SHADER_RESOURCE = "Shaders/StyledRectExtended";

		private static readonly int s_dataId = Shader.PropertyToID("_TeklyStyledData");
		private static readonly int s_dataWidthId = Shader.PropertyToID("_TeklyStyledDataWidth");
		private static readonly int s_srcBlendId = Shader.PropertyToID("_SrcBlend");
		private static readonly int s_dstBlendId = Shader.PropertyToID("_DstBlend");
		private static readonly int s_blendColorOutputId = Shader.PropertyToID("_BlendColorOutput");
		private static readonly int s_blendAlphaOutputId = Shader.PropertyToID("_BlendAlphaOutput");

		private const int BLEND_MODE_COUNT = 4;

		// What the shader writes for a blend mode. Color: premultiplied, or faded toward white so that a
		// transparent pixel multiplies by one. Alpha: the coverage, or a constant that leaves the alpha
		// behind it untouched.
		private const int COLOR_PREMULTIPLIED = 0;
		private const int COLOR_TOWARD_WHITE = 1;
		private const int ALPHA_COVERAGE = 0;
		private const int ALPHA_ZERO = 1;
		private const int ALPHA_ONE = 2;

		private static StyledRectStore s_instance;

		/// <summary>The store, created on first use.</summary>
		public static StyledRectStore Instance => s_instance ??= new StyledRectStore();

		/// <summary>How many times the texture has been uploaded. At most one per frame.</summary>
		public int UploadCount { get; private set; }

		private readonly Dictionary<int, Stack<int>> m_freeSpans = new Dictionary<int, Stack<int>>();
		private readonly Texture2D m_texture;
		private readonly Shader m_shader;
		private readonly Shader m_extendedShader;

		// One per blend mode for the Core shader, then one per blend mode for the Extended shader.
		private readonly Material[] m_materials = new Material[BLEND_MODE_COUNT * 2];
		// Native, not managed: Texture2D.SetPixelData allocates on every call when given a managed array.
		private NativeArray<Vector4> m_data;
		private int m_nextAddress;
		private int m_liveSpans;
		private bool m_dirty;

		public StyledRectStore()
		{
			if (!SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat)) {
				Debug.LogError($"[StyledRect] RGBAFloat textures are not supported on [{SystemInfo.graphicsDeviceType}], so StyledRects will not draw");
			}

			m_texture = new Texture2D(WIDTH, START_HEIGHT, TextureFormat.RGBAFloat, false, true) {
				name = "Styled Rect Data",
				filterMode = FilterMode.Point,
				wrapMode = TextureWrapMode.Clamp,
				hideFlags = HideFlags.HideAndDontSave
			};

			m_data = new NativeArray<Vector4>(WIDTH * START_HEIGHT, Allocator.Persistent);

			m_shader = Resources.Load<Shader>(SHADER_RESOURCE);
			m_extendedShader = Resources.Load<Shader>(EXTENDED_SHADER_RESOURCE);

			m_dirty = true;

			// The upload has to come after the canvas rebuild, because a rect writes its texels when its mesh
			// is built. Handlers run in the order they were added and the rebuild is the registry's handler,
			// so make sure the registry exists and has added its own first.
			_ = CanvasUpdateRegistry.instance;
			Canvas.willRenderCanvases += Flush;
		}

		/// <summary>
		/// The shared material for a blend mode, created the first time it is used. Every rect with the same
		/// mode and the same shader tier uses the same material, so they batch.
		/// </summary>
		/// <param name="extended">Use the Extended shader, for rects with dashes or wobble.</param>
		public Material GetMaterial(StyledBlendMode blendMode, bool extended = false)
		{
			var index = Mathf.Clamp((int)blendMode, 0, BLEND_MODE_COUNT - 1);
			var slot = index + (extended ? BLEND_MODE_COUNT : 0);
			var material = m_materials[slot];

			if (material != null) {
				return material;
			}

			material = new Material(extended ? m_extendedShader : m_shader) {
				name = $"Styled Rect ({(StyledBlendMode)index}{(extended ? ", Extended" : "")})",
				hideFlags = HideFlags.HideAndDontSave
			};

			var (source, destination, colorOutput, alphaOutput) = (StyledBlendMode)index switch {
				// behind + color
				StyledBlendMode.Additive => (GpuBlendMode.One, GpuBlendMode.One, COLOR_PREMULTIPLIED, ALPHA_ZERO),
				// behind * lerp(1, color, alpha)
				StyledBlendMode.Multiply => (GpuBlendMode.DstColor, GpuBlendMode.Zero, COLOR_TOWARD_WHITE, ALPHA_ONE),
				// 1 - (1 - behind) * (1 - color)
				StyledBlendMode.Screen => (GpuBlendMode.OneMinusDstColor, GpuBlendMode.One, COLOR_PREMULTIPLIED, ALPHA_ZERO),
				// Normal: premultiplied alpha
				_ => (GpuBlendMode.One, GpuBlendMode.OneMinusSrcAlpha, COLOR_PREMULTIPLIED, ALPHA_COVERAGE)
			};

			material.SetFloat(s_srcBlendId, (float)source);
			material.SetFloat(s_dstBlendId, (float)destination);
			material.SetFloat(s_blendColorOutputId, colorOutput);
			material.SetFloat(s_blendAlphaOutputId, alphaOutput);

			m_materials[slot] = material;
			return material;
		}

		/// <summary>Reserves a run of texels and returns the address of the first one.</summary>
		public int Allocate(int texelCount)
		{
			m_liveSpans++;

			if (m_freeSpans.TryGetValue(texelCount, out var free) && free.Count > 0) {
				return free.Pop();
			}

			var address = m_nextAddress;
			m_nextAddress += texelCount;

			while (m_nextAddress > m_data.Length) {
				Grow();
			}

			return address;
		}

		/// <summary>Returns a run of texels. Runs are reused by rects that need the same count.</summary>
		public void Free(int address, int texelCount)
		{
			m_liveSpans--;

			// Nothing is in use, so start over from the first texel. This is what keeps the store from
			// carrying anything from one play session into the next.
			if (m_liveSpans <= 0) {
				m_liveSpans = 0;
				m_nextAddress = 0;
				m_freeSpans.Clear();
				return;
			}

			if (!m_freeSpans.TryGetValue(texelCount, out var free)) {
				free = new Stack<int>();
				m_freeSpans[texelCount] = free;
			}

			free.Push(address);
		}

		public void Write(int address, Vector4 value)
		{
			// A rect can outlive a disposed store for a moment during a domain reload.
			if (!m_data.IsCreated) {
				return;
			}

			m_data[address] = value;
			m_dirty = true;
		}

		public void Dispose()
		{
			Canvas.willRenderCanvases -= Flush;
			DestroyObject(m_texture);

			foreach (var material in m_materials) {
				DestroyObject(material);
			}

			if (m_data.IsCreated) {
				m_data.Dispose();
			}
		}

		private void Grow()
		{
			var height = m_texture.height * 2;

			var grown = new NativeArray<Vector4>(WIDTH * height, Allocator.Persistent);
			NativeArray<Vector4>.Copy(m_data, grown, m_data.Length);
			m_data.Dispose();
			m_data = grown;

			m_texture.Reinitialize(WIDTH, height);
			m_dirty = true;
		}

		private void Flush()
		{
			// Globals are lost when the editor reloads shaders, and setting them is cheap.
			Shader.SetGlobalTexture(s_dataId, m_texture);
			Shader.SetGlobalFloat(s_dataWidthId, WIDTH);

			if (!m_dirty || !m_data.IsCreated) {
				return;
			}

			m_texture.SetPixelData(m_data, 0);
			m_texture.Apply(false, false);
			m_dirty = false;
			UploadCount++;
		}

		private static void DestroyObject(UnityEngine.Object target)
		{
			if (target == null) {
				return;
			}

			if (Application.isPlaying) {
				UnityEngine.Object.Destroy(target);
			} else {
				UnityEngine.Object.DestroyImmediate(target);
			}
		}

#if UNITY_EDITOR
		// A domain reload clears the static but not the native texture and material, which would leak.
		[UnityEditor.InitializeOnLoadMethod]
		private static void RegisterReloadCleanup()
		{
			UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += DisposeInstance;
		}

		private static void DisposeInstance()
		{
			s_instance?.Dispose();
			s_instance = null;
		}
#endif
	}
}
