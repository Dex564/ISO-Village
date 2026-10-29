using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class LevelGenerator : MonoBehaviour
{
    [SerializeField] private Tilemap levelTilemap;
    [SerializeField] private TileBase dirt;
    [SerializeField] private TileBase stone;

    [SerializeField] private Tilemap collisionTilemap;
    [SerializeField] private TileBase collisionTile;

    [SerializeField] private int size = 100;
    [SerializeField] private float scale = 0.1f;

    public void GenerateLevel()
    {

        levelTilemap.ClearAllTiles();

        TileBase[] tileMap = new TileBase[size * size];
        TileBase[] collisionMap = new TileBase[size * size];

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                int index = x + y * size;

                if (x == 0 || x == size-1)
                {
                    collisionMap[index] = collisionTile;
                } else if (y == 0 || y == size -1)
                {
                    collisionMap[index] = collisionTile;
                } else
                {
                    float n = Mathf.PerlinNoise(x * scale, y * scale);
                    
                    tileMap[index] = (n > 0.21f) ? dirt : stone;
                }
            }
        }

        levelTilemap.SetTilesBlock(new BoundsInt(0, 0, 0, size, size, 1), tileMap);
        collisionTilemap.SetTilesBlock(new BoundsInt(0, 0, 0, size, size, 1), collisionMap);

        Debug.Log($"Generated {size}x{size} floor");
    }

    void Start()
    {
        GenerateLevel();
    }
}