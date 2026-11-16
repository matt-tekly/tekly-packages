using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Tekly.Wipes
{
    /// <summary>
    /// Imports a .wiperamp file as a WipeRamp. The file holds nothing: the texture is an import setting.
    /// The import depends on the texture, so the ranges are worked out again whenever it changes.
    /// </summary>
    [ScriptedImporter(1, EXTENSION)]
    public class WipeRampImporter : ScriptedImporter
    {
        public const string EXTENSION = "wiperamp";

        [Tooltip("Greyscale, dark parts first. Import it uncompressed as R8 with no mipmaps and Clamp wrap")]
        [SerializeField] private LazyLoadReference<Texture2D> m_texture;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var ramp = ScriptableObject.CreateInstance<WipeRamp>();
            var texture = WipeImportUtils.LoadSource(ctx, m_texture);

            if (texture != null) {
                var copy = WipeImportUtils.ReadCopy(ctx, texture, texture.width, texture.height);

                if (copy != null) {
                    ramp.SetBaked(texture, copy.GetPixels32(), copy.width, copy.height);
                    DestroyImmediate(copy);
                }
            }

            ctx.AddObjectToAsset("ramp", ramp);
            ctx.SetMainObject(ramp);
        }

        [MenuItem("Assets/Create/Tekly/Wipe Ramp")]
        private static void Create()
        {
            WipeImportUtils.CreateFile("WipeRamp", EXTENSION);
        }
    }
}
