using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Доска задач: список ресурсов, помеченных игроком к добыче.
/// Свободный поселенец спрашивает у доски, чем заняться (TryClaim), и получает ближайший
/// подходящий ему ресурс, до которого можно дойти.
/// </summary>
public class JobBoard : MonoBehaviour
{
    public static JobBoard Instance { get; private set; }

    [Tooltip("Сколько ближайших ресурсов проверять на достижимость за один запрос")]
    [SerializeField] private int maxPathChecks = 5;
    [Tooltip("Через сколько секунд пробовать снова, если к ресурсу не подойти")]
    [SerializeField] private float unreachableRetrySeconds = 5f;

    private readonly List<ResourceNode> designated = new List<ResourceNode>();
    private readonly List<ResourceNode> candidates = new List<ResourceNode>();

    public int Count => designated.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Designate(ResourceNode node)
    {
        if (node == null || node.IsDepleted || node.IsDesignated) return;

        node.SetDesignated(true);
        node.NextPathAttemptTime = 0f;
        designated.Add(node);
    }

    public void Cancel(ResourceNode node)
    {
        if (node == null) return;

        designated.Remove(node);
        node.SetDesignated(false);
    }

    /// <summary>Убрать из списка без смены внешнего вида (ресурс исчерпан).</summary>
    public void Remove(ResourceNode node)
    {
        designated.Remove(node);
    }

    public void Release(ResourceNode node, Settler settler)
    {
        if (node != null && node.ClaimedBy == settler) node.ClaimedBy = null;
    }

    /// <summary>
    /// Подбирает поселенцу работу. Возвращает ближайший подходящий по навыку и достижимый ресурс
    /// и путь к клетке рядом с ним. Ресурс помечается занятым этим поселенцем.
    /// </summary>
    public bool TryClaim(Settler settler, out ResourceNode claimed, out List<Vector2Int> path)
    {
        claimed = null;
        path = null;

        WorldMap map = WorldMap.Instance;
        if (map == null || map.Size == 0) return false;

        candidates.Clear();
        for (int i = designated.Count - 1; i >= 0; i--)
        {
            ResourceNode n = designated[i];
            if (n == null || n.IsDepleted)
            {
                designated.RemoveAt(i);
                continue;
            }

            if (n.ClaimedBy != null) continue;                    // уже кто-то работает
            if (Time.time < n.NextPathAttemptTime) continue;      // недавно не нашли путь
            if (!settler.CanWork(n.Data)) continue;               // не подходит по навыку

            candidates.Add(n);
        }

        if (candidates.Count == 0) return false;

        Vector2Int from = settler.CellPos;
        candidates.Sort((a, b) =>
            (a.CellPos - from).sqrMagnitude.CompareTo((b.CellPos - from).sqrMagnitude));

        int checks = Mathf.Min(maxPathChecks, candidates.Count);
        for (int i = 0; i < checks; i++)
        {
            ResourceNode n = candidates[i];
            List<Vector2Int> found = Pathfinder.FindPathToNeighbour(map, from, n.CellPos);

            if (found == null)
            {
                n.NextPathAttemptTime = Time.time + unreachableRetrySeconds;
                continue;
            }

            n.ClaimedBy = settler;
            claimed = n;
            path = found;
            return true;
        }

        return false;
    }
}