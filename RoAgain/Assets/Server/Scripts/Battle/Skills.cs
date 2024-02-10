using OwlLogging;
using Shared;
using System.Collections.Generic;

namespace Server
{
    public class ServerSkillExecution : ASkillExecution, IAutoInitPoolObject
    {
        private SkillId _skillId;
        public override SkillId SkillId => _skillId;
        public ServerMapInstance Map;
        public ServerBattleEntity UserTyped => User as ServerBattleEntity;
        public ServerBattleEntity EntityTargetTyped => Target.EntityTarget as ServerBattleEntity;
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
        public virtual Dictionary<SkillId, float> GetSkillCoolDowns(ServerSkillExecution skillExec) { return null; }

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

                if (skillExec.Target.EntityTarget.MapId != skillExec.User.MapId)
                    return SkillFailReason.WrongMap;

                int distance = Extensions.GridDistanceSquare(skillExec.User.Coordinates, skillExec.Target.EntityTarget.Coordinates);
                // TODO: Divide this into "OutOfRange_Near" & "OutOfRange_Far" for purposes of allowing out-of-range cast finishes
                if (distance > skillExec.Range)
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
                // Don't use StandardPhysicalAttack here - Auto-attacks can crit!
                AttackParams parameters = AutoInitResourcePool<AttackParams>.Acquire();
                parameters.InitForPhysicalSkill(skillExec);
                parameters.CanCrit = true;
                parameters.SkillFactor = 1.0f;

                skillExec.Map.BattleModule.PerformAttack(skillExec.Target.EntityTarget as ServerBattleEntity, parameters);
                AutoInitResourcePool<AttackParams>.Return(parameters);
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
                    skillExec.Map.SkillModule.ReceiveSkillExecutionRequest(skillExec.SkillId, skillExec.SkillLvl, skillExec.UserTyped, skillExec.Target);
                skillExec.Var1 = 1;
            }
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

    // Basic Skill: Needs PassiveSkill system

    // PlayDead: Needs Buff&Debuff system

    public class FirstAidSkillImpl : ASkillImpl
    {
        // Var1: Amount of HP healed
        public override void OnExecute(ServerSkillExecution skillExec)
        {
            base.OnExecute(skillExec);
            skillExec.Map.BattleModule.ChangeHp(skillExec.UserTyped, skillExec.Var1, skillExec.UserTyped);
        }
    }

    public class BashSkillImpl : ASkillImpl
    {
        // Var1: SkillRatio in %: 100 = 100%
        // Var2: bonus Hit amount in points

        public override void OnExecute(ServerSkillExecution skillExec)
        {
            base.OnExecute(skillExec);
            AttackParams param = AutoInitResourcePool<AttackParams>.Acquire();
            param.InitForPhysicalSkill(skillExec);
            param.PostAttackCallback = PostAttackCallback;
            param.PreAttackCallback = PreAttackCallback;

            skillExec.Map.BattleModule.PerformAttack(skillExec.EntityTargetTyped, param);
            AutoInitResourcePool<AttackParams>.Return(param);
        }

        private void PreAttackCallback(ServerSkillExecution skillExec)
        {
            skillExec.UserTyped.Hit.ModifyAdd(skillExec.Var2);
        }

        private void PostAttackCallback(ServerSkillExecution skillExec)
        {
            skillExec.UserTyped.Hit.ModifyAdd(-skillExec.Var2);
        }
    }

    public class MagnumBreakSkillImpl : ASkillImpl
    {
        // Var1: SkillRatio in %: 100 = 100%
        // Var2: bonus Hit amount in points
        // Var3: Range of AoE
        // Var4: Atk Buff strength in percent
        // Var5: Atk Buff duration in seconds

        public override SkillFailReason CheckTarget(ServerSkillExecution skillExec)
        {
            if(skillExec.Target.EntityTarget != skillExec.User)
                return SkillFailReason.TargetInvalid;

            return base.CheckTarget(skillExec);
        }

        public override void OnExecute(ServerSkillExecution skillExec)
        {
            base.OnExecute(skillExec);
            AttackParams param = AutoInitResourcePool<AttackParams>.Acquire();
            param.InitForPhysicalSkill(skillExec);

            param.OverrideElement = EntityElement.Fire1;
            param.PostAttackCallback = PostAttackCallback;
            param.PreAttackCallback = PreAttackCallback;

            foreach(ServerBattleEntity target in skillExec.Map.Grid.GetOccupantsInRangeSquare<ServerBattleEntity>(skillExec.User.Coordinates, skillExec.Var3))
            {
                skillExec.Map.BattleModule.PerformAttack(target, param);
            }

            // TODO: Apply buff
            
            AutoInitResourcePool<AttackParams>.Return(param);
        }

        private void PreAttackCallback(ServerSkillExecution skillExec)
        {
            skillExec.UserTyped.Hit.ModifyAdd(skillExec.Var2);
        }

        private void PostAttackCallback(ServerSkillExecution skillExec)
        {
            skillExec.UserTyped.Hit.ModifyAdd(-skillExec.Var2);
        }
    }

    // One Hand Sword Mastery: Needs PassiveSkill system

    // Two Hand Sword Mastery: Needs PassiveSkill system

    // Increased Hp Recovery: Needs PassiveSkill system

    // Provoke: Needs Buff&Debuff system

    // Endure: Needs Buff&Debuff system

    // AutoBerserk: Needs PassiveSkill & Buff&Debuff system

    // HpRecWhileMoving: Needs PassiveSkill system
    
    // FatalBlow: Needs PassiveSkill system

    public class FireBoltSkillImpl : ASkillImpl
    {
        // Var1: Skill Ratio in %: 100 = 100%
        // Var2: Number of hits
        public override void OnExecute(ServerSkillExecution skillExec)
        {
            base.OnExecute(skillExec);
            skillExec.Map.BattleModule.StandardMagicAttack(skillExec, skillExec.Var1, EntityElement.Fire1, skillExec.Var2);
        }

        public override Dictionary<SkillId, float> GetSkillCoolDowns(ServerSkillExecution skillExec)
        {
            Dictionary<SkillId, float> cds = new()
            {
                { SkillId.ALL_EXCEPT_AUTO, 0.8f + 0.2f * skillExec.SkillLvl }
            };
            return cds;
        }
    }

    public class FireBallSkillImpl : ASkillImpl
    {
        public override void OnExecute(ServerSkillExecution skillExec)
        {
            base.OnExecute(skillExec);
            AttackParams param = AutoInitResourcePool<AttackParams>.Acquire();
            param.InitForMagicalSkill(skillExec);

            param.OverrideElement = EntityElement.Fire1;

            foreach (ServerBattleEntity target in skillExec.Map.Grid.GetOccupantsInRangeSquare<ServerBattleEntity>(skillExec.Target.EntityTarget.Coordinates, skillExec.Var3))
            {
                skillExec.Map.BattleModule.PerformAttack(target, param);
            }

            AutoInitResourcePool<AttackParams>.Return(param);
        }

        public override Dictionary<SkillId, float> GetSkillCoolDowns(ServerSkillExecution skillExec)
        {
            Dictionary<SkillId, float> cds = new();
            if (skillExec.SkillLvl <= 5)
                cds.Add(SkillId.ALL_EXCEPT_AUTO, 1.5f);
            else
                cds.Add(SkillId.ALL_EXCEPT_AUTO, 1.0f);
            return cds;
        }
    }
}

