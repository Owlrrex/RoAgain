using OwlLogging;
using Shared;
using System.Collections.Generic;

namespace Server
{
    /***************************************
     * General Purpose & Abstract classes
     ***************************************/

    public class ServerSkillExecution : ASkillExecution, IAutoInitPoolObject
    {
        private SkillId _skillId;
        public override SkillId SkillId => _skillId;
        public MapInstance Map;
        public ServerBattleEntity UserTyped => User as ServerBattleEntity;
        public ServerBattleEntity EntityTargetTyped => Target.EntityTarget as ServerBattleEntity;
        public int Var1, Var2, Var3, Var4, Var5;
        public object[] runtimeVar = null;

        public int InitializeFromStatic(SkillId skillId, int skillLvl, ServerBattleEntity user, SkillTarget target, MapInstance map)
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

            ASkillImpl logic = Map.SkillModule.GetActiveSkillImpl(skillId);
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

            // If general absolute Casttime-Mods are ever added, they go exactly here
            // if(!entry.GetValueForLevel(entry.IsCastTimeFixed, skillLvl) { add additive bonuses }

            float castTimeMod = 1.0f;

            // We always apply conditional mods, since they're tailored to the skill's properties (including IsCastTimeFixed)
            if (user is CharacterRuntimeData charUser)
            {
                // If conditional absolute Casttime-Mods are ever added, they go exactly here

                if(charUser.ConditionalStats?.TryGetValue(EntityPropertyType.CastTime_Mod_Mult, out var multList) == true)
                {
                    foreach (ConditionalStat stat in multList)
                    {
                        if (stat.Condition.Evaluate(this)) // lots of properties of "this" aren't set yet, because Initialize() hasn't been called. Keep an eye on that!
                            castTimeMod *= stat.Value;
                    }
                }
            }
            
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
            // TODO: apply generic modifications to animCd here
            logic.UpdateAnimCdMod(rawAnimCd, ref animCdMod);
            float actualAnimCd = rawAnimCd * animCdMod;

            Var1 = entry.GetValueForLevel(entry.Var1, skillLvl);
            Var2 = entry.GetValueForLevel(entry.Var2, skillLvl);
            Var3 = entry.GetValueForLevel(entry.Var3, skillLvl);
            Var4 = entry.GetValueForLevel(entry.Var4, skillLvl);
            Var5 = entry.GetValueForLevel(entry.Var5, skillLvl);

            CanBeInterrupted = entry.GetValueForLevel(entry.CanBeInterrupted, skillLvl);

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

