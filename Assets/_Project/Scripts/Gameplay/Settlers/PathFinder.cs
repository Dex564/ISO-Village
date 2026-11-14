using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Поиск пути A* по клеткам WorldMap.
/// Ходим в 8 направлениях, диагональ разрешена, только если обе соседние по осям клетки проходимы.
/// Стоимость шага равна реальной длине шага на экране, поэтому путь получается самым быстрым по времени.
/// Список кандидатов хранится в бинарной куче, так что поиск быстрый и на большой карте.
/// </summary>
public static class Pathfinder
{
    // Первые 4 это шаги по осям клетки, последние 4 это диагонали клетки.
    private static readonly Vector2Int[] Dirs =
    {
        new Vector2Int( 1,  0), new Vector2Int(-1,  0), new Vector2Int( 0,  1), new Vector2Int( 0, -1),
        new Vector2Int( 1,  1), new Vector2Int( 1, -1), new Vector2Int(-1,  1), new Vector2Int(-1, -1)
    };

    /// <summary>
    /// Путь от start до goal (start в список не входит, goal входит).
    /// Пустой список значит «уже на месте». null значит «пути нет».
    /// </summary>
    public static List<Vector2Int> FindPath(WorldMap map, Vector2Int start, Vector2Int goal)
    {
        if (map == null || !map.InBounds(start) || !map.InBounds(goal)) return null;
        if (start == goal) return new List<Vector2Int>();

        Cell goalCell = map.GetCell(goal);
        if (goalCell == null || !goalCell.Walkable) return null;

        return Search(map, start, goal, false);
    }

    /// <summary>
    /// Путь к ближайшей проходимой клетке рядом с target (среди 8 соседей).
    /// Нужно для добычи: сама клетка с ресурсом непроходима, поселенец встаёт рядом.
    /// Пустой список значит «уже стоим рядом».
    /// </summary>
    public static List<Vector2Int> FindPathToNeighbour(WorldMap map, Vector2Int start, Vector2Int target)
    {
        if (map == null || !map.InBounds(start) || !map.InBounds(target)) return null;
        return Search(map, start, target, true);
    }

    private static List<Vector2Int> Search(WorldMap map, Vector2Int start, Vector2Int target, bool toNeighbour)
    {
        int size = map.Size;
        Vector2Int origin = map.Origin;
        int total = size * size;

        // Сетка линейна: мировой вектор смещения (dx, dy) = dx * ex + dy * ey.
        Vector3 c0 = map.CellToWorld(start);
        Vector2 ex = map.CellToWorld(start + Vector2Int.right) - c0;
        Vector2 ey = map.CellToWorld(start + Vector2Int.up) - c0;

        float Dist(Vector2Int d) => (d.x * ex + d.y * ey).magnitude;
        int Index(Vector2Int p) => (p.x - origin.x) + (p.y - origin.y) * size;

        // Самый длинный шаг: дальше этого от центра цели соседняя клетка быть не может.
        float maxStep = 0f;
        for (int i = 0; i < Dirs.Length; i++) maxStep = Mathf.Max(maxStep, Dist(Dirs[i]));

        bool IsGoal(Vector2Int p)
        {
            if (!toNeighbour) return p == target;

            Vector2Int d = p - target;
            return p != target && Mathf.Abs(d.x) <= 1 && Mathf.Abs(d.y) <= 1;
        }

        float Heuristic(Vector2Int p)
        {
            float d = Dist(target - p);
            return toNeighbour ? Mathf.Max(0f, d - maxStep) : d;
        }

        if (IsGoal(start)) return new List<Vector2Int>();

        float[] g = new float[total];       // стоимость пути от старта
        int[] parent = new int[total];      // откуда пришли
        bool[] closed = new bool[total];    // уже обработанные клетки

        for (int i = 0; i < total; i++)
        {
            g[i] = float.MaxValue;
            parent[i] = -1;
        }

        var heap = new MinHeap();
        int startIndex = Index(start);
        g[startIndex] = 0f;
        heap.Push(startIndex, Heuristic(start));

        while (heap.Count > 0)
        {
            int cur = heap.Pop();
            if (closed[cur]) continue; // устаревшая запись в куче
            closed[cur] = true;

            Vector2Int cp = new Vector2Int(cur % size + origin.x, cur / size + origin.y);
            if (IsGoal(cp)) return Reconstruct(parent, cur, startIndex, origin, size);

            for (int d = 0; d < Dirs.Length; d++)
            {
                Vector2Int np = cp + Dirs[d];
                Cell nc = map.GetCell(np);
                if (nc == null || !nc.Walkable) continue;

                int ni = Index(np);
                if (closed[ni]) continue;

                // Диагональ: нельзя срезать угол через занятую клетку.
                if (d >= 4)
                {
                    Cell a = map.GetCell(new Vector2Int(cp.x + Dirs[d].x, cp.y));
                    Cell b = map.GetCell(new Vector2Int(cp.x, cp.y + Dirs[d].y));
                    if (a == null || !a.Walkable || b == null || !b.Walkable) continue;
                }

                float ng = g[cur] + Dist(Dirs[d]);
                if (ng >= g[ni]) continue; // уже есть путь не хуже

                g[ni] = ng;
                parent[ni] = cur;
                heap.Push(ni, ng + Heuristic(np));
            }
        }

        return null; // кандидаты кончились, до цели не добраться
    }

    private static List<Vector2Int> Reconstruct(int[] parent, int cur, int startIndex, Vector2Int origin, int size)
    {
        var path = new List<Vector2Int>();

        while (cur != startIndex)
        {
            path.Add(new Vector2Int(cur % size + origin.x, cur / size + origin.y));
            cur = parent[cur];
        }

        path.Reverse();
        return path;
    }

    // Минимальная куча: Pop() всегда отдаёт элемент с наименьшим ключом.
    private sealed class MinHeap
    {
        private readonly List<int> items = new List<int>();
        private readonly List<float> keys = new List<float>();

        public int Count => items.Count;

        public void Push(int item, float key)
        {
            items.Add(item);
            keys.Add(key);

            int i = items.Count - 1;
            while (i > 0)
            {
                int up = (i - 1) / 2;
                if (keys[up] <= keys[i]) break;
                Swap(i, up);
                i = up;
            }
        }

        public int Pop()
        {
            int top = items[0];
            int last = items.Count - 1;

            items[0] = items[last];
            keys[0] = keys[last];
            items.RemoveAt(last);
            keys.RemoveAt(last);

            int n = items.Count;
            int i = 0;
            while (true)
            {
                int left = 2 * i + 1;
                int right = left + 1;
                int smallest = i;

                if (left < n && keys[left] < keys[smallest]) smallest = left;
                if (right < n && keys[right] < keys[smallest]) smallest = right;
                if (smallest == i) break;

                Swap(i, smallest);
                i = smallest;
            }

            return top;
        }

        private void Swap(int a, int b)
        {
            (items[a], items[b]) = (items[b], items[a]);
            (keys[a], keys[b]) = (keys[b], keys[a]);
        }
    }
}