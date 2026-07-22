using UnityEngine;
using System.Collections.Generic;

public class ItemSpawner : MonoBehaviour
{
    [SerializeField] bool isRandom;
    [SerializeField] float spawnDelay;
    [SerializeField] Transform spawnPoint;
    [SerializeField] float spawnHeightOffset = 0.5f;

    [System.Serializable]
    public class Item
    {
        public bool isOn;
        public GameObject itemPrefab; 
        public float rarity;          
    }

    [SerializeField] Item[] items;

    float timer = 0f;
    int nextFixedIndex = 0;
    GameObject currentItem;

    void Update()
    {
        if (currentItem != null) return;

        timer += Time.deltaTime;
        if (timer >= spawnDelay)
        {
            timer = 0f;
            SpawnItem();
        }
    }

    void SpawnItem()
    {
        if (items == null || items.Length == 0) return;

        GameObject prefabToSpawn = isRandom ? GetRandomItem() : GetNextItem();
        if (prefabToSpawn == null) return;

        Vector3 basePos = spawnPoint != null ? spawnPoint.position : transform.position;
        Vector3 pos = basePos + Vector3.up * spawnHeightOffset;

        currentItem = Instantiate(prefabToSpawn, pos, Quaternion.identity);
    }

    GameObject GetRandomItem()
    {
        float totalRarity = 0f;
        foreach (var item in items)
        {
            if (!item.isOn)
                continue;
            totalRarity += item.rarity;
        }
        if (totalRarity <= 0f) return null;

        float roll = Random.Range(0f, totalRarity);
        float cumulative = 0f;

        foreach (var item in items)
        {
            if (!item.isOn)
                continue;
            cumulative += item.rarity;
            if (roll <= cumulative)
                return item.itemPrefab;
        }

        return items[items.Length - 1].itemPrefab; 
    }

    GameObject GetNextItem()
    {
        GameObject prefab = items[nextFixedIndex].itemPrefab;
        nextFixedIndex = (nextFixedIndex + 1) % items.Length;
        return prefab;
    }
}
