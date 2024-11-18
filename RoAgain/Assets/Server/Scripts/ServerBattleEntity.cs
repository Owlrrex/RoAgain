using OwlLogging;
using Shared;
using System;
using System.Collections.Generic;

namespace Server
{
    public class ServerBattleEntity : BattleEntity
    {
        public WatchableProperty<int, EntityPropertyType> BaseLvl = new(EntityPropertyType.BaseLvl);

        public EntityRace Race; // watchable?
        public EntitySize Size; // watchable?
        public EntityElement Element; // watchable?

        // Maybe a Stat instead, to accomodate Bow equips & other increases? Depends on Calculation-function
        public WatchableProperty<int, EntityPropertyType> BaseAttackRange = new(EntityPropertyType.Range);

        public readonly Stat Str = new();
        public readonly Stat Agi = new();
        public readonly Stat Vit = new();
        public readonly Stat Int = new();
        public readonly Stat Dex = new();
        public readonly Stat Luk = new();

        public readonly Stat HpRegenAmount = new();
        public float HpRegenTime; // not watchable since usually doesn't change
        public float HpRegenCounter;

        public readonly Stat SpRegenAmount = new();
        public float SpRegenTime; // not watchable since usually doesn't change
        public float SpRegenCounter;

        public readonly Stat MeleeAtkMin = new();
        public readonly Stat MeleeAtkMax = new();
        public readonly Stat RangedAtkMin = new();
        public readonly Stat RangedAtkMax = new();
        public Stat CurrentAtkMin => IsRanged() ? RangedAtkMin : MeleeAtkMin;
        public Stat CurrentAtkMax => IsRanged() ? RangedAtkMax : MeleeAtkMax;
        public readonly Stat MatkMin = new();
        public readonly Stat MatkMax = new();
        public readonly Stat AnimationSpeed = new();
        public readonly Stat HardDef = new();
        public readonly Stat SoftDef = new();
        public readonly Stat HardMDef = new();
        public readonly Stat SoftMDef = new();
        public readonly Stat Crit = new();
        public readonly Stat CritShield = new();
        public readonly Stat PerfectFlee = new();
        public readonly Stat Flee = new();
        public readonly Stat Hit = new();

        public readonly Stat ResistanceBleed = new();
        public readonly Stat ResistanceBlind = new();
        public readonly Stat ResistanceCurse = new();
        public readonly Stat ResistanceFrozen = new();
        public readonly Stat ResistancePoison = new();
        public readonly Stat ResistanceSilence = new();
        public readonly Stat ResistanceSleep = new();
        public readonly Stat ResistanceStone = new();
        public readonly Stat ResistanceStun = new();

        public WatchableProperty<float, EntityPropertyType> FlinchSpeed = new(EntityPropertyType.FlinchSpeed);

        public Dictionary<int, float> BattleContributions = new();

        public Action<ServerBattleEntity, float> Update;

        public ServerBattleEntity(Coordinate coordinates, LocalizedStringId locNameId, int modelId, float movespeed, int maxHp, int maxSp,
            int id = -1) : base(coordinates, locNameId, modelId, movespeed, maxHp, maxSp, id)
        {
            
        }

        // TODO: Skill List? Only if I want to have a skill List that an AI can dynamically interact with
        // original AI just "uses" skills, without a Mob-held skill list

        public override Packet ToDataPacket()
        {
            return new BattleEntityDataPacket()
            {
                EntityId = Id,
                LocalizedNameId = LocalizedNameId,
                NameOverride = NameOverride,
                MapId = MapId,
                Path = Path,
                PathCellIndex = PathCellIndex,
                Movespeed = Movespeed.Value,
                MovementCooldown = MovementCooldown,
                Orientation = Orientation,
                Coordinates = Coordinates,
                ModelId = ModelId,

                MaxHp = MaxHp.Total,
                Hp = CurrentHp,
                MaxSp = MaxSp.Total,
                Sp = CurrentSp
            };
        }

        public virtual bool IsRanged()
        {
            return false;
        }

        public virtual EquipmentType GetDefaultWeaponType(out EquipmentSlot usedSlot, out bool isTwoHanded)
        {
            usedSlot = EquipmentSlot.TwoHand;
            isTwoHanded = true;
            return EquipmentType.Unarmed;
        }

        public virtual EntityElement GetDefensiveElement()
        {
            return EntityElement.Neutral1;
        }

        public virtual EntityElement GetOffensiveElement()
        {
            return EntityElement.Neutral1;
        }

        public MapInstance GetMapInstance()
        {
            if (AServer.Instance == null)
                return null;

            return AServer.Instance.MapModule?.GetMapInstance(MapId);
        }

