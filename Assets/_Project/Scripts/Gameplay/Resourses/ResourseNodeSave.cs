using System;
using UnityEngine;

/// <summary>Запись о ресурсе, запас которого отличается от того, что создаёт генератор.</summary>
[Serializable]
public class ResourceNodeState
{
    public int x;
    public int y;
    public int remaining;   // 0 = добыт полностью
}

/// <summary>
/// Связка ресурсов с сохранением. Мир каждый раз строится заново по seed, поэтому сохраняем
/// только отличия: какие ресурсы тронуты и сколько в них осталось.
/// Нужно поле в SaveData: public List&lt;ResourceNodeState&gt; resourceNodes = new();
/// и вызов ResourceNodeSave.ApplyAll(worldMap) в GameBootstrap после генерации мира.
/// </summary>
public static class ResourceNodeSave
{
    public static void Record(Vector2Int cell, int remaining)
    {
        var list = SaveManager.Current.resourceNodes;
        ResourceNodeState entry = list.Find(s => s.x == cell.x && s.y == cell.y);

        if (entry == null)
            list.Add(new ResourceNodeState { x = cell.x, y = cell.y, remaining = remaining });
        else
            entry.remaining = remaining;
    }

    public static void ApplyAll(WorldMap map)
    {
        if (map == null) return;

        // Копия списка: SetRemaining(0) удаляет ресурс, а удаление не должно менять список во время обхода.
        var states = SaveManager.Current.resourceNodes.ToArray();
        foreach (ResourceNodeState s in states)
        {
            Cell cell = map.GetCell(new Vector2Int(s.x, s.y));
            if (cell == null || cell.Occupant == null) continue;

            ResourceNode node = cell.Occupant.GetComponent<ResourceNode>();
            if (node != null) node.SetRemaining(s.remaining);
        }
    }
}