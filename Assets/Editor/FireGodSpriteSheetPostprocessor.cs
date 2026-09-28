using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

internal sealed class FireGodSpriteSheetPostprocessor : AssetPostprocessor
{
    private static readonly Dictionary<string, Vector2Int> Sheets = new Dictionary<string, Vector2Int>
    {
        { "FireGodIdle", new Vector2Int(8, 68) },
        { "FireGodWalk", new Vector2Int(8, 68) },
        { "FireGodRun", new Vector2Int(13, 68) },
        { "FireGodActive", new Vector2Int(13, 68) },
        { "FireGodUltimate", new Vector2Int(13, 68) },
        { "FireGodHit", new Vector2Int(9, 68) },
        { "FireGodDeath", new Vector2Int(9, 68) },
        { "AureliaIdle", new Vector2Int(6, 64) },
        { "AureliaRun", new Vector2Int(8, 64) },
        { "AureliaBasic", new Vector2Int(6, 64) },
        { "AureliaActive", new Vector2Int(10, 64) },
        { "AureliaUltimate", new Vector2Int(12, 64) },
        { "AureliaHit", new Vector2Int(4, 64) },
        { "AureliaDeath", new Vector2Int(8, 64) },
        { "AureliaWaterSerpent", new Vector2Int(8, 32) },
        { "AureliaHydroBead", new Vector2Int(6, 16) },
        { "AureliaCleansingRing", new Vector2Int(8, 64) },
        { "AureliaDragonAura", new Vector2Int(8, 64) },
        { "AureliaDragonPressure", new Vector2Int(10, 96) },
        { "AureliaDomain", new Vector2Int(12, 128) },
        { "AureliaShieldLoop", new Vector2Int(8, 64) },
        { "AureliaShieldBreak", new Vector2Int(8, 64) },
        { "AureliaUltimateDragon", new Vector2Int(12, 128) },
        { "AureliaBlessingImpact", new Vector2Int(8, 64) },
        { "KronosIdle", new Vector2Int(8, 64) },
        { "KronosRun", new Vector2Int(8, 64) },
        { "KronosBasic", new Vector2Int(8, 64) },
        { "KronosActive", new Vector2Int(12, 64) },
        { "KronosUltimate", new Vector2Int(14, 64) },
        { "KronosHit", new Vector2Int(5, 64) },
        { "KronosDeath", new Vector2Int(10, 64) },
        { "KronosDimensionalTear", new Vector2Int(10, 96) },
        { "KronosVoidField", new Vector2Int(12, 128) },
        { "KronosMassOrbit", new Vector2Int(10, 64) },
        { "KronosResonanceBurst", new Vector2Int(10, 96) },
        { "KronosParasite", new Vector2Int(8, 32) },
        { "KronosParasiteDrain", new Vector2Int(8, 64) },
        { "KronosSingularity", new Vector2Int(12, 128) },
        { "KronosLeviathanAwaken", new Vector2Int(14, 128) },
        { "KronosLeviathanAura", new Vector2Int(12, 96) },
        { "KronosVoidSlash", new Vector2Int(8, 96) },
        { "KronosCollapse", new Vector2Int(10, 96) },
        { "KronosDecayPulse", new Vector2Int(10, 96) }
    };

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Art/Characters/Nova/Combat/") &&
            !assetPath.StartsWith("Assets/Art/Characters/Aurelia/Combat/") &&
            !assetPath.StartsWith("Assets/Resources/VFX/Aurelia/") &&
            !assetPath.StartsWith("Assets/Art/Characters/Kronos/Combat/") &&
            !assetPath.StartsWith("Assets/Resources/VFX/Kronos/"))
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
        importer.spritePixelsPerUnit = sheet.y;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        var sprites = new SpriteMetaData[sheet.x];
        for (var index = 0; index < sheet.x; index++)
        {
            sprites[index] = new SpriteMetaData
            {
                name = $"{sheetName}_{index:00}",
                rect = new Rect(index * sheet.y, 0, sheet.y, sheet.y),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            };
        }
        importer.spritesheet = sprites;
    }
}
