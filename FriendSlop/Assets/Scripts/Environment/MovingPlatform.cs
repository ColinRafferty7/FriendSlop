using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Moves this platform along a Bezier path. The platform itself is kinematic, so
/// Unity's own physics naturally carries any dynamic Rigidbody resting on it via
/// normal contact/friction resolution - no manual translation code needed here or
/// on the rider's side.
///
/// This script still tracks riders and sends them the platform's current velocity,
/// but that's now used ONLY for BallController's rolling-rotation math (so a ball
/// doesn't visually spin just from being carried with zero slip) - not for moving
/// the ball itself.
/// </summary>
public class MovingPlatform : NetworkBehaviour
{
    [Header("Path")]
    [SerializeField] private List<BezierAnchor> anchors = new List<BezierAnchor>();

    [SerializeField] private MovingPlatformPath.WrapMode wrapMode = MovingPlatformPath.WrapMode.Loop;

    [Header("Movement")]
    [SerializeField] private float speed = 0.1f;

    [Header("Gizmos")]
    [SerializeField] private int gizmoCurveSteps = 50;
    [SerializeField] private float gizmoAnchorRadius = 0.2f;
    [SerializeField] private float gizmoHandleRadius = 0.1f;

    private readonly List<Rigidbody> riders = new List<Rigidbody>();

    private Rigidbody rb;

    private readonly NetworkVariable<float> t = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private float waitTimer = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        AdvanceServerTime();

        if (!HasValidPath()) return;

        Vector3 previousPosition = rb.position;
        Vector3 targetPosition = MovingPlatformPath.Evaluate(anchors, t.Value, wrapMode);
        Vector3 platformVelocity = (targetPosition - previousPosition) / Time.fixedDeltaTime;

        rb.MovePosition(targetPosition);

        for (int i = riders.Count - 1; i >= 0; i--)
        {
            Rigidbody rider = riders[i];

            if (rider == null)
            {
                riders.RemoveAt(i);
                continue;
            }

            BallController ball = rider.GetComponentInParent<BallController>();
            if (ball == null) continue;

            // Purely informational for rotation-matching purposes now - see class
            // summary above. Not used to move the ball.
            ball.SetPlatformVelocity(platformVelocity);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!IsServer) return;

        Rigidbody rider = collision.rigidbody;
        if (rider == null) return;

        if (!riders.Contains(rider))
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                if (Vector3.Dot(contact.normal, Vector3.down) > 0.5f)
                {
                    riders.Add(rider);

                    BallController newBall = rider.GetComponentInParent<BallController>();
                    if (newBall != null)
                    {
                        newBall.SetOnPlatform(true);
                    }

                    break;
                }
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!IsServer) return;

        Rigidbody rider = collision.collider.GetComponentInParent<Rigidbody>();
        if (rider == null) return;

        riders.Remove(rider);

        BallController ball = rider.GetComponentInParent<BallController>();
        if (ball != null)
        {
            ball.SetPlatformVelocity(Vector3.zero);
            ball.SetOnPlatform(false);
        }
    }

    private void AdvanceServerTime()
    {
        if (waitTimer > 0)
        {
            waitTimer -= Time.fixedDeltaTime;
            return;
        }

        int segmentCount = wrapMode == MovingPlatformPath.WrapMode.Loop ? anchors.Count : anchors.Count - 1;
        if (segmentCount <= 0) return;

        float advanced = t.Value + Time.fixedDeltaTime * speed;

        int prevBoundary = Mathf.FloorToInt(t.Value * segmentCount + 0.0001f);
        int newBoundary = Mathf.FloorToInt(advanced * segmentCount);

        // Check EVERY boundary crossed this frame, not just the first - if speed is
        // high enough relative to waypoint spacing, more than one waypoint can be
        // passed within a single physics step, and any of them might have a stop
        // duration that shouldn't get silently skipped.
        for (int boundary = prevBoundary + 1; boundary <= newBoundary; boundary++)
        {
            float boundaryT = (float)boundary / segmentCount;

            int anchorIndex = wrapMode == MovingPlatformPath.WrapMode.Loop
                ? boundary % anchors.Count
                : Mathf.Min(boundary, anchors.Count - 1);

            float stopDuration = anchors[anchorIndex].stopDuration;

            if (stopDuration > 0)
            {
                advanced = boundaryT;
                waitTimer = stopDuration;
                break; // stop at the FIRST qualifying waypoint reached, in order
            }
        }

        t.Value = wrapMode == MovingPlatformPath.WrapMode.Loop
            ? Mathf.Repeat(advanced, 1f)
            : Mathf.Clamp01(advanced);
    }

    private bool HasValidPath()
    {
        if (anchors.Count < 2) return false;

        foreach (var anchor in anchors)
        {
            if (anchor.point == null || anchor.inHandle == null || anchor.outHandle == null)
            {
                return false;
            }
        }

        return true;
    }

    private void OnDrawGizmos()
    {
        if (!HasValidPath()) return;

        foreach (var a in anchors)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(a.point.position, gizmoAnchorRadius);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(a.point.position, a.inHandle.position);
            Gizmos.DrawSphere(a.inHandle.position, gizmoHandleRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(a.point.position, a.outHandle.position);
            Gizmos.DrawSphere(a.outHandle.position, gizmoHandleRadius);
        }

        Gizmos.color = Color.yellow;
        Vector3 prevPoint = MovingPlatformPath.Evaluate(anchors, 0f, wrapMode);

        for (int i = 1; i <= gizmoCurveSteps; i++)
        {
            float sampleT = i / (float)gizmoCurveSteps;
            Vector3 point = MovingPlatformPath.Evaluate(anchors, sampleT, wrapMode);
            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }
    }
}