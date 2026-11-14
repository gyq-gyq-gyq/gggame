using UnityEngine;

[DisallowMultipleComponent]
[ExecuteAlways]
public sealed class WastelandBackground : MonoBehaviour
{
    [Header("Sky")]
    [SerializeField] private Color skyTop = new Color(0.18f, 0.14f, 0.13f, 1f);
    [SerializeField] private Color skyBottom = new Color(0.46f, 0.29f, 0.18f, 1f);

    [Header("Silhouettes")]
    [SerializeField] private Color distantRuinColor = new Color(0.12f, 0.09f, 0.08f, 1f);
    [SerializeField] private Color midRuinColor = new Color(0.19f, 0.14f, 0.11f, 1f);
    [SerializeField] private Color hazeColor = new Color(0.32f, 0.23f, 0.16f, 0.42f);
    [SerializeField] private Color rustAccent = new Color(0.43f, 0.19f, 0.09f, 1f);
    [SerializeField] private Color toxicAccent = new Color(0.20f, 0.32f, 0.16f, 1f);

    private const string BackgroundRootName = "WastelandBackgroundRuntime";

    private Camera targetCamera;
    private Sprite whiteSprite;
    private Texture2D whiteTexture;
    private Texture2D skyTexture;
    private Transform backgroundRoot;

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            ClearBackground();
            BuildBackground(false);
        }
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += BuildEditorBackground;
#endif
        }
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall -= BuildEditorBackground;
#endif
        ClearBackground();
        ReleaseTextures();
    }

    private void OnDestroy()
    {
        ReleaseTextures();
    }

    private void BuildBackground(bool editorPreview)
    {
        if (backgroundRoot != null)
        {
            return;
        }

        targetCamera = GetComponent<Camera>();
        if (targetCamera == null || !targetCamera.orthographic)
        {
            return;
        }

        float viewHeight = targetCamera.orthographicSize * 2f;
        float viewWidth = viewHeight * targetCamera.aspect;
        float halfWidth = viewWidth * 0.5f;
        float halfHeight = viewHeight * 0.5f;

        whiteSprite = CreateWhiteSprite();
        skyTexture = CreateSkyGradient(skyBottom, skyTop);

        if (Application.isPlaying)
        {
            targetCamera.backgroundColor = skyTop;
        }

        backgroundRoot = new GameObject(BackgroundRootName).transform;
        backgroundRoot.SetParent(transform, false);
        backgroundRoot.localPosition = new Vector3(0f, 0f, 10f);
        if (editorPreview)
        {
            backgroundRoot.gameObject.hideFlags = HideFlags.HideAndDontSave;
        }

        CreateQuad(
            "SkyGradient",
            backgroundRoot,
            new Vector3(0f, 0f, 0f),
            new Vector2(viewWidth, viewHeight),
            Sprite.Create(
                skyTexture,
                new Rect(0f, 0f, skyTexture.width, skyTexture.height),
                new Vector2(0.5f, 0.5f),
                100f),
            -100,
            Color.white,
            editorPreview);

        BuildDistantRuins(halfWidth, halfHeight, editorPreview);
        BuildMidGround(halfWidth, halfHeight, editorPreview);
        BuildForegroundAccents(halfWidth, halfHeight, editorPreview);

        CreateQuad(
            "WastelandHaze",
            backgroundRoot,
            new Vector3(0f, -halfHeight * 0.08f, 0f),
            new Vector2(viewWidth, viewHeight * 0.42f),
            whiteSprite,
            -55,
            hazeColor,
            editorPreview);
    }

    private void BuildDistantRuins(float halfWidth, float halfHeight, bool editorPreview)
    {
        int ruinCount = Mathf.Max(7, Mathf.CeilToInt(halfWidth * 1.8f));
        float spacing = (halfWidth * 2f) / ruinCount;

        for (int i = 0; i < ruinCount; i++)
        {
            float t = i / (float)(ruinCount - 1);
            float x = -halfWidth + spacing * i;
            float height = Mathf.Lerp(halfHeight * 0.28f, halfHeight * 0.52f, Mathf.PerlinNoise(i * 0.37f, 1f));
            float y = -halfHeight + height * 0.42f;
            CreateQuad(
                "DistantRuin_" + (i + 1).ToString("00"),
                backgroundRoot,
                new Vector3(x, y, 0f),
                new Vector2(spacing * 0.82f, height),
                whiteSprite,
                -90,
                Color.Lerp(distantRuinColor, skyBottom, t * 0.18f),
                editorPreview);
        }
    }

    private void BuildMidGround(float halfWidth, float halfHeight, bool editorPreview)
    {
        float groundHeight = halfHeight * 0.24f;
        CreateQuad(
            "MidGroundBand",
            backgroundRoot,
            new Vector3(0f, -halfHeight + groundHeight * 0.5f, 0f),
            new Vector2(halfWidth * 2f, groundHeight),
            whiteSprite,
            -80,
            midRuinColor,
            editorPreview);

        int wreckCount = Mathf.Max(5, Mathf.CeilToInt(halfWidth * 1.2f));
        float spacing = (halfWidth * 2f) / wreckCount;

        for (int i = 0; i < wreckCount; i++)
        {
            float x = -halfWidth + spacing * (i + 0.5f);
            float height = Mathf.Lerp(groundHeight * 0.45f, groundHeight * 0.95f, Mathf.PerlinNoise(i * 0.83f, 2f));
            CreateQuad(
                "MidWreck_" + (i + 1).ToString("00"),
                backgroundRoot,
                new Vector3(x, -halfHeight + groundHeight + height * 0.5f, 0f),
                new Vector2(spacing * 0.42f, height),
                whiteSprite,
                -78,
                Color.Lerp(midRuinColor, rustAccent, (i % 3) * 0.2f),
                editorPreview);
        }
    }

    private void BuildForegroundAccents(float halfWidth, float halfHeight, bool editorPreview)
    {
        int accentCount = Mathf.Max(4, Mathf.CeilToInt(halfWidth * 0.8f));
        float spacing = (halfWidth * 2f) / accentCount;

        for (int i = 0; i < accentCount; i++)
        {
            float x = -halfWidth + spacing * (i + 0.5f) + Mathf.PerlinNoise(i * 1.17f, 3f) * spacing * 0.35f;
            float height = Mathf.Lerp(0.12f, 0.34f, Mathf.PerlinNoise(i * 1.91f, 4f));
            float y = -halfHeight + height * 0.5f;
            CreateQuad(
                "WreckAccent_" + (i + 1).ToString("00"),
                backgroundRoot,
                new Vector3(x, y, 0f),
                new Vector2(0.12f, height),
                whiteSprite,
                -70,
                i % 2 == 0 ? rustAccent : toxicAccent,
                editorPreview);
        }
    }

    private void CreateQuad(
        string name,
        Transform parent,
        Vector3 localPosition,
        Vector2 size,
        Sprite sprite,
        int sortingOrder,
        Color color,
        bool editorPreview)
    {
        GameObject quad = new GameObject(name);
        quad.transform.SetParent(parent, false);
        quad.transform.localPosition = localPosition;
        if (editorPreview)
        {
            quad.hideFlags = HideFlags.HideAndDontSave;
        }

        SpriteRenderer renderer = quad.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.drawMode = SpriteDrawMode.Simple;
        renderer.sortingOrder = sortingOrder;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        Vector2 spriteSize = sprite.bounds.size;
        quad.transform.localScale = new Vector3(
            size.x / Mathf.Max(0.001f, spriteSize.x),
            size.y / Mathf.Max(0.001f, spriteSize.y),
            1f);
    }

    private Sprite CreateWhiteSprite()
    {
        whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        whiteTexture.name = "WastelandBackgroundWhite";
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();
        return Sprite.Create(
            whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            100f);
    }

    private Texture2D CreateSkyGradient(Color bottom, Color top)
    {
        const int width = 128;
        const int height = 256;

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = "WastelandBackgroundSkyGradient";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < height; y++)
        {
            Color color = Color.Lerp(bottom, top, y / (float)(height - 1));
            for (int x = 0; x < width; x++)
            {
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply(false, true);
        return texture;
    }

    private void ClearBackground()
    {
        if (backgroundRoot == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(backgroundRoot.gameObject);
        }
        else
        {
            DestroyImmediate(backgroundRoot.gameObject);
        }

        backgroundRoot = null;
    }

    private void ReleaseTextures()
    {
        if (skyTexture != null)
        {
            if (Application.isPlaying)
            {
                Destroy(skyTexture);
            }
            else
            {
                DestroyImmediate(skyTexture);
            }

            skyTexture = null;
        }

        if (whiteTexture != null)
        {
            if (Application.isPlaying)
            {
                Destroy(whiteTexture);
            }
            else
            {
                DestroyImmediate(whiteTexture);
            }

            whiteTexture = null;
        }
    }

#if UNITY_EDITOR
    private void BuildEditorBackground()
    {
        if (this == null || !isActiveAndEnabled || Application.isPlaying || backgroundRoot != null)
        {
            return;
        }

        BuildBackground(true);
    }
#endif
}
