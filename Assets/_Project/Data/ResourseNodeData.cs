using UnityEngine;

/// <summary>
/// Описание вида ресурса на карте (камень, дерево, кристальная жила...).
/// Это данные, а не объект: один такой файл используют все камни на карте.
/// Создаётся в меню: Create -> Colony -> Resource Node Data.
/// </summary>
[CreateAssetMenu(fileName = "NewResource", menuName = "Colony/Resource Node Data")]
public class ResourceNodeData : ScriptableObject
{
    [Header("Идентификация")]
    [Tooltip("Уникальный неизменный ключ (для сохранений), например stone")]
    public string id = "stone";
    public string displayName = "Камень";

    [Header("Что даёт")]
    public ResourceType yieldType = ResourceType.Stone;
    [Min(1)] public int yieldPerAction = 1;

    [Header("Кто может добывать")]
    public WorkSkill skill = WorkSkill.Mining;
    [Min(1)] public int requiredSkillLevel = 1;

    [Header("Скорость добычи")]
    [Tooltip("Секунд на одну порцию при скорости навыка 1.0")]
    [Min(0.1f)] public float baseWorkSeconds = 1.5f;
    [Tooltip("Быстрее этого времени добыть нельзя, как бы силён ни был поселенец (предел скорости добычи)")]
    [Min(0.05f)] public float minWorkSeconds = 0.5f;

    [Header("Запас (пул)")]
    [Min(1)] public int minPool = 4;
    [Min(1)] public int maxPool = 8;

    /// <summary>Сколько секунд займёт одна порция при данной скорости навыка.</summary>
    public float GetWorkSeconds(float skillSpeed)
    {
        return Mathf.Max(minWorkSeconds, baseWorkSeconds / Mathf.Max(0.01f, skillSpeed));
    }

    private void OnValidate()
    {
        maxPool = Mathf.Max(minPool, maxPool);
        minWorkSeconds = Mathf.Min(minWorkSeconds, baseWorkSeconds);
    }
}