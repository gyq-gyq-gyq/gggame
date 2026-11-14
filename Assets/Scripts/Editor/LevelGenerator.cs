using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the complete seven-zone puzzle into the currently open scene.
/// Player prefabs and controller scripts are left intact.
/// </summary>
public static class LevelGenerator
{
    private const string RootName = "--- Puzzle Level ---";
    private const float EdgeRadius = 0.08f;
    private const float ColliderInset = EdgeRadius * 2f;
    private const float NarrowClearance = 0.85f;
    private const float PoisonJetHeightScale = 0.75f;
    private const float Zone5LaunchY = 3.25f;
    private const float Zone5LaunchHeight = 0.5f;
    private const float Zone5MaxWingClimb = 5f;
    private const float Zone5GoalSafetyMargin = 0.8f;
    private const float Zone5AntiFlyGap = 1.75f;
    private const float Zone5AntiFlyCeilingHeight = 0.5f;
    private const string BounceMaterialPath = "Assets/Generated2D/PuzzleHighBounce.physicsMaterial2D";

    private static readonly Color GroundColor = new Color(0.20f, 0.23f, 0.27f, 1f);
    private static readonly Color WallColor = new Color(0.26f, 0.30f, 0.35f, 1f);
    private static readonly Color SpikeColor = new Color(0.52f, 0.08f, 0.10f, 1f);
    private static readonly Color PoisonColor = new Color(0.55f, 1f, 0.45f, 0.68f);
    private static readonly Color PadColor = new Color(1f, 0.78f, 0.08f, 1f);
    private static readonly Color PlatformColor = new Color(0.38f, 0.58f, 0.78f, 1f);
    private static readonly Color BounceColor = new Color(0.20f, 0.92f, 0.92f, 1f);
    private static readonly Color PortalColor = new Color(0.43f, 0.26f, 1f, 0.92f);
    private static readonly Color PortalGlowColor = new Color(0.22f, 0.64f, 1f, 0.28f);

    private static Transform levelRoot;
    private static Sprite whiteSprite;
    private static PhysicsMaterial2D bounceMaterial;

