using OwlLogging;
using Shared;
using UnityEngine;

namespace Server
{
    public abstract class AServerSkillExecution : ASkillExecution
    {
        public ServerMapInstance Map;
        public new ServerBattleEntity User => base.User as ServerBattleEntity;
        //public int[] Var1 = null;

        protected int Initialize(int skillLvl, BattleEntity user, int spCost, int range, float castTime, float animCd, SkillTarget target, ServerMapInstance map)
        {
            Map = map;
            return Initialize(skillLvl, user, spCost, range, castTime, animCd, target);
        }

        //public int InitializeFromStatic(int skillLvl, ServerBattleEntity user, SkillTarget target, ServerMapInstance map)
        //{
        //    // TODO: (Optional) Clear values in case of pooled SkillExecutions
        //    // TODO: Look up static data from skill db
        //    // TODO: Modify Static values based on user
        //}
    }

    public class AutoAttackSkillExecution : AServerSkillExecution
    {
        public override SkillId SkillId => SkillId.AutoAttack;
        private bool _hasQueuedFollowup = false;

        public override bool IsExecutionFinished()
        {
            return _hasQueuedFollowup;
        }

        public static AutoAttackSkillExecution Create(int skillLvl, BattleEntity user, SkillTarget target, ServerMapInstance map)
        {
            int range = 1;
            // TODO: Calculate range based on equip & Co.
            // TODO: Move creation-code for physical attacks into common base-class, like with Magical Attack

            // TODO: Validate Target (or somewhere in a base class)

            AutoAttackSkillExecution skill = new();

            if(skillLvl != 2)
            {
                skill._hasQueuedFollowup = true;
            }

            if (skill.Initialize(skillLvl, user, 0, range, 0f, user.GetDefaultAnimationCooldown(), target, map) != 0)
                return null;
            return skill;
        }

        public override void OnExecute()
        {
            if(!HasExecutionStarted)
            {
                // Attack logic
                Map.BattleModule.PerformPhysicalAttack(User, Target.EntityTarget as ServerBattleEntity, 1.0f);
                base.OnExecute();
            }
            else if(!_hasQueuedFollowup)
            {
                // Indicates an auto-attack with Sticky-Attacking enabled, and last auto-attack succeeded
                // Have entity attack again if no other skill has been queued up
                // Depending on system, queueing up skill this early may not even have been possible for the user - which is fine.
                if (User.QueuedSkill == null)
                    Map.SkillModule.ReceiveSkillExecutionRequest(SkillId, 2, User, Target);
                _hasQueuedFollowup = true;
            }
        }
    }

    public abstract class AMagicAttackSkillExecution : AServerSkillExecution
    {
        public override SkillId SkillId => _skillId;
        protected SkillId _skillId;
        protected EntityElement _attackElement;
        protected float _skillFactor;

        protected int Initialize(int skillLvl, BattleEntity user, int spCost, int range, float castTime, float animCd, SkillTarget target, ServerMapInstance map,
            SkillId skillId, float skillFactor, float baseCastTime, EntityElement attackElement)
        {
            int initResult = Initialize(skillLvl, user, skillLvl * 7, 9, 0, user.GetDefaultAnimationCooldown(), target, map);
            if(initResult == 0)
            {
                PostInit(skillId, skillFactor, baseCastTime, attackElement);
            }
            return initResult;
        }

        protected void PostInit(SkillId skillId, float skillFactor, float baseCastTime, EntityElement attackElement)
        {
            _skillId = skillId;
            _skillFactor = skillFactor;
            _attackElement = attackElement;
            if (baseCastTime == 0.0f)
                return;

            if(User != null)
            {
                OwlLogger.LogWarning($"Init skill before User is set - can't modify cast time!", GameComponent.Battle);
                CastTime.Initialize(Mathf.Abs(baseCastTime));
            }
            else
            {
                if(baseCastTime < 0.0f)
                {
                    CastTime.Initialize(Mathf.Abs(baseCastTime));
                }
                else
                {
                    float castTime = baseCastTime;
                    if (User is CharacterRuntimeData character)
                    {
                        castTime = Mathf.Max(0, castTime * character.CastTime.Total);
                    }
                    CastTime.Initialize(castTime);
                }
            }
        }

        public override void OnExecute()
        {
            base.OnExecute();

            Map.BattleModule.PerformMagicalAttack(User, Target.EntityTarget as ServerBattleEntity, _skillFactor, _attackElement, false, false, false);
        }
    }

    public class FireBoltSkillExecution : AMagicAttackSkillExecution
    {
        public static FireBoltSkillExecution Create(int skillLvl, BattleEntity user, SkillTarget target, ServerMapInstance map)
        {
            FireBoltSkillExecution skill = new();

            if (skill.Initialize(skillLvl, user, skillLvl * 7, 9, 0, user.GetDefaultAnimationCooldown(), target, map, SkillId.FireBolt, skillLvl * 0.5f, (skillLvl - 1) * 0.7f, EntityElement.Fire1) != 0)
                return null;

            return skill;
        }
    }

    public class PlaceWarpSkillExecution : AServerSkillExecution
    {
        public override SkillId SkillId => SkillId.PlaceWarp;
        public Vector2Int WarpTargetCoords;
        public string TargetMap;

        public static PlaceWarpSkillExecution Create(int skillLvl, BattleEntity user, SkillTarget target, ServerMapInstance map)
        {
            PlaceWarpSkillExecution skill = new();

            float castTime = skillLvl * 2;
            // TODO: This feels like it should be centrally handled, with flags for dfiferent casttime calculations.
            if (user is CharacterRuntimeData character)
            {
                castTime = Mathf.Max(0, castTime * character.CastTime.Total);
            }

            if (skill.Initialize(skillLvl, user, 5 * skillLvl, 10, castTime, user.GetDefaultAnimationCooldown(), target, map) != 0)
                return null;

            skill.TargetMap = user.MapId;
            skill.WarpTargetCoords = user.Coordinates;
            return skill;
        }

        public override void OnExecute()
        {
            base.OnExecute();

            RectangleCenterGridShape shape = new() { Center = Target.GroundTarget, Radius = SkillLvl-1 };
            WarpCellEffectGroup cellEffectGroup = new();
            cellEffectGroup.Create(Map.Grid, shape, TargetMap, WarpTargetCoords, SkillLvl * 10f);
        }
    }
}

