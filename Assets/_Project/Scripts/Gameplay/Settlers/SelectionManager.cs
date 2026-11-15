using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Выбор поселенца кликом ЛКМ. Выбранный получает ручные приказы (ПКМ), его информация
/// показывается в панели справа. Клик по пустому месту снимает выбор.
/// Не срабатывает, когда включено строительство, режим выделения области или курсор над интерфейсом.
/// </summary>
public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance { get; private set; }

    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlacementSystem placement;
    [SerializeField] private AreaSelector areaSelector;
    [SerializeField] private int selectMouseButton = 0;
    [Tooltip("Запас вокруг спрайта поселенца, в единицах мира (маленькие спрайты трудно выбирать)")]
    [SerializeField] private float clickPadding = 0.15f;

    public Settler Selected { get; private set; }

    /// <summary>Срабатывает при смене выбора (аргумент null = выбор снят).</summary>
    public event Action<Settler> OnSelectionChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (mainCamera == null) mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(selectMouseButton)) return;

        if (placement != null && placement.IsPlacing) return;
        if (areaSelector != null && areaSelector.IsModeActive) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Vector3 mouse = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Select(FindSettlerAt(mouse));
    }

    public void Select(Settler settler)
    {
        if (settler == Selected) return;

        if (Selected != null) Selected.SetSelected(false);
        Selected = settler;
        if (Selected != null) Selected.SetSelected(true);

        OnSelectionChanged?.Invoke(Selected);
    }

    // Если под курсором несколько, берём того, чья позиция ближе к курсору.
    private Settler FindSettlerAt(Vector3 world)
    {
        Settler best = null;
        float bestDistance = float.MaxValue;

        foreach (Settler s in Settler.All)
        {
            if (!s.ContainsPoint(world, clickPadding)) continue;

            float d = ((Vector2)s.transform.position - (Vector2)world).sqrMagnitude;
            if (d < bestDistance)
            {
                bestDistance = d;
                best = s;
            }
        }

        return best;
    }
}