    [MenuItem("Tools/Generate Full Puzzle Level", priority = 20)]
    public static void GenerateFullPuzzleLevel()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("LevelGenerator: exit Play Mode before generating the puzzle level.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("LevelGenerator: no editable scene is currently loaded.");
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Generate Full Puzzle Level");

        try
        {
            EnsureClimbableTreeTag();
            CleanupPreviousTestMaps(scene);
            whiteSprite = ResolveWhiteSprite();
            bounceMaterial = GetOrCreateBounceMaterial();

            GameObject rootObject = CreateObject(RootName, null, Vector2.zero);
            levelRoot = rootObject.transform;
            Undo.AddComponent<PuzzleSceneMarker>(rootObject);

            Transform spawnPoint = BuildZone1();
            BuildZone2();
            BuildZone3();
            BuildZone4();
            BuildZone5();
            BuildZone6();
            BuildZone7();

            MoveExistingPlayerToSpawn(spawnPoint);
            PrepareCameraForVerticalLevel(spawnPoint);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = rootObject;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.in2DMode = true;
                SceneView.lastActiveSceneView.FrameSelected();
            }

            Debug.Log(
                "LevelGenerator: generated the seven-zone puzzle. " +
                "Narrow passage physics clearance is exactly 0.85 units.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    [MenuItem("Tools/Generate Full Puzzle Level", true)]
    private static bool CanGenerateFullPuzzleLevel()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    private static Transform BuildZone1()
    {
        Transform zone = CreateGroup("Zone 1 - Spawn And Slit");

        GameObject spawn = CreateObject("PlayerSpawn", zone, new Vector2(0.75f, 0.62f));
        CreateSolidBlock(
            "StartGround",
            zone,
            new Vector2(1.25f, -0.5f),
            new Vector2(2.5f, 1f),
            GroundColor);
        CreateSolidBlock(
            "StartBackstop",
            zone,
            new Vector2(0.1f, 1.1f),
            new Vector2(0.5f, 3.2f),
            WallColor);

        const float floorVisualCenterY = -0.5f;
        const float floorVisualHeight = 1f;
        const float ceilingVisualHeight = 0.5f;

        float floorColliderTop = floorVisualCenterY + (floorVisualHeight - ColliderInset) * 0.5f;
        float ceilingColliderHeight = ceilingVisualHeight - ColliderInset;
        float ceilingCenterY = floorColliderTop + NarrowClearance + ceilingColliderHeight * 0.5f;

        // Keep the floor under the slit as a separate piece. This removes the
        // long StartGround collider's rounded end from the compression entry,
        // so the slime can settle flat before its shell enters the 0.85-unit
        // passage.
        CreateSolidBlock(
            "SlitFloor",
            zone,
            new Vector2(4.75f, floorVisualCenterY),
            new Vector2(4.5f, floorVisualHeight),
            GroundColor);

        BoxCollider2D slitCeiling = CreateSolidBlock(
            "SlimeCompressionSpikeCeiling",
            zone,
            new Vector2(4.75f, ceilingCenterY),
            new Vector2(4.5f, ceilingVisualHeight),
            SpikeColor);

        float measuredClearance = slitCeiling.bounds.min.y - floorColliderTop;
        if (!Mathf.Approximately(measuredClearance, NarrowClearance))
        {
            throw new InvalidOperationException(
                $"Narrow passage clearance is {measuredClearance:F4}, expected {NarrowClearance:F4}.");
        }

        // Harmless teeth communicate danger without turning the compression
        // surface into a death trigger. The rounded solid ceiling is what
        // actually forces the player to use the slime form.
        for (int i = 0; i < 11; i++)
        {
            float x = 2.72f + i * 0.38f;
            GameObject tooth = CreateVisual(
                $"CeilingTooth_{i + 1:00}",
                zone,
                new Vector2(x, slitCeiling.bounds.min.y + 0.015f),
                new Vector2(0.14f, 0.14f),
                SpikeColor,
                1);
            tooth.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
        }

        return spawn.transform;
    }

    private static void BuildZone2()
    {
        Transform zone = CreateGroup("Zone 2 - Adhesion Shaft");

        CreateSolidBlock(
            "ShaftFloor",
            zone,
            new Vector2(8.5f, -0.5f),
            new Vector2(3f, 1f),
            GroundColor);

        // The raised left wall leaves the low slit open but prevents the wing
        // form from simply entering the upper shaft from the side.
        CreateSolidBlock(
            "ShaftLeftUpperWall",
            zone,
            new Vector2(7.18f, 4.65f),
            new Vector2(0.55f, 6.7f),
            WallColor);

        BoxCollider2D climbWall = CreateSolidBlock(
            "SlimeClimbRightWall",
            zone,
            new Vector2(9.72f, 4f),
            new Vector2(0.55f, 8.4f),
            WallColor);
        Undo.AddComponent<ClimbablePole>(climbWall.gameObject);

        // Keep the shaft mouth completely clear. The soft body must remain in
        // contact with the right wall while its gripping nodes pull the rest
        // of the shell over the lip, so a ceiling hazard here would intersect
        // the intended climbing path.
    }

    private static void BuildZone3()
    {
        Transform zone = CreateGroup("Zone 3 - Switch And Moving Platform");

        CreateSolidBlock(
            "PressurePadLedge",
            zone,
            new Vector2(11.25f, 7.5f),
            new Vector2(3f, 1f),
            GroundColor);
        CreateSolidBlock(
            "MovingPlatformLanding",
            zone,
            new Vector2(24f, 7.5f),
            new Vector2(2f, 1f),
            GroundColor);

        Transform pointA = CreateObject(
            "MovingPlatform_PointA",
            zone,
            new Vector2(13f, 8.2f)).transform;
        Transform pointB = CreateObject(
            "MovingPlatform_PointB",
            zone,
            new Vector2(23f, 8.2f)).transform;

        BoxCollider2D movingCollider = CreateSolidBlock(
            "ActivatedMovingPlatform",
            zone,
            pointA.position,
            new Vector2(2.2f, 0.45f),
            PlatformColor);
        Undo.AddComponent<Rigidbody2D>(movingCollider.gameObject);
        MovingPlatform movingPlatform = Undo.AddComponent<MovingPlatform>(movingCollider.gameObject);
        movingPlatform.Configure(pointA, pointB, 2.6f);
        EditorUtility.SetDirty(movingPlatform);

        GameObject padObject = CreateObject(
            "MovingPlatformPressurePad",
            zone,
            new Vector2(11.2f, 8.07f));
        GameObject padVisual = CreateVisual(
            "PadVisual",
            padObject.transform,
            padObject.transform.position,
            new Vector2(1.25f, 0.24f),
            PadColor,
            3);
        BoxCollider2D padTrigger = Undo.AddComponent<BoxCollider2D>(padObject);
        padTrigger.size = new Vector2(1.21f, 0.20f);
        padTrigger.edgeRadius = 0.04f;
        padTrigger.isTrigger = true;
        PressurePad pad = Undo.AddComponent<PressurePad>(padObject);
        pad.Configure(movingPlatform, padVisual.transform);
        EditorUtility.SetDirty(pad);

        // Falling from the moving platform is now an explicit failure instead
        // of an endless fall through empty space.
        CreateSolidBlock(
            "MovingPlatformSpikeBase",
            zone,
            new Vector2(18f, 5f),
            new Vector2(11f, 0.6f),
            GroundColor);

        const int movingPlatformSpikeCount = 15;
        float movingPlatformSpikeWidth = 11f / movingPlatformSpikeCount;
        for (int i = 0; i < movingPlatformSpikeCount; i++)
        {
            CreateSpike(
                $"MovingPlatformSpike_{i + 1:00}",
                zone,
                new Vector2(12.5f + movingPlatformSpikeWidth * (i + 0.5f), 5.56f),
                new Vector2(movingPlatformSpikeWidth + 0.03f, 0.7f),
                false);
        }
    }

    private static void BuildZone4()
    {
        Transform zone = CreateGroup("Zone 4 - Spike And Poison Corridor");

        CreateSolidBlock(
            "PoisonCorridorCeiling",
            zone,
            new Vector2(32.5f, 9.5f),
            new Vector2(15.4f, 1f),
            GroundColor);

        GameObject flightZone = CreateObject(
            "InfiniteFlapZone",
            zone,
            new Vector2(32.5f, 4.5f));
        BoxCollider2D flightZoneCollider = Undo.AddComponent<BoxCollider2D>(flightZone);
        flightZoneCollider.size = new Vector2(15f, 9f);
        flightZoneCollider.isTrigger = true;
        Undo.AddComponent<InfiniteFlapZone>(flightZone);

        // The roof itself blocks upward movement. These end guards continue
        // above both roof edges so the wing form cannot fly around an end,
        // land on top and bypass the poison corridor.
        CreateSolidBlock(
            "PoisonCeilingEntryGuard",
            zone,
            new Vector2(24.9f, 14f),
            new Vector2(0.5f, 9f),
            GroundColor);
        CreateSolidBlock(
            "PoisonCeilingExitGuard",
            zone,
            new Vector2(40.1f, 14f),
            new Vector2(0.5f, 9f),
            GroundColor);

        const int spikeCount = 20;
        float spikeWidth = 15f / spikeCount;
        for (int i = 0; i < spikeCount; i++)
        {
            CreateSpike(
                $"CorridorFloorSpike_{i + 1:00}",
                zone,
                new Vector2(25f + spikeWidth * (i + 0.5f), 0.34f),
                new Vector2(spikeWidth + 0.03f, 0.68f),
                false);
        }

        CreateLaserGate("LaserGate_A", zone, 29.5f, 0f);
        CreateLaserGate("LaserGate_B", zone, 35.5f, 0.65f);

        // A safe midpoint between the alternating jets. Wing form can land
        // here, recover stamina and wait for the second spray cycle.
        CreateSolidBlock(
            "PoisonCorridorRestPlatform",
            zone,
            new Vector2(32.5f, 2.2f),
            new Vector2(2.2f, 0.45f),
            PlatformColor);
    }

    private static void BuildZone5()
    {
        Transform zone = CreateGroup("Zone 5 - Momentum Relay To Trees");

        float launchSurfaceY = Zone5LaunchY + (Zone5LaunchHeight - ColliderInset) * 0.5f;
        float goalSurfaceY = launchSurfaceY + Zone5MaxWingClimb + Zone5GoalSafetyMargin;
        float antiFlyBottomY = launchSurfaceY + Zone5AntiFlyGap;
        float antiFlyColliderHeight = Zone5AntiFlyCeilingHeight - ColliderInset;
        float antiFlyCenterY = antiFlyBottomY + antiFlyColliderHeight * 0.5f;

        if (goalSurfaceY - launchSurfaceY <= Zone5MaxWingClimb)
        {
            throw new InvalidOperationException(
                "Zone 5 design rule failed: the goal platform is reachable by the wing form.");
        }

        if (antiFlyBottomY >= goalSurfaceY)
        {
            throw new InvalidOperationException(
                "Zone 5 design rule failed: anti-fly ceiling is not lower than the goal route.");
        }

        CreateSolidBlock(
            "HighLaunchPlatform",
            zone,
            new Vector2(42f, Zone5LaunchY),
            new Vector2(3f, Zone5LaunchHeight),
            GroundColor);

        BoxCollider2D bounceCollider = CreateSolidBlock(
            "HighBounceFloor",
            zone,
            new Vector2(44f, -0.35f),
            new Vector2(3f, 0.7f),
            BounceColor);
        bounceCollider.sharedMaterial = bounceMaterial;
        ElasticPlatform bounce = Undo.AddComponent<ElasticPlatform>(bounceCollider.gameObject);
        bounce.Configure(18f, 1.45f, 1f);

        // Low, rounded roof over the chasm. It prevents a winged player from
        // gaining altitude or crossing the void in a straight horizontal line.
        // The roof ends before the goal slab, leaving the upward bounce lane
        // open once the slime has travelled past its right edge.
        CreateSolidBlock(
            "Zone5_AntiFlyCeiling",
            zone,
            new Vector2(47.4f, antiFlyCenterY),
            new Vector2(6.2f, Zone5AntiFlyCeilingHeight),
            SpikeColor);

        // This is now an approach ledge for the tree climb, not an early goal.
        // The only portal is at the Zone 7 pipe outlet.
        float approachPlatformHeight = 0.5f;
        float approachPlatformCenterY = goalSurfaceY - (approachPlatformHeight - ColliderInset) * 0.5f;
        CreateSolidBlock(
            "TreeApproachPlatform",
            zone,
            new Vector2(52.5f, approachPlatformCenterY),
            new Vector2(6f, approachPlatformHeight),
            GroundColor);
    }

    private static void BuildZone6()
    {
        Transform zone = CreateGroup("Zone 6 - Cat Tree Parkour");
        CreateSolidBlock("TreeZoneFloor", zone, new Vector2(62f, -0.5f), new Vector2(18f, 1f), GroundColor);

        // Each trunk is taller than the last. The short horizontal gaps are
        // deliberate: the player climbs, flips to choose an outer side, then
        // wall-kicks toward the next tree at a higher elevation.
        float[] treeXs = { 57.2f, 60.5f, 63.8f, 67.1f };
        float[] treeHeights = { 10.6f, 12.9f, 15.2f, 17.5f };
        for (int i = 0; i < treeXs.Length; i++)
        {
            CreateTreeTrunk($"ClimbableTree_{i + 1:00}", zone,
                new Vector2(treeXs[i], treeHeights[i] * 0.5f),
                new Vector2(0.8f, treeHeights[i]));
        }

        // The fourth canopy feeds directly into the smooth pipe mouth.
        CreateSolidBlock(
            "TreeTopPipePlatform",
            zone,
            new Vector2(69.4f, 16.94f),
            new Vector2(3.6f, 0.6f),
            GroundColor);
    }

    private static void BuildZone7()
    {
        Transform zone = CreateGroup("Zone 7 - Pipe Descent Goal");
        const float pipeCentrelineSeparation = 1.15f;
        List<Vector2> lowerWall = CurvedPipeGenerator.SampleCubic(
            new Vector2(71.1f, 17.08f),
            new Vector2(75.0f, 17.08f),
            new Vector2(78.8f, 1.45f),
            new Vector2(82.5f, 0.85f),
            40);
        List<Vector2> upperWall = CurvedPipeGenerator.OffsetAlongNormals(
            lowerWall,
            pipeCentrelineSeparation);

        CurvedPipeGenerator.ValidateClearance(lowerWall, upperWall, NarrowClearance);
        CurvedPipeGenerator.CreatePipe(zone, "CurvedPipeLowerWall", lowerWall, 0.3f, WallColor);
        CurvedPipeGenerator.CreatePipe(zone, "CurvedPipeUpperWall", upperWall, 0.3f, WallColor);

        CreateSolidBlock("PipeExitFloor", zone, new Vector2(84.6f, -0.5f), new Vector2(5.8f, 1f), GroundColor);
        CreateGoalPortal(zone, new Vector2(83.45f, 1.28f));
    }

    private static void CreateTreeTrunk(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject tree = CreateVisual(name, parent, position, size, new Color(0.42f, 0.24f, 0.12f), 0);
        tree.tag = "ClimbableTree";
        BoxCollider2D solid = Undo.AddComponent<BoxCollider2D>(tree);
        solid.size = size - Vector2.one * ColliderInset;
        solid.edgeRadius = EdgeRadius;
        CatTreeClimber climber = Undo.AddComponent<CatTreeClimber>(tree);
        EditorUtility.SetDirty(climber);
    }

    private static void CreateGoalPortal(Transform parent, Vector2 position)
    {
        GameObject portal = CreateVisual(
            "GoalPortal",
            parent,
            position,
            new Vector2(1.2f, 1.65f),
            PortalColor,
            6);
        BoxCollider2D portalTrigger = Undo.AddComponent<BoxCollider2D>(portal);
        portalTrigger.size = new Vector2(1.1f, 1.55f);
        portalTrigger.isTrigger = true;

        GameObject glow = CreateVisual(
            "PortalGlow",
            portal.transform,
            portal.transform.position,
            new Vector2(1.65f, 2.05f),
            PortalGlowColor,
            5);
        GoalPortal goal = Undo.AddComponent<GoalPortal>(portal);
        goal.Configure(glow.transform);
        EditorUtility.SetDirty(goal);
    }

    private static void EnsureClimbableTreeTag()
    {
        string[] existingTags = InternalEditorUtility.tags;
        for (int i = 0; i < existingTags.Length; i++)
        {
            if (existingTags[i] == "ClimbableTree")
            {
                return;
            }
        }

        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0)
        {
            throw new InvalidOperationException("TagManager.asset could not be loaded.");
        }

        SerializedObject tagManager = new SerializedObject(assets[0]);
        SerializedProperty tags = tagManager.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == "ClimbableTree")
            {
                tagManager.ApplyModifiedProperties();
                return;
            }
        }

        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = "ClimbableTree";
        tagManager.ApplyModifiedProperties();
    }

    private static void CreatePoisonJet(string name, Transform parent, Vector2 position, bool startsActive)
    {
        const float originalJetHeight = 5.3f;
        const float jetTopY = 9f;
        float jetHeight = originalJetHeight * PoisonJetHeightScale;
        position.y = jetTopY - jetHeight * 0.5f;

        GameObject nozzle = CreateVisual(
            name + "_Nozzle",
            parent,
            new Vector2(position.x, 8.82f),
            new Vector2(1.15f, 0.35f),
            SpikeColor,
            3);

        // The nozzle is deliberately visual-only; the main corridor ceiling
        // owns the rounded solid collider at this height.
        nozzle.transform.SetAsLastSibling();

        GameObject jet = CreateVisual(
            name,
            parent,
            position,
            new Vector2(0.9f, jetHeight),
            PoisonColor,
            4);
        BoxCollider2D trigger = Undo.AddComponent<BoxCollider2D>(jet);
        trigger.size = new Vector2(0.82f, jetHeight - 0.08f);
        trigger.isTrigger = true;
        PoisonHazard poison = Undo.AddComponent<PoisonHazard>(jet);
        poison.Configure(startsActive, 1.5f, 1.5f, 3.5f, 5.5f);
        EditorUtility.SetDirty(poison);
    }

    private static void CreateLaserGate(string name, Transform parent, float x, float initialDelay)
    {
        const float ceilingInsideY = 9f;
        const float floorInsideY = 0.7f;
        const float safeGap = 2f;
        const float lowestGapCenter = 2.6f;
        const float highestGapCenter = 6f;

        GameObject gateObject = CreateObject(name, parent, new Vector2(x, 0f));

        float initialGapCenter = (lowestGapCenter + highestGapCenter) * 0.5f;
        float gapBottom = initialGapCenter - safeGap * 0.5f;
        float gapTop = initialGapCenter + safeGap * 0.5f;

        float topLength = ceilingInsideY - gapTop;
        float bottomLength = gapBottom - floorInsideY;
        GameObject top = CreateVisual(
            "TopLaser",
            gateObject.transform,
            new Vector2(x, (ceilingInsideY + gapTop) * 0.5f),
            new Vector2(0.9f, topLength),
            PoisonColor,
            4);
        GameObject bottom = CreateVisual(
            "BottomLaser",
            gateObject.transform,
            new Vector2(x, (floorInsideY + gapBottom) * 0.5f),
            new Vector2(0.9f, bottomLength),
            PoisonColor,
            4);

        ConfigureLaserGateBeam(top, new Vector2(0.82f, topLength));
        ConfigureLaserGateBeam(bottom, new Vector2(0.82f, bottomLength));

        LaserGate gate = Undo.AddComponent<LaserGate>(gateObject);
        gate.Configure(
            top.transform,
            bottom.transform,
            ceilingInsideY,
            floorInsideY,
            safeGap,
            lowestGapCenter,
            highestGapCenter,
            initialDelay,
            true);
        EditorUtility.SetDirty(gate);
    }

    private static void ConfigureLaserGateBeam(GameObject beam, Vector2 colliderSize)
    {
        BoxCollider2D collider = Undo.AddComponent<BoxCollider2D>(beam);
        collider.size = colliderSize;
        collider.isTrigger = true;
        Undo.AddComponent<LaserGateBeam>(beam);
    }

    private static GameObject CreateSpike(
        string name,
        Transform parent,
        Vector2 position,
        Vector2 size,
        bool pointsDown)
    {
        GameObject spike = CreateVisual(name, parent, position, size, SpikeColor, 4);
        PolygonCollider2D trigger = Undo.AddComponent<PolygonCollider2D>(spike);
        float halfWidth = size.x * 0.5f;
        float halfHeight = size.y * 0.5f;
        trigger.points = pointsDown
            ? new[]
            {
                new Vector2(-halfWidth, halfHeight),
                new Vector2(halfWidth, halfHeight),
                new Vector2(0f, -halfHeight)
            }
            : new[]
            {
                new Vector2(-halfWidth, -halfHeight),
                new Vector2(0f, halfHeight),
                new Vector2(halfWidth, -halfHeight)
            };
        trigger.isTrigger = true;
        Undo.AddComponent<SpikeTrap>(spike);
        return spike;
    }

    /// <summary>
    /// The only creation path for solid rectangular level geometry. Keeping
    /// the inset and radius here guarantees no individual floor or ceiling can
    /// accidentally reintroduce an outward collider bulge.
    /// </summary>
    private static BoxCollider2D CreateSolidBlock(
        string name,
        Transform parent,
        Vector2 position,
        Vector2 spriteSize,
        Color color)
    {
        Vector2 colliderSize = spriteSize - Vector2.one * ColliderInset;
        if (colliderSize.x <= 0f || colliderSize.y <= 0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(spriteSize),
                $"{name} must be larger than {ColliderInset:F2} on both axes.");
        }

        GameObject block = CreateVisual(name, parent, position, spriteSize, color, 0);
        BoxCollider2D collider = Undo.AddComponent<BoxCollider2D>(block);
        collider.size = colliderSize;
        collider.edgeRadius = EdgeRadius;
        collider.isTrigger = false;
        return collider;
    }

    private static Transform CreateGroup(string name)
    {
        return CreateObject(name, levelRoot, Vector2.zero).transform;
    }

    private static GameObject CreateVisual(
        string name,
        Transform parent,
        Vector2 worldPosition,
        Vector2 size,
        Color color,
        int sortingOrder)
    {
        GameObject visual = CreateObject(name, parent, worldPosition);
        SpriteRenderer renderer = Undo.AddComponent<SpriteRenderer>(visual);
        renderer.sprite = whiteSprite;
        renderer.color = color;
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = size;
        renderer.sortingOrder = sortingOrder;
        return visual;
    }

    private static GameObject CreateObject(string name, Transform parent, Vector2 worldPosition)
    {
        GameObject created = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(created, "Generate Full Puzzle Level");
        if (parent != null)
        {
            created.transform.SetParent(parent, false);
        }

        created.transform.position = new Vector3(worldPosition.x, worldPosition.y, 0f);
        return created;
    }

    private static void CleanupPreviousTestMaps(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = roots.Length - 1; i >= 0; i--)
        {
            GameObject candidate = roots[i];
            if (IsGeneratedOrLegacyTestRoot(candidate))
            {
                Undo.DestroyObjectImmediate(candidate);
            }
        }
    }

    private static bool IsGeneratedOrLegacyTestRoot(GameObject candidate)
    {
        string objectName = candidate.name;
        if (string.Equals(objectName, RootName, StringComparison.Ordinal) ||
            objectName.IndexOf("TestMap", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        // Known roots from the project's earlier generated test courses.
        if (string.Equals(objectName, "LevelRoot", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(objectName, "PuzzleLevelRoot", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(objectName, "SlimeRelayCourse", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(objectName, "Ground", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return candidate.GetComponentInChildren<PuzzleSceneMarker>(true) != null ||
               candidate.GetComponent<SlimeRelayCourse>() != null;
    }

    private static Sprite ResolveWhiteSprite()
    {
        Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        if (sprite == null)
        {
            sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        }

        if (sprite == null)
        {
            throw new InvalidOperationException("Unity's built-in white sliced sprite could not be loaded.");
        }

        return sprite;
    }

    private static PhysicsMaterial2D GetOrCreateBounceMaterial()
    {
        const string folder = "Assets/Generated2D";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets", "Generated2D");
        }

        PhysicsMaterial2D material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(BounceMaterialPath);
        if (material == null)
        {
            material = new PhysicsMaterial2D("PuzzleHighBounce");
            AssetDatabase.CreateAsset(material, BounceMaterialPath);
        }

        material.friction = 0f;
        material.bounciness = 1f;
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }

    private static void MoveExistingPlayerToSpawn(Transform spawnPoint)
    {
        PlayerController player = UnityEngine.Object.FindObjectOfType<PlayerController>();
        if (player == null)
        {
            return;
        }

        Undo.RecordObject(player.transform, "Move Player To Puzzle Spawn");
        player.transform.position = spawnPoint.position;

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            Undo.RecordObject(body, "Reset Player Velocity");
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
        }
    }

    private static void PrepareCameraForVerticalLevel(Transform spawnPoint)
    {
        CameraFollow follow = UnityEngine.Object.FindObjectOfType<CameraFollow>();
        if (follow == null)
        {
            return;
        }

        Undo.RecordObject(follow, "Configure Puzzle Camera");
        SerializedObject cameraSettings = new SerializedObject(follow);
        SerializedProperty keepGroundVisible = cameraSettings.FindProperty("keepGroundVisible");
        if (keepGroundVisible != null)
        {
            keepGroundVisible.boolValue = false;
        }
        cameraSettings.ApplyModifiedProperties();

        Undo.RecordObject(follow.transform, "Move Puzzle Camera");
        Vector3 cameraPosition = follow.transform.position;
        cameraPosition.x = spawnPoint.position.x;
        cameraPosition.y = spawnPoint.position.y + 1.5f;
        cameraPosition.z = -10f;
        follow.transform.position = cameraPosition;
    }
}
