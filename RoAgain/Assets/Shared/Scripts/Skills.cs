using System.Collections.Generic;
using UnityEngine;


namespace Shared
{
    public enum SkillFailReason
    {
        None,
        NotEnoughSp,
        CantAct,
        AlreadyCasting,
        NotLearned,
        OutOfRange,
        OnCooldown,
        Death
    }

    public static class Skills
    {
        public static bool IsGroundSkill(SkillId skillId)
        {
            return skillId switch
            {
                SkillId.PlaceWarp => true,
                _ => false,
            };
        }
    }

    // This class represents a single use of a given skill.
    public abstract class ASkillExecution
    {
        public abstract SkillId SkillId { get; }
        public int SkillLvl;
        public BattleEntity User;

        // These are the values modified by the user's stats & any other circumstance
        public TimerFloat CastTime = new();
        public bool CanInterruptCast = true;
        public TimerFloat AnimationCooldown = new();
        // Field will be needed to deduct SP correctly
        // Using field for the "canExecute" check is uncertain - could check in this class' CanBeExecutedBy()
        // or the battleModule could check all Costs in bulk
        public int SpCost; // TODO: Create Type for other types of cost: HP, Items

        public int Range;

        // Not sure if this is ideal
        public bool HasExecuted { get; private set; }

        protected int Initialize(int skillLvl, BattleEntity user, int spCost, int range, float castTime, float animCd)
        {
            // TODO: Validate inputs

            SkillLvl = skillLvl;
            User = user;
            SpCost = spCost;
            Range = range;
            CastTime.Initialize(castTime);
            AnimationCooldown.Initialize(animCd);
            return 0;
        }

        // This function should only contain skill-specific logic, like FreeCast, movement-skills being blocked by conditions, etc.
        public virtual SkillFailReason CanBeExecutedBy(BattleEntity entity)
        {
            return SkillFailReason.None;
        }

        public virtual SkillFailReason CanTarget()
        {
            return SkillFailReason.NotLearned;
        }

        // Which skills will go on a cooldown other than AnimationDelay
        public virtual Dictionary<SkillId, float> GetSkillCoolDowns()
        {
            return null;
        }

        public virtual bool IsFinishedExecuting()
        {
            return CastTime.IsFinished() && AnimationCooldown.IsFinished();
        }

        // Not called for skills with 0 cast time
        public virtual void OnCastStart() { }
        public virtual void OnCastEnd(bool wasInterrupted) { }

        // Called when CastTime (if any) is finished, contains main skill effect, animationdelay is about to start
        public virtual void OnExecute() { HasExecuted = true; }
        // Called when CastTime & AnimationDelay of this skill are over, execution is completely complete, skill is about to be removed from entity. Will be called for interrupted skills as well!
        public virtual void OnCompleted() { }
    }

    public abstract class AGroundSkillExecution : ASkillExecution
    {
        public Vector2Int Target;

        protected int Initialize(int skillLvl, BattleEntity user, int spCost, int range, float castTime, float animCd, Vector2Int targetCoords)
        {
            Target = targetCoords;
            return Initialize(skillLvl, user, spCost, range, castTime, animCd);
        }

        public override SkillFailReason CanTarget()
        {
            // TODO: Sightline-check?
            if (Extensions.GridDistanceSquare(User.Coordinates, Target) > Range)
                return SkillFailReason.OutOfRange;
            return SkillFailReason.None;
        }
    }

    public abstract class AEntitySkillExecution : ASkillExecution
    {
        public BattleEntity Target;

        protected int Initialize(int skillLvl, BattleEntity user, int spCost, int range, float castTime, float animCd, BattleEntity target)
        {
            Target = target;
            return Initialize(skillLvl, user, spCost, range, castTime, animCd);
        }

        public override SkillFailReason CanTarget()
        {
            if (Target.IsDead())
                return SkillFailReason.Death;

            if (Target.MapId != User.MapId
                || Extensions.GridDistanceSquare(User.Coordinates, Target.Coordinates) > Range)
                return SkillFailReason.OutOfRange;

            return SkillFailReason.None;
        }
    }
}
