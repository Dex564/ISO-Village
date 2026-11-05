using System.Collections.Generic;
using UnityEngine;

/// Хранит данные карты и отвечает за все переводы координат.
/// Остальные скрипты не должны вызывать grid.WorldToCell сами, только через этот класс.

public class WorldMap : MonoBehaviour
{
    public static WorldMap Instance { get; private set; }

    [SerializeField] private Grid grid;

    public int Size { get; private set; }

    /// Координата клетки, лежащей в cells[0, 0]. Для размера 64 это (-32, -32).
    public Vector2Int Origin { get; private set; }

    private Cell[,] cells;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }


    public void Init(int size)
    {
        Size = size;
        Origin = new Vector2Int(-size / 2, -size / 2);
        cells = new Cell[size, size];
    }

    public void SetCell(Cell cell)
    {
        Vector2Int i = cell.Pos - Origin;
        cells[i.x, i.y] = cell;
    }


    public bool InBounds(Vector2Int pos)
    {
        Vector2Int i = pos - Origin;
        return i.x >= 0 && i.y >= 0 && i.x < Size && i.y < Size;
    }

    public Cell GetCell(Vector2Int pos)
    {
        if (!InBounds(pos)) return null;
        return cells[pos.x - Origin.x, pos.y - Origin.y];
    }

    public Cell GetCellAtWorld(Vector3 world)
    {
        return GetCell(WorldToCell(world));
    }

    public IEnumerable<Cell> AllCells()
    {
        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
                yield return cells[x, y];
    }


    public Vector2Int WorldToCell(Vector3 world)
    {
        world.z = 0f;
        Vector3Int c = grid.WorldToCell(world);
        return new Vector2Int(c.x, c.y);
    }

    public Vector3 CellToWorld(Vector2Int pos)
    {
        Vector3 w = grid.GetCellCenterWorld(new Vector3Int(pos.x, pos.y, 0));
        w.z = 0f;
        return w;
    }

    // origin: клетка с наименьшими x и y; footprint: размер основания в клетках, например (2, 2).
    public bool CanPlace(Vector2Int origin, Vector2Int footprint)
    {
        for (int x = 0; x < footprint.x; x++)
        {
            for (int y = 0; y < footprint.y; y++)
            {
                Cell c = GetCell(origin + new Vector2Int(x, y));
                if (c == null || !c.Buildable) return false;
            }
        }
        return true;
    }

    public void Occupy(Vector2Int origin, Vector2Int footprint, GameObject occupant)
    {
        for (int x = 0; x < footprint.x; x++)
        {
            for (int y = 0; y < footprint.y; y++)
            {
                Cell c = GetCell(origin + new Vector2Int(x, y));
                if (c != null) c.Occupant = occupant;
            }
        }
    }

    public void Free(Vector2Int origin, Vector2Int footprint, GameObject occupant)
    {
        for (int x = 0; x < footprint.x; x++)
        {
            for (int y = 0; y < footprint.y; y++)
            {
                Cell c = GetCell(origin + new Vector2Int(x, y));
                if (c != null && c.Occupant == occupant) c.Occupant = null;
            }
        }
    }
}

