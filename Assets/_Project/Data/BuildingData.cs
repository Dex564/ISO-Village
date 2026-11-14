using UnityEngine;

[CreateAssetMenu(fileName = "NewBuilding", menuName = "Placement/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Identity")]
    public string buildingNameID = "Building";
    public string buildingName = "Здание";

    [Header("Prefabs")]
    public GameObject buildingPrefab;

    [Header("Size")]
    public Vector2Int footprint;

    [Header("Gameplay")]
    [Min(0)] public int coins = 0;
    [Min(0)] public int wood = 0;
    [Min(0)] public int stone = 0;
}