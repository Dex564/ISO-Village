using UnityEngine;

[CreateAssetMenu(fileName = "NewBuilding", menuName = "Placement/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Identity")]
    public string buildingName = "Building";

    [Header("Prefabs")]
    public GameObject buildingPrefab;

    [Header("Size")]
    public Vector2Int footprint;

    [Header("Gameplay")]
    [Min(0)] public int cost = 0;
}