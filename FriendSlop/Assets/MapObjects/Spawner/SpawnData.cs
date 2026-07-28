using Unity.VectorGraphics;
using UnityEngine;


[CreateAssetMenu(fileName = "SpawnData", menuName = "Maps/SpawnData")]
public class SpawnData : ScriptableObject
{
    public string MapName;
    public Vector3[] SpawnPoints;

    public Vector3 GetRandomSpawnPoint()
    {
        if (SpawnPoints.Length == 0) return Vector3.zero;

        else return SpawnPoints[Random.Range(0, SpawnPoints.Length)];
    }
}