        // This function's overrides should only contain skill-specific logic, like FreeCast, movement-skills being blocked by conditions, etc.
        public virtual SkillFailReason CheckTarget(ServerSkillExecution skillExec)
        {
            if (!skillExec.Target.IsValid())
                return SkillFailReason.TargetInvalid;

            if (skillExec.Target.IsGroundTarget())
            {
                if (skillExec.User.Coordinates.GridDistanceSquare(skillExec.Target.GroundTarget) > skillExec.Range)
                    return SkillFailReason.OutOfRange;
            }
            else
            {
                if (skillExec.Target.EntityTarget.IsDead())
                    return SkillFailReason.TargetDead;

                if (skillExec.Target.EntityTarget.MapId != skillExec.User.MapId)
                    return SkillFailReason.WrongMap;

                int distance = skillExec.User.Coordinates.GridDistanceSquare(skillExec.Target.EntityTarget.Coordinates);
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
                return SkillFailReason.UserDead;

            if (user.IsCasting())
                return SkillFailReason.AlreadyCasting;

            if (!user.CanAct())
                return SkillFailReason.AnimationLocked;

            if (user.SkillCooldowns.ContainsKey(SkillId.ALL_EXCEPT_AUTO)
                && !user.SkillCooldowns[SkillId.ALL_EXCEPT_AUTO].IsFinished())
                return SkillFailReason.OnCooldown;
            else if (user.SkillCooldowns.ContainsKey(skillExec.SkillId)
                && !user.SkillCooldowns[skillExec.SkillId].IsFinished())
                return SkillFailReason.OnCooldown;

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

    public abstract class APassiveSkillImpl
    {
        public abstract void Apply(ServerBattleEntity owner, int skillLvl, bool recalculate = true);

        public abstract void Unapply(ServerBattleEntity owner, int skillLvl, bool recalculate = true);
    }

    public abstract class APassiveConditionalSingleStatBoostImpl : APassiveSkillImpl
    {
        protected abstract Condition _condition { get; }
        protected abstract SkillId _skillId { get; }
        protected abstract EntityPropertyType _propertyType { get; }

        protected readonly Dictionary<int, ConditionalStat> stats = new();

        public override void Apply(ServerBattleEntity owner, int skillLvl, bool recalculate = true)
        {
            if (owner is not CharacterRuntimeData charOwner)
                return;

            if (!stats.TryGetValue(skillLvl, out ConditionalStat stat))
            {
                SkillStaticDataEntry entry = SkillStaticDataDatabase.GetSkillStaticData(_skillId);
                int statIncrease = entry.GetValueForLevel(entry.Var1, skillLvl);
                stat = new ConditionalStat()
                {
                    Condition = _condition,
                    Value = statIncrease,
                };
                stats.Add(skillLvl, stat);
            }

            charOwner.AddConditionalStat(_propertyType, stat);
        }

        public override void Unapply(ServerBattleEntity owner, int skillLvl, bool recalculate = true)
        {
            if (owner is CharacterRuntimeData charOwner)
            {
                // It's ok to exception here if stats[skillLvl] isn't set - that would indicate that
                // This passive skill wasn't applied before, which shouldn't be possible
                charOwner.RemoveConditionalStat(_propertyType, stats[skillLvl]);
            }
        }
    }

    /*************************
     * Skill implementations
     *************************/

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
                
                if (skillExec.User.CurrentPathingAction == null
                    && !skillExec.EntityTargetTyped.IsDead())
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
            SquareCenterGridShape shape = new() { Center = skillExec.Target.GroundTarget, Radius = skillExec.Var1 };
            WarpCellEffectGroup cellEffectGroup = new();
            cellEffectGroup.Create(skillExec.Map.Grid, shape, skillExec.User.MapId, skillExec.User.Coordinates, skillExec.Var2);
        }
    }

    // Basic Skill: Needs systems that it interacts with
    public class BasicSkillDebugSkillImpl : APassiveSkillImpl
    {
        public override void Apply(ServerBattleEntity owner, int skillLvl, bool recalculate = true)
        {
            owner.MaxHp.ModifyAdd(50 * skillLvl, recalculate);
        }

        public override void Unapply(ServerBattleEntity owner, int skillLvl, bool recalculate = true)
        {
            owner.MaxHp.ModifyAdd(-50 * skillLvl, recalculate);
        }
    }

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

    public class OneHandSwordMasterySkillImpl : APassiveConditionalSingleStatBoostImpl
    {
        // Var 1: Atk increase
        private Condition _conditionPriv = new UserWeaponTypeCondition() { WeaponType = AttackWeaponType.OneHandSword };
        protected override Condition _condition => _conditionPriv;

        protected override SkillId _skillId => SkillId.OneHandSwordMastery;

        protected override EntityPropertyType _propertyType => EntityPropertyType.MeleeAtk_Mod_Add;
    }

    public class TwoHandSwordMasterySkillImpl : APassiveConditionalSingleStatBoostImpl
    {
        // Var 1: Atk increase
        private Condition _conditionPriv = new UserWeaponTypeCondition() { WeaponType = AttackWeaponType.TwoHandSword };
        protected override Condition _condition => _conditionPriv;

        protected override SkillId _skillId => SkillId.TwoHandSwordMastery;

        protected override EntityPropertyType _propertyType => EntityPropertyType.MeleeAtk_Mod_Add;
    }

