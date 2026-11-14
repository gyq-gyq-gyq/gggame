using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Builds the standalone horizontal 2D puzzle scene without touching player assets.</summary>
public static class PuzzleLevelBuilder
{
    private const string ScenePath = "Assets/Scenes/PuzzleSolo2D.unity";
    private const string SpritePath = "Assets/Generated2D/PuzzleWhite.png";

    private static Sprite sprite;
    private static Transform root;
    private static readonly Dictionary<string, int> Layers = new Dictionary<string, int>();

    [MenuItem("Tools/Puzzle Level/Build Standalone 2D Scene")]
    public static void BuildStandaloneScene()
    {
        EnsureLayers();
        sprite = CreatePersistentWhiteSprite();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        root = new GameObject("LevelRoot").transform;
        root.position = Vector3.zero;

        CreateCamera();
        CreateBackground();
        Transform spawn = BuildArea1();
        BuildArea2();
        BuildArea3();
        BuildArea4();
        BuildArea5();
        BuildArea6(spawn);
        CreateMechanismAnchors();

        GameObject marker = new GameObject("PuzzleSceneMarker");
        marker.transform.SetParent(root, false);
        marker.AddComponent<PuzzleSceneMarker>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();

        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.in2DMode = true;
            SceneView.RepaintAll();
        }

