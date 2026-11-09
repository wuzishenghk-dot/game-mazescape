#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
namespace MazeGame.Editor
{
    public class CharacterTextureImport : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (assetPath != "Assets/Resources/Characters/Adventurers.png" && assetPath != "Assets/Resources/Monsters/Monsters.png") return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.isReadable = true;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
        }
    }
}
#endif
