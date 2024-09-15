using Tekly.EditorUtils.Attributes;
using UnityEngine;

namespace Tekly.Favorites
{
	[CreateAssetMenu(menuName = "Favorites/Scripted Action")]
	public class FavoriteActionScriptAsset : FavoriteActionAsset
	{
		[SerializeReference] [Polymorphic] private IFavoriteActionScript m_referencedType;

		public override void Activate()
		{
			m_referencedType?.Activate();
		}
	}
}