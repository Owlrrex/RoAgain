using System;
using UnityEngine; // TODO: Remove Unity-dependencies

[Serializable]
public class Stat
{
    [SerializeField]
    private int _base;
    public int Base => _base;

    [SerializeField]
    private int _total;
    public int Total => _total;

    [SerializeField]
    private int _modAdd;
    public int ModifiersAdd => _modAdd;

    [SerializeField]
    private float _modMult;
    public float ModifiersMult => _modMult;

    public Action<Stat> ValueChanged;

    public void CopyTo(Stat other)
    {
        if (other == null)
            return;

        int oldTotal = _total;

        other._base = _base;
        other._modAdd = _modAdd;
        other._modMult = _modMult;
        other._total = _total;

        if(oldTotal != _total)
            ValueChanged?.Invoke(this);
    }

    public void Recalculate()
    {
        int oldTotal = _total;
        _total = (int)((_base + _modAdd) * (1 + _modMult));
        if (oldTotal != _total)
            ValueChanged?.Invoke(this);
    }

    public void SetBase(int value, bool recalculate = true)
    {
        if (value == _base)
            return;

        _base = value;
        if (recalculate)
            Recalculate();
    }

    public void ModifyBase(int change, bool recalculate = true)
    {
        if (change == 0)
            return;

        _base += change;
        if (recalculate)
            Recalculate();
    }

    public void ModifyAdd(int change, bool recalculate = true)
    {
        if (change == 0)
            return;

        _modAdd += change;
        if(recalculate)
            Recalculate();
    }

    public void ModifyMult(float change, bool recalculate = true)
    {
        if (change == 0)
            return;

        _modMult += change;
        if (recalculate)
            Recalculate();
    }
}

[Serializable]
public class StatFloat
{
    [SerializeField]
    private float _base;
    public float Base => _base;

    [SerializeField]
    private float _total;
    public float Total => _total;

    [SerializeField]
    private float _modAdd;
    public float ModifiersAdd => _modAdd;

    [SerializeField]
    private float _modMult;
    public float ModifiersMult => _modMult;

    [NonSerialized]
    public Action<StatFloat> ValueChanged;

    public void CopyTo(StatFloat other)
    {
        if (other == null)
            return;

        float oldTotal = _total;

        other._base = _base;
        other._modAdd = _modAdd;
        other._modMult = _modMult;
        other._total = _total;

        if (oldTotal != _total)
            ValueChanged?.Invoke(this);
    }

    public void Recalculate()
    {
        float oldTotal = _total;
        _total = (_base + _modAdd) * (1 + _modMult);
        if (oldTotal != _total)
            ValueChanged?.Invoke(this);
    }

    public void SetBase(float value, bool recalculate = true)
    {
        if (value == _base)
            return;

        _base = value;
        if (recalculate)
            Recalculate();
    }

    public void ModifyBase(float change, bool recalculate = true)
    {
        if (change == 0)
            return;

        _base += change;
        if (recalculate)
            Recalculate();
    }

    public void ModifyAdd(float change, bool recalculate = true)
    {
        if (change == 0)
            return;

        _modAdd += change;
        if (recalculate)
            Recalculate();
    }

    public void ModifyMult(float change, bool recalculate = true)
    {
        if (change == 0)
            return;

        _modMult += change;
        if (recalculate)
            Recalculate();
    }
}

// This could be useful for NetworkQueue or serialization
public enum EntityPropertyType
{
    Unknown,
    Str,
    Agi,
    Vit,
    Int,
    Dex,
    Luk,
    MaxHp,
    HpRegenAmount,
    HpRegenTime,
    MaxSp,
    SpRegenAmount,
    SpRegenTime,
    MeleeAtkMin,
    MeleeAtkMax,
    MeleeAtk_Mod_Add,
    MeleeAtk_Mod_Mult,
    RangedAtkMin,
    RangedAtkMax,
    RangedAtk_Mod_Add,
    RangedAtk_Mod_Mult,
    CurrentAtkMin,
    CurrentAtkMax,
    MatkMin,
    MatkMax,
    Matk_Mod_Add,
    Matk_Mod_Mult,
    AnimationSpeed,
    HardDef,
    SoftDef,
    HardMDef,
    SoftMDef,
    Crit,
    CritShield,
    Flee,
    PerfectFlee,
    Hit,
    ResistanceBleed,
    ResistanceBlind,
    ResistanceCurse,
    ResistanceFrozen,
    ResistancePoison,
    ResistanceSilence,
    ResistanceSleep,
    ResistanceStone,
    ResistanceStun,
    FlinchSpeed,
    BaseLvl,
    JobLvl,
    JobId,
    Gender,
    Range,
    Movespeed,
    WeightLimit,
    CastTime_Mod_Mult,
    Damage_Mod_Mult, // Should be used as little as possible in favour of Atk- & Matk-mods
    DamageReduction_Mod_Add,
    Crit_Mod_Add,
    SpCost_Mult, // TODO: Implement
}

public enum EntityRace
{
    Unknown,
    Angel,
    Animal,
    Humanoid,
    Demon,
    Dragon,
    Fish,
    Formless,
    Insect,
    Plant,
    // Player,
    Undead,
}

public enum EntitySize
{
    Unknown,
    Small,
    Medium,
    Large,
}

public enum EntityElement
{
    Unknown,
    Neutral1, Neutral2, Neutral3, Neutral4,
    Water1, Water2, Water3, Water4,
    Earth1, Earth2, Earth3, Earth4,
    Fire1, Fire2, Fire3, Fire4,
    Wind1, Wind2, Wind3, Wind4,
    Poison1, Poison2, Poison3, Poison4,
    Holy1, Holy2, Holy3, Holy4,
    Shadow1, Shadow2, Shadow3, Shadow4,
    Ghost1, Ghost2, Ghost3, Ghost4,
    Undead1, Undead2, Undead3, Undead4,
}