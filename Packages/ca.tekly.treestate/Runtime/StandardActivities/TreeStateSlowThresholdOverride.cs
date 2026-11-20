using UnityEngine;

namespace Tekly.TreeState.StandardActivities
{
	/// <summary>
	/// Overrides the slow load/unload threshold for the TreeState on this GameObject and the listed activities.
	/// The TreeState is always included as its load time includes its activities.
	/// </summary>
	public class TreeStateSlowThresholdOverride : MonoBehaviour
	{
		[Tooltip("Seconds a load or unload can take before it is reported as slow. Negative never reports.")]
		[SerializeField] private float m_threshold;
		
		[SerializeField] private TreeStateActivity[] m_targets;

		[SerializeField, HideInInspector] private TreeState m_treeState;

		private void Awake()
		{
			if (m_treeState != null) {
				m_treeState.SlowThreshold = m_threshold;
			}

			if (m_targets != null) {
				foreach (var target in m_targets) {
					if (target != null) {
						target.SlowThreshold = m_threshold;	
					}
				}	
			}
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			m_treeState = GetComponent<TreeState>();
		}
#endif
	}
}
