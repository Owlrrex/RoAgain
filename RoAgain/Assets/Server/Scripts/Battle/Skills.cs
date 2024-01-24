using OwlLogging;
using Shared;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Server
{
    public class ServerSkillExecution : ASkillExecution, IAutoInitPoolObject
    {
        private SkillId _skillId;
        public override SkillId SkillId => _skillId;
        public ServerMapInstance Map;
        //public new ServerBattleEntity User => base.User as ServerBattleEntity;
        public int Var1, Var2, Var3, Var4, Var5;
        public object[] runtimeVar = null;

        public int InitializeFromStatic(SkillId skillId, int skillLvl, ServerBattleEntity user, SkillTarget target, ServerMapInstance map)
        {
            if(skillId == SkillId.Unknown)
            {
                OwlLogger.LogError($"Can't create SkillExecution for Unknown Skill", GameComponent.Skill);
                return -1;
            }

            if (skillLvl <= 0)
            {
                OwlLogger.LogError($"Can't create SkillExecution with skillLvl {skillLvl}", GameComponent.Skill);
                return -1;
            }

            if (user == null)
            {
                OwlLogger.LogError("Can't create SkillExecution with null user!", GameComponent.Skill);
                return -1;
            }

            if (!target.IsSet())
            {
                OwlLogger.LogError("Can't create SkillExecution with unset target!", GameComponent.Skill);
                return -1;
            }

            if(map == null)
            {
                OwlLogger.LogError("Can't create SkillExecution with null map!", GameComponent.Skill);
                return -1;
            }

            if (user.MapId != map.MapId)
            {
                OwlLogger.LogError($"Tried to create SkillExecution for skill {skillId}, user {user.Id}, but user's not on map {map.MapId}!", GameComponent.Skill);
                return -2;
            }

            _skillId = skillId;
            Map = map;

            SkillStaticDataEntry entry = SkillStaticDataDatabase.GetSkillStaticData(skillId);
            if(entry == null)
            {
                return -4; // Error already logged in Db
            }

            ASkillImpl logic = Map.SkillModule.GetSkillLogic(skillId);
            if (logic == null)
            {
                return -3; // Already logged inside GetSkillLogic
            }

            int rawSpCost = entry.GetValueForLevel(entry.SpCost, skillLvl);
            float spCostMod = 1.0f;
            // TODO: apply non-skill-specific SP cost manipulation effects
            logic.UpdateSpCostMod(rawSpCost, ref spCostMod);
            int actualSpCost = (int)(rawSpCost * spCostMod);

            int rawRange = entry.GetValueForLevel(entry.Range, skillLvl);
            int rangeMod = 0; // Don't expect there ever to be a "percentage range increase"
            // TODO: Apply non-skill-specific Range manipulation effects
            logic.UpdateRangeMod(rawRange, ref rangeMod);
            int actualRange = rawRange + rangeMod;

            float rawCastTime = entry.GetValueForLevel(entry.BaseCastTime, skillLvl);
            float castTimeMod = 1.0f;
            if(!entry.GetValueForLevel(entry.IsCastTimeFixed, skillLvl))
            {
                if(user is CharacterRuntimeData character)
                {
                    castTimeMod = character.CastTime.Total;
                }
            }
            logic.UpdateCastTimeMod(rawCastTime, ref castTimeMod);
            float actualCastTime = rawCastTime * castTimeMod;

            float rawAnimCd = entry.GetValueForLevel(entry.AnimCd, skillLvl);
            float animCdMod = 1.0f;
            // TODO: non-skill-specific apply modifications to animCd here
            logic.UpdateAnimCdMod(rawAnimCd, ref animCdMod);
            float actualAnimCd = rawAnimCd * animCdMod;

            Var1 = entry.GetValueForLevel(entry.Var1, skillLvl);
            Var2 = entry.GetValueForLevel(entry.Var2, skillLvl);
            Var3 = entry.GetValueForLevel(entry.Var3, skillLvl);
            Var4 = entry.GetValueForLevel(entry.Var4, skillLvl);
            Var5 = entry.GetValueForLevel(entry.Var5, skillLvl);

            int initResult = Initialize(skillLvl, user, actualSpCost, actualRange, actualCastTime, actualAnimCd, target);
            if(initResult != 0)
            {
                return initResult;
            }

            logic.OnCreate(this);

            return 0;
        }

        public override void Reset()
        {
            base.Reset();
            _skillId = SkillId.Unknown;
            Map = null;
            Var1 = 0;
            Var2 = 0;
            Var3 = 0;
            Var4 = 0;
            Var5 = 0;
            runtimeVar = null;
        }
    }

    public abstract class ASkillImpl
    {
        public virtual void UpdateSpCostMod(int rawSpCost, ref float generalSpCostMod) { }
        public virtual void UpdateRangeMod(int rawRange, ref int generalRangeMod) { }
        public virtual void UpdateCastTimeMod(float rawCastTime, ref float generalCastTimeMod) { }
        public virtual void UpdateAnimCdMod(float rawAnimCd, ref float generalAnimCdMod) { }

        // Called after the skillExec has been initialized
        public virtual void OnCreate(ServerSkillExecution skillExec) { }

        // Not called for skills with 0 cast time
        public virtual void OnCastStart(ServerSkillExecution skillExec) { }
        public virtual void OnCastEnd(ServerSkillExecution skillExec, bool wasInterrupted) { }

        // Called when CastTime (if any) & animation delay is about to start
        // contains main skill effect
        // Will be called repeatedly for skills that don't have 
        public virtual void OnExecute(ServerSkillExecution skillExec) { skillExec.HasExecutionStarted = true; }
        // Called when CastTime & AnimationDelay of this skill are over, execution is completely complete, skill is about to be removed from entity. Will be called for interrupted skills as well!
        public virtual void OnCompleted(ServerSkillExecution skillExec, bool wasSuccessful) { }

        // Which skills will go on a cooldown other than AnimationDelay
        public virtual Dictionary<SkillId, float> GetSkillCoolDowns() { return null; }

        public virtual SkillFailReason CheckTarget(ServerSkillExecution skillExec)
        {
            if (!skillExec.Target.IsValid())
                return SkillFailReason.TargetInvalid;

            if (skillExec.Target.IsGroundTarget())
            {
                if (Extensions.GridDistanceSquare(skillExec.User.Coordinates, skillExec.Target.GroundTarget) > skillExec.Range)
                    return SkillFailReason.OutOfRange;
            }
            else
            {
                if (skillExec.Target.EntityTarget.IsDead())
                    return SkillFailReason.Death;

                if (skillExec.Target.EntityTarget.MapId != skillExec.User.MapId
                    || Extensions.GridDistanceSquare(skillExec.User.Coordinates, skillExec.Target.EntityTarget.Coordinates) > skillExec.Range)
                    return SkillFailReason.OutOfRange;
            }

            return SkillFailReason.None;
        }

        // This function's overrides should only contain skill-specific logic, like FreeCast, movement-skills being blocked by conditions, etc.
        public virtual SkillFailReason CanBeExecuted(ServerSkillExecution skillExec, BattleEntity user)
        {
            if (user.IsDead())
                return SkillFailReason.Death;

            if (!user.CanAct())
                return SkillFailReason.AnimationLocked;

            if (user.SkillCooldowns.ContainsKey(SkillId.ALL_EXCEPT_AUTO)
                && !user.SkillCooldowns[SkillId.ALL_EXCEPT_AUTO].IsFinished())
                return SkillFailReason.OnCooldown;
            else if (user.SkillCooldowns.ContainsKey(skillExec.SkillId)
                && !user.SkillCooldowns[skillExec.SkillId].IsFinished())
                return SkillFailReason.OnCooldown;

            if (user.IsCasting())
                return SkillFailReason.AlreadyCasting;

            if (user.CurrentSp < skillExec.SpCost)
                return SkillFailReason.NotEnoughSp;

            if(user is CharacterRuntimeData character)
            {
                if (character.GetSkillLevel(skillExec.SkillId) < skillExec.SkillLvl)
                    return SkillFailReason.NotLearned;
            }

            // TODO: Check for statuses like Silence, other general conditions like Ammo

            return SkillFailReason.None;
        }

        public virtual bool HasFinishedResolving(ServerSkillExecution skillExec)
        {
            return skillExec.CastTime.IsFinished() && skillExec.AnimationCooldown.IsFinished();
        }

        public virtual bool IsExecutionFinished(ServerSkillExecution skillExec)
        {
            return skillExec.HasExecutionStarted;
        }
    }

    public class AutoAttackSkillImpl : ASkillImpl
    {
        // Var1: bool 0 or 1: Has followup attack been queued?
        public override bool IsExecutionFinished(ServerSkillExecution skillExec)
        {
            return skillExec.Var1 > 0; // Delay Execution end until a followup-attack has been queued
        }

        public override void OnCreate(ServerSkillExecution skillExec)
        {
            base.OnCreate(skillExec);

            if (skillExec.SkillLvl != 2)
            {
                skillExec.Var1 = 1; // Only level 2 queues followup attacks
            }
        }

        public override void OnExecute(ServerSkillExecution skillExec)
        {
            if (!skillExec.HasExecutionStarted)
            {
                // Don't use StandardAttack here - Auto-attacks can crit!
                skillExec.Map.BattleModule.PerformPhysicalAttack(skillExec.User as ServerBattleEntity, skillExec.Target.EntityTarget as ServerBattleEntity, 1.0f, EntityElement.Unknown, true, true, false);
                base.OnExecute(skillExec);
            }
            else if (skillExec.Var1 == 0)
            {
                // Doing this in an else delays it by 1 tick, which makes sure that we're animation-locked by this skill
                // so that ReceiveSkillExecutionRequest queues the next attack, instead of trying to perform it instantly

                // Indicates an auto-attack with Sticky-Attacking enabled, and last auto-attack succeeded
                // Have entity attack again if no other skill has been queued up
                // Depending on system, queueing up skill this early may not even have been possible for the user - which is fine.
                if (skillExec.User.QueuedSkill == null)
                    skillExec.Map.SkillModule.ReceiveSkillExecutionRequest(skillExec.SkillId, skillExec.SkillLvl, skillExec.User as ServerBattleEntity, skillExec.Target);
                skillExec.Var1 = 1;
            }
        }
    }

    public class FireBoltSkillImpl : ASkillImpl
    {
        // Var1: Skill Ratio in %: 100 = 100%
        public override void OnExecute(ServerSkillExecution skillExec)
        {
            base.OnExecute(skillExec);
            skillExec.Map.BattleModule.StandardMagicAttack(skillExec, skillExec.Var1 / 100.0f, EntityElement.Fire1);
        }
    }

    public class PlaceWarpSkillImpl : ASkillImpl
    {
        // Var1: Radius of Warp-area
        // Var2: Duration of Warp-area
        public override void OnExecute(ServerSkillExecution skillExec)
        {
            base.OnExecute(skillExec);
            RectangleCenterGridShape shape = new() { Center = skillExec.Target.GroundTarget, Radius = skillExec.Var1 };
            WarpCellEffectGroup cellEffectGroup = new();
            cellEffectGroup.Create(skillExec.Map.Grid, shape, skillExec.User.MapId, skillExec.User.Coordinates, skillExec.Var2);
        }
    }
}

