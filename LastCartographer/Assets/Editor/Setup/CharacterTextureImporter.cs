using UnityEditor;

namespace OWSBG.Setup
{
    /// <summary>
    /// Import settings for character sprite sheets (CHR-03) and the dialogue portraits (CHR-13): straight alpha, no
    /// mipmaps (a sheet is read one frame window at a time), bilinear, clamped, uncompressed so the ink line stays a line.
    /// </summary>
    public sealed class CharacterTextureImporter : AssetPostprocessor
    {
        public const string Folder = "Assets/_Project/Art/Characters/";
        public const string PortraitFolder = "Assets/_Project/Art/Portraits/";

        void OnPreprocessTexture()
        {
            var path = assetPath.Replace('\\', '/');
            if (!(path.StartsWith(Folder) || path.StartsWith(PortraitFolder)) || path.EndsWith("Placeholder_Wren.png")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = UnityEngine.FilterMode.Bilinear;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 8192;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
