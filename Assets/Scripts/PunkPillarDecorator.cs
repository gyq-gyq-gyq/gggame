using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[ExecuteAlways]
public sealed class PunkPillarDecorator : MonoBehaviour
{
    [Header("Punk Metal")]
    [SerializeField] private Color metalColor = new Color(0.20f, 0.22f, 0.24f, 1f);
    [SerializeField] private Color neonAccent = new Color(0.95f, 0.20f, 0.58f, 1f);
    [SerializeField] private Color boltColor = new Color(0.85f, 0.78f, 0.28f, 1f);
    [SerializeField] private float bandHeight = 0.09f;
    [SerializeField] private int bandCount = 4;

    [Header("Vines")]
    [SerializeField] private Color vineColor = new Color(0.16f, 0.48f, 0.20f, 1f);
    [SerializeField] private Color leafColor = new Color(0.30f, 0.72f, 0.24f, 1f);
    [SerializeField] private int vinePointCount = 22;
    [SerializeField] private int vinePathCount = 3;

    private readonly List<GameObject> decorations = new List<GameObject>();

    private void OnEnable()
    {
        RefreshDecorations();
    }

    private void OnDisable()
    {
        ClearDecorations();
    }

    private void RefreshDecorations()
    {
        ClearDecorations();

        GameObject[] pillars = GameObject.FindGameObjectsWithTag("ClimbableTree");
        for (int i = 0; i < pillars.Length; i++)
        {
            DecoratePillar(pillars[i]);
        }
    }

    private void DecoratePillar(GameObject pillar)
    {
        SpriteRenderer source = pillar.GetComponent<SpriteRenderer>();
        if (source == null)
        {
            return;
        }

        float width = source.size.x;
        float height = source.size.y;

        for (int i = 0; i < bandCount; i++)
        {
            float t = (i + 1f) / (bandCount + 1f);
            float y = Mathf.Lerp(-height * 0.5f, height * 0.5f, t);
            CreateRect(pillar.transform, "PunkBand_" + (i + 1).ToString("00"),
                new Vector3(0f, y, 0f), new Vector2(width * 1.18f, bandHeight),
                metalColor, 2, 0f);
            CreateRect(pillar.transform, "PunkNeon_" + (i + 1).ToString("00"),
                new Vector3(0f, y + bandHeight * 0.35f, 0f),
                new Vector2(width * 1.06f, bandHeight * 0.18f),
                neonAccent, 3, 0f);

            CreateRect(pillar.transform, "PunkBolt_L_" + (i + 1).ToString("00"),
                new Vector3(-width * 0.42f, y, 0f), new Vector2(0.07f, 0.07f),
                boltColor, 4, 45f);
            CreateRect(pillar.transform, "PunkBolt_R_" + (i + 1).ToString("00"),
                new Vector3(width * 0.42f, y, 0f), new Vector2(0.07f, 0.07f),
                boltColor, 4, 45f);
        }

        for (int path = 0; path < vinePathCount; path++)
        {
            CreateVinePath(pillar.transform, width, height, path);
        }
    }

    private void CreateVinePath(Transform parent, float width, float height, int path)
    {
        float phase = path * 1.7f + 0.4f;

        for (int i = 0; i < vinePointCount; i++)
        {
            float t = i / (float)(vinePointCount - 1);
            float y = Mathf.Lerp(-height * 0.48f, height * 0.48f, t);
            float x = Mathf.Sin(t * Mathf.PI * 2.5f + phase) * width * 0.28f;

            CreateRect(parent, "Vine_" + path + "_" + i.ToString("00"),
                new Vector3(x, y, 0f), new Vector2(0.055f, 0.055f),
                vineColor, 5, 0f);

            if (i % 4 == 0)
            {
                CreateRect(parent, "Leaf_" + path + "_" + i.ToString("00"),
                    new Vector3(x + width * 0.07f, y + 0.04f, 0f),
                    new Vector2(0.10f, 0.06f), leafColor, 6, 45f);
            }
        }
    }

    private void CreateRect(
        Transform parent,
        string name,
        Vector3 localPosition,
        Vector2 size,
        Color color,
        int sortingOrder,
        float localRotationZ)
    {
        GameObject decoration = new GameObject(name);
        decoration.transform.SetParent(parent, false);
        decoration.transform.localPosition = localPosition;
        decoration.transform.localRotation = Quaternion.Euler(0f, 0f, localRotationZ);

        if (!Application.isPlaying)
        {
            decoration.hideFlags = HideFlags.HideAndDontSave;
        }

        SpriteRenderer renderer = decoration.AddComponent<SpriteRenderer>();
        renderer.sprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        renderer.color = color;
        renderer.drawMode = SpriteDrawMode.Simple;
        renderer.sortingOrder = sortingOrder;
        decoration.transform.localScale = new Vector3(size.x, size.y, 1f);

        decorations.Add(decoration);
    }

    private void ClearDecorations()
    {
        for (int i = 0; i < decorations.Count; i++)
        {
            GameObject decoration = decorations[i];
            if (decoration == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(decoration);
            }
            else
            {
                DestroyImmediate(decoration);
            }
        }

        decorations.Clear();
    }
}