        Debug.Log("PuzzleLevelBuilder: PuzzleSolo2D scene built successfully.");
    }

    public static void BuildSceneFromBatch() => BuildStandaloneScene();

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(root, false);
        cameraObject.transform.position = Vector3.zero; // Explicitly requested for the 2D scene.
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 9f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.055f, 0.075f, 0.11f);
    }

    private static void CreateBackground()
    {
        CreateVisual("BackgroundFill", new Vector2(0f, 0f), new Vector2(72f, 18f),
            new Color(0.08f, 0.12f, 0.18f), "Background", -20);
        CreateVisual("RegionDivider_Left", new Vector2(-22f, 0f), new Vector2(0.08f, 15f),
            new Color(0.18f, 0.25f, 0.32f), "Background", -10);
        CreateVisual("RegionDivider_Right", new Vector2(22f, 0f), new Vector2(0.08f, 15f),
            new Color(0.18f, 0.25f, 0.32f), "Background", -10);
    }

    // Area 1: spawn enclosure, reset platforms, start portal and Mechanism_4.
    private static Transform BuildArea1()
    {
        Transform area = CreateGroup("Area_1_Spawn");
        Transform spawn = CreateEmpty("PlayerSpawn", area, new Vector2(-30f, -5.4f), "Player");
        CreatePlatform("SpawnFloor", area, new Vector2(-28.5f, -6.7f), new Vector2(12f, 0.7f), new Color(0.34f, 0.48f, 0.6f));
        CreatePlatform("SpawnLeftWall", area, new Vector2(-34.2f, -2.7f), new Vector2(0.6f, 8.6f), new Color(0.34f, 0.48f, 0.6f));
        CreatePlatform("SpawnRightWall", area, new Vector2(-22.8f, -2.7f), new Vector2(0.6f, 8.6f), new Color(0.34f, 0.48f, 0.6f));
        CreatePlatform("ResetPlatform_A", area, new Vector2(-31.2f, -4.8f), new Vector2(2.5f, 0.35f), new Color(0.48f, 0.66f, 0.76f));
        CreatePlatform("ResetPlatform_B", area, new Vector2(-27.8f, -3.65f), new Vector2(2.2f, 0.35f), new Color(0.48f, 0.66f, 0.76f));
        CreatePlatform("ResetPlatform_C", area, new Vector2(-24.5f, -4.7f), new Vector2(2.2f, 0.35f), new Color(0.48f, 0.66f, 0.76f));

        Transform landing = CreateEmpty("StartPortalLanding", area, new Vector2(-21.4f, -5.8f), "Player");
        GameObject portal = CreateTriggerSprite("StartPortal", area, new Vector2(-31.2f, -5.75f), new Vector2(0.8f, 1.3f), new Color(0.28f, 0.86f, 1f), "Item");
        portal.AddComponent<PuzzlePortal>().Configure(landing, false);
        return spawn;
    }

    // Area 2: stairs, narrow passage, spike row and Mechanism_8.
    private static void BuildArea2()
    {
        Transform area = CreateGroup("Area_2_LeftMiddle");
        CreatePlatform("LeftCorridorFloor", area, new Vector2(-17.5f, -6.7f), new Vector2(10.4f, 0.7f), new Color(0.34f, 0.48f, 0.6f));
        CreatePlatform("Step_1", area, new Vector2(-20.4f, -5.4f), new Vector2(2f, 0.35f), new Color(0.45f, 0.6f, 0.72f));
        CreatePlatform("Step_2", area, new Vector2(-18.1f, -4.25f), new Vector2(1.7f, 0.35f), new Color(0.45f, 0.6f, 0.72f));
        CreatePlatform("NarrowPassage", area, new Vector2(-14.8f, -3.1f), new Vector2(2.6f, 0.3f), new Color(0.45f, 0.6f, 0.72f));
        CreatePlatform("NarrowRoof", area, new Vector2(-14.8f, -1.95f), new Vector2(2.6f, 0.3f), new Color(0.45f, 0.6f, 0.72f));
        for (int i = 0; i < 5; i++)
        {
            CreateHazard("SpikeRow_" + (i + 1), area, new Vector2(-16.3f + i * 0.72f, -6.05f), new Vector2(0.55f, 0.55f), new Color(0.95f, 0.34f, 0.35f));
        }
        CreateEmpty("Mechanism_8", area, new Vector2(-13f, -4.7f), "Mechanism");
    }

    // Area 3: long route, pressure plate, pickup, destructible platform and poison spout.
    private static void BuildArea3()
    {
        Transform area = CreateGroup("Area_3_CentralRoute");
        CreatePlatform("CentralLongWalkway", area, new Vector2(-1f, -4.2f), new Vector2(21f, 0.6f), new Color(0.38f, 0.54f, 0.67f));
        GameObject plateObject = CreateTriggerSprite("Mechanism_1_PressurePlate", area, new Vector2(-8f, -3.72f), new Vector2(1.35f, 0.22f), new Color(1f, 0.76f, 0.2f), "Mechanism");
        PuzzlePressurePlate plate = plateObject.AddComponent<PuzzlePressurePlate>();
        CreatePickup("Mechanism_2_ShieldPotion", area, new Vector2(-5.7f, -3.2f), new Color(0.3f, 0.95f, 0.9f));
        GameObject destructible = CreatePlatform("BreakableHorizontalPlatform", area, new Vector2(-0.5f, -1.75f), new Vector2(4.2f, 0.35f), new Color(0.87f, 0.64f, 0.33f));
        destructible.AddComponent<PuzzleBreakablePlatform>();
        GameObject spout = CreateTriggerSprite("PeriodicPoisonSpout", area, new Vector2(3.5f, -3.45f), new Vector2(0.8f, 1.3f), new Color(0.2f, 0.35f, 0.16f), "Trap");
        PuzzlePoisonSpout poison = spout.AddComponent<PuzzlePoisonSpout>();
        plate.Configure(null, poison);
        for (int i = 0; i < 7; i++)
        {
            CreateHazard("CentralCeilingSpike_" + (i + 1), area, new Vector2(4.6f + i * 0.65f, -0.55f), new Vector2(0.5f, 0.5f), new Color(0.95f, 0.34f, 0.35f));
        }
        CreateEmpty("Mechanism_5", area, new Vector2(7.5f, -3.4f), "Mechanism");
    }

    // Area 4: pressure-driven moving platform, gap, saw and low breakable platform.
    private static void BuildArea4()
    {
        Transform area = CreateGroup("Area_4_RightMiddle");
        CreatePlatform("RightMiddleFloor_A", area, new Vector2(11.3f, -4.2f), new Vector2(3.8f, 0.6f), new Color(0.38f, 0.54f, 0.67f));
        CreatePlatform("RightMiddleFloor_B", area, new Vector2(18.9f, -4.2f), new Vector2(3.4f, 0.6f), new Color(0.38f, 0.54f, 0.67f));
        GameObject moving = CreatePlatform("Mechanism_MovingPlatform", area, new Vector2(14.4f, -3.2f), new Vector2(2.4f, 0.32f), new Color(0.72f, 0.5f, 0.96f));
        PuzzleMovingPlatform movement = moving.AddComponent<PuzzleMovingPlatform>();
        movement.Configure(new Vector2(0f, 3.2f), 1.3f);
        CreateHazard("SawDanger", area, new Vector2(17.2f, -3.45f), new Vector2(1.05f, 1.05f), new Color(0.95f, 0.42f, 0.18f));
        GameObject lowBreak = CreatePlatform("Mechanism_3_LowBreakPlatform", area, new Vector2(20.5f, -5.55f), new Vector2(2.4f, 0.32f), new Color(0.87f, 0.64f, 0.33f));
        lowBreak.AddComponent<PuzzleBreakablePlatform>();

        PuzzlePressurePlate plate = Object.FindObjectOfType<PuzzlePressurePlate>();
        PuzzlePoisonSpout poison = Object.FindObjectOfType<PuzzlePoisonSpout>();
        if (plate != null) plate.Configure(movement, poison);
        CreateEmpty("Mechanism_3", area, new Vector2(20.5f, -5.05f), "Mechanism");
    }

    // Area 5: layered upper route, traps, morph-potion anchor and scythe trigger.
    private static void BuildArea5()
    {
        Transform area = CreateGroup("Area_5_UpperRoute");
        CreatePlatform("UpperRoute_Low", area, new Vector2(21.8f, -1.5f), new Vector2(3.5f, 0.35f), new Color(0.44f, 0.61f, 0.74f));
        CreatePlatform("UpperRoute_Mid", area, new Vector2(25.1f, 0.55f), new Vector2(3.1f, 0.35f), new Color(0.44f, 0.61f, 0.74f));
        CreatePlatform("UpperRoute_High", area, new Vector2(28.3f, 2.5f), new Vector2(3.4f, 0.35f), new Color(0.44f, 0.61f, 0.74f));
        CreatePlatform("UpperRoute_Return", area, new Vector2(23.7f, 3.65f), new Vector2(2.2f, 0.35f), new Color(0.44f, 0.61f, 0.74f));
        for (int i = 0; i < 4; i++)
        {
            CreateHazard("UpperSpike_" + (i + 1), area, new Vector2(25.8f + i * 0.62f, 1.15f), new Vector2(0.48f, 0.48f), new Color(0.95f, 0.34f, 0.35f));
        }
        CreatePickup("MorphPotionPoint", area, new Vector2(23.6f, -0.9f), new Color(0.82f, 0.42f, 1f));
        CreateHazard("Mechanism_7_Scythe", area, new Vector2(28.5f, 1.55f), new Vector2(1.25f, 1.25f), new Color(0.88f, 0.9f, 0.95f));
        CreateEmpty("Mechanism_7", area, new Vector2(29.9f, 1.55f), "Mechanism");
    }

    // Area 6: final enclosure and trigger portal.
    private static void BuildArea6(Transform spawn)
    {
        Transform area = CreateGroup("Area_6_Exit");
        CreatePlatform("ExitFloor", area, new Vector2(33.4f, -4.2f), new Vector2(6.2f, 0.6f), new Color(0.38f, 0.54f, 0.67f));
        CreatePlatform("ExitWall", area, new Vector2(36.2f, -1f), new Vector2(0.6f, 6.9f), new Color(0.34f, 0.48f, 0.6f));
        CreatePlatform("ExitRoof", area, new Vector2(33.5f, 2.15f), new Vector2(5.8f, 0.35f), new Color(0.34f, 0.48f, 0.6f));
        GameObject portal = CreateTriggerSprite("ExitPortal", area, new Vector2(33.5f, -3.45f), new Vector2(0.9f, 1.45f), new Color(0.3f, 1f, 0.58f), "Item");
        portal.AddComponent<PuzzlePortal>().Configure(spawn, true);
        CreateEmpty("Mechanism_6", area, new Vector2(35f, -3.4f), "Mechanism");
    }

    private static void CreateMechanismAnchors()
    {
        CreateEmpty("Mechanism_4", root, new Vector2(-29f, -3.6f), "Mechanism");
        CreateEmpty("Mechanism_1", root, new Vector2(-8f, -3.7f), "Mechanism");
        CreateEmpty("Mechanism_2", root, new Vector2(-5.7f, -3.2f), "Mechanism");
    }

    private static Transform CreateGroup(string name)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(root, false);
        group.layer = Layer("Background");
        return group.transform;
    }

    private static Transform CreateEmpty(string name, Transform parent, Vector2 position, string layerName)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.position = new Vector3(position.x, position.y, 0f);
        obj.layer = Layer(layerName);
        return obj.transform;
    }

    private static GameObject CreatePlatform(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        GameObject obj = CreateVisual(name, position, size, color, "Platform", -1, parent);
        obj.AddComponent<BoxCollider2D>().size = size;
        return obj;
    }

    private static GameObject CreateTriggerSprite(string name, Transform parent, Vector2 position, Vector2 size, Color color, string layerName)
    {
        GameObject obj = CreateVisual(name, position, size, color, layerName, 2, parent);
        BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
        collider.size = size;
        collider.isTrigger = true;
        return obj;
    }

    private static void CreatePickup(string name, Transform parent, Vector2 position, Color color)
    {
        GameObject item = CreateTriggerSprite(name, parent, position, new Vector2(0.55f, 0.8f), color, "Item");
        item.AddComponent<PuzzlePickup>();
    }

    private static void CreateHazard(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        GameObject hazard = CreateVisual(name, position, size, color, "Trap", 3, parent);
        PolygonCollider2D collider = hazard.AddComponent<PolygonCollider2D>();
        collider.points = new[]
        {
            new Vector2(-0.5f * size.x, -0.5f * size.y),
            new Vector2(0f, 0.5f * size.y),
            new Vector2(0.5f * size.x, -0.5f * size.y)
        };
        collider.isTrigger = true;
        hazard.AddComponent<PuzzleHazard>();
    }

    private static GameObject CreateVisual(string name, Vector2 position, Vector2 size, Color color, string layerName, int sortingOrder, Transform parent = null)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent == null ? root : parent, false);
        obj.transform.position = new Vector3(position.x, position.y, 0f);
        obj.layer = Layer(layerName);
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = size;
        renderer.sortingOrder = sortingOrder;
        return obj;
    }

    private static int Layer(string layerName) => Layers[layerName];

    private static void EnsureLayers()
    {
        string[] required = { "Background", "Platform", "Trap", "Mechanism", "Item", "Player" };
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        SerializedObject tagManager = new SerializedObject(assets[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        foreach (string layerName in required)
        {
            int existing = LayerMask.NameToLayer(layerName);
            if (existing < 0)
            {
                for (int i = 8; i < layers.arraySize; i++)
                {
                    SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                    if (string.IsNullOrEmpty(layer.stringValue))
                    {
                        layer.stringValue = layerName;
                        existing = i;
                        break;
                    }
                }
            }
            if (existing < 0) throw new System.InvalidOperationException("No free Unity layer for " + layerName);
            Layers[layerName] = existing;
        }
        tagManager.ApplyModifiedProperties();
    }

    private static Sprite CreatePersistentWhiteSprite()
    {
        string directory = Path.GetDirectoryName(SpritePath);
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        if (!File.Exists(SpritePath))
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply();
            File.WriteAllBytes(SpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceUpdate);
        }

        TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        TextureImporterSettings spriteSettings = new TextureImporterSettings();
        importer.ReadTextureSettings(spriteSettings);
        spriteSettings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(spriteSettings);
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
    }

    private static void AddToBuildSettings(string path)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool present = false;
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == path)
            {
                scenes[i].enabled = true;
                present = true;
            }
        }
        if (!present) scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
