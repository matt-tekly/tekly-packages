using System.Linq;
using ImageMagick;
using UnityEditor;
using UnityEngine;

namespace Tekly.ImageUtils
{
    public static class GridAtlasGenerator
    {
        [MenuItem("Tools/Image Utils/Generate Grid Atlas")]
        public static void MenuItem()
        {
            var textures = Selection.GetFiltered<Texture2D>(SelectionMode.Assets);

            if (textures == null || textures.Length == 0) {
                Debug.LogError("No Textures Selected");
                return;
            }

            var savePath = EditorUtility.SaveFilePanelInProject("Save Atlas", "atlas.png", "png", "Choose file to save atlas to");

            if (string.IsNullOrEmpty(savePath)) {
                return;
            }

            var paths = textures.Select(AssetDatabase.GetAssetPath).ToArray();
            GenerateAtlas(paths, savePath);
        }

        private static void GenerateAtlas(string[] paths, string outputPath, int padding = 2)
        {
            var images = paths.Select(x => new MagickImage(x)).ToArray();

            var maxWidth = 0;
            var maxHeight = 0;

            foreach (var image in images) {
                maxWidth = Mathf.Max(maxWidth, image.Width);
                maxHeight = Mathf.Max(maxHeight, image.Height);
            }

            var totalPadding = padding * (images.Length - 1);
            var magickImage = new MagickImage(MagickColors.Transparent, maxWidth * images.Length + totalPadding, maxHeight);

            for (var index = 0; index < images.Length; index++) {
                var image = images[index];
                var x = index * maxWidth + index * padding + (maxWidth / 2) - (image.Width / 2);
                var y = (maxHeight / 2) - (image.Height / 2);
                magickImage.CopyPixels(image, image.Page, x, y);
            }
            
            magickImage.Write(outputPath);
        }
    }
}