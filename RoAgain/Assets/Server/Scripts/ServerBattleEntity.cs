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
            // TODO: More parameters to constructor
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

        public virtual AttackWeaponType GetWeaponType()
        {
            return AttackWeaponType.Unarmed;
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
    }
}

