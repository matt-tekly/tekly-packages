using UnityEngine;

namespace Tekly.Curtains
{
    /// <summary>
    /// A greyscale ramp for a Gradient curtain: a pixel is covered once the curtain's progress passes its
    /// value, like After Effects' Gradient Wipe. Dark parts go first.
    ///
    /// The curtain crops the ramp to cover the screen, so the darkest and lightest values may be cut off.
    /// The asset keeps the least and greatest value of each cell of a coarse grid, baked in the editor, so
    /// the curtain can stretch its progress over the values it can see without the texture being readable.
    ///
    /// Import ramps uncompressed as R8, or R16 for slow, smooth wipes, with bilinear filtering, no mipmaps
    /// and Clamp wrap. Compression bands and blocks show as steps in the wipe.
    /// </summary>
    [CreateAssetMenu(menuName = "Tekly/Curtain Gradient", fileName = "CurtainGradient")]
    public class CurtainGradient : ScriptableObject
    {
        public const int GRID_SIZE = 16;

        public Texture2D Texture => m_texture;
        public bool IsBaked => m_least != null && m_least.Length == GRID_SIZE * GRID_SIZE;

        [SerializeField] private Texture2D m_texture;

        // Per cell, as bytes. The shader clamps the field, so rounding never shows a pixel early.
        [SerializeField, HideInInspector] private byte[] m_least;
        [SerializeField, HideInInspector] private byte[] m_greatest;
        [SerializeField, HideInInspector] private Hash128 m_bakedHash;

        /// <summary>
        /// The least and greatest ramp values inside a uv rect. The whole 0 to 1 range when not baked.
        /// </summary>
        public void GetRange(Rect uv, out float least, out float greatest)
        {
            least = 0;
            greatest = 1;

            if (!IsBaked) {
                return;
            }

            var xMin = Mathf.Clamp(Mathf.FloorToInt(uv.xMin * GRID_SIZE), 0, GRID_SIZE - 1);
            var yMin = Mathf.Clamp(Mathf.FloorToInt(uv.yMin * GRID_SIZE), 0, GRID_SIZE - 1);
            var xMax = Mathf.Clamp(Mathf.CeilToInt(uv.xMax * GRID_SIZE) - 1, xMin, GRID_SIZE - 1);
            var yMax = Mathf.Clamp(Mathf.CeilToInt(uv.yMax * GRID_SIZE) - 1, yMin, GRID_SIZE - 1);

            var low = 255;
            var high = 0;

            for (var y = yMin; y <= yMax; y++) {
                for (var x = xMin; x <= xMax; x++) {
                    var cell = y * GRID_SIZE + x;
                    low = Mathf.Min(low, m_least[cell]);
                    high = Mathf.Max(high, m_greatest[cell]);
                }
            }

            least = low / 255f;
            greatest = high / 255f;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_texture == null) {
                m_least = null;
                m_greatest = null;
                return;
            }

            if (!IsBaked || m_bakedHash != m_texture.imageContentsHash) {
                // Reading pixels back is not allowed in the middle of OnValidate.
                UnityEditor.EditorApplication.delayCall += Bake;
            }
        }

        /// <summary>
        /// Reads the ramp back through a render texture, so it works on textures that are not readable or
        /// are compressed, and stores each grid cell's range.
        /// </summary>
        [ContextMenu("Bake")]
        public void Bake()
        {
            if (this == null || m_texture == null) {
                return;
            }

            var width = m_texture.width;
            var height = m_texture.height;

            var target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var previous = RenderTexture.active;

            Graphics.Blit(m_texture, target);
            RenderTexture.active = target;

            var readback = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            readback.ReadPixels(new Rect(0, 0, width, height), 0, 0);

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);

            var pixels = readback.GetPixels32();
            DestroyImmediate(readback);

            var least = new byte[GRID_SIZE * GRID_SIZE];
            var greatest = new byte[GRID_SIZE * GRID_SIZE];

            for (var i = 0; i < least.Length; i++) {
                least[i] = 255;
            }

            for (var y = 0; y < height; y++) {
                var row = Mathf.Min(y * GRID_SIZE / height, GRID_SIZE - 1) * GRID_SIZE;

                for (var x = 0; x < width; x++) {
                    var cell = row + Mathf.Min(x * GRID_SIZE / width, GRID_SIZE - 1);
                    var value = pixels[y * width + x].r;

                    if (value < least[cell]) {
                        least[cell] = value;
                    }

                    if (value > greatest[cell]) {
                        greatest[cell] = value;
                    }
                }
            }

            m_least = least;
            m_greatest = greatest;
            m_bakedHash = m_texture.imageContentsHash;

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
