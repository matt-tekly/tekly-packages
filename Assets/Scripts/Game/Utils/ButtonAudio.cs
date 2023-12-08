using Tekly.DataModels.Binders;
using Tekly.Lofi.Core;
using UnityEngine;

namespace TeklySample.Game.Utils
{
	public class ButtonAudio : ButtonAction
	{
		[SerializeField] private string m_clipId;
		
		public override void Activate()
		{
			Lofi.Instance.PlayOneShot(m_clipId);
		}
	}
}