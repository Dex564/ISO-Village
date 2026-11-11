using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WorldMeta
{
    public string id;
    public string worldName;
    public int seed;
    public string lastPlayed;
}

[Serializable]
public class SaveData
{
    [SerializeField] public Dictionary<ResourceType, int> resources = new()
    {
        [ResourceType.Coins] = 9999,
        [ResourceType.Wood] = 9999,
        [ResourceType.Stone] = 9999,
        [ResourceType.Crystals] = 9999,
    };
    public string worldName;
    public int seed = 0;
    public string lastPlayed;
    public List<PlacedBuildingData> buildings = new();
}

[Serializable]
public class PlacedBuildingData
{
    public string buildingName;
    public int originX;
    public int originY;
    public int footprintX;
    public int footprintY;
    public bool flipped;
}