        public virtual bool AddStat(EntityPropertyType type, Stat change)
        {
            switch (type)
            {
                case EntityPropertyType.Str:
                    Str.ModifyBoth(change);
                    break;
                case EntityPropertyType.Agi:
                    Agi.ModifyBoth(change);
                    break;
                case EntityPropertyType.Vit:
                    Vit.ModifyBoth(change);
                    break;
                case EntityPropertyType.Int:
                    Int.ModifyBoth(change);
                    break;
                case EntityPropertyType.Dex:
                    Dex.ModifyBoth(change);
                    break;
                case EntityPropertyType.Luk:
                    Luk.ModifyBoth(change);
                    break;
                case EntityPropertyType.MaxHp:
                    MaxHp.ModifyBoth(change);
                    break;
                case EntityPropertyType.HpRegenAmount:
                    HpRegenAmount.ModifyBoth(change);
                    break;
                case EntityPropertyType.HpRegenTime:
                    if (change.ModifiersAdd != 0)
                        OwlLogger.LogError("Modifying HpRegenTime additively is not supported!", GameComponent.Battle);
                    HpRegenTime *= change.ModifiersMult;
                    break;
                case EntityPropertyType.MaxSp:
                    MaxSp.ModifyBoth(change);
                    break;
                case EntityPropertyType.SpRegenAmount:
                    SpRegenAmount.ModifyBoth(change);
                    break;
                case EntityPropertyType.SpRegenTime:
                    if (change.ModifiersAdd != 0)
                        OwlLogger.LogError("Modifying SpRegenTime additively is not supported!", GameComponent.Battle);
                    SpRegenTime *= change.ModifiersMult;
                    break;
                case EntityPropertyType.MeleeAtkMin:
                    MeleeAtkMin.ModifyBoth(change);
                    break;
                case EntityPropertyType.MeleeAtkMax:
                    MeleeAtkMax.ModifyBoth(change);
                    break;
                case EntityPropertyType.MeleeAtkBoth:
                    MeleeAtkMin.ModifyBoth(change);
                    MeleeAtkMax.ModifyBoth(change);
                    break;
                case EntityPropertyType.RangedAtkMin:
                    RangedAtkMin.ModifyBoth(change);
                    break;
                case EntityPropertyType.RangedAtkMax:
                    RangedAtkMax.ModifyBoth(change);
                    break;
                case EntityPropertyType.RangedAtkBoth:
                    RangedAtkMin.ModifyBoth(change);
                    RangedAtkMax.ModifyBoth(change);
                    break;
                case EntityPropertyType.CurrentAtkMin:
                    MeleeAtkMin.ModifyBoth(change);
                    RangedAtkMin.ModifyBoth(change);
                    break;
                case EntityPropertyType.CurrentAtkMax:
                    MeleeAtkMax.ModifyBoth(change);
                    RangedAtkMax.ModifyBoth(change);
                    break;
                case EntityPropertyType.CurrentAtkBoth:
                    MeleeAtkMin.ModifyBoth(change);
                    RangedAtkMin.ModifyBoth(change);
                    MeleeAtkMax.ModifyBoth(change);
                    RangedAtkMax.ModifyBoth(change);
                    break;
                case EntityPropertyType.MatkMin:
                    MatkMin.ModifyBoth(change);
                    break;
                case EntityPropertyType.MatkMax:
                    MatkMax.ModifyBoth(change);
                    break;
                case EntityPropertyType.MatkBoth:
                    MatkMin.ModifyBoth(change);
                    MatkMax.ModifyBoth(change);
                    break;
                case EntityPropertyType.AnimationSpeed:
                    AnimationSpeed.ModifyBoth(change);
                    break;
                case EntityPropertyType.HardDef:
                    HardDef.ModifyBoth(change);
                    break;
                case EntityPropertyType.SoftDef:
                    SoftDef.ModifyBoth(change);
                    break;
                case EntityPropertyType.HardMDef:
                    HardMDef.ModifyBoth(change);
                    break;
                case EntityPropertyType.SoftMDef:
                    SoftMDef.ModifyBoth(change);
                    break;
                case EntityPropertyType.Crit:
                    Crit.ModifyBoth(change);
                    break;
                case EntityPropertyType.CritShield:
                    CritShield.ModifyBoth(change);
                    break;
                case EntityPropertyType.Flee:
                    Flee.ModifyBoth(change);
                    break;
                case EntityPropertyType.PerfectFlee:
                    PerfectFlee.ModifyBoth(change);
                    break;
                case EntityPropertyType.Hit:
                    Hit.ModifyBoth(change);
                    break;
                case EntityPropertyType.ResistanceBleed:
                    ResistanceBleed.ModifyBoth(change);
                    break;
                case EntityPropertyType.ResistanceBlind:
                    ResistanceBlind.ModifyBoth(change);
                    break;
                case EntityPropertyType.ResistanceCurse:
                    ResistanceCurse.ModifyBoth(change);
                    break;
                case EntityPropertyType.ResistanceFrozen:
                    ResistanceFrozen.ModifyBoth(change);
                    break;
                case EntityPropertyType.ResistancePoison:
                    ResistancePoison.ModifyBoth(change);
                    break;
                case EntityPropertyType.ResistanceSilence:
                    ResistanceSilence.ModifyBoth(change);
                    break;
                case EntityPropertyType.ResistanceSleep:
                    ResistanceSleep.ModifyBoth(change);
                    break;
                case EntityPropertyType.ResistanceStone:
                    ResistanceStone.ModifyBoth(change);
                    break;
                case EntityPropertyType.ResistanceStun:
                    ResistanceStun.ModifyBoth(change);
                    break;
                case EntityPropertyType.FlinchSpeed:
                    if (change.ModifiersAdd != 0)
                        OwlLogger.LogError("Modifying FlinchSpeed additively is not supported!", GameComponent.Battle);
                    FlinchSpeed.Value *= change.ModifiersMult;
                    break;
                case EntityPropertyType.Range:
                    // TODO
                    break;
                case EntityPropertyType.Movespeed:
                    if (change.ModifiersAdd != 0)
                        OwlLogger.LogError("Modifying Movespeed additively is not supported!", GameComponent.Battle);
                    Movespeed.Value *= change.ModifiersMult;
                    break;
                default:
                    return false;
            }
            return true;
        }

        public virtual bool RemoveStat(EntityPropertyType type, Stat change)
        {
            // TODO: profile if this allocates too much
            Stat negative = new();
            negative.ModifyAdd(-change.ModifiersAdd, false);
            negative.ModifyMult(-change.ModifiersMult);
            return AddStat(type, negative);
        }
    }
}

