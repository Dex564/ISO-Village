using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldGenerator : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private WorldMap map;
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Transform resourcesParent;

    [Header("Земля")]
    [SerializeField] private TileBase[] grassTiles = new TileBase[2];
    [SerializeField, Range(0.05f, 0.3f)] private float grassNoiseScale = 0.12f;
    [SerializeField, Range(0f, 1f)] private float grassVariantThreshold = 0.55f;

    [Header("Камни")]
    [SerializeField] private GameObject stonePrefab;
    [SerializeField, Range(0.05f, 0.3f)] private float stoneNoiseScale = 0.1f;
    [SerializeField, Range(0f, 1f)] private float stoneNoiseThreshold = 0.6f;
    [SerializeField, Range(0f, 1f)] private float stoneChance = 0.35f;

    [Header("Карта")]
    [SerializeField] private int size = 64;
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private int seed = 12345;

    [Header("Стартовая зона")]
    [SerializeField] private int startRadius = 20;
    [SerializeField] private int guaranteedStonesRadius = 30;
    [SerializeField] private int guaranteedStonesCount = 4;

    private void Start()
    {
        Generate();
    }

    // Можно вызвать в режиме Play: ПКМ по заголовку компонента -> Generate.
    [ContextMenu("Generate")]
    public void Generate()
    {
        if (!ValidateReferences()) return;

        if (useRandomSeed) seed = Random.Range(0, 1000000);
        Debug.Log($"WorldGenerator: seed = {seed}");

        Clear();

        var rng = new System.Random(seed);
        Vector2 grassOffset = RandomOffset(rng);
        Vector2 stoneOffset = RandomOffset(rng);

        map.Init(size);
        FillCells(grassOffset);
        PaintGround();
        PlaceStones(rng, stoneOffset);
        EnsureStartStones(rng);
    }


    private void FillCells(Vector2 offset)
    {
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2Int pos = map.Origin + new Vector2Int(x, y);
                float n = Mathf.PerlinNoise(offset.x + x * grassNoiseScale, offset.y + y * grassNoiseScale);
                int variant = n > grassVariantThreshold ? 1 : 0;
                map.SetCell(new Cell(pos, TerrainType.Grass, variant));
            }
        }
    }

    private void PaintGround()
    {
        var tiles = new TileBase[size * size];
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Cell cell = map.GetCell(map.Origin + new Vector2Int(x, y));
                tiles[x + y * size] = grassTiles[cell.GroundVariant];
            }
        }
        groundTilemap.SetTilesBlock(new BoundsInt(map.Origin.x, map.Origin.y, 0, size, size, 1), tiles);
    }

    private void PlaceStones(System.Random rng, Vector2 offset)
    {
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2Int pos = map.Origin + new Vector2Int(x, y);
                if (MaxDistanceFromCenter(pos) <= startRadius) continue;

                float n = Mathf.PerlinNoise(offset.x + x * stoneNoiseScale, offset.y + y * stoneNoiseScale);
                if (n < stoneNoiseThreshold) continue;
                if (rng.NextDouble() > stoneChance) continue;

                TryPlaceStone(pos);
            }
        }
    }

    
    private void EnsureStartStones(System.Random rng)
    {
        int count = CountStonesInRadius(guaranteedStonesRadius);
        int attempts = 0;

        while (count < guaranteedStonesCount && attempts < 500)
        {
            attempts++;
            Vector2Int pos = new Vector2Int(
                rng.Next(-guaranteedStonesRadius, guaranteedStonesRadius + 1),
                rng.Next(-guaranteedStonesRadius, guaranteedStonesRadius + 1));

            if (MaxDistanceFromCenter(pos) <= startRadius) continue;
            if (TryPlaceStone(pos)) count++;
        }

        if (count < guaranteedStonesCount)
            Debug.LogWarning("WorldGenerator: не удалось разместить нужное число камней у старта.");
    }


    private bool TryPlaceStone(Vector2Int pos)
    {
        Cell cell = map.GetCell(pos);
        if (cell == null || !cell.Buildable) return false;
        if (HasOccupiedNeighbour(pos)) return false;

        GameObject stone = Instantiate(stonePrefab, map.CellToWorld(pos), Quaternion.identity, resourcesParent);
        stone.name = $"Stone {pos.x},{pos.y}";
        cell.Occupant = stone;
        return true;
    }

    private bool HasOccupiedNeighbour(Vector2Int pos)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                Cell c = map.GetCell(pos + new Vector2Int(dx, dy));
                if (c != null && c.IsOccupied) return true;
            }
        }
        return false;
    }

    private int CountStonesInRadius(int radius)
    {
        int count = 0;
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Cell c = map.GetCell(new Vector2Int(x, y));
                if (c != null && c.IsOccupied) count++;
            }
        }
        return count;
    }

    private static int MaxDistanceFromCenter(Vector2Int pos)
    {
        return Mathf.Max(Mathf.Abs(pos.x), Mathf.Abs(pos.y));
    }

    private static Vector2 RandomOffset(System.Random rng)
    {
        return new Vector2(rng.Next(0, 10000), rng.Next(0, 10000));
    }

    [ContextMenu("Clear")]
    private void Clear()
    {
        groundTilemap.ClearAllTiles();
        for (int i = resourcesParent.childCount - 1; i >= 0; i--)
            DestroyImmediate(resourcesParent.GetChild(i).gameObject);
    }

    private bool ValidateReferences()
    {
        if (map == null || groundTilemap == null || resourcesParent == null || stonePrefab == null)
        {
            Debug.LogError("WorldGenerator: не назначены ссылки в инспекторе (map, groundTilemap, resourcesParent, stonePrefab).");
            return false;
        }
        if (grassTiles == null || grassTiles.Length < 2 || grassTiles[0] == null || grassTiles[1] == null)
        {
            Debug.LogError("WorldGenerator: нужны два тайла травы в массиве grassTiles.");
            return false;
        }
        return true;
    }
}