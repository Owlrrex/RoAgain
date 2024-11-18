using OwlLogging;
using System;
using UnityEngine; // TODO: Remove Unity-dependencies

[Serializable]
public class Stat
{
    [SerializeField]
    private float _base;
    public float Base => _base;

    [NonSerialized]
    private float _total;
    public float Total => _total;

    [SerializeField]
    private float _modAdd;
    public float ModifiersAdd => _modAdd;

    [SerializeField]
    private float _modMult;
    public float ModifiersMult => _modMult;

    [NonSerialized]
    public Action<Stat> ValueChanged;

    public void CopyTo(Stat other)
    {
        if (other == null)
        {
            OwlLogger.LogError("Can't copy to null Stat!", GameComponent.Other);
            return;
        }

        other._base = _base;
        other._modAdd = _modAdd;
        other._modMult = _modMult;
        other.Recalculate();
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

    public void ModifyBoth(Stat change, bool recalculate = true)
    {
        if (change.ModifiersAdd == 0 && change.ModifiersMult == 0)
            return;

        _modAdd += change.ModifiersAdd;
        _modMult += change.ModifiersMult;

        if(recalculate)
            Recalculate();
    }

    public void ModifyBothNeg(Stat change, bool recalculate = true)
    {
        if (change.ModifiersAdd == 0 && change.ModifiersMult == 0)
            return;

        _modAdd -= change.ModifiersAdd;
        _modMult -= change.ModifiersMult;

        if (recalculate)
            Recalculate();
    }
}

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
    MeleeAtkBoth,
    RangedAtkMin,
    RangedAtkMax,
    RangedAtkBoth,
    CurrentAtkMin,
    CurrentAtkMax,
    CurrentAtkBoth,
    MatkMin,
    MatkMax,
    MatkBoth,
    AnimationSpeed,
    HardDef,
    SoftDef,
    HardMDef,
    SoftMDef,
    Crit,
    CritShield,
    CritDamage,
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
    CastTime,
    Cooldown,
    DamageDealt, // Should be used as little as possible in favour of multiplicative Atk- & Matk-mods. Also, additive part of this is not used
    DamageReceived, // Additive part of this is not used
    SpCost, // TODO: Implement
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