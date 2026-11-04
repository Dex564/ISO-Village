using UnityEngine;

public enum TerrainType
{
    Grass
    // Water, Sand, Hill, Mountain
}

public class Cell
{
    public readonly Vector2Int Pos;
    public TerrainType Terrain;
    public int GroundVariant;      // какой из тайлов, нужно только для отрисовки
    public GameObject Occupant;

    public Cell(Vector2Int pos, TerrainType terrain, int groundVariant)
    {
        Pos = pos;
        Terrain = terrain;
        GroundVariant = groundVariant;
    }

    public bool IsOccupied => Occupant != null;

    public bool TerrainWalkable => Terrain == TerrainType.Grass;
    public bool TerrainBuildable => Terrain == TerrainType.Grass;

    public bool Walkable => TerrainWalkable && !IsOccupied;
    public bool Buildable => TerrainBuildable && !IsOccupied;
}
