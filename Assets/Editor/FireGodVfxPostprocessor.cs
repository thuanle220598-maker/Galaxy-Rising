using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

internal sealed class FireGodVfxPostprocessor : AssetPostprocessor
{
    private static readonly Dictionary<string, Vector3Int> Sheets = new Dictionary<string, Vector3Int>
    {
        { "FireGodFlameProjectile", new Vector3Int(48, 48, 8) },
        { "FireGodFlameImpact", new Vector3Int(64, 64, 8) },
        { "FireGodAshOne", new Vector3Int(32, 32, 8) },
        { "FireGodAshTwo", new Vector3Int(32, 32, 8) },
        { "FireGodAshSigil", new Vector3Int(64, 64, 8) },
        { "FireGodAshWarning", new Vector3Int(64, 64, 6) },
        { "FireGodAshDetonation", new Vector3Int(96, 96, 10) },
        { "FireGodHellfireImpact", new Vector3Int(128, 128, 12) },
        { "FireGodMagmaLoop", new Vector3Int(128, 128, 8) },
        { "FireGodMagmaBurst", new Vector3Int(128, 128, 10) },
        { "FireGodCrimsonGaleCore", new Vector3Int(256, 160, 48) },
        { "FireGodCrimsonGaleRibbon", new Vector3Int(256, 160, 48) },
        { "FireGodCrimsonGaleSparks", new Vector3Int(256, 160, 48) },
        { "FireGodCrimsonGaleImpact", new Vector3Int(256, 160, 48) },
        { "FireGodCrimsonGaleResidue", new Vector3Int(256, 160, 48) },
        { "FireGodScorchLoop", new Vector3Int(192, 128, 8) },
        { "FireGodFirestormConvert", new Vector3Int(192, 192, 10) },
        { "FireGodFirestormLoop", new Vector3Int(192, 192, 8) },
        { "FireGodFlameShieldSpawn", new Vector3Int(96, 96, 8) },
        { "FireGodFlameShieldLoop", new Vector3Int(96, 96, 8) },
        { "FireGodFlameShieldBreak", new Vector3Int(96, 96, 6) },
        { "FireGodHeatAura", new Vector3Int(96, 96, 8) },
        { "FireGodEmberProjectile", new Vector3Int(48, 48, 8) },
        { "FireGodEmberIgnite", new Vector3Int(64, 64, 6) },
        { "FireGodAshDissolve", new Vector3Int(96, 96, 10) }
    };

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/VFX/FireGod/"))
        {
            return;
        }

        var sheetName = Path.GetFileNameWithoutExtension(assetPath);
        if (!Sheets.TryGetValue(sheetName, out var sheet))
        {
            return;
        }

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 64f;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        var sprites = new SpriteMetaData[sheet.z];
        for (var index = 0; index < sheet.z; index++)
        {
            sprites[index] = new SpriteMetaData
            {
                name = $"{sheetName}_{index:00}",
                rect = new Rect(index * sheet.x, 0, sheet.x, sheet.y),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            };
        }
        importer.spritesheet = sprites;
    }
}
