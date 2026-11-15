using System;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>Навыки работы. Новый вид работы = новая строка в этом списке.</summary>
public enum WorkSkill
{
    Mining,
    Woodcutting,
    Farming,
    Crafting
}

public static class WorkSkillNames
{
    /// <summary>Название навыка для интерфейса.</summary>
    public static string DisplayName(this WorkSkill skill)
    {
        switch (skill)
        {
            case WorkSkill.Mining: return "Добыча";
            case WorkSkill.Woodcutting: return "Лесорубство";
            case WorkSkill.Farming: return "Земледелие";
            case WorkSkill.Crafting: return "Ремесло";
            default: return skill.ToString();
        }
    }
}

/// <summary>Что делает особенность расы.</summary>
public enum RaceModifierType
{
    WorkSpeed,   // value = множитель скорости работы (1.2 = на 20% быстрее)
    BonusYield,  // value = во сколько раз больше добычи (2 = вдвое), chance = шанс срабатывания
    XpGain       // value = множитель получаемого опыта навыка
}

/// <summary>Одна особенность расы. Привязана к навыку.</summary>
[Serializable]
public struct RaceModifier
{
    [Tooltip("Текст для интерфейса, например: Горняк: 20% шанс удвоить добычу руды")]
    public string label;
    public RaceModifierType type;
    public WorkSkill skill;
    [Tooltip("WorkSpeed: множитель скорости. BonusYield: во сколько раз больше. XpGain: множитель опыта.")]
    public float value;
    [Range(0f, 1f)]
    [Tooltip("Только для BonusYield: шанс срабатывания за одну порцию добычи")]
    public float chance;
}

/// <summary>
/// Данные расы: скорость ходьбы, потолок уровня каждого навыка и особенности.
/// Создаётся в меню: Create -> Colony -> Race Data.
/// Навык, которого нет в списке или у которого максимальный уровень 0, раса освоить не может.
/// </summary>
[CreateAssetMenu(fileName = "NewRace", menuName = "Colony/Race Data")]
public class RaceData : ScriptableObject
{
    [Serializable]
    public struct SkillEntry
    {
        public WorkSkill skill;
        [FormerlySerializedAs("level")]
        [Min(0)] public int maxLevel;   // потолок: выше этого уровня поселенец этой расы не прокачается
    }

    public string raceName = "Человек";
    [Min(0.1f)] public float moveSpeed = 1.5f;
    public SkillEntry[] skills;
    public RaceModifier[] modifiers;

    /// <summary>Максимальный уровень навыка для расы (0 = не может освоить).</summary>
    public int GetMaxLevel(WorkSkill skill)
    {
        if (skills == null) return 0;

        foreach (SkillEntry e in skills)
        {
            if (e.skill == skill) return Mathf.Max(0, e.maxLevel);
        }
        return 0;
    }

    /// <summary>Произведение всех множителей скорости работы для навыка.</summary>
    public float GetWorkSpeedMultiplier(WorkSkill skill)
    {
        return ProductOf(RaceModifierType.WorkSpeed, skill);
    }

    /// <summary>Произведение всех множителей получаемого опыта для навыка.</summary>
    public float GetXpMultiplier(WorkSkill skill)
    {
        return ProductOf(RaceModifierType.XpGain, skill);
    }

    /// <summary>
    /// Бросает кубики для бонусов добычи. Возвращает 1 (обычная добыча)
    /// или произведение сработавших множителей (например, 2 для дварфа с удачей).
    /// </summary>
    public float RollYieldMultiplier(WorkSkill skill)
    {
        float result = 1f;
        if (modifiers == null) return result;

        foreach (RaceModifier m in modifiers)
        {
            if (m.type != RaceModifierType.BonusYield || m.skill != skill) continue;
            if (UnityEngine.Random.value < m.chance) result *= Mathf.Max(1f, m.value);
        }
        return result;
    }

    private float ProductOf(RaceModifierType type, WorkSkill skill)
    {
        float result = 1f;
        if (modifiers == null) return result;

        foreach (RaceModifier m in modifiers)
        {
            if (m.type == type && m.skill == skill && m.value > 0f) result *= m.value;
        }
        return result;
    }
}