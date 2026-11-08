using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FreeCameraController : MonoBehaviour
{
    [Header("Клавиатура")]
    [Tooltip("Скорость в единицах мира в секунду на единицу размера камеры (чем дальше зум, тем быстрее).")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Мышь")]
    [Tooltip("0 = ЛКМ, 1 = ПКМ, 2 = средняя кнопка")]
    [SerializeField] private int dragMouseButton = 2;

    [Header("Границы карты")]
    [SerializeField] private bool clampToMap = true;
    [Tooltip("Запас за краем карты, в единицах мира")]
    [SerializeField] private float boundsPadding = 2f;

    private Camera cam;
    private Vector3 dragOrigin;
    private bool dragging;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Update()
    {
        HandleKeyboard();
        HandleDrag();
        ClampToMap();
    }

    private void HandleKeyboard()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 1f) input.Normalize();

        float speed = moveSpeed * cam.orthographicSize;
        Vector2 delta = input * (speed * Time.unscaledDeltaTime);
        transform.position += new Vector3(delta.x, delta.y, 0f);
    }

    private void HandleDrag()
    {
        if (Input.GetMouseButtonDown(dragMouseButton))
        {
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
            dragging = true;
        }

        if (Input.GetMouseButtonUp(dragMouseButton))
        {
            dragging = false;
        }

        if (dragging && Input.GetMouseButton(dragMouseButton))
        {
            Vector3 diff = dragOrigin - cam.ScreenToWorldPoint(Input.mousePosition);
            transform.position += new Vector3(diff.x, diff.y, 0f);
        }
    }

    private void ClampToMap()
    {
        if (!clampToMap) return;

        WorldMap map = WorldMap.Instance;
        if (map == null || map.Size == 0) return;

        Vector2Int min = map.Origin;
        Vector2Int max = map.Origin + new Vector2Int(map.Size - 1, map.Size - 1);

        Vector3 bottom = map.CellToWorld(min);
        Vector3 top = map.CellToWorld(max);
        Vector3 left = map.CellToWorld(new Vector2Int(min.x, max.y));
        Vector3 right = map.CellToWorld(new Vector2Int(max.x, min.y));

        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, left.x - boundsPadding, right.x + boundsPadding);
        p.y = Mathf.Clamp(p.y, bottom.y - boundsPadding, top.y + boundsPadding);
        transform.position = p;
    }
}