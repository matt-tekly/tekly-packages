using UnityEngine;

namespace Tekly.Wipes
{
    /// <summary>
    /// How a ramp's texture is fitted to the rect it wipes.
    /// </summary>
    public enum WipeRampFit
    {
        /// <summary>The ramp keeps its aspect and fills the rect, cropping what overflows</summary>
        Fill,

        /// <summary>The ramp is stretched to the rect</summary>
        Stretch
    }

    /// <summary>
    /// A greyscale texture for a Ramp wipe: a pixel is covered once the wipe's coverage passes its value,
    /// like a gradient wipe in a video tool. Dark parts go first.
    ///
    /// A ramp rarely uses the whole range from black to white, and a ramp that keeps its aspect is cropped
    /// by the rect, which can cut its darkest and lightest parts off. Either would leave the wipe doing
    /// nothing at its start or end. The asset keeps the least and greatest value of each cell of a coarse
    /// grid, so the wipe is stretched over the values that are on screen without the texture having to be
    /// readable.
    ///
    /// It is an imported asset: a .wiperamp file whose importer names the texture and works the ranges
    /// out. The import depends on the texture, so the ranges follow it whenever it changes.
    ///
    /// Import ramps uncompressed as R8, or R16 for slow, smooth wipes, with bilinear filtering, no mipmaps
    /// and Clamp wrap. Compression shows as steps and blocks in the wipe.
    /// </summary>
    public class WipeRamp : ScriptableObject
    {
        public const int GRID_SIZE = 16;

        public Texture2D Texture => m_texture;

        /// <summary>
        /// True when the ranges are known. A ramp that isn't baked wipes over the whole 0 to 1 range.
        /// </summary>
        public bool IsBaked => m_least != null && m_least.Length == GRID_SIZE * GRID_SIZE;

        [SerializeField] private Texture2D m_texture;

        // Per cell, as bytes. The shader clamps the field, so rounding never covers a pixel early.
        [SerializeField, HideInInspector] private byte[] m_least;
        [SerializeField, HideInInspector] private byte[] m_greatest;

        /// <summary>
        /// A ramp for a texture made at runtime, such as a field worked out in code. Its ranges are read
        /// when the texture is readable. Otherwise it wipes over the whole 0 to 1 range. Destroy it when it
        /// is no longer needed.
        /// </summary>
        public static WipeRamp Create(Texture2D texture)
        {
            var ramp = CreateInstance<WipeRamp>();
            ramp.m_texture = texture;

            if (texture != null && texture.isReadable) {
                ramp.SetRanges(texture.GetPixels32(), texture.width, texture.height);
            }

            return ramp;
        }

        /// <summary>
        /// Sets the texture and works its ranges out from a copy of its pixels. For the importer.
        /// </summary>
        internal void SetBaked(Texture2D texture, Color32[] pixels, int width, int height)
        {
            m_texture = texture;
            SetRanges(pixels, width, height);
        }

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

        /// <summary>
        /// Stores each grid cell's least and greatest value from the texture's pixels.
        /// </summary>
        private void SetRanges(Color32[] pixels, int width, int height)
        {
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
        }
    }
}
