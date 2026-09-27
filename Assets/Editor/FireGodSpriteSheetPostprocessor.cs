using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

internal sealed class FireGodSpriteSheetPostprocessor : AssetPostprocessor
{
    private static readonly Dictionary<string, int> Counts = new Dictionary<string, int>
    {
        { "FireGodIdle", 8 },
        { "FireGodWalk", 8 },
        { "FireGodRun", 13 },
        { "FireGodActive", 13 },
        { "FireGodUltimate", 13 },
        { "FireGodHit", 9 },
        { "FireGodDeath", 9 }
    };

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Art/Characters/Nova/Combat/"))
        {
            return;
        }

        var sheetName = Path.GetFileNameWithoutExtension(assetPath);
        if (!Counts.TryGetValue(sheetName, out var count))
        {
            return;
        }

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 68f;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        var sprites = new SpriteMetaData[count];
        for (var index = 0; index < count; index++)
        {
            sprites[index] = new SpriteMetaData
            {
                name = $"{sheetName}_{index:00}",
                rect = new Rect(index * 68, 0, 68, 68),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            };
        }
        importer.spritesheet = sprites;
    }
}
