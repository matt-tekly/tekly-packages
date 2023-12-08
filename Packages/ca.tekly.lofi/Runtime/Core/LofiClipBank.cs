using System;
using System.Collections.Generic;
using System.Linq;
using Tekly.Content;

namespace Tekly.Lofi.Core
{
	public class LofiClipBank : IDisposable
	{
		public string Label { get; }
		public bool IsLoading => !m_operation.IsDone;
		public int References { get; private set; }
		
		private LofiClip[] m_clips;
		private readonly IContentOperation<IList<LofiClipDefinition>> m_operation;
		private readonly Dictionary<string, LofiClip> m_clipMap;
		
		public LofiClipBank(string label, Dictionary<string, LofiClip> clipMap)
		{
			m_clipMap = clipMap;
			Label = label;
			
			m_operation = ContentProvider.Instance.LoadAssetsAsync<LofiClipDefinition>(label);
			m_operation.Completed += OperationCompleted;

			References = 1;
		}

		private void OperationCompleted(IContentOperation<IList<LofiClipDefinition>> operation)
		{
			if (!m_operation.HasError) {
				m_clips = m_operation.Result.Select(x => x.CreateClip()).ToArray();
				foreach (var clip in m_clips) {
					m_clipMap.Add(clip.Name, clip);
				}
			}
		}

		public void Dispose()
		{
			if (m_clips != null) {
				foreach (var clip in m_clips) {
					clip.Unload();
					m_clipMap.Remove(clip.Name);
				}
			}
			
			m_operation.Release();
		}

		public void AddRef()
		{
			References++;
		}

		public void RemoveRef()
		{
			References--;
		}
	}
}