using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Editor helper for smooth, rounded pipe walls built from Bezier samples.</summary>
public static class CurvedPipeGenerator
{
    public const float EdgeRadius = 0.08f;
    public const float MinimumClearance = 0.85f;
    private static PhysicsMaterial2D pipeMaterial;

    public static GameObject CreatePipe(
        Transform parent,
        string name,
        IReadOnlyList<Vector2> centerline,
        float width,
        Color color)
    {
        if (centerline == null || centerline.Count < 2)
        {
            throw new ArgumentException("A pipe needs at least two centreline points.", nameof(centerline));
        }

        if (width <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(width),
                "Pipe wall visual width must be greater than zero.");
        }

        GameObject pipe = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(pipe, "Generate Curved Pipe");
        pipe.transform.SetParent(parent, false);

        EdgeCollider2D wall = pipe.AddComponent<EdgeCollider2D>();
        wall.edgeRadius = EdgeRadius;
        wall.sharedMaterial = GetPipeMaterial();
        wall.points = ToLocalPoints(centerline, pipe.transform);

        LineRenderer visual = pipe.AddComponent<LineRenderer>();
        visual.useWorldSpace = false;
        visual.positionCount = centerline.Count;
        visual.widthMultiplier = width;
        visual.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");
        visual.startColor = color;
        visual.endColor = color;
        Vector2[] localPoints = ToLocalPoints(centerline, pipe.transform);
        for (int i = 0; i < centerline.Count; i++)
        {
            visual.SetPosition(i, localPoints[i]);
        }

        return pipe;
    }

    private static PhysicsMaterial2D GetPipeMaterial()
    {
        if (pipeMaterial == null)
        {
            pipeMaterial = new PhysicsMaterial2D("CurvedPipeNoFriction")
            {
                friction = 0f,
                bounciness = 0f
            };
        }

        return pipeMaterial;
    }

    /// <summary>
    /// Validates the free space between two matching smooth wall samples.
    /// EdgeCollider2D edge radii consume clearance on both sides.
    /// </summary>
    public static void ValidateClearance(
        IReadOnlyList<Vector2> firstWall,
        IReadOnlyList<Vector2> secondWall,
        float minimumClearance = MinimumClearance)
    {
        if (firstWall == null || secondWall == null || firstWall.Count != secondWall.Count || firstWall.Count < 2)
        {
            throw new ArgumentException("Pipe walls must contain the same number of at least two samples.");
        }

        for (int i = 0; i < firstWall.Count; i++)
        {
            float freeSpace = Vector2.Distance(firstWall[i], secondWall[i]) - EdgeRadius * 2f;
            if (freeSpace < minimumClearance)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumClearance),
                    $"Pipe clearance at sample {i} is {freeSpace:F3}, below {minimumClearance:F3}.");
            }
        }
    }

    public static List<Vector2> SampleQuadratic(Vector2 start, Vector2 control, Vector2 end, int samples = 12)
    {
        samples = Mathf.Max(2, samples);
        List<Vector2> points = new List<Vector2>(samples + 1);
        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            float inverse = 1f - t;
            points.Add(inverse * inverse * start + 2f * inverse * t * control + t * t * end);
        }
        return points;
    }

    /// <summary>
    /// Samples a cubic Bezier with independent entry and exit tangents. This
    /// is used for long slide pipes, where the mouth and outlet must both be
    /// smooth enough for the soft body to avoid a hard edge.
    /// </summary>
    public static List<Vector2> SampleCubic(
        Vector2 start,
        Vector2 startControl,
        Vector2 endControl,
        Vector2 end,
        int samples = 24)
    {
        samples = Mathf.Max(3, samples);
        List<Vector2> points = new List<Vector2>(samples + 1);
        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            float inverse = 1f - t;
            points.Add(
                inverse * inverse * inverse * start +
                3f * inverse * inverse * t * startControl +
                3f * inverse * t * t * endControl +
                t * t * t * end);
        }
        return points;
    }

    /// <summary>
    /// Offsets a sampled wall along its local normal. Unlike a simple vertical
    /// offset this retains the pipe's usable width through steep sections.
    /// </summary>
    public static List<Vector2> OffsetAlongNormals(IReadOnlyList<Vector2> centreline, float distance)
    {
        if (centreline == null || centreline.Count < 2)
        {
            throw new ArgumentException("A pipe needs at least two centreline points.", nameof(centreline));
        }

        List<Vector2> offset = new List<Vector2>(centreline.Count);
        for (int i = 0; i < centreline.Count; i++)
        {
            Vector2 tangent;
            if (i == 0)
            {
                tangent = centreline[1] - centreline[0];
            }
            else if (i == centreline.Count - 1)
            {
                tangent = centreline[i] - centreline[i - 1];
            }
            else
            {
                tangent = centreline[i + 1] - centreline[i - 1];
            }

            tangent.Normalize();
            Vector2 upwardNormal = new Vector2(-tangent.y, tangent.x);
            if (upwardNormal.y < 0f)
            {
                upwardNormal = -upwardNormal;
            }

            offset.Add(centreline[i] + upwardNormal * distance);
        }

        return offset;
    }

    private static Vector2[] ToLocalPoints(IReadOnlyList<Vector2> points, Transform target)
    {
        Vector2[] local = new Vector2[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            local[i] = target.InverseTransformPoint(points[i]);
        }
        return local;
    }
}
