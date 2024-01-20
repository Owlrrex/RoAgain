using OwlLogging;
using System.Collections.Generic;
using UnityEngine;


namespace Shared
{
    public enum SkillFailReason
    {
        None,
        NotEnoughSp,
        AnimationLocked,
        AlreadyCasting,
        NotLearned,
        OutOfRange,
        OnCooldown,
        Death,
        TargetInvalid
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

    public struct SkillTarget
    {
        public Vector2Int GroundTarget { get; private set; }
        public BattleEntity EntityTarget { get; private set; }

        public SkillTarget(Vector2Int groundTarget)
        {
            GroundTarget = groundTarget;
            EntityTarget = null;
        }

        public SkillTarget(BattleEntity entityTarget)
        {
            GroundTarget = GridData.INVALID_COORDS;
            EntityTarget = entityTarget;
        }

        public void SetGroundTarget(Vector2Int groundTarget)
        {
            if(EntityTarget != null)
            {
                OwlLogger.LogError($"GroundTarget is being set when EntityTarget is already set!", GameComponent.Skill);
            }
            GroundTarget = groundTarget;
        }

        public void SetEntityTarget(BattleEntity entityTarget)
        {
            if(GroundTarget != GridData.INVALID_COORDS)
            {
                OwlLogger.LogError($"EntityTarget is being set when GroundTarget is already set!", GameComponent.Skill);
            }
            EntityTarget = entityTarget;
        }

        public bool IsSet()
        {
            return IsGroundTarget() || IsEntityTarget();
        }

        public bool IsValid()
        {
            return IsGroundTarget() ^ IsEntityTarget();
        }

        public bool IsGroundTarget()
        {
            return GroundTarget != GridData.INVALID_COORDS;
        }

        public bool IsEntityTarget()
        {
            return EntityTarget != null;
        }

        public Vector2Int GetTargetCoordinates()
        {
            if (IsEntityTarget())
                return EntityTarget.Coordinates;
            else
                return GroundTarget;
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
        public bool CanBeInterrupted = true;
        public TimerFloat AnimationCooldown = new();
        // Field will be needed to deduct SP correctly
        // Using field for the "canExecute" check is uncertain - could check in this class' CanBeExecutedBy()
        // or the battleModule could check all Costs in bulk
        public int SpCost; // TODO: Create Type for other types of cost: HP, Items

        public int Range;

        public SkillTarget Target;

        // Not sure if this is ideal
        public bool HasExecutionStarted { get; private set; }

        protected int Initialize(int skillLvl, BattleEntity user, int spCost, int range, float castTime, float animCd, SkillTarget target)
        {
            if(skillLvl <= 0)
            {
                OwlLogger.LogError($"Can't create SkillExecution with skillLvl {skillLvl}", GameComponent.Skill);
                return -1;
            }

            if(user == null)
            {
                OwlLogger.LogError("Can't create SkillExecution with null user!", GameComponent.Skill);
                return -1;
            }

            // allow negative SP cost for sp healing skills

            if(range < 0)
            {
                OwlLogger.LogError($"Can't create SkillExecution with range {range}", GameComponent.Skill);
                return -1;
            }

            if(castTime < 0)
            {
                OwlLogger.LogError($"Can't create SkillExecution with castTime {castTime}", GameComponent.Skill);
                return -1;
            }

            if(animCd < 0)
            {
                OwlLogger.LogError($"Can't create SkillExecution with animCd {animCd}", GameComponent.Skill);
                return -1;
            }

            if(!target.IsSet())
            {
                OwlLogger.LogError("Can't create SkillExecution with unset target!", GameComponent.Skill);
                return -1;
            }

            SkillLvl = skillLvl;
            User = user;
            SpCost = spCost;
            Range = range;
            CastTime.Initialize(castTime);
            AnimationCooldown.Initialize(animCd);
            Target = target;
            return 0;
        }

        // This function should only contain skill-specific logic, like FreeCast, movement-skills being blocked by conditions, etc.
        public virtual SkillFailReason CanBeExecutedBy(BattleEntity entity)
        {
            return SkillFailReason.None;
        }

        public virtual SkillFailReason CheckTarget()
        {
            if(!Target.IsValid())
                return SkillFailReason.TargetInvalid;
            
            if(Target.IsGroundTarget())
            {
                if (Extensions.GridDistanceSquare(User.Coordinates, Target.GroundTarget) > Range)
                    return SkillFailReason.OutOfRange;
            }
            else
            {
                if (Target.EntityTarget.IsDead())
                    return SkillFailReason.Death;

                if (Target.EntityTarget.MapId != User.MapId
                    || Extensions.GridDistanceSquare(User.Coordinates, Target.EntityTarget.Coordinates) > Range)
                    return SkillFailReason.OutOfRange;
            }

            return SkillFailReason.None;
        }

        // Which skills will go on a cooldown other than AnimationDelay
        public virtual Dictionary<SkillId, float> GetSkillCoolDowns()
        {
            return null;
        }

        public virtual bool HasFinishedResolving()
        {
            return CastTime.IsFinished() && AnimationCooldown.IsFinished();
        }

        public virtual bool IsExecutionFinished()
        {
            return HasExecutionStarted;
        }

        // Does/did this execution require a cast time, no matter its progress state?
        public bool HasCastTime()
        {
            return CastTime.MaxValue == 0;
        }

        // Not called for skills with 0 cast time
        public virtual void OnCastStart() { }
        public virtual void OnCastEnd(bool wasInterrupted) { }

        // Called when CastTime (if any) & animation delay is about to start
        // contains main skill effect
        // Will be called repeatedly for skills that don't have 
        public virtual void OnExecute() { HasExecutionStarted = true; }
        // Called when CastTime & AnimationDelay of this skill are over, execution is completely complete, skill is about to be removed from entity. Will be called for interrupted skills as well!
        public virtual void OnCompleted(bool wasSuccessful) { }
    }
}
