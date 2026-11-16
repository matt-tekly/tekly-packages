using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Tekly.Wipes
{
    public enum WipeShapeResolution
    {
        [InspectorName("64")] Size64 = 64,
        [InspectorName("128")] Size128 = 128,
        [InspectorName("256")] Size256 = 256,
        [InspectorName("512")] Size512 = 512
    }

    public enum WipeShapeChannel
    {
        Alpha,
        Red
    }

    /// <summary>
    /// Imports a .wipeshape file as a WipeShape with its baked distance texture. The file holds nothing:
    /// the source texture and how it is baked are import settings. The import depends on the source, so
    /// the silhouette is baked again whenever it changes.
    /// </summary>
    [ScriptedImporter(6, EXTENSION)]
    public class WipeShapeImporter : ScriptedImporter
    {
        public const string EXTENSION = "wipeshape";

        private const int DIRECTIONS = WipeShape.DIRECTIONS;

        // The largest size the silhouette is measured at before it is averaged down to the baked size.
        private const int MAX_WORKING_SIZE = 1024;

        private const float MIN_THRESHOLD = 0.05f;
        private const float MAX_THRESHOLD = 0.95f;

        // Stands in for a distance to something that isn't there.
        private const double FAR = 1e20;

        [Tooltip("A texture with the silhouette centered, with clear space around it")]
        [SerializeField] private LazyLoadReference<Texture2D> m_source;

        [Tooltip("The size of the baked distance texture. Raise it for a silhouette with fine detail or sharp corners, lower it for a simple one")]
        [SerializeField] private WipeShapeResolution m_resolution = WipeShapeResolution.Size128;

        [Tooltip("The channel the silhouette is read from. Red is for a black and white mask with no alpha")]
        [SerializeField] private WipeShapeChannel m_channel = WipeShapeChannel.Alpha;

        [Tooltip("The channel value from which a pixel counts as inside the silhouette. Lower it to fatten a soft edged source, raise it to thin it")]
        [SerializeField, Range(MIN_THRESHOLD, MAX_THRESHOLD)] private float m_threshold = 0.5f;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var shape = ScriptableObject.CreateInstance<WipeShape>();
            ctx.AddObjectToAsset("shape", shape);
            ctx.SetMainObject(shape);

            var source = WipeImportUtils.LoadSource(ctx, m_source);

            if (source == null) {
                return;
            }

            var size = (int) m_resolution;

            // A pixel is either inside or outside, so distances measured at the baked size put the edge
            // on whole pixels, which shows as steps when the silhouette fills the screen. The silhouette
            // is read and measured at a few times the size instead, then averaged down, which places the
            // edge within a pixel the way the source's antialiasing does.
            var samples = Mathf.Clamp(MAX_WORKING_SIZE / size, 2, 4);
            var working = size * samples;

            var copy = WipeImportUtils.ReadCopy(ctx, source, working, working);

            if (copy == null) {
                return;
            }

            var inside = ReadSilhouette(copy.GetPixels32(), working, samples, Mathf.Max(source.width, source.height));
            DestroyImmediate(copy);

            // For every pixel, the squared distance to the nearest pixel outside and inside the silhouette
            var toOutside = SquaredDistances(inside, working, false);
            var toInside = SquaredDistances(inside, working, true);

            // The reach is a fixed part of the texture, so it covers the same part of the silhouette at
            // every resolution
            var reach = working * 0.5f * WipeShape.REACH;

            // Half floats, since they are finest near 0, where the edge is. With 8 bits the edge can only
            // sit on one of 127 steps across the reach, and a silhouette that fills the screen shows
            // every one of them.
            var pixels = new ushort[size * size];

            for (var y = 0; y < size; y++) {
                for (var x = 0; x < size; x++) {
                    var sum = 0f;

                    for (var sy = 0; sy < samples; sy++) {
                        var row = (y * samples + sy) * working + x * samples;

                        for (var sx = 0; sx < samples; sx++) {
                            var i = row + sx;
                            var distance = Mathf.Min(EdgeDistance(inside[i] ? toOutside[i] : toInside[i]), reach);
                            sum += inside[i] ? distance : -distance;
                        }
                    }

                    var signed = sum / (samples * samples);
                    pixels[y * size + x] = Mathf.FloatToHalf(signed / (2f * reach));
                }
            }

            var baked = new Texture2D(size, size, TextureFormat.RHalf, false, true) {
                name = "Distance",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            baked.SetPixelData(pixels, 0);
            baked.Apply(false, true);

            ctx.AddObjectToAsset("distance", baked);

            var center = working / 2 * working + working / 2;
            var inscribed = inside[center] ? EdgeDistance(toOutside[center]) / (working * 0.5f) : 0f;

            shape.SetBaked(baked, inscribed, Extents(inside, working));
        }

        [MenuItem("Assets/Create/Tekly/Wipe Shape")]
        private static void Create()
        {
            WipeImportUtils.CreateFile("WipeShape", EXTENSION);
        }

        /// <summary>
        /// Which pixels are inside the silhouette. A source smaller than the size it is read at was
        /// stretched, which leaves a kink in the edge at every source pixel, so the values are smoothed
        /// over about one source pixel first.
        /// </summary>
        private bool[] ReadSilhouette(Color32[] colors, int size, int border, int sourceSize)
        {
            var values = new float[size * size];

            for (var i = 0; i < values.Length; i++) {
                values[i] = m_channel == WipeShapeChannel.Alpha ? colors[i].a : colors[i].r;
            }

            var radius = Mathf.CeilToInt(size / (float) Mathf.Max(sourceSize, 1) * 0.5f);

            if (size > sourceSize && radius > 0) {
                // Two box blurs make a tent, which is close enough to a Gaussian here
                var scratch = new float[values.Length];
                Blur(values, scratch, size, radius);
                Blur(values, scratch, size, radius);
            }

            // At the very ends every pixel the edge touches falls on one side, which brings the steps back
            var threshold = Mathf.Clamp(m_threshold, MIN_THRESHOLD, MAX_THRESHOLD) * 255f;
            var inside = new bool[size * size];

            for (var y = 0; y < size; y++) {
                for (var x = 0; x < size; x++) {
                    // The border is always outside, so a clamped sample past the texture is too.
                    var onBorder = x < border || y < border || x >= size - border || y >= size - border;
                    inside[y * size + x] = !onBorder && values[y * size + x] >= threshold;
                }
            }

            return inside;
        }

        /// <summary>
        /// A box blur along the rows and then the columns, repeating the edge pixels.
        /// </summary>
        private static void Blur(float[] values, float[] scratch, int size, int radius)
        {
            var weight = 1f / (2 * radius + 1);

            for (var y = 0; y < size; y++) {
                var row = y * size;

                for (var x = 0; x < size; x++) {
                    var sum = 0f;

                    for (var k = -radius; k <= radius; k++) {
                        sum += values[row + Mathf.Clamp(x + k, 0, size - 1)];
                    }

                    scratch[row + x] = sum * weight;
                }
            }

            for (var y = 0; y < size; y++) {
                for (var x = 0; x < size; x++) {
                    var sum = 0f;

                    for (var k = -radius; k <= radius; k++) {
                        sum += scratch[Mathf.Clamp(y + k, 0, size - 1) * size + x];
                    }

                    values[y * size + x] = sum * weight;
                }
            }
        }

        /// <summary>
        /// The distance in pixels to the edge from a squared distance to the nearest pixel on the other
        /// side of it. The edge runs between the two pixels.
        /// </summary>
        private static float EdgeDistance(double squared)
        {
            return Mathf.Max((float) System.Math.Sqrt(squared) - 0.5f, 0f);
        }

        /// <summary>
        /// The squared distance in pixels from every pixel to the nearest one that is inside the
        /// silhouette, or outside it. An exact distance transform: one pass down the columns, then one
        /// along the rows, each finding the lower envelope of a parabola per pixel. It takes time in
        /// proportion to the pixel count, where checking a window around every pixel does not.
        /// </summary>
        private static double[] SquaredDistances(bool[] inside, int size, bool toInside)
        {
            var distances = new double[size * size];

            var line = new double[size];
            var result = new double[size];
            var parabolas = new int[size];
            var bounds = new double[size + 1];

            for (var x = 0; x < size; x++) {
                for (var y = 0; y < size; y++) {
                    line[y] = inside[y * size + x] == toInside ? 0 : FAR;
                }

                Envelope(line, result, parabolas, bounds, size);

                for (var y = 0; y < size; y++) {
                    distances[y * size + x] = result[y];
                }
            }

            for (var y = 0; y < size; y++) {
                for (var x = 0; x < size; x++) {
                    line[x] = distances[y * size + x];
                }

                Envelope(line, result, parabolas, bounds, size);

                for (var x = 0; x < size; x++) {
                    distances[y * size + x] = result[x];
                }
            }

            return distances;
        }

        /// <summary>
        /// For every position along a line, the least of (position - q) squared plus the line's value at q,
        /// over every q.
        /// </summary>
        private static void Envelope(double[] line, double[] result, int[] parabolas, double[] bounds, int count)
        {
            var last = 0;
            parabolas[0] = 0;
            bounds[0] = double.NegativeInfinity;
            bounds[1] = double.PositiveInfinity;

            for (var q = 1; q < count; q++) {
                double crossing;

                // Drops every parabola the new one is below from where that one started
                while (true) {
                    var p = parabolas[last];
                    crossing = (line[q] + (double) q * q - (line[p] + (double) p * p)) / (2.0 * q - 2.0 * p);

                    if (crossing > bounds[last]) {
                        break;
                    }

                    last--;
                }

                last++;
                parabolas[last] = q;
                bounds[last] = crossing;
                bounds[last + 1] = double.PositiveInfinity;
            }

            var current = 0;

            for (var q = 0; q < count; q++) {
                while (bounds[current + 1] < q) {
                    current++;
                }

                var p = parabolas[current];
                result[q] = (double) (q - p) * (q - p) + line[p];
            }
        }

        /// <summary>
        /// Walks out from the center in each direction to the first pixel outside the silhouette.
        /// </summary>
        private static float[] Extents(bool[] inside, int size)
        {
            var extents = new float[DIRECTIONS];
            var center = size * 0.5f;

            for (var i = 0; i < DIRECTIONS; i++) {
                var angle = i / (float) DIRECTIONS * 2f * Mathf.PI;
                var step = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.5f;
                var position = new Vector2(center, center);
                var distance = 0f;

                while (true) {
                    var x = Mathf.FloorToInt(position.x);
                    var y = Mathf.FloorToInt(position.y);

                    if (x < 0 || y < 0 || x >= size || y >= size || !inside[y * size + x]) {
                        break;
                    }

                    position += step;
                    distance += 0.5f;
                }

                extents[i] = Mathf.Max(distance - 0.5f, 0f) / center;
            }

            return extents;
        }
    }
}
