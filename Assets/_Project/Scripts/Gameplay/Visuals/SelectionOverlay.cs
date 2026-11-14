using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Рисует подсветку клеток поверх карты. Спрайты берутся из пула, поэтому
/// перерисовка области (Begin -> Add... -> End) не создаёт лишних объектов.
/// </summary>
public class SelectionOverlay : MonoBehaviour
{
    [SerializeField] private string sortingLayerName = "Overlay";
    [Tooltip("Заливки рисуются на этом порядке, контуры на единицу выше. Курсор клетки должен быть выше.")]
    [SerializeField] private int sortingOrder = -2;

    private readonly List<SpriteRenderer> pool = new List<SpriteRenderer>();
    private int used;
    private bool layerExists;

    private void Awake()
    {
        layerExists = System.Array.Exists(SortingLayer.layers, l => l.name == sortingLayerName);
        if (!layerExists)
            Debug.LogWarning($"SelectionOverlay: слой '{sortingLayerName}' не найден, подсветка будет на Default.");
    }

    /// <summary>Начать новую перерисовку.</summary>
    public void Begin()
    {
        used = 0;
    }

    /// <summary>Подсветить клетку. filled = заливка, иначе контур.</summary>
    public void Add(Vector2Int cell, Color color, bool filled)
    {
        WorldMap map = WorldMap.Instance;
        if (map == null) return;

        SpriteRenderer sr = Get(used++);
        sr.sprite = filled ? CellSprites.DiamondFilled : CellSprites.Diamond;
        sr.color = color;
        sr.sortingOrder = sortingOrder + (filled ? 0 : 1);
        sr.transform.position = map.CellToWorld(cell);
        sr.enabled = true;
    }

    /// <summary>Закончить перерисовку: неиспользованные спрайты прячутся.</summary>
    public void End()
    {
        for (int i = used; i < pool.Count; i++) pool[i].enabled = false;
    }

    public void Clear()
    {
        Begin();
        End();
    }

    private SpriteRenderer Get(int index)
    {
        while (pool.Count <= index) pool.Add(CreateRenderer());
        return pool[index];
    }

    private SpriteRenderer CreateRenderer()
    {
        var go = new GameObject("OverlayCell");
        go.transform.SetParent(transform, false);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        if (layerExists) sr.sortingLayerName = sortingLayerName;
        sr.enabled = false;
        return sr;
    }
}