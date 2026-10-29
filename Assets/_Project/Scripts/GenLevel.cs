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
    [SerializeField] private float scale = 0.7f;

    void Start()
    {
        levelTilemap.ClearAllTiles();
        GenerateLevel();
        GenerateLevelCollisions();
    }

    public void GenerateLevel()
    {
        TileBase[] tileMap = new TileBase[(size) * (size)];

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                int index = x + y * size;
                float n = Mathf.PerlinNoise(x * scale, y * scale);
                tileMap[index] = (n > 0.21f) ? dirt : stone;
            }
        }

        levelTilemap.SetTilesBlock(new BoundsInt(-size / 2, -size / 2 , 0, size, size, 1), tileMap);
    }

    void GenerateLevelCollisions()
    {
        TileBase[] collisionMap = new TileBase[(size + 2) * (size + 2)];

        for (int x = 0; x < size + 2; x++)
        {
            for (int y = 0; y < size + 2; y++)
            {
                int index = x + y * (size + 2);
                if (y == 0 || y == size + 1)
                {
                    collisionMap[index] = collisionTile;
                } else if (x == 0 || x == size + 1)
                {
                    collisionMap[index] = collisionTile;
                }
            }
        }

        collisionTilemap.SetTilesBlock(new BoundsInt(-(size)/2 - 1, -(size)/2 - 1, 0, size + 2, size + 2, 1), collisionMap);
    }
}