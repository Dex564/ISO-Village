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

    public static bool TrySpend(ResourceType t, int amount)
    {
        if (Get(t) < amount) return false;
        Set(t, Get(t) - amount);
        return true;
    }
}