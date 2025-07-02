using Tekly.EditorUtils.Assets;
using UnityEngine;

namespace Tekly.Lofi.Core
{
	[CreateAssetMenu(menuName = "Tekly/Lofi/Clip Bank")]
	public class LofiClipBankDefinition : ScriptableObject
	{
		public LofiClipDefinition[] Clips;
        
#if UNITY_EDITOR
		[ContextMenu("Update Clips")]
		public void UpdateClips()
		{
			Clips = AssetDatabaseExt.FindAndLoad<LofiClipDefinition>("", this);
		}
#endif
	}
}