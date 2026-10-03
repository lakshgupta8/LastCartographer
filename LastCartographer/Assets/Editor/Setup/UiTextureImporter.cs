using UnityEditor;
using UnityEngine;

namespace OWSBG.Setup
{
    /// <summary>
    /// Import settings for the UI's drawings (ENV-11, docs/design/ui-art.md): everything under Art/UI is a cut-out
    /// read at its own size by UI Toolkit, so straight alpha, no mipmaps, bilinear, clamped, uncompressed so the ink
    /// line stays a line and a nine-slice edge stays crisp. The fonts beside them keep the importer's defaults.
    /// </summary>
    public sealed class UiTextureImporter : AssetPostprocessor
    {
        public const string Folder = "Assets/_Project/Art/UI/";

        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').StartsWith(Folder)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
