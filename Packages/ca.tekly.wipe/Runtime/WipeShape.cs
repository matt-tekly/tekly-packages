using UnityEngine;

namespace Tekly.Wipes
{
    /// <summary>
    /// A silhouette for a Shape wipe, such as a character's head or a logo. The iris closes through it:
    /// the silhouette scales down about the wipe's center and everything outside it is covered.
    ///
    /// The source is any texture with the silhouette in its alpha, centered, with some clear space around
    /// it. It is baked into a small distance texture, so the edge stays crisp when the silhouette fills
    /// the screen. The silhouette must be solid at its center, since the iris opens until that solid part
    /// clears the screen.
    ///
    /// It is an imported asset: a .wipeshape file whose importer names the source texture and bakes it.
    /// The import depends on the texture, so the bake follows it whenever it changes.
    /// </summary>
    public class WipeShape : ScriptableObject
    {
        // How far the baked distance reaches from the edge, where the texture spans -1 to 1.
        internal const float REACH = 0.25f;

        // How many directions the silhouette's extent is stored for.
        internal const int DIRECTIONS = 64;

        // How many points along each side of the rect are checked against the silhouette.
        private const int SIDE_SAMPLES = 12;

        /// <summary>
        /// The baked distance texture, as half floats. 0 is the edge, positive is inside, and 0.5 is the
        /// reach.
        /// </summary>
        public Texture2D Texture => m_baked;

        public bool IsBaked => m_baked != null && m_extents != null && m_extents.Length == DIRECTIONS;

        /// <summary>
        /// How far the baked distance reaches from the edge, where the texture spans -1 to 1.
        /// </summary>
        public float Reach => REACH;

        /// <summary>
        /// The radius of the largest circle around the center that fits inside the silhouette, where the
        /// texture spans -1 to 1. 0 when the center is empty.
        /// </summary>
        public float Inscribed => m_inscribed;

        [SerializeField, HideInInspector] private Texture2D m_baked;
        [SerializeField, HideInInspector] private float m_inscribed;

        // How far the silhouette reaches from its center in each direction before its first gap, where
        // the texture spans -1 to 1. Direction 0 is right and they turn counterclockwise.
        [SerializeField, HideInInspector] private float[] m_extents;

        /// <summary>
        /// The smallest scale at which the silhouette, turned by a rotation in radians and centered on a
        /// point, contains a whole rect. The center and the rect's half size are in the same units, and
        /// the scale is in those units per unit of the silhouette. This is how far the iris has to open
        /// to clear the rect, so it starts closing straight away instead of waiting off screen.
        /// </summary>
        public float OpenScale(Vector2 center, Vector2 half, float rotation)
        {
            if (!IsBaked) {
                return 1f;
            }

            var cos = Mathf.Cos(rotation);
            var sin = Mathf.Sin(rotation);
            var scale = 0f;

            for (var side = 0; side < 4; side++) {
                for (var i = 0; i < SIDE_SAMPLES; i++) {
                    var t = i / (float) SIDE_SAMPLES * 2f - 1f;

                    var point = side switch {
                        0 => new Vector2(t * half.x, -half.y),
                        1 => new Vector2(half.x, t * half.y),
                        2 => new Vector2(-t * half.x, half.y),
                        _ => new Vector2(-half.x, -t * half.y)
                    };

                    var offset = point - center;
                    var turned = new Vector2(offset.x * cos - offset.y * sin, offset.x * sin + offset.y * cos);
                    var direction = Mathf.Atan2(turned.y, turned.x) / (2f * Mathf.PI) * DIRECTIONS;

                    // The shorter of the two stored directions around it, so a gap between them isn't missed
                    var index = Mathf.FloorToInt(direction);
                    var extent = Mathf.Min(m_extents[Wrap(index)], m_extents[Wrap(index + 1)]);

                    scale = Mathf.Max(scale, offset.magnitude / Mathf.Max(extent, 0.01f));
                }
            }

            return scale;
        }

        private static int Wrap(int direction)
        {
            return (direction % DIRECTIONS + DIRECTIONS) % DIRECTIONS;
        }

        /// <summary>
        /// Sets what the importer baked from the source texture.
        /// </summary>
        internal void SetBaked(Texture2D baked, float inscribed, float[] extents)
        {
            m_baked = baked;
            m_inscribed = inscribed;
            m_extents = extents;
        }
    }
}
