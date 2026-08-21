using UnityEngine;
using System.Collections.Generic;
using System;

public class GamePlayPause : MonoBehaviour
{
    public static bool DebugPaused;

    [SerializeField] private List<MonoBehaviour> components = new List<MonoBehaviour>();
    [SerializeField] private Collider col;
    [SerializeField] private Rigidbody rb;
    private Vector3 storedVelo;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            DebugPaused = !DebugPaused;

            foreach (MonoBehaviour comp in components)
            {
                comp.enabled = DebugPaused;
            }

            col.enabled = DebugPaused;

            if (DebugPaused) storedVelo = rb.linearVelocity;

            rb.linearVelocity = DebugPaused ? Vector3.zero : storedVelo;
            rb.useGravity = !DebugPaused;
        }
    }
}