    public class IncHpRecoverySkillImpl : APassiveSkillImpl
    {
        // Var 1: Seconds per trigger
        // Var 2: Constant Hp per trigger
        // Var 3: Percentage of MaxHp per trigger
        // Var 4: Item HP Recovery increase (in percentage points)

        private class Entry
        {
            public TimerFloat Timer;
            public int SkillLvl;
        }

        private readonly Dictionary<int, Entry> _timers = new();
        private SkillStaticDataEntry _staticData = SkillStaticDataDatabase.GetSkillStaticData(SkillId.IncHpRecovery);

        public override void Apply(ServerBattleEntity owner, int skillLvl, bool recalculate = true)
        {
            Entry newEntry = new Entry()
            {
                Timer = new TimerFloat(_staticData.GetValueForLevel(_staticData.Var1, skillLvl)),
                SkillLvl = skillLvl,
            };
            _timers.Add(owner.Id, newEntry);
            owner.Update += OnUpdate;

            // TODO: Implement Increased-Item-Hp-Recovery effect
        }

        private void OnUpdate(ServerBattleEntity owner, float deltaTime)
        {
            if (owner.IsDead())
                return;

            if (owner.IsMoving())
                return;

            // TODO: Detection states that don't permit/slow down regen
            Entry entry = _timers[owner.Id];
            entry.Timer.Update(deltaTime);
            if (entry.Timer.IsFinished())
            {
                int staticHp = _staticData.GetValueForLevel(_staticData.Var2, entry.SkillLvl);
                int dynamicHp = (int)(_staticData.GetValueForLevel(_staticData.Var3, entry.SkillLvl) * owner.MaxHp.Total / 100.0f);
                // TODO: Use a method here that allows (potentially) showing healing-numbers
                owner.GetMapInstance().BattleModule.ChangeHp(owner, staticHp + dynamicHp, owner);
                entry.Timer.Reset();
            }
        }

        public override void Unapply(ServerBattleEntity owner, int skillLvl, bool recalculate = true)
        {
            owner.Update -= OnUpdate;
            _timers.Remove(owner.Id);
        }
    }

    // Provoke: Needs Buff&Debuff system

    // Endure: Needs Buff&Debuff system

    // AutoBerserk: Needs PassiveSkill & Buff&Debuff system
    // This implementation causes a check on every single attack.
    // Subscribing to CurrentHp.Changed would cause a check every single HP-change - roughly equally bad?
    // Needs a Buff to display icon on Client-side?
    public class AutoBerserkSkillImpl : APassiveSkillImpl
    {
        protected readonly Dictionary<int, ConditionalStat> stats = new();

        private readonly Condition _condition = new BelowHpThresholdPercentCondition() { Percentage = 0.25f };

        public override void Apply(ServerBattleEntity owner, int skillLvl, bool recalculate = true)
        {
            if (owner is not CharacterRuntimeData charOwner)
                return;

            if (!stats.TryGetValue(skillLvl, out ConditionalStat stat))
            {
                SkillStaticDataEntry entry = SkillStaticDataDatabase.GetSkillStaticData(SkillId.AutoBerserk);
                int statIncrease = entry.GetValueForLevel(entry.Var1, skillLvl);
                stat = new ConditionalStat()
                {
                    Condition = _condition,
                    Value = statIncrease,
                };
                stats.Add(skillLvl, stat);
            }

            charOwner.AddConditionalStat(EntityPropertyType.MeleeAtk_Mod_Mult, stat);
            charOwner.AddConditionalStat(EntityPropertyType.RangedAtk_Mod_Mult, stat);
        }

        public override void Unapply(ServerBattleEntity owner, int skillLvl, bool recalculate = true)
        {
            if (owner is CharacterRuntimeData charOwner)
            {
                // It's ok to exception here if stats[skillLvl] isn't set - that would indicate that
                // This passive skill wasn't applied before, which shouldn't be possible
                charOwner.RemoveConditionalStat(EntityPropertyType.MeleeAtk_Mod_Mult, stats[skillLvl]);
                charOwner.RemoveConditionalStat(EntityPropertyType.RangedAtk_Mod_Mult, stats[skillLvl]);
            }
        }
    }

