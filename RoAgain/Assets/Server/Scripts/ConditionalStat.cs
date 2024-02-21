namespace Server
{
    public abstract class Condition
    {
        public abstract bool Evaluate(AttackParams attackParams);
        public abstract bool Evaluate(ServerSkillExecution skillExec);
        public abstract bool IsMergeable(Condition other);
    }

    public class ConditionalStat
    {
        public float Value;
        public Condition Condition;
    }

    public abstract class TargetRaceCondition : Condition
    {
        public EntityRace Race;

        public override bool Evaluate(AttackParams attackParams)
        {
            return Evaluate(attackParams.SourceSkillExec);
        }

        public override bool Evaluate(ServerSkillExecution skillExec)
        {
            return skillExec.EntityTargetTyped != null
                && skillExec.EntityTargetTyped.Race == Race;
        }

        public override bool IsMergeable(Condition other)
        {
            return other is TargetRaceCondition trc && trc.Race == Race;
        }
    }
}

