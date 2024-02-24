namespace Server
{
    public abstract class Condition
    {
        public virtual bool Evaluate(AttackParams attackParams)
        {
            return Evaluate(attackParams.SourceSkillExec);
        }
        public abstract bool Evaluate(ServerSkillExecution skillExec);
        public abstract bool IsMergeable(Condition other);
    }

    public class ConditionalStat
    {
        public float Value;
        public Condition Condition;
    }

    public class TargetRaceCondition : Condition
    {
        public EntityRace Race;

        public override bool Evaluate(ServerSkillExecution skillExec)
        {
            return skillExec.EntityTargetTyped?.Race == Race;
        }

        public override bool IsMergeable(Condition other)
        {
            return other is TargetRaceCondition trc && trc.Race == Race;
        }
    }

    public class UserWeaponTypeCondition : Condition
    {
        public AttackWeaponType WeaponType;

        public override bool Evaluate(ServerSkillExecution skillExec)
        {
            return skillExec.UserTyped?.GetWeaponType() == WeaponType;
        }

        public override bool IsMergeable(Condition other)
        {
            return other is UserWeaponTypeCondition uwtc && uwtc.WeaponType == WeaponType;
        }
    }

    public class BelowHpThresholdPercentCondition : Condition
    {
        public float Percentage;

        public override bool Evaluate(ServerSkillExecution skillExec)
        {
            return (skillExec.User.CurrentHp / (float)skillExec.User.MaxHp.Total) <= Percentage;
        }

        public override bool IsMergeable(Condition other)
        {
            return other is BelowHpThresholdPercentCondition otherTyped && otherTyped.Percentage == Percentage;
        }
    }
}

