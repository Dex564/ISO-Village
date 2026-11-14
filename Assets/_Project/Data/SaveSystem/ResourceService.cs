using System;
using UnityEngine;

public static class ResourceService
{
    public static event Action OnChanged;

    public static int Get(ResourceType t)
    {
        foreach (var e in SaveManager.Current.resources)
            if (e.type == t) return e.amount;
        return 0;
    }

    public static void Set(ResourceType t, int value)
    {
        value = Mathf.Max(0, value);
        var list = SaveManager.Current.resources;
        var entry = list.Find(e => e.type == t);
        if (entry == null) list.Add(new ResourceEntry { type = t, amount = value });
        else entry.amount = value; //
        OnChanged?.Invoke();
    }

    public static void Add(ResourceType t, int amount) => Set(t, Get(t) + amount);

    public static bool TrySpend(params ResourceCost[] costs)
    {
        foreach (var c in costs)
            if (Get(c.Type) < c.Amount) return false;

        foreach (var c in costs)
            Set(c.Type, Get(c.Type) - c.Amount);

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