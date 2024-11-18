using Shared;
using UnityEngine;

namespace Server
{
    public interface ICondition
    {
        public virtual bool Evaluate(AttackParams attackParams)
        {
            return Evaluate(attackParams.SourceSkillExec);
        }
        public bool Evaluate(ServerSkillExecution skillExec);
        public string Serialize();
        public bool IsMergeable(ACondition other);
        public bool ReadParams(string[] parts);
    }

    public class TargetRaceCondition : ATargetRaceCondition, ICondition
    {
        public TargetRaceCondition()
        {
            _bec = new RaceBEC();
        }

        public bool Evaluate(ServerSkillExecution skillExec)
        {
            return ((IBattleEntityCriterium)_bec).Evaluate(skillExec.EntityTargetTyped);
        }
    }

    public class UserAllSlotsAreTypesCondition : AUserAllSlotsAreTypesCondition, ICondition
    {
        public UserAllSlotsAreTypesCondition()
        {
            _bec = new EquipSlotsAreAllTypesBEC();
        }

        public bool Evaluate(ServerSkillExecution skillExec)
        {
            return ((IBattleEntityCriterium)_bec).Evaluate(skillExec.UserTyped);
        }
    }

    public class BelowHpThresholdPercentCondition : ABelowHpThresholdPercentCondition, ICondition
    {
        public BelowHpThresholdPercentCondition()
        {
            _bec = new BelowHpThresholdPercentBEC();
        }

        public bool Evaluate(ServerSkillExecution skillExec)
        {
            return ((IBattleEntityCriterium)_bec).Evaluate(skillExec.UserTyped);
        }
    }

    public class SkillIdCondition : ASkillIdCondition, ICondition
    {
        public bool Evaluate(ServerSkillExecution skillExec)
        {
            return skillExec.SkillId == SkillId;
        }
    }

    public class AttackWeaponTypeCondition : AAttackWeaponTypeCondition, ICondition
    {
        public AttackWeaponTypeCondition()
        {
            _bec = new DefaultWeaponTypeBEC();
        }

        public virtual bool Evaluate(AttackParams attackParams)
        {
            return attackParams.AttackWeaponType == WeaponType && attackParams.IsTwoHanded == IsTwoHanded;
        }

        public bool Evaluate(ServerSkillExecution skillExec)
        {
            return ((IBattleEntityCriterium)_bec).Evaluate(skillExec.UserTyped);
        }
    }

    public static class ConditionalStatHelpers
    {
        public static ACondition ConditionIdResolver(int id)
        {
            return id switch
            {
                1 => new TargetRaceCondition(),
                2 => new UserAllSlotsAreTypesCondition(),
                3 => new BelowHpThresholdPercentCondition(),
                4 => new SkillIdCondition(),
                5 => new AttackWeaponTypeCondition(),
                _ => null
            };
        }
    }
}

