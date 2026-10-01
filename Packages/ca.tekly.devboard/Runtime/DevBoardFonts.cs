using TMPro;
using UnityEngine;

namespace Tekly.DevBoard
{
	/// <summary>
	/// Registers DevBoard fonts and materials with TMP so rich text tags and styles can find them by name
	/// without them living in Resources.
	/// </summary>
	public static class DevBoardFonts
	{
		public static void Register(DevBoardAssets assets)
		{
			if (assets == null) {
				return;
			}

			RegisterFonts(assets.Fonts);
			RegisterMaterials(assets.FontMaterials);
		}

		private static void RegisterFonts(TMP_FontAsset[] fonts)
		{
			if (fonts == null) {
				return;
			}

			// Found by name from <font="name"> tags, including those inside styles
			foreach (var font in fonts) {
				if (font != null) {
					MaterialReferenceManager.AddFontAsset(font);
				}
			}
		}

		private static void RegisterMaterials(Material[] materials)
		{
			if (materials == null) {
				return;
			}

			// TMP hashes <material="name"> and <font="x" material="name"> values case-insensitively.
			// AddFontMaterial throws on duplicates so check first.
			foreach (var material in materials) {
				if (material == null) {
					continue;
				}

				var hashCode = TMP_TextUtilities.GetHashCode(material.name);

				if (!MaterialReferenceManager.TryGetMaterial(hashCode, out _)) {
					MaterialReferenceManager.AddFontMaterial(hashCode, material);
				}
			}
		}
	}
}
