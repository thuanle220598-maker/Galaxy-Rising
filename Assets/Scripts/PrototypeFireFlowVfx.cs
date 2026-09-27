using UnityEngine;

internal sealed class PrototypeFireFlowVfx : MonoBehaviour
{
    private static readonly int FadeId = Shader.PropertyToID("_Fade");
    private MeshRenderer meshRenderer;
    private Material material;
    private MaterialPropertyBlock properties;
    private float duration;
    private float elapsed;

    internal static PrototypeFireFlowVfx Spawn(
        Transform parent,
        Vector3 localPosition,
        Vector2 size,
        float phase,
        float intensity,
        int sortingOrder,
        float duration)
    {
        var shader = Resources.Load<Shader>("Shaders/FireGodProceduralFire");
        if (shader == null || !shader.isSupported)
        {
            return null;
        }

        var layer = new GameObject("Flowing Fire");
        layer.transform.SetParent(parent, false);
        layer.transform.localPosition = localPosition;
        layer.transform.localRotation = Quaternion.Inverse(parent.rotation);
        layer.transform.localScale = new Vector3(size.x, size.y, 1f);

        var meshFilter = layer.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

        var effect = layer.AddComponent<PrototypeFireFlowVfx>();
        effect.meshRenderer = layer.AddComponent<MeshRenderer>();
        effect.meshRenderer.sortingOrder = sortingOrder;
        effect.meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        effect.meshRenderer.receiveShadows = false;
        effect.material = new Material(shader)
        {
            hideFlags = HideFlags.DontSave
        };
        effect.material.SetFloat("_Phase", phase);
        effect.material.SetFloat("_Intensity", intensity);
        effect.meshRenderer.sharedMaterial = effect.material;
        effect.properties = new MaterialPropertyBlock();
        effect.duration = Mathf.Max(0.1f, duration);
        effect.SetFade(0f);
        return effect;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        var progress = Mathf.Clamp01(elapsed / duration);
        var fadeIn = Mathf.SmoothStep(0f, 1f, progress / 0.1f);
        var fadeOut = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.68f, 1f, progress));
        SetFade(fadeIn * fadeOut);

        if (elapsed >= duration)
        {
            Destroy(gameObject);
        }
    }

    private void SetFade(float value)
    {
        meshRenderer.GetPropertyBlock(properties);
        properties.SetFloat(FadeId, value);
        meshRenderer.SetPropertyBlock(properties);
    }

    private void OnDestroy()
    {
        if (material != null)
        {
            Destroy(material);
        }
    }
}
