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
        public readonly StatFloat HardDef = new();
        public readonly Stat SoftDef = new();
        public readonly StatFloat HardMDef = new();
        public readonly Stat SoftMDef = new();
        public readonly StatFloat Crit = new();
        public readonly StatFloat CritShield = new();
        public readonly StatFloat PerfectFlee = new();
        public readonly Stat Flee = new();
        public readonly Stat Hit = new();

        public readonly StatFloat ResistanceBleed = new();
        public readonly StatFloat ResistanceBlind = new();
        public readonly StatFloat ResistanceCurse = new();
        public readonly StatFloat ResistanceFrozen = new();
        public readonly StatFloat ResistancePoison = new();
        public readonly StatFloat ResistanceSilence = new();
        public readonly StatFloat ResistanceSleep = new();
        public readonly StatFloat ResistanceStone = new();
        public readonly StatFloat ResistanceStun = new();

        public WatchableProperty<float, EntityPropertyType> FlinchSpeed = new(EntityPropertyType.FlinchSpeed);

        public Dictionary<int, int> BattleContributions = new();

        public Action<ServerBattleEntity, float> Update;

        // TODO: Skill List? Only if I want to have a skill List that an AI can dynamically interact with
        // original AI just "uses" skills, without a Mob-held skill list

        public new BattleEntityDataPacket ToDataPacket()
        {
            return new BattleEntityDataPacket()
            {
                UnitId = Id,
                UnitName = Name,
                MapId = MapId,
                Path = Path,
                PathCellIndex = PathCellIndex,
                Movespeed = Movespeed.Value,
                MovementCooldown = MovementCooldown,
                Orientation = Orientation,
                Coordinates = Coordinates,

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

        public ServerMapInstance GetMapInstance()
        {
            if (ServerMain.Instance == null)
                return null;

            return ServerMain.Instance.Server?.MapModule?.GetMapInstance(MapId);
        }
    }
}

