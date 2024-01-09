using TMPro;
using UnityEngine;

namespace TeklySample.Samples.CubeMovement
{
	public class PerformanceStats : MonoBehaviour
	{
		[SerializeField] private TMP_Text m_fps;

		private Framerate m_framerate = new Framerate();
		
		private void Update()
		{
			m_framerate.Tick();
			m_fps.SetText("FPS: {0}", m_framerate.Fps);
		}
	}
	
	public class Framerate
	{
		public int Fps;
		
		private int m_lastFrame;
		private float m_timer;
		
		private readonly float m_timeBetweenUpdates;
		private readonly float[] m_deltaTimes;
		
		public Framerate(int frames = 60, float timeBetweenUpdates = 0.5f)
		{
			m_deltaTimes = new float[frames];
			m_timeBetweenUpdates = timeBetweenUpdates;
		}

		public void Tick()
		{
			var deltaTime = Time.deltaTime;
			m_deltaTimes[m_lastFrame] = deltaTime;
			m_lastFrame = (m_lastFrame + 1) % m_deltaTimes.Length;

			m_timer += deltaTime;
			if (m_timer > m_timeBetweenUpdates) {
				m_timer -= m_timeBetweenUpdates;
				var total = 0f;
			
				foreach (var delta in m_deltaTimes) {
					total += delta;
				}

				Fps = Mathf.FloorToInt(m_deltaTimes.Length / total);
			}
		}
	}
}