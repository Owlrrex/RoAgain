using OwlLogging;
using Shared;
using System;

namespace Client
{
    public class TargetRaceCondition : ATargetRaceCondition, ICondition
    {
        public TargetRaceCondition()
        {
            _bec = new RaceBEC();
        }

        public string ToDescription()
        {
            throw new NotImplementedException();
        }
    }

    public class UserAllSlotsAreTypesCondition : AUserAllSlotsAreTypesCondition, ICondition
    {
        public UserAllSlotsAreTypesCondition()
        {
            _bec = new EquipSlotsAreAllTypesBEC();
        }

        public string ToDescription()
        {
            throw new NotImplementedException();
        }
    }

    public class BelowHpThresholdPercentCondition : ABelowHpThresholdPercentCondition, ICondition
    {
        public BelowHpThresholdPercentCondition()
        {
            _bec = new BelowHpThresholdPercentBEC();
        }

        public string ToDescription()
        {
            throw new NotImplementedException();
        }
    }

    public class SkillIdCondition : ASkillIdCondition, ICondition
    {
        public string ToDescription()
        {
            throw new NotImplementedException();
        }
    }

    public class AttackWeaponTypeCondition : AAttackWeaponTypeCondition, ICondition
    {
        public AttackWeaponTypeCondition()
        {
            _bec = new DefaultWeaponTypeBEC();
        }

        public string ToDescription()
        {
            throw new NotImplementedException();
        }
    }

    public interface ICondition
    {
        public int Id { get; }
        public bool ReadParams(string[] parts);
        public bool IsMergeable(ACondition other);

        /// <returns>A formatted string to use in UI that human-readably expresses this Condition</returns>
        public string ToDescription();
    }
}
