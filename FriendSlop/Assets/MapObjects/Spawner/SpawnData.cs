using Unity.VectorGraphics;
using UnityEngine;
using System.Collections.Generic;


[CreateAssetMenu(fileName = "SpawnData", menuName = "Maps/SpawnData")]
public class SpawnData : ScriptableObject
{
    public string MapName;
    public List<Vector3> SpawnPoints = new List<Vector3>();

    public Vector3 GetRandomSpawnPoint()
    {
        if (SpawnPoints.Count == 0) return Vector3.zero;

        else return SpawnPoints[Random.Range(0, SpawnPoints.Count)];
    }
}
