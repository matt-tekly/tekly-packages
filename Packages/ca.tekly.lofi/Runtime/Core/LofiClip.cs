using System.Collections.Generic;
using Tekly.Common.Collections;
using Tekly.Lofi.Emitters;
using UnityEngine;

namespace Tekly.Lofi.Core
{
	public class LofiClip
	{
		public bool CanRun => Time.realtimeSinceStartup >= m_nextRunTime;
		public string Name => m_definition.name;
		
		private readonly LofiClipDefinition m_definition;
		private readonly List<LofiClipRunner> m_runners = new List<LofiClipRunner>();

		private float m_nextRunTime;
		
		public LofiClip(LofiClipDefinition definition)
		{
			m_definition = definition;
		}

		public LofiClipRunner CreateRunner(LofiEmitter emitter)
		{
			m_nextRunTime = Time.realtimeSinceStartup + m_definition.MinTimeBetweenPlays;
			
			var runner = OnCreateRunner(emitter);
			m_runners.Add(runner);

			return runner;
		}

		protected virtual LofiClipRunner OnCreateRunner(LofiEmitter emitter)
		{
			var clip = m_definition.Clips.Random();
			
			return new LofiClipRunner(emitter, new LofiClipRunnerData {
				SourceClip = this,
				Clip = clip.Clip,
				Volume = m_definition.Volume.Get(),
				Pitch = m_definition.Pitch.Get(),
				MixerGroup = m_definition.MixerGroup != null ? m_definition.MixerGroup.MixerGroup : null,
				Loop = m_definition.Loop
			});
		}
		
		public void RunnerCompleted(LofiClipRunner runner)
		{
			m_runners.Remove(runner);
		}

		public void Unload()
		{
			for (var index = m_runners.Count - 1; index >= 0; index--) {
				m_runners[index].Dispose();
			}
		}
	}
}