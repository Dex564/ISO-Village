using System;
using UnityEngine;

public static class ResourceService
{
    public static event Action OnChanged;

    public static int Get(ResourceType t) => SaveManager.Current.resources.TryGetValue(t, out var v) ? v : 0;

    public static void Set(ResourceType t, int value)
    {
        SaveManager.Current.resources[t] = Mathf.Max(0, value);
        OnChanged?.Invoke();
    }

    public static void Add(ResourceType t, int amount) => Set(t, Get(t) + amount);

    public static bool TrySpend(params ResourceCost[] costs)
    {
        foreach (var c in costs)
            if (Get(c.Type) < c.Amount) return false;

        foreach (var c in costs)
            SaveManager.Current.resources[c.Type] -= c.Amount;

        OnChanged?.Invoke();
        return true;
    }

    public readonly struct ResourceCost
    {
        public readonly ResourceType Type;
        public readonly int Amount;
        public ResourceCost(ResourceType type, int amount) { Type = type; Amount = amount; }
    }
}