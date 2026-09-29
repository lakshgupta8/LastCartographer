using System.IO;
using UnityEditor;
using UnityEngine;

namespace OWSBG.Setup
{
    /// <summary>
    /// Import settings for the paper kits (ENV-01, docs/design/paper-kit.md): everything under Art/Environment is a
    /// drawing with straight alpha and mipmaps. A "Paper_" strip maps once across its quad and clamps; anything
    /// else is a tile and repeats.
    /// </summary>
    public sealed class EnvironmentTextureImporter : AssetPostprocessor
    {
        public const string Folder = "Assets/_Project/Art/Environment/";

        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').StartsWith(Folder)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 4;
            importer.maxTextureSize = 4096;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.wrapMode = Path.GetFileName(assetPath).StartsWith("Paper_") ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
        }
    }
}
