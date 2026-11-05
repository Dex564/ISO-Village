using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public int coins = 9999;
    public int wood = 9999;
    public int stone = 9999;

    public int seed = 13376767;
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