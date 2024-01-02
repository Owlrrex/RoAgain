using System;
using System.Collections.Generic;

namespace Shared
{
    [Serializable]
    public class BattleEntity : GridEntity
    {
        public Stat MaxHp = new();
        public int CurrentHp; // Watchable? Not for packet sending, since we need distinction between HpUpdate and DamageTaken!
        public Stat MaxSp = new();
        public int CurrentSp; // Watchable? Not for packet sending, since we need distinction between HpUpdate and DamageTaken!
        //public Status Status;
        // Some form of reference to Skill-List

        private bool _isInAnimationCooldown = false;

        [NonSerialized]
        public ASkillExecution QueuedSkill = null;
        [NonSerialized]
        public List<ASkillExecution> CurrentlyExecutingSkills = new();
        [NonSerialized]
        public Dictionary<SkillId, TimerFloat> SkillCooldowns = new();

        public Action<BattleEntity, int, bool> TookDamage;
        public Action<BattleEntity, BattleEntity> Death;

        // This function should only have logic applicable to most skills generally
        public virtual SkillFailReason CanExecuteSkill(ASkillExecution skill)
        {
            if (IsDead())
                return SkillFailReason.Death;

            if (!CanAct())
                return SkillFailReason.CantAct;

            if (SkillCooldowns.ContainsKey(skill.SkillId)
                && !SkillCooldowns[skill.SkillId].IsFinished())
                return SkillFailReason.OnCooldown;

            if (IsCasting())
                return SkillFailReason.AlreadyCasting;

            if (CurrentSp < skill.SpCost)
                return SkillFailReason.NotEnoughSp;

            // Check for statuses like Silence, other general conditions like Ammo
            return skill.CanBeExecutedBy(this); // Otherwise, this function may contain special checks for unusual item requirements, etc
        }

        public override bool CanMove()
        {
            return base.CanMove() && !IsInAnimationCooldown() && !IsCasting() && !IsDead(); // TODO: More advanced conditions: Statuses, FreeCast, etc
        }

        // Don't reference CanMove() here since CanMove may include some statuses like Ankle Snare that root, but don't incapacitate
        public virtual bool CanAct()
        {
            return MovementCooldown <= 0 && !IsInAnimationCooldown() && !IsDead();
        }

        public void MarkAsDead(bool newValue)
        {

        }

        public bool IsDead()
        {
            return CurrentHp <= 0;
        }

        public bool IsInAnimationCooldown()
        {
            return _isInAnimationCooldown;
        }

        public bool IsCasting()
        {
            foreach (ASkillExecution skill in CurrentlyExecutingSkills)
            {
                if (skill.CastTime.MaxValue > 0 && skill.CastTime.RemainingValue > 0)
                    return true;
            }
            return false;
        }

        public virtual void UpdateSkills(float deltaTime)
        {
            _isInAnimationCooldown = false;
            for (int i = CurrentlyExecutingSkills.Count - 1; i >= 0; i--)
            {
                ASkillExecution skill = CurrentlyExecutingSkills[i];
                skill.CastTime.Update(deltaTime);
                skill.AnimationCooldown.Update(deltaTime);

                if (!skill.IsFinishedExecuting())
                {
                    _isInAnimationCooldown |= skill.AnimationCooldown.RemainingValue > 0;
                }
            }
        }

        // The animation cooldown for an auto-attack swing
        public virtual float GetDefaultAnimationCooldown()
        {
            // TODO: Factor in Agi, Gear, Status, etc
            return 1.0f;
        }
    }
}

