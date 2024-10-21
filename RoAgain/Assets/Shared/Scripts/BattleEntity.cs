using System;
using System.Collections.Generic;

namespace Shared
{
    [Serializable]
    public class BattleEntity : GridEntity
    {
        static List<SkillId> _skillCooldownsToRemove_Reuse = new();

        public Stat MaxHp = new();
        public float CurrentHp; // Watchable? Not for packet sending, since we need distinction between HpUpdate and DamageTaken!
        public Stat MaxSp = new();
        public float CurrentSp; // Watchable? Not for packet sending, since we need distinction between HpUpdate and DamageTaken!
        //public Status Status;
        // Some form of reference to Skill-List

        private bool _isAnimationLocked = false;

        //[NonSerialized]
        //public ASkillExecution QueuedSkill = null;
        [NonSerialized]
        public List<ASkillExecution> CurrentlyResolvingSkills = new();
        [NonSerialized]
        public Dictionary<SkillId, TimerFloat> SkillCooldowns = new();

        public Action<BattleEntity, int, bool, bool, int> TookDamage;
        public Action<BattleEntity, BattleEntity> Death;

        public BattleEntity(Coordinate coordinates, LocalizedStringId locNameId, int modelId, float movespeed, float maxHp, float maxSp, int id = -1) : base(coordinates, locNameId, modelId, movespeed, id)
        {
            MaxHp.SetBase(maxHp);
            MaxSp.SetBase(maxSp);
        }

        public override bool CanMove()
        {
            return base.CanMove() && !IsAnimationLocked() && !IsCasting() && !IsDead(); // TODO: More advanced conditions: Statuses, FreeCast, etc
        }

        // Don't reference CanMove() here since CanMove may include some statuses like Ankle Snare that root, but don't incapacitate
        public virtual bool CanAct()
        {
            return !IsAnimationLocked() && !IsDead() && !IsCasting(); // Should Casting be considered here?
        }

        public void MarkAsDead(bool newValue)
        {

        }

        public bool IsDead()
        {
            return CurrentHp <= 0;
        }

        public override bool IsAnimationLocked()
        {
            return base.IsAnimationLocked() || _isAnimationLocked;
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
            for (int i = CurrentlyResolvingSkills.Count - 1; i >= 0; i--)
            {
                ASkillExecution skill = CurrentlyResolvingSkills[i];
                if(!skill.CastTime.IsFinished())
                {
                    skill.CastTime.Update(deltaTime);
                }

                if (skill.HasExecutionStarted)
                {
                    skill.AnimationCooldown.Update(deltaTime);
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

        public void UpdateAnimationLockedState()
        {
            _isAnimationLocked = false;
            foreach(ASkillExecution skillExec in CurrentlyResolvingSkills)
            {
                _isAnimationLocked |= skillExec.HasExecutionStarted && !skillExec.AnimationCooldown.IsFinished();
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

