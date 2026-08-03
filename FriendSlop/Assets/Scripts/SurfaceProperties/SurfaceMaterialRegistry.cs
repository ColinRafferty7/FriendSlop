using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "SurfaceMaterialRegistry", menuName = "Surfaces/Surface Material Registry")]
public class SurfaceMaterialRegistry : ScriptableObject
{
    [System.Serializable]
    public class Mapping
    {
        public PhysicsMaterial physicsMaterial;
        public SurfaceData surfaceData;
    }

    public List<Mapping> mappings = new List<Mapping>();


    private Dictionary<PhysicsMaterial, SurfaceData> lookup;

    public SurfaceData GetSurfaceData(PhysicsMaterial material)
    {
        if (material == null) return null;

        if (lookup == null)
        {
            lookup = new Dictionary<PhysicsMaterial, SurfaceData>();
            foreach (var mapping in mappings)
            {
                if (mapping.physicsMaterial != null && !lookup.ContainsKey(mapping.physicsMaterial))
                {
                    lookup.Add(mapping.physicsMaterial, mapping.surfaceData);
                }
            }
        }

        lookup.TryGetValue(material, out SurfaceData data);
        return data;
    }
}
