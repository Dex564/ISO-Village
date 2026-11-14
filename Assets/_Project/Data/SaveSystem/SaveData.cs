using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class ResourceEntry { public ResourceType type; public int amount; }

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
    public List<ResourceEntry> resources = new()
    {
        new() { type = ResourceType.Coins,    amount = 100 },
        new() { type = ResourceType.Wood,     amount = 50 },
        new() { type = ResourceType.Stone,    amount = 50 },
        new() { type = ResourceType.Crystals, amount = 0 },
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