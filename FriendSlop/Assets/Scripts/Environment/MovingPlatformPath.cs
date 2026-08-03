using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class BezierAnchor
{
    public Transform point;
    public Transform inHandle;
    public Transform outHandle;
    public float stopDuration = 0f;
}

public static class MovingPlatformPath
{
    public enum WrapMode
    {
        Loop,
        Clamp
    }


    public static Vector3 Evaluate(List<BezierAnchor> anchors, float t, WrapMode wrapMode)
    {
        int count = anchors.Count;
        if (count == 0) return Vector3.zero;
        if (count == 1) return anchors[0].point.position;

        int segmentCount = wrapMode == WrapMode.Loop ? count : count - 1;

        t = wrapMode == WrapMode.Loop ? Mathf.Repeat(t, 1f) : Mathf.Clamp01(t);

        float scaledT = t * segmentCount;
        int segmentIndex = Mathf.Clamp(Mathf.FloorToInt(scaledT), 0, segmentCount - 1);
        float localT = scaledT - segmentIndex;

        int nextIndex = wrapMode == WrapMode.Loop
            ? (segmentIndex + 1) % count
            : Mathf.Min(segmentIndex + 1, count - 1);

        BezierAnchor start = anchors[segmentIndex];
        BezierAnchor end = anchors[nextIndex];

        return CubicBezier(
            start.point.position,
            start.outHandle.position,
            end.inHandle.position,
            end.point.position,
            localT);
    }

    private static Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        float uu = u * u;
        float uuu = uu * u;
        float tt = t * t;
        float ttt = tt * t;

        return (uuu * p0) + (3f * uu * t * p1) + (3f * u * tt * p2) + (ttt * p3);
    }
}
