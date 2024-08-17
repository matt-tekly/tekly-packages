using System.Collections.Generic;
using Tekly.Common.Collections;
using Tekly.Lofi.Emitters;
using UnityEngine;
using UnityEngine.Audio;

namespace Tekly.Lofi.Core
{
	public class LofiClip
	{
		public bool CanRun => Time.realtimeSinceStartup >= m_nextRunTime;
		public string Name => m_definition.name;
		public AudioClip RandomClip => m_definition.Clips.Random().Clip;

		public LofiMixerGroup MixerGroup => m_definition.MixerGroup;
		
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
		
		public LofiClipRunner CreateRunner(LofiEmitter emitter, LofiClipRunnerData runnerData)
		{
			m_nextRunTime = Time.realtimeSinceStartup + m_definition.MinTimeBetweenPlays;
			
			var runner =  new LofiClipRunner(emitter, runnerData);
			m_runners.Add(runner);

			return runner;
		}

		public LofiClipRunnerData CreateRunnerData(float pitch)
		{
			return CreateRunnerData(pitch, Vector3.zero);
		}
		
		public LofiClipRunnerData CreateRunnerData(float pitch, Vector3 position)
		{
			return new LofiClipRunnerData {
				SourceClip = this,
				Clip = RandomClip,
				Volume = m_definition.Volume.Get(),
				Pitch = pitch,
				Position = position,
				MixerGroup = MixerGroup,
				Loop = m_definition.Loop
			};
		}

		protected virtual LofiClipRunner OnCreateRunner(LofiEmitter emitter)
		{
			return new LofiClipRunner(emitter, new LofiClipRunnerData {
				SourceClip = this,
				Clip = RandomClip,
				Volume = m_definition.Volume.Get(),
				Pitch = m_definition.Pitch.Get(),
				Position = Vector3.zero,
				MixerGroup = MixerGroup,
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