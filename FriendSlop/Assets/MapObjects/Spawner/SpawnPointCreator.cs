using System;
using UnityEditor;
using UnityEngine;
using UnityEditor;

// This script is meant to make it easier to create spawn point scribtable objects
// Instead of typing in all the vectors manually, create an object containing this script
// 1. Add a game object for every spawn point
// 2. Add all of the game objects into the spawnPoints field
// 3. Enter the map name
// 4. Start game
// 5. A scriptable object will be added into the Spawner folder containting all of the spawn points for the level
// 6. Delete all of the temp game objects
public class SpawnPointCreator : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private string mapName;
    [SerializeField] private GameObject[] spawnPoints;
    private void Start()
    {
        SpawnData spawns = ScriptableObject.CreateInstance<SpawnData>();
        spawns.MapName = mapName;

        foreach (GameObject go in spawnPoints)
        {
            spawns.SpawnPoints.Add(go.transform.position);
        }

        AssetDatabase.CreateAsset(spawns, $"Assets/MapObjects/Spawner/{mapName}.asset");
        AssetDatabase.SaveAssets();
    }
#endif
}
