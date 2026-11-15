using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class CatPlayerVisual : MonoBehaviour
{
    [Header("Black And White Fur")]
    [SerializeField] private Color whiteFur = new Color(0.93f, 0.93f, 0.93f, 1f);
    [SerializeField] private Color blackFur = new Color(0.07f, 0.07f, 0.07f, 1f);
    [SerializeField] private Color innerEarColor = new Color(0.88f, 0.57f, 0.61f, 1f);

    [Header("Distinctive Face")]
    [SerializeField] private Color leftEyeColor = new Color(0.15f, 0.62f, 0.78f, 1f);
    [SerializeField] private Color rightEyeColor = new Color(0.26f, 0.72f, 0.35f, 1f);
    [SerializeField] private Color noseColor = new Color(0.33f, 0.14f, 0.17f, 1f);

    private const int TextureSize = 128;
    private const int RenderScale = 4;
    private const float PixelsPerUnit = 800f;
    private const string RunningFramesResourcePath = "CatRunning/CatRun_";
    private const int RunningFrameCount = 4;
    private const float RunningFrameDuration = 0.1f;

    private SpriteRenderer spriteRenderer;
    private PlayerController player;
    private Rigidbody2D body;
    private readonly List<Texture2D> generatedTextures = new List<Texture2D>();

    private Sprite idleFrame;
    private Sprite walkFrameA;
    private Sprite walkFrameB;
    private Sprite jumpFrame;
    private Sprite wingUpFrame;
    private Sprite wingDownFrame;
    private Sprite[] runningFrames;
    private Sprite[] flightFrames;
    private Sprite[] climbFrames;
    private float climbCycleTime;

    private bool showWalkFrameA;
    private float walkCycleTime;
    private float wingCycleTime;
    private Vector3 baseLocalPosition;

    private void OnEnable()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GetComponentInParent<PlayerController>();
        body = GetComponentInParent<Rigidbody2D>();
        baseLocalPosition = transform.localPosition;
        BuildFrames();
    }

    private void OnDisable()
    {
        ReleaseTextures();
    }

    private void OnDestroy()
    {
        ReleaseTextures();
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying || spriteRenderer == null || player == null || body == null)
        {
            return;
        }

        UpdateAnimation();
        UpdateFacing();
    }

    private void BuildFrames()
    {
        if (idleFrame != null)
        {
            return;
        }

        runningFrames = new Sprite[RunningFrameCount];
        bool hasCompleteRunCycle = true;
        for (int i = 0; i < RunningFrameCount; i++)
        {
            runningFrames[i] = Resources.Load<Sprite>(RunningFramesResourcePath + i.ToString("00"));
            hasCompleteRunCycle &= runningFrames[i] != null;
        }

        if (!hasCompleteRunCycle)
        {
            runningFrames = null;
        }

        flightFrames = LoadActionFrames("Flight_", 3);
        climbFrames = LoadActionFrames("Climb_", 4);

        idleFrame = runningFrames != null ? runningFrames[0] : CreateFrame(DrawIdle);
        walkFrameA = runningFrames != null ? runningFrames[0] : CreateFrame(t => DrawWalk(t, true));
        walkFrameB = runningFrames != null ? runningFrames[1] : CreateFrame(t => DrawWalk(t, false));
        jumpFrame = runningFrames != null ? runningFrames[1] : CreateFrame(DrawJump);
        wingUpFrame = CreateFrame(t => DrawWing(t, true));
        wingDownFrame = CreateFrame(t => DrawWing(t, false));

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = idleFrame;
        }
    }

    private void UpdateAnimation()
    {
        if (player.currentForm == Form.Slime)
        {
            return;
        }

        if (player.currentForm == Form.Wing)
        {
            wingCycleTime += Time.deltaTime * 10f;
            spriteRenderer.sprite = flightFrames != null
                ? flightFrames[Mathf.FloorToInt(wingCycleTime) % flightFrames.Length]
                : (Mathf.FloorToInt(wingCycleTime) % 2 == 0 ? wingUpFrame : wingDownFrame);
            transform.localPosition = new Vector3(
                baseLocalPosition.x,
                baseLocalPosition.y + Mathf.Abs(Mathf.Sin(Time.time * 12f)) * 0.015f,
                baseLocalPosition.z);
            return;
        }

        if (player.IsClimbingTree && climbFrames != null)
        {
            if (Mathf.Abs(body.velocity.y) > 0.05f)
                climbCycleTime = (climbCycleTime + Time.deltaTime * 8f) % climbFrames.Length;
            spriteRenderer.sprite = climbFrames[Mathf.FloorToInt(climbCycleTime)];
            transform.localPosition = baseLocalPosition;
            return;
        }
        climbCycleTime = 0f;

        bool grounded = IsGrounded();
        if (!grounded)
        {
            walkCycleTime = 0f;
            spriteRenderer.sprite = jumpFrame;
            transform.localPosition = baseLocalPosition;
            return;
        }

        if (Mathf.Abs(body.velocity.x) > 0.1f)
        {
            if (runningFrames != null)
            {
                walkCycleTime += Time.deltaTime;
                float cycleDuration = RunningFrameDuration * runningFrames.Length;
                walkCycleTime %= cycleDuration;
                int frameIndex = Mathf.FloorToInt(walkCycleTime / RunningFrameDuration);
                spriteRenderer.sprite = runningFrames[frameIndex];
            }
            else
            {
                walkCycleTime += Time.deltaTime * Mathf.Abs(body.velocity.x);
                if (walkCycleTime >= 0.12f)
                {
                    walkCycleTime = 0f;
                    showWalkFrameA = !showWalkFrameA;
                }

                spriteRenderer.sprite = showWalkFrameA ? walkFrameA : walkFrameB;
            }

            transform.localPosition = new Vector3(
                baseLocalPosition.x,
                baseLocalPosition.y + Mathf.Abs(Mathf.Sin(Time.time * 14f)) * 0.02f,
                baseLocalPosition.z);
        }
        else
        {
            walkCycleTime = 0f;
            showWalkFrameA = true;
            spriteRenderer.sprite = idleFrame;
            transform.localPosition = baseLocalPosition;
        }
    }

    private void UpdateFacing()
    {
        if (player.IsClimbingTree)
        {
            // All climbing frames face right: always face into the trunk.
            spriteRenderer.flipX = player.TreeAttachmentSide > 0;
            return;
        }
        if (body.velocity.x < -0.05f)
        {
            spriteRenderer.flipX = true;
        }
        else if (body.velocity.x > 0.05f)
        {
            spriteRenderer.flipX = false;
        }
    }

    private static Sprite[] LoadActionFrames(string prefix, int count)
    {
        Sprite[] result = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = Resources.Load<Sprite>("CatActions/" + prefix + i.ToString("00"));
            if (result[i] == null) return null;
        }
        return result;
    }

    private bool IsGrounded()
    {
        return body.IsTouchingLayers() && Mathf.Abs(body.velocity.y) <= 0.05f;
    }

    private Sprite CreateFrame(Action<Texture2D> draw)
    {
        int renderWidth = TextureSize * RenderScale;
        int renderHeight = TextureSize * RenderScale;

        Texture2D renderTexture = new Texture2D(renderWidth, renderHeight, TextureFormat.RGBA32, false);
        renderTexture.name = "SideViewKittenFrameHighRes";
        renderTexture.wrapMode = TextureWrapMode.Clamp;
        renderTexture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < renderHeight; y++)
        {
            for (int x = 0; x < renderWidth; x++)
            {
                renderTexture.SetPixel(x, y, Color.clear);
            }
        }

        draw(renderTexture);
        renderTexture.Apply(false, false);

        Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        texture.name = "SideViewKittenFrame";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float u = (x + 0.5f) / TextureSize;
                float v = (y + 0.5f) / TextureSize;
                texture.SetPixel(x, y, renderTexture.GetPixelBilinear(u, v));
            }
        }

        texture.Apply(false, true);
        generatedTextures.Add(texture);

        if (Application.isPlaying)
        {
            Destroy(renderTexture);
        }
        else
        {
            DestroyImmediate(renderTexture);
        }

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, TextureSize, TextureSize),
            new Vector2(0.5f, 0.5f),
            PixelsPerUnit);
    }

    private void DrawIdle(Texture2D texture)
    {
        DrawBodyAndHead(texture);
        DrawLeg(texture, 46, 36, 46, 11);
        DrawLeg(texture, 58, 36, 58, 11);
        DrawLeg(texture, 74, 36, 74, 11);
        DrawLeg(texture, 86, 36, 86, 11);
    }

    private void DrawWalk(Texture2D texture, bool phaseA)
    {
        DrawBodyAndHead(texture);

        if (phaseA)
        {
            DrawLeg(texture, 46, 36, 39, 12);
            DrawLeg(texture, 58, 36, 64, 12);
            DrawLeg(texture, 74, 36, 70, 12);
            DrawLeg(texture, 86, 36, 93, 12);
        }
        else
        {
            DrawLeg(texture, 46, 36, 53, 12);
            DrawLeg(texture, 58, 36, 51, 12);
            DrawLeg(texture, 74, 36, 82, 12);
            DrawLeg(texture, 86, 36, 79, 12);
        }
    }

    private void DrawJump(Texture2D texture)
    {
        DrawBodyAndHead(texture);
        DrawLeg(texture, 46, 36, 40, 27);
        DrawLeg(texture, 58, 36, 54, 25);
        DrawLeg(texture, 74, 36, 80, 25);
        DrawLeg(texture, 86, 36, 92, 27);
    }

    private void DrawWing(Texture2D texture, bool wingUp)
    {
        DrawBodyAndHead(texture);
        DrawLeg(texture, 46, 36, 42, 24);
        DrawLeg(texture, 58, 36, 54, 22);
        DrawLeg(texture, 74, 36, 80, 22);
        DrawLeg(texture, 86, 36, 92, 24);

        if (wingUp)
        {
            DrawFilledTriangle(texture, 67, 68, 40, 101, 54, 74, blackFur);
            DrawFilledTriangle(texture, 66, 70, 44, 91, 55, 72, whiteFur);
        }
        else
        {
            DrawFilledTriangle(texture, 67, 68, 31, 66, 54, 74, blackFur);
            DrawFilledTriangle(texture, 66, 70, 38, 67, 55, 72, whiteFur);
        }
    }

    private void DrawBodyAndHead(Texture2D texture)
    {
        DrawLine(texture, 24, 53, 7, 79, 5, blackFur);
        DrawLine(texture, 9, 77, 6, 83, 4, whiteFur);

        DrawFilledEllipse(texture, 64, 52, 40, 20, whiteFur);
        DrawFilledEllipse(texture, 52, 62, 27, 10, blackFur);

        DrawFilledEllipse(texture, 98, 70, 20, 18, whiteFur);
        DrawFilledEllipse(texture, 98, 81, 19, 12, blackFur);
        DrawFilledEllipse(texture, 104, 64, 9, 7, whiteFur);

        DrawFilledTriangle(texture, 84, 79, 73, 101, 96, 88, blackFur);
        DrawFilledTriangle(texture, 87, 84, 79, 96, 93, 89, innerEarColor);
        DrawFilledTriangle(texture, 109, 82, 123, 98, 108, 94, blackFur);
        DrawFilledTriangle(texture, 111, 86, 119, 93, 108, 91, innerEarColor);

        DrawFilledEllipse(texture, 104, 73, 5, 7, rightEyeColor);
        DrawFilledEllipse(texture, 102, 76, 2, 3, whiteFur);
        DrawFilledTriangle(texture, 111, 66, 107, 62, 114, 62, noseColor);
        DrawLine(texture, 111, 62, 111, 57, 1, blackFur);
        DrawLine(texture, 111, 57, 106, 53, 1, blackFur);
        DrawLine(texture, 111, 57, 117, 53, 1, blackFur);

        DrawLine(texture, 91, 67, 99, 65, 1, blackFur);
        DrawLine(texture, 91, 63, 99, 63, 1, blackFur);
    }

    private void DrawLeg(Texture2D texture, int x0, int y0, int x1, int y1)
    {
        DrawLine(texture, x0, y0, x1, y1, 4, blackFur);
        DrawFilledEllipse(texture, x1, y1 - 2, 3, 2, whiteFur);
    }

    private void ReleaseTextures()
    {
        for (int i = 0; i < generatedTextures.Count; i++)
        {
            Texture2D texture = generatedTextures[i];
            if (texture == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(texture);
            }
            else
            {
                DestroyImmediate(texture);
            }
        }

        generatedTextures.Clear();
        runningFrames = null;
        idleFrame = null;
        walkFrameA = null;
        walkFrameB = null;
        jumpFrame = null;
        wingUpFrame = null;
        wingDownFrame = null;
    }

    private static void DrawFilledEllipse(
        Texture2D texture,
        int centerX,
        int centerY,
        int radiusX,
        int radiusY,
        Color color)
    {
        int scaledCenterX = centerX * RenderScale;
        int scaledCenterY = centerY * RenderScale;
        int scaledRadiusX = radiusX * RenderScale;
        int scaledRadiusY = radiusY * RenderScale;

        for (int y = scaledCenterY - scaledRadiusY; y <= scaledCenterY + scaledRadiusY; y++)
        {
            for (int x = scaledCenterX - scaledRadiusX; x <= scaledCenterX + scaledRadiusX; x++)
            {
                float dx = (x - scaledCenterX) / (float)scaledRadiusX;
                float dy = (y - scaledCenterY) / (float)scaledRadiusY;
                if (dx * dx + dy * dy <= 1f && IsInsideTexture(x, y, texture.width, texture.height))
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }
    }

    private static void DrawFilledTriangle(
        Texture2D texture,
        int x0,
        int y0,
        int x1,
        int y1,
        int x2,
        int y2,
        Color color)
    {
        int sx0 = x0 * RenderScale;
        int sy0 = y0 * RenderScale;
        int sx1 = x1 * RenderScale;
        int sy1 = y1 * RenderScale;
        int sx2 = x2 * RenderScale;
        int sy2 = y2 * RenderScale;

        int minX = Mathf.Min(sx0, sx1, sx2);
        int maxX = Mathf.Max(sx0, sx1, sx2);
        int minY = Mathf.Min(sy0, sy1, sy2);
        int maxY = Mathf.Max(sy0, sy1, sy2);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (PointInTriangle(x, y, sx0, sy0, sx1, sy1, sx2, sy2) &&
                    IsInsideTexture(x, y, texture.width, texture.height))
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }
    }

    private static void DrawLine(
        Texture2D texture,
        int x0,
        int y0,
        int x1,
        int y1,
        int thickness,
        Color color)
    {
        int sx0 = x0 * RenderScale;
        int sy0 = y0 * RenderScale;
        int sx1 = x1 * RenderScale;
        int sy1 = y1 * RenderScale;
        int scaledThickness = thickness * RenderScale;

        int steps = Mathf.Max(Mathf.Abs(sx1 - sx0), Mathf.Abs(sy1 - sy0));
        for (int i = 0; i <= steps; i++)
        {
            float t = steps == 0 ? 0f : i / (float)steps;
            int x = Mathf.RoundToInt(Mathf.Lerp(sx0, sx1, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(sy0, sy1, t));

            for (int oy = -scaledThickness; oy <= scaledThickness; oy++)
            {
                for (int ox = -scaledThickness; ox <= scaledThickness; ox++)
                {
                    if (IsInsideTexture(x + ox, y + oy, texture.width, texture.height))
                    {
                        texture.SetPixel(x + ox, y + oy, color);
                    }
                }
            }
        }
    }

    private static bool PointInTriangle(
        int px,
        int py,
        int x0,
        int y0,
        int x1,
        int y1,
        int x2,
        int y2)
    {
        float d0 = Sign(px, py, x0, y0, x1, y1);
        float d1 = Sign(px, py, x1, y1, x2, y2);
        float d2 = Sign(px, py, x2, y2, x0, y0);
        bool hasNegative = d0 < 0f || d1 < 0f || d2 < 0f;
        bool hasPositive = d0 > 0f || d1 > 0f || d2 > 0f;
        return !(hasNegative && hasPositive);
    }

    private static float Sign(int px, int py, int ax, int ay, int bx, int by)
    {
        return (px - bx) * (ay - by) - (ax - bx) * (py - by);
    }

    private static bool IsInsideTexture(int x, int y, int width, int height)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }
}
