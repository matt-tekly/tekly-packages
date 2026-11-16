using UnityEngine;

namespace TeklySample.Samples.Wipe
{
    /// <summary>
    /// Spins and bobs an object, so the sample shows that what is behind and inside a wipe stays live.
    /// </summary>
    public class WipeSampleMover : MonoBehaviour
    {
        [Tooltip("Degrees per second")]
        [SerializeField] private float m_spin = 90f;

        [Tooltip("How far it moves side to side, in its parent's units")]
        [SerializeField] private float m_sway;

        [Tooltip("Seconds one sway takes")]
        [SerializeField] private float m_swaySeconds = 3f;

        private Vector3 m_start;

        private void Awake()
        {
            m_start = transform.localPosition;
        }

        private void Update()
        {
            transform.Rotate(0, 0, -m_spin * Time.unscaledDeltaTime);

            if (m_sway != 0f && m_swaySeconds > 0f) {
                var offset = Mathf.Sin(Time.unscaledTime * 2f * Mathf.PI / m_swaySeconds) * m_sway;
                transform.localPosition = m_start + new Vector3(offset, 0, 0);
            }
        }
    }
}
