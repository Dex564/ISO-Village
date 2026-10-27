using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class LevelGenerator : MonoBehaviour
{
    [Header("Variables")]
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase dirt;
    [SerializeField] private TileBase stone;

    [Header("Floor size")]
    [SerializeField] private int size = 100;
    [SerializeField] private float scale = 0.1f;

    [ContextMenu("Generate floor")]
    public void GenerateLevel()
    {

        tilemap.ClearAllTiles();

        TileBase[] tileMap = new TileBase[size * size];

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float n = Mathf.PerlinNoise(x * scale, y * scale);
                int index = x + y * size;
                tileMap[index] = (n > 0.21f) ? dirt : stone;
            }
        }

        tilemap.SetTilesBlock(new BoundsInt(0, 0, 0, size, size, 1), tileMap);

        Debug.Log($"Generated {size}x{size} floor");
    }

    void Start()
    {
        GenerateLevel();
    }
}