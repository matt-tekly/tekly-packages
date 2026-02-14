namespace Tekly.Thunk.Core
{
	public struct ThunkClipRequest
	{
		public ThunkClipState Source;
		public float? Pitch;
		public float? Volume;
		public float? Delay;
	}
	
	public class ThunkClipInstance
	{
		public readonly int Id;
		public bool IsPlaying => !m_disposed && m_audioSource is { IsPlaying: true };
		public bool IsDisposed => m_disposed;
		public ThunkAudioSource AudioSource => m_audioSource;
		
		private readonly ThunkEmitter m_emitter;
		private readonly ThunkAudioSource m_audioSource;
		private readonly ThunkClipState m_clip;

		private bool m_disposed;
		
		public ThunkClipInstance(int id, ThunkEmitter emitter, ThunkClipRequest request)
		{
			Id = id;
			m_emitter = emitter;
			m_clip = request.Source;
			
			m_audioSource = emitter.GetAudioSource();
			m_audioSource.Play(request);
		}

		public void OnEmitterStopped()
		{
			// When the emitter is stopped or destroyed no cleanup is needed
			m_disposed = true;
		}
		
		public void Dispose()
		{
			if (m_disposed) {
				return;
			}

			if (m_emitter != null) {
				m_emitter.ClipInstanceDisposed(this);
			}
			
			m_disposed = true;
		}
	}
}