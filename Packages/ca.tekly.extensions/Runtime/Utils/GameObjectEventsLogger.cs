using Tekly.Logging;
using UnityEngine;

namespace Tekly.Extensions.Utils
{
	public class GameObjectEventsLogger : MonoBehaviour
	{
		[SerializeField] private string m_name;

		private readonly TkLogger m_logger = TkLogger.Get<GameObjectEventsLogger>();

		~GameObjectEventsLogger()
		{
			// It seems sometimes the logger _may_ not exist when this is called
			Debug.Log($"Finalize [{m_name}]");
		}
		
		protected virtual void Awake()
		{
			m_logger.InfoContext("Awake [{name}]", this, ("name", m_name));
		}
		
		protected virtual void Start()
		{
			m_logger.InfoContext("Start [{name}]", this, ("name", m_name));
		}
		
		protected virtual void OnEnable()
		{
			m_logger.InfoContext("OnEnable [{name}]", this, ("name", m_name));
		}
		
		protected virtual void OnDisable()
		{
			m_logger.InfoContext("OnDisable [{name}]", this, ("name", m_name));
		}
		
		protected virtual void OnDestroy()
		{
			m_logger.InfoContext("OnDestroy [{name}]", this, ("name", m_name));
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (Application.isPlaying) {
				return;
			}
			
			m_name = name;
		}
#endif
	}
}