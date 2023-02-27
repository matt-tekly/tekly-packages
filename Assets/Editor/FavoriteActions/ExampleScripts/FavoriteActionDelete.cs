using JetBrains.Annotations;
using UnityEditor;

namespace Tekly.Favorites
{
	[UsedImplicitly]
	public class FavoriteActionDelete : IFavoriteActionScript
	{
		public void Activate()
		{
			if (Selection.activeGameObject != null) {
				Undo.DestroyObjectImmediate(Selection.activeGameObject);
			}
		}
	}
}