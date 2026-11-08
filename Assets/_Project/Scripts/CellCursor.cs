using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CellCursor : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Camera mainCamera;

    [Header("Вид")]
    [SerializeField] private string sortingLayerName = "Overlay";
    [SerializeField] private int sortingOrder = 0;
    [SerializeField] private Color freeColor = new Color(0.3f, 1f, 0.4f, 0.9f);
    [SerializeField] private Color blockedColor = new Color(1f, 0.3f, 0.3f, 0.9f);

    [Header("Положение")]
    [Tooltip("Сдвиг ромба в пикселях при PPU 32. Y = -2 опускает ромб на 2 пикселя.")]
    [SerializeField] private Vector2Int pixelOffset = Vector2Int.zero;
    [SerializeField] private float pixelsPerUnit = 32f;

    [Header("Отладка")]
    [SerializeField] private bool logOnClick = true;

    public bool HasHoveredCell { get; private set; }

    public Vector2Int HoveredCell { get; private set; }

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (mainCamera == null) mainCamera = Camera.main;

        if (sr.sprite == null) sr.sprite = CreateDiamondSprite();

        if (System.Array.Exists(SortingLayer.layers, l => l.name == sortingLayerName))
        {
            sr.sortingLayerName = sortingLayerName;
        }
        else
        {
            Debug.LogWarning($"CellCursor: слой сортировки '{sortingLayerName}' не найден. " +
                             "Создай его в Project Settings -> Tags and Layers -> Sorting Layers (ниже Default).");
        }
        sr.sortingOrder = sortingOrder;

        sr.enabled = false;
    }

    private void Update()
    {
        HasHoveredCell = false;

        WorldMap map = WorldMap.Instance;
        if (map == null || map.Size == 0)
        {
            sr.enabled = false;
            return;
        }

        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int cellPos = map.WorldToCell(mouseWorld);
        Cell cell = map.GetCell(cellPos);

        if (cell == null)
        {
            sr.enabled = false;
            return;
        }

        HasHoveredCell = true;
        HoveredCell = cellPos;

        transform.position = map.CellToWorld(cellPos) + new Vector3(pixelOffset.x, pixelOffset.y, 0f) / pixelsPerUnit;

        sr.color = cell.Walkable ? freeColor : blockedColor;
        sr.enabled = true;

        if (logOnClick && Input.GetMouseButtonDown(0))
        {
            string occupant = cell.Occupant != null ? cell.Occupant.name : "нет";
            Debug.Log($"Клетка {cellPos}: местность = {cell.Terrain}, объект = {occupant}, проходима = {cell.Walkable}");
        }
    }

    private static Sprite CreateDiamondSprite()
    {
        const int w = 32;
        const int h = 16;
        const float edge = 0.16f;

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - w * 0.5f) / (w * 0.5f);
                float dy = Mathf.Abs(y + 0.5f - h * 0.5f) / (h * 0.5f);
                float f = dx + dy;

                byte alpha = 0;
                if (f <= 1f) alpha = f > 1f - edge ? (byte)255 : (byte)40;

                pixels[x + y * w] = new Color32(255, 255, 255, alpha);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
    }
}