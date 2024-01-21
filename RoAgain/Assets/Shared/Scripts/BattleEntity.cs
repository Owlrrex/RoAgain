using System;
using System.Collections.Generic;

namespace Shared
{
    [Serializable]
    public class BattleEntity : GridEntity
    {
        static List<SkillId> _skillCooldownsToRemove_Reuse = new();

        public Stat MaxHp = new();
        public int CurrentHp; // Watchable? Not for packet sending, since we need distinction between HpUpdate and DamageTaken!
        public Stat MaxSp = new();
        public int CurrentSp; // Watchable? Not for packet sending, since we need distinction between HpUpdate and DamageTaken!
        //public Status Status;
        // Some form of reference to Skill-List

        private bool _isAnimationLocked = false;

        [NonSerialized]
        public ASkillExecution QueuedSkill = null;
        [NonSerialized]
        public List<ASkillExecution> CurrentlyResolvingSkills = new();
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
                return SkillFailReason.AnimationLocked;

            if (SkillCooldowns.ContainsKey(SkillId.ALL_EXCEPT_AUTO)
                && !SkillCooldowns[SkillId.ALL_EXCEPT_AUTO].IsFinished())
                return SkillFailReason.OnCooldown;
            else if (SkillCooldowns.ContainsKey(skill.SkillId)
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
            return base.CanMove() && !IsAnimationLocked() && !IsCasting() && !IsDead(); // TODO: More advanced conditions: Statuses, FreeCast, etc
        }

        // Don't reference CanMove() here since CanMove may include some statuses like Ankle Snare that root, but don't incapacitate
        public virtual bool CanAct()
        {
            return MovementCooldown <= 0 && !IsAnimationLocked() && !IsDead();
        }

        public void MarkAsDead(bool newValue)
        {

        }

        public bool IsDead()
        {
            return CurrentHp <= 0;
        }

        public bool IsAnimationLocked()
        {
            return _isAnimationLocked;
        }

        public bool IsCasting()
        {
            foreach (ASkillExecution skill in CurrentlyResolvingSkills)
            {
                if (skill.CastTime.MaxValue > 0 && skill.CastTime.RemainingValue > 0)
                    return true;
            }
            return false;
        }

        public virtual void UpdateSkills(float deltaTime)
        {
            _isAnimationLocked = false;
            for (int i = CurrentlyResolvingSkills.Count - 1; i >= 0; i--)
            {
                ASkillExecution skill = CurrentlyResolvingSkills[i];
                if(!skill.CastTime.IsFinished())
                {
                    skill.CastTime.Update(deltaTime);
                }

                if(skill.HasExecutionStarted)
                {
                    skill.AnimationCooldown.Update(deltaTime);
                }

                if (!skill.HasFinishedResolving())
                {
                    _isAnimationLocked |= skill.HasExecutionStarted && skill.AnimationCooldown.RemainingValue > 0;
                }
            }

            _skillCooldownsToRemove_Reuse.Clear();
            foreach (KeyValuePair<SkillId, TimerFloat> kvp in SkillCooldowns)
            {
                kvp.Value.Update(deltaTime);
                if (kvp.Value.IsFinished())
                    _skillCooldownsToRemove_Reuse.Add(kvp.Key);
            }

            foreach (SkillId skillId in _skillCooldownsToRemove_Reuse)
            {
                SkillCooldowns.Remove(skillId);
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

