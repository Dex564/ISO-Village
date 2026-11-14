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
    [SerializeField] private CellAnchor stoneAnchor = CellAnchor.Center;
    [SerializeField, Range(0f, 0.25f)] private float stoneJitter = 0f;
    [Tooltip("Описание камня: запас, скорость добычи, навык. Файл Create -> Colony -> Resource Node Data")]
    [SerializeField] private ResourceNodeData stoneData;

    [Header("Карта")]
    [SerializeField] private int size = 64;
    [SerializeField] private bool useRandomSeed = true;

    [Header("Стартовая зона")]
    [SerializeField] private int startRadius = 20;
    [SerializeField] private int guaranteedStonesRadius = 30;
    [SerializeField] private int guaranteedStonesCount = 4;

    public event System.Action OnWorldGenerated;

    // «Соли» для SeededRandom: у каждого назначения случайного числа свой номер.
    private const int SaltStoneChance = 101;
    private const int SaltStoneJitterX = 102;
    private const int SaltStoneJitterY = 103;
    private const int SaltStonePool = 104;

    private int currentSeed;

    [ContextMenu("Generate")]
    public void Generate(int seed)
    {
        if (!ValidateReferences()) return;

        if (useRandomSeed && string.IsNullOrEmpty(SaveManager.CurrentWorldId))
        {
            seed = Random.Range(0, 1000000);
            WorldManager.Seed = seed;
        }
        Debug.Log($"WorldGenerator: seed = {seed}");
        currentSeed = seed;

        Clear();

        var rng = new System.Random(seed);
        Vector2 grassOffset = RandomOffset(rng);
        Vector2 stoneOffset = RandomOffset(rng);

        map.Init(size);
        FillCells(grassOffset);
        PaintGround();
        PlaceStones(stoneOffset);
        EnsureStartStones(rng);
        LogChecksum();

        OnWorldGenerated?.Invoke();
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

    private void PlaceStones(Vector2 offset)
    {
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2Int pos = map.Origin + new Vector2Int(x, y);
                if (MaxDistanceFromCenter(pos) <= startRadius) continue;

                float n = Mathf.PerlinNoise(offset.x + x * stoneNoiseScale, offset.y + y * stoneNoiseScale);
                if (n < stoneNoiseThreshold) continue;
                // Шанс считаем по (seed, клетка), а не по общему потоку случайных чисел:
                // результат для клетки не зависит от порядка обхода и от других ресурсов.
                if (SeededRandom.Value01(currentSeed, pos, SaltStoneChance) > stoneChance) continue;

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

        Vector2 offset = stoneAnchor.ToOffset();
        if (stoneJitter > 0f)
        {
            float jx = SeededRandom.Value01(currentSeed, pos, SaltStoneJitterX) * 2f - 1f;
            float jy = SeededRandom.Value01(currentSeed, pos, SaltStoneJitterY) * 2f - 1f;
            offset += new Vector2(jx, jy) * stoneJitter;
        }

        GameObject stone = Instantiate(stonePrefab, map.CellToWorld(pos, offset), Quaternion.identity, resourcesParent);
        stone.name = $"Stone {pos.x},{pos.y}";

        // Запас камня тоже зависит только от (seed, клетка).
        int pool = SeededRandom.Range(currentSeed, pos, SaltStonePool, stoneData.minPool, stoneData.maxPool);

        ResourceNode node = stone.GetComponent<ResourceNode>();
        if (node == null) node = stone.AddComponent<ResourceNode>();
        node.Init(stoneData, pos, pool);

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

    // Контрольная сумма мира: одинаковый seed обязан давать одинаковое число.
    private void LogChecksum()
    {
        unchecked
        {
            uint hash = 2166136261u;
            foreach (Cell c in map.AllCells())
            {
                hash = (hash ^ (uint)c.GroundVariant) * 16777619u;

                ResourceNode n = c.Occupant != null ? c.Occupant.GetComponent<ResourceNode>() : null;
                if (n == null) continue;

                hash = (hash ^ (uint)c.Pos.x) * 16777619u;
                hash = (hash ^ (uint)c.Pos.y) * 16777619u;
                hash = (hash ^ (uint)n.Data.yieldType) * 16777619u;
                hash = (hash ^ (uint)n.Remaining) * 16777619u;
            }
            Debug.Log($"WorldGenerator: контрольная сумма мира = {hash} (при том же seed число должно совпадать)");
        }
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
        if (map == null || groundTilemap == null || resourcesParent == null || stonePrefab == null || stoneData == null)
        {
            Debug.LogError("WorldGenerator: не назначены ссылки в инспекторе (map, groundTilemap, resourcesParent, stonePrefab, stoneData).");
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