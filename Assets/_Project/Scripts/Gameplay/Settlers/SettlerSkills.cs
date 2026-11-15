using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Формулы прокачки в одном месте: удобно подкручивать баланс.</summary>
public static class SkillProgression
{
    public const int StartLevel = 1;

    /// <summary>Сколько опыта нужно, чтобы с уровня level перейти на level + 1.</summary>
    public static float XpToNext(int level)
    {
        return 20f * level; // 20, 40, 60...
    }

    /// <summary>Множитель скорости работы от уровня: +10% за каждый уровень выше первого.</summary>
    public static float SpeedMultiplier(int level)
    {
        return 1f + 0.1f * (Mathf.Max(1, level) - 1);
    }
}

/// <summary>Один навык одного поселенца. Простой набор данных: его легко сохранять.</summary>
[Serializable]
public class SkillState
{
    public WorkSkill skill;
    public int level = SkillProgression.StartLevel;
    public float xp;
}

/// <summary>Навыки поселенца: какие есть, какого уровня, сколько опыта накоплено.</summary>
[Serializable]
public class SettlerSkills
{
    [SerializeField] private List<SkillState> states = new List<SkillState>();

    public IReadOnlyList<SkillState> All => states;

    /// <summary>
    /// Добавляет недостающие навыки расы с начальным уровнем 1.
    /// Уже существующие не трогает (кроме обрезки по потолку расы).
    /// Раса null: добавляются все навыки.
    /// </summary>
    public void EnsureFor(RaceData race)
    {
        foreach (WorkSkill skill in Enum.GetValues(typeof(WorkSkill)))
        {
            bool allowed = race == null || race.GetMaxLevel(skill) >= SkillProgression.StartLevel;
            SkillState s = Get(skill);

            if (!allowed)
            {
                if (s != null) states.Remove(s);
                continue;
            }

            if (s == null) states.Add(new SkillState { skill = skill });
            else if (race != null) s.level = Mathf.Clamp(s.level, SkillProgression.StartLevel, race.GetMaxLevel(skill));
        }
    }

    public SkillState Get(WorkSkill skill)
    {
        foreach (SkillState s in states)
        {
            if (s.skill == skill) return s;
        }
        return null;
    }

    /// <summary>Уровень навыка, 0 если поселенец этого не умеет.</summary>
    public int GetLevel(WorkSkill skill)
    {
        SkillState s = Get(skill);
        return s != null ? s.level : 0;
    }

    /// <summary>Хватает ли уровня навыка, чтобы добывать этот ресурс.</summary>
    public bool CanWork(ResourceNodeData resource)
    {
        return resource != null && GetLevel(resource.skill) >= resource.requiredSkillLevel;
    }

    /// <summary>Начисляет опыт. Возвращает true, если уровень вырос.</summary>
    public bool AddXp(WorkSkill skill, float amount, int maxLevel)
    {
        SkillState s = Get(skill);
        if (s == null || amount <= 0f || s.level >= maxLevel) return false;

        s.xp += amount;
        bool leveledUp = false;

        while (s.level < maxLevel && s.xp >= SkillProgression.XpToNext(s.level))
        {
            s.xp -= SkillProgression.XpToNext(s.level);
            s.level++;
            leveledUp = true;
        }

        if (s.level >= maxLevel) s.xp = 0f;
        return leveledUp;
    }
}