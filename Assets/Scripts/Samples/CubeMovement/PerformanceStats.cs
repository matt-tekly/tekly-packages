using Tekly.Common.Utils;
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
			if (m_framerate.Tick()) {
				m_fps.SetText("FPS: {0}", m_framerate.Fps);
			}
		}
	}
}