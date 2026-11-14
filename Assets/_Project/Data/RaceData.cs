using System;
using UnityEngine;

/// <summary>Навыки работы. Новый вид работы = новая строка в этом списке.</summary>
public enum WorkSkill
{
    Mining,
    Woodcutting,
    Farming,
    Crafting
}

/// <summary>
/// Данные расы поселенцев: скорость ходьбы и что умеют делать.
/// Создаётся в меню: Create -> Colony -> Race Data.
/// Навык с уровнем 0 (или его отсутствие в списке) означает «не умеет».
/// </summary>
[CreateAssetMenu(fileName = "NewRace", menuName = "Colony/Race Data")]
public class RaceData : ScriptableObject
{
    [Serializable]
    public struct SkillEntry
    {
        public WorkSkill skill;
        [Min(0)] public int level;                // 0 = не умеет
        [Tooltip("Множитель скорости работы: 2 = в два раза быстрее")]
        [Min(0.1f)] public float speed;
    }

    public string raceName = "Человек";
    [Min(0.1f)] public float moveSpeed = 1.5f;
    public SkillEntry[] skills;

    public bool CanWork(WorkSkill skill, int requiredLevel)
    {
        if (skills == null) return false;

        foreach (SkillEntry e in skills)
        {
            if (e.skill == skill) return e.level >= requiredLevel && e.level > 0;
        }
        return false;
    }

    public float GetSpeed(WorkSkill skill)
    {
        if (skills == null) return 1f;

        foreach (SkillEntry e in skills)
        {
            if (e.skill == skill) return e.speed > 0f ? e.speed : 1f;
        }
        return 1f;
    }
}