using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BuildingData[] availableBuildings;
    [SerializeField] private Transform buildingsParent;

    private readonly Dictionary<string, BuildingData> _byName = new();

    private void Awake()
    {
        foreach (var b in availableBuildings)
        {
            if (b == null || string.IsNullOrEmpty(b.buildingName)) continue;
            if (_byName.ContainsKey(b.buildingName))
            {
                Debug.LogError($"[BuildingManager] Дублирующийся buildingName '{b.buildingName}'");
                continue;
            }
            _byName[b.buildingName] = b;
        }
    }

    public void RegisterPlaced(BuildingData data, Vector2Int origin, Vector2Int footprint, bool flipped)
    {
        if (data == null || string.IsNullOrEmpty(data.buildingName)) return;

        SaveManager.Current.buildings.Add(new PlacedBuildingData
        {
            buildingName = data.buildingName,
            originX = origin.x,
            originY = origin.y,
            footprintX = footprint.x,
            footprintY = footprint.y,
            flipped = flipped
        });
    }

    public void RebuildAll(WorldMap map)
    {
        if (map == null)
        {
            Debug.LogError("[BuildingManager] RebuildAll: WorldMap == null");
            return;
        }

        foreach (var b in SaveManager.Current.buildings)
        {
            if (!_byName.TryGetValue(b.buildingName, out var data))
            {
                Debug.LogWarning($"[BuildingManager] Пропущен неизвестный buildingName '{b.buildingName}'");
                continue;
            }

            Vector2Int origin = new(b.originX, b.originY);
            Vector2Int footprint = new(
                Mathf.Max(1, b.footprintX),
                Mathf.Max(1, b.footprintY));

            Vector3 center = GetCenter(map, origin, footprint);
            GameObject building = Instantiate(data.buildingPrefab, center, Quaternion.identity, buildingsParent);
            building.name = data.buildingName;

            Vector3 scale = data.buildingPrefab.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (b.flipped ? -1f : 1f);
            building.transform.localScale = scale;

            map.Occupy(origin, footprint, building);
        }
    }

    private static Vector3 GetCenter(WorldMap map, Vector2Int origin, Vector2Int footprint)
    {
        Vector3 a = map.CellToWorld(origin);
        Vector3 b = map.CellToWorld(origin + footprint - Vector2Int.one);
        return (a + b) * 0.5f;
    }
}