using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class FireGodProceduralPreviewExporter
{
    private const int Width = 540;
    private const int Height = 960;
    private const int FrameRate = 60;
    private const int NativeFrames = 96;
    private const int SlowFrames = 192;
    private const float Duration = 1.6f;
    private const int SectionFrames = 144;
    private static readonly string[][] AllSections =
    {
        new[] { "FireGodFlameProjectile", "FireGodFlameImpact", "FireGodAshOne",
            "FireGodAshTwo", "FireGodAshSigil", "FireGodAshWarning", "FireGodAshDetonation",
            "FireGodAshDissolve" },
        new[] { "FireGodHellfireImpact", "FireGodMagmaLoop", "FireGodMagmaBurst" },
        new[] { "FireGodCrimsonGaleCore", "FireGodCrimsonGaleRibbon",
            "FireGodCrimsonGaleSparks", "FireGodCrimsonGaleImpact",
            "FireGodCrimsonGaleResidue", "FireGodScorchLoop",
            "FireGodFirestormConvert", "FireGodFirestormLoop" },
        new[] { "FireGodFlameShieldSpawn", "FireGodFlameShieldLoop",
            "FireGodFlameShieldBreak", "FireGodHeatAura",
            "FireGodEmberProjectile", "FireGodEmberIgnite" }
    };

    [MenuItem("Tools/Fire God/Export Procedural Preview")]
    public static void Export()
    {
        var output = Path.GetFullPath(Path.Combine(
            Application.dataPath, "../Library/FireGodProceduralPreviewFrames"));
        Directory.CreateDirectory(output);

        for (var index = 0; index < NativeFrames + SlowFrames; index++)
        {
            var sourceTime = index < NativeFrames
                ? index / (float)FrameRate
                : (index - NativeFrames) / (FrameRate * 0.5f);
            var frame = RenderFrame(sourceTime, Width, Height);
            File.WriteAllBytes(
                Path.Combine(output, $"frame_{index:000}.png"),
                frame.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(frame);

            if ((index + 1) % 24 == 0)
            {
                Debug.Log($"Procedural fire preview: {index + 1}/{NativeFrames + SlowFrames}");
            }
        }

        Debug.Log($"Procedural fire preview frames exported to {output}");
    }

    [MenuItem("Tools/Fire God/Export All Procedural VFX")]
    public static void ExportAll()
    {
        var output = Path.GetFullPath(Path.Combine(
            Application.dataPath, "../Library/FireGodAllProceduralPreviewFrames"));
        Directory.CreateDirectory(output);

        for (var sectionIndex = 0; sectionIndex < AllSections.Length; sectionIndex++)
        {
            var root = new GameObject($"Fire God Procedural Section {sectionIndex + 1}");
            var cameraObject = new GameObject("Preview Camera");
            var renderTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            Camera camera = null;
            try
            {
                camera = cameraObject.AddComponent<Camera>();
                camera.transform.position = new Vector3(0f, 0f, -10f);
                camera.orthographic = true;
                camera.orthographicSize = 8.5f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.025f, 0.008f, 0.045f, 1f);
                camera.targetTexture = renderTexture;

                var names = AllSections[sectionIndex];
                var renderers = new SpriteRenderer[names.Length];
                var spriteFrames = new Sprite[names.Length][];
                var rows = Mathf.CeilToInt(names.Length / 2f);
                for (var effectIndex = 0; effectIndex < names.Length; effectIndex++)
                {
                    var frames = Resources.LoadAll<Sprite>($"VFX/FireGod/{names[effectIndex]}");
                    if (frames == null || frames.Length == 0)
                    {
                        throw new InvalidOperationException($"Missing preview resource {names[effectIndex]}.");
                    }
                    Array.Sort(frames, (left, right) => string.CompareOrdinal(left.name, right.name));
                    spriteFrames[effectIndex] = frames;

                    var layer = new GameObject(names[effectIndex]);
                    layer.transform.SetParent(root.transform, false);
                    layer.transform.localPosition = new Vector3(
                        effectIndex % 2 == 0 ? -2.3f : 2.3f,
                        (rows - 1) * 1.8f - effectIndex / 2 * 3.6f,
                        0f);
                    var renderer = layer.AddComponent<SpriteRenderer>();
                    renderer.sprite = frames[0];
                    renderer.sortingOrder = 100 + effectIndex;
                    var bounds = renderer.sprite.bounds.size;
                    var scale = Mathf.Min(3.7f / bounds.x, 3.2f / bounds.y);
                    layer.transform.localScale = Vector3.one * scale;
                    ApplyRuntimeProfile(renderer, names[effectIndex], Vector2.up);
                    renderers[effectIndex] = renderer;
                }

                renderTexture.Create();
                for (var localFrame = 0; localFrame < SectionFrames; localFrame++)
                {
                    var sourceTime = localFrame / (float)FrameRate;
                    for (var effectIndex = 0; effectIndex < renderers.Length; effectIndex++)
                    {
                        var frames = spriteFrames[effectIndex];
                        renderers[effectIndex].sprite = frames[
                            Mathf.FloorToInt(sourceTime * 30f) % frames.Length];
                        var properties = new MaterialPropertyBlock();
                        renderers[effectIndex].GetPropertyBlock(properties);
                        properties.SetFloat("_PreviewTime", sourceTime);
                        renderers[effectIndex].SetPropertyBlock(properties);
                    }

                    camera.Render();
                    RenderTexture.active = renderTexture;
                    var frame = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                    frame.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
                    frame.Apply(false, false);
                    var outputIndex = sectionIndex * SectionFrames + localFrame;
                    File.WriteAllBytes(
                        Path.Combine(output, $"frame_{outputIndex:000}.png"),
                        frame.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(frame);

                    if ((localFrame + 1) % 24 == 0)
                    {
                        Debug.Log(
                            $"All procedural VFX preview: {outputIndex + 1}/{AllSections.Length * SectionFrames}");
                    }
                }
            }
            finally
            {
                RenderTexture.active = previousActive;
                if (camera != null)
                {
                    camera.targetTexture = null;
                }
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        Debug.Log($"All procedural VFX preview frames exported to {output}");
    }

    internal static Texture2D RenderFrame(float sourceTime, int width, int height)
    {
        var shader = Resources.Load<Shader>("Shaders/FireGodProceduralFire");
        if (shader == null || !shader.isSupported)
        {
            throw new InvalidOperationException("Procedural fire shader is missing or unsupported.");
        }

        var root = new GameObject("Fire God Procedural Preview");
        var cameraObject = new GameObject("Preview Camera");
        var materials = new List<Material>();
        var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        var previousActive = RenderTexture.active;
        Camera camera = null;
        try
        {
            camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 8.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.008f, 0.045f, 1f);
            camera.targetTexture = renderTexture;

            var progress = Mathf.Clamp01(sourceTime / Duration);
            var fadeIn = Mathf.SmoothStep(0f, 1f, progress / 0.1f);
            var fadeOut = 1f - Mathf.SmoothStep(
                0f, 1f, Mathf.InverseLerp(0.68f, 1f, progress));
            var fade = fadeIn * fadeOut;

            AddFlowLayer(root.transform, shader, materials,
                new Vector3(0f, 0.45f, 0f), new Vector2(8.8f, 5.4f),
                0.3f, 0.72f, 108, sourceTime, fade);
            AddFlowLayer(root.transform, shader, materials,
                new Vector3(0.25f, 0.18f, 0f), new Vector2(7.2f, 4.45f),
                4.7f, 1.05f, 111, sourceTime, fade);

            var spriteFrame = Mathf.Clamp(Mathf.FloorToInt(sourceTime * 30f), 0, 47);
            AddSpriteLayer(root.transform, "FireGodCrimsonGaleResidue", 109, spriteFrame);
            AddSpriteLayer(root.transform, "FireGodCrimsonGaleRibbon", 110, spriteFrame);
            AddSpriteLayer(root.transform, "FireGodCrimsonGaleCore", 112, spriteFrame);
            AddSpriteLayer(root.transform, "FireGodCrimsonGaleSparks", 113, spriteFrame);
            AddSpriteLayer(root.transform, "FireGodCrimsonGaleImpact", 114, spriteFrame);

            renderTexture.Create();
            camera.Render();
            RenderTexture.active = renderTexture;
            var result = new Texture2D(width, height, TextureFormat.RGB24, false);
            result.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            result.Apply(false, false);
            return result;
        }
        finally
        {
            RenderTexture.active = previousActive;
            if (camera != null)
            {
                camera.targetTexture = null;
            }
            renderTexture.Release();
            UnityEngine.Object.DestroyImmediate(renderTexture);
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            foreach (var material in materials)
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }
    }

    internal static Texture2D RenderEffectFrame(
        string resourceName,
        float sourceTime,
        int width,
        int height)
    {
        var frames = Resources.LoadAll<Sprite>($"VFX/FireGod/{resourceName}");
        if (frames == null || frames.Length == 0)
        {
            throw new InvalidOperationException($"Missing preview resource {resourceName}.");
        }
        Array.Sort(frames, (left, right) => string.CompareOrdinal(left.name, right.name));

        var root = new GameObject($"{resourceName} Procedural Preview");
        var cameraObject = new GameObject("Preview Camera");
        var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        var previousActive = RenderTexture.active;
        Camera camera = null;
        try
        {
            camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.008f, 0.045f, 1f);
            camera.targetTexture = renderTexture;

            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[Mathf.FloorToInt(sourceTime * 30f) % frames.Length];
            renderer.sortingOrder = 100;
            var bounds = renderer.sprite.bounds.size;
            var scale = Mathf.Min(3.7f / bounds.x, 6f / bounds.y);
            root.transform.localScale = Vector3.one * scale;
            ApplyRuntimeProfile(renderer, resourceName, Vector2.up);
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            properties.SetFloat("_PreviewTime", sourceTime);
            renderer.SetPropertyBlock(properties);

            renderTexture.Create();
            camera.Render();
            RenderTexture.active = renderTexture;
            var result = new Texture2D(width, height, TextureFormat.RGB24, false);
            result.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            result.Apply(false, false);
            return result;
        }
        finally
        {
            RenderTexture.active = previousActive;
            if (camera != null)
            {
                camera.targetTexture = null;
            }
            renderTexture.Release();
            UnityEngine.Object.DestroyImmediate(renderTexture);
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }

    private static void AddFlowLayer(
        Transform parent,
        Shader shader,
        ICollection<Material> materials,
        Vector3 localPosition,
        Vector2 size,
        float phase,
        float intensity,
        int sortingOrder,
        float sourceTime,
        float fade)
    {
        var layer = new GameObject("Flowing Fire Preview");
        layer.transform.SetParent(parent, false);
        layer.transform.localPosition = localPosition;
        layer.transform.localScale = new Vector3(size.x, size.y, 1f);
        layer.AddComponent<MeshFilter>().sharedMesh =
            Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        var renderer = layer.AddComponent<MeshRenderer>();
        renderer.sortingOrder = sortingOrder;
        var material = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        material.SetFloat("_Phase", phase);
        material.SetFloat("_Intensity", intensity);
        material.SetFloat("_Fade", fade);
        material.SetFloat("_PreviewTime", sourceTime);
        renderer.sharedMaterial = material;
        materials.Add(material);
    }

    private static void AddSpriteLayer(
        Transform parent,
        string resourceName,
        int sortingOrder,
        int frameIndex)
    {
        var frames = Resources.LoadAll<Sprite>($"VFX/FireGod/{resourceName}");
        if (frames.Length != 48)
        {
            throw new InvalidOperationException($"{resourceName} must contain 48 frames.");
        }
        Array.Sort(frames, (left, right) => string.CompareOrdinal(left.name, right.name));

        var layer = new GameObject(resourceName);
        layer.transform.SetParent(parent, false);
        layer.transform.localScale = Vector3.one * 2.35f;
        var renderer = layer.AddComponent<SpriteRenderer>();
        renderer.sprite = frames[frameIndex];
        renderer.sortingOrder = sortingOrder;
    }

    private static void ApplyRuntimeProfile(
        SpriteRenderer renderer,
        string resourceName,
        Vector2 flowDirection)
    {
        var type = Type.GetType("PrototypeFireVfx, Assembly-CSharp");
        var method = type?.GetMethod(
            "ApplyProceduralMaterial", BindingFlags.NonPublic | BindingFlags.Static);
        if (method == null || !(bool)method.Invoke(
            null, new object[] { renderer, resourceName, flowDirection }))
        {
            throw new InvalidOperationException($"Could not apply profile for {resourceName}.");
        }
    }
}
