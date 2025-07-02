using System;
using System.Collections.Generic;
using System.Linq;
using Tekly.Content;

namespace Tekly.Lofi.Core
{
	public class LofiClipBank : IDisposable
	{
		public string Label { get; }
		public LofiClipBankDefinitionRef BankRef { get; }
		
		public bool IsLoading {
			get {
				if (m_labelOperation != null) {
					return !m_labelOperation.IsDone;
				}

				return !m_refOperation.IsDone;
			}
		}
		
		public int References { get; private set; }
		
		private LofiClip[] m_clips;
		
		private readonly Dictionary<string, LofiClip> m_clipMap;
		
		private readonly IContentOperation<IList<LofiClipDefinition>> m_labelOperation;
		private readonly IContentOperation<LofiClipBankDefinition> m_refOperation;
		
		public LofiClipBank(string label, Dictionary<string, LofiClip> clipMap)
		{
			m_clipMap = clipMap;
			Label = label;
			
			m_labelOperation = ContentProvider.Instance.LoadAssetsAsync<LofiClipDefinition>(label);
			m_labelOperation.Completed += OperationCompleted;

			References = 1;
		}

		public LofiClipBank(LofiClipBankDefinitionRef bankRef, Dictionary<string, LofiClip> clipMap)
		{
			m_clipMap = clipMap;
			BankRef = bankRef;
			
			m_refOperation = ContentProvider.Instance.LoadAssetAsync<LofiClipBankDefinition>(bankRef);
			m_refOperation.Completed += OperationCompleted;

			References = 1;
		}

		private void OperationCompleted(IContentOperation<LofiClipBankDefinition> obj)
		{
			if (!m_refOperation.HasError) {
				AddClips(m_refOperation.Result.Clips);
			}
		}

		private void OperationCompleted(IContentOperation<IList<LofiClipDefinition>> operation)
		{
			if (!m_labelOperation.HasError) {
				AddClips(operation.Result);
			}
		}

		private void AddClips(IList<LofiClipDefinition> clips)
		{
			m_clips = clips.Select(x => x.CreateClip()).ToArray();
			foreach (var clip in m_clips) {
				m_clipMap.Add(clip.Name, clip);
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
			
			m_labelOperation?.Release();
			m_refOperation?.Release();
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