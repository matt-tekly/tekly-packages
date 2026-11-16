using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tekly.Wipes
{
    internal static class WipeImportUtils
    {
        private const string FILE_CONTENT = "The texture is set in this asset's import settings.\n";

        /// <summary>
        /// Creates an asset file beside what is selected in the Project window and selects it.
        /// </summary>
        public static void CreateFile(string name, string extension)
        {
            var folder = AssetDatabase.GetAssetPath(Selection.activeObject);

            if (string.IsNullOrEmpty(folder)) {
                folder = "Assets";
            } else if (!AssetDatabase.IsValidFolder(folder)) {
                // The asset database only knows paths with forward slashes
                folder = Path.GetDirectoryName(folder).Replace('\\', '/');
            }

            var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{name}.{extension}");

            File.WriteAllText(path, FILE_CONTENT);
            AssetDatabase.ImportAsset(path);

            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(path);
        }

        /// <summary>
        /// Loads an importer's source texture and makes the import depend on it, so the asset is imported
        /// again whenever the texture changes. Null when there is none.
        /// </summary>
        public static Texture2D LoadSource(AssetImportContext ctx, LazyLoadReference<Texture2D> source)
        {
            if (!source.isSet || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out var guid, out long _)) {
                return null;
            }

            ctx.DependsOnArtifact(new GUID(guid));
            return source.asset;
        }

        /// <summary>
        /// A readable copy of a texture at a size, which the caller destroys. It is read back through the
        /// GPU, so it holds what a shader samples whatever the texture's format and import settings. With
        /// no graphics device, as on a build machine, a PNG or JPG source file is decoded instead.
        /// </summary>
        public static Texture2D ReadCopy(AssetImportContext ctx, Texture2D texture, int width, int height)
        {
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null) {
                return ReadThroughGpu(texture, width, height);
            }

            var path = AssetDatabase.GetAssetPath(texture);
            var extension = Path.GetExtension(path).ToLowerInvariant();

            if (extension != ".png" && extension != ".jpg" && extension != ".jpeg") {
                ctx.LogImportError($"[{ctx.assetPath}] can't read [{path}] without a graphics device. Only PNG and JPG sources can be read then");
                return null;
            }

            var decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);

            if (!decoded.LoadImage(File.ReadAllBytes(path))) {
                Object.DestroyImmediate(decoded);
                ctx.LogImportError($"[{ctx.assetPath}] couldn't decode [{path}]");
                return null;
            }

            // A shader samples an sRGB texture as linear values in a linear project
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            var linearize = importer != null && importer.sRGBTexture && QualitySettings.activeColorSpace == ColorSpace.Linear;

            var copy = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            var colors = new Color[width * height];

            for (var y = 0; y < height; y++) {
                for (var x = 0; x < width; x++) {
                    var color = decoded.GetPixelBilinear((x + 0.5f) / width, (y + 0.5f) / height);
                    colors[y * width + x] = linearize ? color.linear : color;
                }
            }

            copy.SetPixels(colors);
            Object.DestroyImmediate(decoded);

            return copy;
        }

        private static Texture2D ReadThroughGpu(Texture2D texture, int width, int height)
        {
            var target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var previous = RenderTexture.active;

            Graphics.Blit(texture, target);
            RenderTexture.active = target;

            var copy = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            copy.ReadPixels(new Rect(0, 0, width, height), 0, 0);

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);

            return copy;
        }
    }
}
