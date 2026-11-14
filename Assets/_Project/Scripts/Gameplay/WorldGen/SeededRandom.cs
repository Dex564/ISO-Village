using UnityEngine;

/// <summary>
/// Случайные числа, которые зависят только от (seed, клетка, соль) и не зависят от порядка вызовов.
/// Пример: SeededRandom.Value01(seed, клетка, 101) всегда даёт одно и то же число для этой клетки.
/// Поэтому добавление нового вида ресурса не сдвигает уже существующие.
/// «Соль» это номер назначения числа (шанс появления, запас, сдвиг...), чтобы они не совпадали.
/// </summary>
public static class SeededRandom
{
    public static uint Hash(int seed, Vector2Int cell, int salt)
    {
        unchecked
        {
            uint h = (uint)seed;
            h = Mix(h ^ ((uint)cell.x * 0x9E3779B1u));
            h = Mix(h ^ ((uint)cell.y * 0x85EBCA77u));
            h = Mix(h ^ ((uint)salt * 0xC2B2AE3Du));
            return h;
        }
    }

    /// <summary>Число от 0 (включительно) до 1 (не включая).</summary>
    public static float Value01(int seed, Vector2Int cell, int salt)
    {
        return (Hash(seed, cell, salt) >> 8) / 16777216f;
    }

    /// <summary>Целое от min до max включительно.</summary>
    public static int Range(int seed, Vector2Int cell, int salt, int min, int max)
    {
        if (max <= min) return min;
        return min + (int)(Hash(seed, cell, salt) % (uint)(max - min + 1));
    }

    private static uint Mix(uint h)
    {
        unchecked
        {
            h ^= h >> 16;
            h *= 0x7FEB352Du;
            h ^= h >> 15;
            h *= 0x846CA68Bu;
            h ^= h >> 16;
            return h;
        }
    }
}