using UnityEngine;

/// <summary>
/// Builds a compact Picopark-style relay course at runtime. The course uses
/// simple colliders so it remains easy to replace with authored art later.
/// </summary>
public sealed class SlimeRelayCourse : MonoBehaviour
{
    private static readonly Color SlimeColor = new Color(0.18f, 0.85f, 0.35f);
    private static readonly Color WingColor = new Color(0.2f, 0.65f, 1f);
    private static readonly Color CeilingColor = new Color(0.32f, 0.38f, 0.34f);
    private static readonly Color KeyColor = new Color(1f, 0.84f, 0.18f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        // The standalone puzzle scene owns its complete layout.  Keep this
        // legacy runtime course available to the old scene only, without
        // touching any player resources or form behaviour.
        if (Object.FindObjectOfType<PuzzleSceneMarker>() != null)
        {
            return;
        }

        if (Object.FindObjectOfType<SlimeRelayCourse>() != null)
        {
            return;
        }

        GameObject courseObject = new GameObject("SlimeRelayCourse");
        courseObject.AddComponent<SlimeRelayCourse>();
    }

    private void Awake()
    {
        gameObject.AddComponent<KeyDoorManager>();
        BuildCourse();
    }

    private void BuildCourse()
    {
        // Shared hub: collect all three keys in any order before opening the exit.
        CreatePlatform("HubFloor", new Vector2(0f, -2.8f), new Vector2(19f, 0.4f), CeilingColor);
        CreatePlatform("ExitFloor", new Vector2(14f, -2.8f), new Vector2(10f, 0.4f), CeilingColor);

        // Key A: ground-connected horizontal slit that only the compressed slime can enter.
        GameObject compressionCeiling = CreatePlatform(
            "CompressionCeiling",
            new Vector2(-3.5f, -1.65f),
            new Vector2(4.8f, 0.3f),
            SlimeColor);
        BoxCollider2D compressionCollider = compressionCeiling.GetComponent<BoxCollider2D>();
        compressionCollider.edgeRadius = 0.2f;
        CreateKey("CompressionKey", new Vector2(-1.45f, -2.15f));

        // Key B: a solid gray column with a wider trigger zone for slow climbing.
        GameObject climbPole = CreatePlatform("GrayClimbPole", new Vector2(3.4f, -0.15f), new Vector2(0.45f, 4.9f), CeilingColor);
        climbPole.AddComponent<ClimbablePole>();
        CreatePlatform("TowerKeyDeck", new Vector2(4.75f, 2.25f), new Vector2(2.6f, 0.35f), CeilingColor);
        CreateKey("TowerKey", new Vector2(5.1f, 2.8f));

        // Key C: wing route is bounded by a roof, so it cannot bypass the map from above.
        CreatePlatform("WingLaunch", new Vector2(7.25f, 2.25f), new Vector2(3f, 0.35f), WingColor);
        CreatePlatform("WingRoof", new Vector2(9.5f, 3.45f), new Vector2(8.5f, 0.3f), CeilingColor);
        CreatePlatform("WingKeyDeck", new Vector2(10.8f, 0.35f), new Vector2(2.2f, 0.35f), WingColor);
        CreateKey("WingKey", new Vector2(10.8f, 0.95f));

        // All branches lead back to the hub; after three keys, enter the right exit.
        CreatePlatform("WingReturnRamp", new Vector2(12.3f, -1.1f), new Vector2(2.2f, 0.35f), WingColor);
        GameObject exitDoor = CreatePlatform("ExitDoor", new Vector2(13.4f, -1.55f), new Vector2(0.55f, 2.2f), CeilingColor);
        exitDoor.AddComponent<LevelExitDoor>();
        CreatePlatform("ExitMarker", new Vector2(16.2f, -2.15f), new Vector2(2.2f, 0.35f), WingColor);
    }

    private static void CreateKey(string keyName, Vector2 position)
    {
        GameObject key = new GameObject(keyName);
        key.transform.position = position;

        CircleCollider2D collider = key.AddComponent<CircleCollider2D>();
        collider.radius = 0.3f;
        collider.isTrigger = true;

        SpriteRenderer renderer = key.AddComponent<SpriteRenderer>();
        renderer.sprite = CreatePixelSprite();
        renderer.color = KeyColor;
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = new Vector2(0.55f, 0.55f);
        renderer.sortingOrder = 2;

        key.AddComponent<LevelKey>();
    }

    private static GameObject CreatePlatform(string platformName, Vector2 position, Vector2 size, Color color)
    {
        GameObject platform = new GameObject(platformName);
        platform.transform.position = position;

        BoxCollider2D collider = platform.AddComponent<BoxCollider2D>();
        collider.size = size;

        SpriteRenderer renderer = platform.AddComponent<SpriteRenderer>();
        renderer.sprite = CreatePixelSprite();
        renderer.color = color;
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = size;
        renderer.sortingOrder = -1;
        return platform;
    }

    private static Sprite CreatePixelSprite()
    {
        return Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f,
            0,
            SpriteMeshType.FullRect);
    }
}