    // HpRecWhileMoving: Hardcoded into Hp Regeneration system

    // FatalBlow: Needs PassiveSkill system & Debuff system

    public class FireBoltSkillImpl : ASkillImpl
    {
        // Var1: Skill Ratio per hit in %: 100 = 100%
        // Var2: Number of hits
        public override void OnExecute(ServerSkillExecution skillExec)
        {
            base.OnExecute(skillExec);
            skillExec.Map.BattleModule.StandardMagicAttack(skillExec, skillExec.Var1, EntityElement.Fire1, skillExec.Var2);
        }

        private static readonly List<Dictionary<SkillId, float>> _cds = new()
        {
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } }
        };
        public override Dictionary<SkillId, float> GetSkillCoolDowns(ServerSkillExecution skillExec)
        {
            if (skillExec.SkillLvl < _cds.Count)
                return _cds[skillExec.SkillLvl];
            else
                return _cds[^1];
        }
    }

    public class FireBallSkillImpl : ASkillImpl
    {
        // Var1: Skill Ratio in %: 100 = 100%
        // Var2: AoE range
        public override void OnExecute(ServerSkillExecution skillExec)
        {
            base.OnExecute(skillExec);
            AttackParams param = AutoInitResourcePool<AttackParams>.Acquire();
            param.InitForMagicalSkill(skillExec);

            param.OverrideElement = EntityElement.Fire1;

            foreach (ServerBattleEntity target in skillExec.Map.Grid.GetOccupantsInRangeSquare<ServerBattleEntity>(skillExec.Target.EntityTarget.Coordinates, skillExec.Var2))
            {
                skillExec.Map.BattleModule.PerformAttack(target, param);
            }

            AutoInitResourcePool<AttackParams>.Return(param);
        }

        private static readonly List<Dictionary<SkillId, float>> _cds = new()
        {
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.5f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } }
        };

        public override Dictionary<SkillId, float> GetSkillCoolDowns(ServerSkillExecution skillExec)
        {
            if (skillExec.SkillLvl <= 5)
                return _cds[0];
            else
                return _cds[1];
        }
    }

    // Sight: Needs buff&debuff system

    // Firewall: Needs knockback system

    public class LightningBoltSkillImpl : ASkillImpl
    {
        // Var1: Skill Ratio per hit in %: 100 = 100%
        // Var2: Number of hits
        public override void OnExecute(ServerSkillExecution skillExec)
        {
            base.OnExecute(skillExec);
            skillExec.Map.BattleModule.StandardMagicAttack(skillExec, skillExec.Var1, EntityElement.Wind1, skillExec.Var2);
        }

        private static readonly List<Dictionary<SkillId, float>> _cds = new()
        {
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } }
        };
        public override Dictionary<SkillId, float> GetSkillCoolDowns(ServerSkillExecution skillExec)
        {
            if (skillExec.SkillLvl < _cds.Count)
                return _cds[skillExec.SkillLvl];
            else
                return _cds[^1];
        }
    }

    public class ThunderstormSkillImpl : ASkillImpl
    {
        // Var1: Skill Ratio per hit in %: 100 = 100%
        // Var2: Number of hits
        // Var3: AoE Range
        public override void OnExecute(ServerSkillExecution skillExec)
        {
            base.OnExecute(skillExec);
            AttackParams param = AutoInitResourcePool<AttackParams>.Acquire();
            param.InitForMagicalSkill(skillExec);

            param.OverrideElement = EntityElement.Wind1;
            param.ChainCount = skillExec.Var2;

            foreach (ServerBattleEntity target in skillExec.Map.Grid.GetOccupantsInRangeSquare<ServerBattleEntity>(skillExec.Target.GroundTarget, skillExec.Var3))
            {
                skillExec.Map.BattleModule.PerformAttack(target, param);
            }

            AutoInitResourcePool<AttackParams>.Return(param);
        }

        private static readonly Dictionary<SkillId, float> _cd = new() { { SkillId.ALL_EXCEPT_AUTO, 2.0f } };

        public override Dictionary<SkillId, float> GetSkillCoolDowns(ServerSkillExecution skillExec)
        {
            return _cd;
        }
    }

    public class ColdBoltSkillImpl : ASkillImpl
    {
        // Var1: Skill Ratio per hit in %: 100 = 100%
        // Var2: Number of hits
        public override void OnExecute(ServerSkillExecution skillExec)
        {
            base.OnExecute(skillExec);
            skillExec.Map.BattleModule.StandardMagicAttack(skillExec, skillExec.Var1, EntityElement.Water1, skillExec.Var2);
        }

        private static readonly List<Dictionary<SkillId, float>> _cds = new()
        {
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } }
        };

        public override Dictionary<SkillId, float> GetSkillCoolDowns(ServerSkillExecution skillExec)
        {
            if (skillExec.SkillLvl < _cds.Count)
                return _cds[skillExec.SkillLvl];
            else
                return _cds[^1];
        }
    }

    // Frost Diver: Needs Buff&Debuff system

    public class NapalmBeatSkillImpl : ASkillImpl
    {
        // Var1: Skill Ratio in %: 100 = 100%
        // Var2: AoE Range
        public override void OnExecute(ServerSkillExecution skillExec)
        {
            base.OnExecute(skillExec);

            List<ServerBattleEntity> targetList = skillExec.Map.Grid.GetOccupantsInRangeSquare<ServerBattleEntity>(skillExec.Target.EntityTarget.Coordinates, skillExec.Var2);

            AttackParams param = AutoInitResourcePool<AttackParams>.Acquire();
            param.InitForMagicalSkill(skillExec);

            param.SkillFactor /= targetList.Count;
            param.OverrideElement = EntityElement.Ghost1;

            foreach (ServerBattleEntity target in targetList)
            {
                skillExec.Map.BattleModule.PerformAttack(target, param);
            }

            AutoInitResourcePool<AttackParams>.Return(param);
        }

        static readonly private List<Dictionary<SkillId, float>> _cds = new()
        {
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 0.9f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 0.9f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 0.8f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 0.8f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 0.7f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 0.6f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 0.5f } }
        };

        public override Dictionary<SkillId, float> GetSkillCoolDowns(ServerSkillExecution skillExec)
        {
            if (skillExec.SkillLvl < _cds.Count)
                return _cds[skillExec.SkillLvl];
            else
                return _cds[^1];
        }
    }

    public class SoulStrikeSkillImpl : ASkillImpl
    {
        // Var1: Skill Ratio per hit in %: 100 = 100%
        // Var2: Number of hits
        public override void OnExecute(ServerSkillExecution skillExec)
        {
            base.OnExecute(skillExec);
            skillExec.Map.BattleModule.StandardMagicAttack(skillExec, skillExec.Var1, EntityElement.Ghost1, skillExec.Var2);
        }

        static readonly private List<Dictionary<SkillId, float>> _cds = new()
        {
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.2f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.4f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.2f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.6f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.4f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.8f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.6f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 2.0f } },
            new() { { SkillId.ALL_EXCEPT_AUTO, 1.8f } }
        };

        public override Dictionary<SkillId, float> GetSkillCoolDowns(ServerSkillExecution skillExec)
        {
            if (skillExec.SkillLvl < _cds.Count)
                return _cds[skillExec.SkillLvl];
            else
                return _cds[^1];
        }
    }

    // Safety Wall: CellEffect is available, needs buff&debuff system? Or some kind of event-handling to intercept attack code?

    // Stone Curse: Needs buff&debuff system

    public class IncSpRecoverySkillImpl : APassiveSkillImpl
    {
        // Var 1: Seconds per trigger
        // Var 2: Constant Sp per trigger
        // Var 3: Percentage of MaxSp per trigger
        // Var 4: Item Sp Recovery increase (in percentage points)

        private class Entry
        {
            public TimerFloat Timer;
            public int SkillLvl;
        }

        // Making this static makes the skill work cross-map easily, but introduces a link between different SkillImpl instances.
        // It would probably be cleaner to either force this class to be static as a whole, or have instances transfer ownership of
        // Entities when they change maps
        private static readonly Dictionary<int, Entry> _timers = new();

        private SkillStaticDataEntry _staticData = SkillStaticDataDatabase.GetSkillStaticData(SkillId.IncSpRecovery);

        public override void Apply(ServerBattleEntity owner, int skillLvl, bool recalculate = true)
        {
            Entry newEntry = new()
            {
                Timer = new TimerFloat(_staticData.GetValueForLevel(_staticData.Var1, skillLvl)),
                SkillLvl = skillLvl,
            };
            _timers.Add(owner.Id, newEntry);
            owner.Update += OnUpdate;

            // TODO: Implement Increased-Item-Sp-Recovery effect
        }

        private void OnUpdate(ServerBattleEntity owner, float deltaTime)
        {
            if (owner.IsDead())
                return;

            if (owner.IsMoving())
                return;

            // TODO: Detection states that don't permit/slow down regen
            Entry entry = _timers[owner.Id];
            entry.Timer.Update(deltaTime);
            if (entry.Timer.IsFinished())
            {
                int staticSp = _staticData.GetValueForLevel(_staticData.Var2, entry.SkillLvl);
                int dynamicSp = (int)(_staticData.GetValueForLevel(_staticData.Var3, entry.SkillLvl) * owner.MaxSp.Total / 100.0f);
                // TODO: Use a method here that allows (potentially) showing healing-numbers
                owner.GetMapInstance().BattleModule.ChangeHp(owner, staticSp + dynamicSp, owner);
                entry.Timer.Reset();
            }
        }

        public override void Unapply(ServerBattleEntity owner, int skillLvl, bool recalculate = true)
        {
            owner.Update -= OnUpdate;
            _timers.Remove(owner.Id);
        }
    }

    // Energy Coat: Needs Buff&debuff system

    // Owl Eye: Ready

    // Vulture Eye: Ready, may need Range System expansion to discern "melee" and "range" abilities

    // Improve Concentration: Needs Buff System

    // Double Strafe: Ready

    // Arrow Shower: Needs Knockback System

    // Arrow Crafting: Needs skill script system

    // Arrow Repel: Needs knockback system

    // Enlarge Weight Limit: Ready

    // Discount: Needs Shop system

    // Overcharge: Needs Shop system

    // Pushcart: Needs Pushcart/mount/similar system

    // Vending: Needs Player Store system

    // Mammonite: Needs custom-cost system

    // Identify: Needs Unidentified-Item-System & skill dialog system

    // Change Cart: Needs Pushcart system

    // Cart Revolution: Needs knockback System

    // Crazy Uproar: Needs Buff system

    // DoubleAttack: Ready? (is system for modifying skill executions sufficient?)

    // Improve Dodge: Ready

    // Steal: Needs Inventory system & Loot tables

    // Hiding: Needs Buff System & expanded visibility system

    // Envenom: Needs Buff system

    // Detoxify: Needs Buff system

    // Sand Attack: Needs Buff system

    // Find Stone: Needs Inventory system

    // Throw Stone: Needs custom-cost system

    // Backslide: Needs knockback system

    // Ruwach: Needs Buff system

    // Teleport: Ready

    // Warp Portal: Ready

    // Pneuma: Maybe ready, maybe needs Buff system

    // Demon Bane: Ready

    // Divine Protection: Ready

    // Signum Crucis: Needs buff system

    // Angelus: Needs buff system & party system

    // Blessing: Needs buff system

    // Aqua Benedicta: Needs custom cost system & inventory system

    // Heal: Ready

    // Cure: Needs buff system

    // Increase Agi: Needs buff system

    // Decrease Agi: Needs buff system

    // Holy Light: Ready
}

