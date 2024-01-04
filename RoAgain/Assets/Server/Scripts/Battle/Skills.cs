using OwlLogging;
using Shared;
using UnityEngine;

namespace Server
{
    public abstract class AServerGroundSkillExecution : AGroundSkillExecution
    {
        public ServerMapInstance Map;
        public new ServerBattleEntity User => base.User as ServerBattleEntity;

        protected int Initialize(int skillLvl, BattleEntity user, int spCost, int range, float castTime, float animCd, Vector2Int targetCoords, ServerMapInstance map)
        {
            Map = map;
            return Initialize(skillLvl, user, spCost, range, castTime, animCd, targetCoords);
        }
    }

    public abstract class AServerEntitySkillExecution : AEntitySkillExecution
    {
        public ServerMapInstance Map;
        public new ServerBattleEntity Target => base.Target as ServerBattleEntity;
        public new ServerBattleEntity User => base.User as ServerBattleEntity;

        protected int Initialize(int skillLvl, BattleEntity user, int spCost, int range, float castTime, float animCd, BattleEntity target, ServerMapInstance map)
        {
            Map = map;
            return Initialize(skillLvl, user, spCost, range, castTime, animCd, target);
        }
    }

    public class AutoAttackSkillExecution : AServerEntitySkillExecution
    {
        public override SkillId SkillId => SkillId.AutoAttack;

        public static AutoAttackSkillExecution Create(int skillLvl, BattleEntity user, BattleEntity target, ServerMapInstance map)
        {
            int range = 1;
            // TODO: Calculate range based on equip & Co.
            // TODO: Move creation-code for physical attacks into common base-class, like with Magical Attack

            AutoAttackSkillExecution skill = new();
            if (skill.Initialize(skillLvl, user, 0, range, 0f, user.GetDefaultAnimationCooldown(), target, map) != 0)
                return null;
            return skill;
        }

        public override void OnExecute()
        {
            base.OnExecute();

            // Indicates an auto-attack with Sticky-Attacking enabled, and last auto-attack succeeded
            if (SkillLvl == 2)
            {
                // Have entity attack again if no other skill has been queued up
                // Depending on system, queueing up skill this early may not even have been possible for the user - which is fine.
                User.QueuedSkill ??= Create(2, User, Target, Map);
            }

            // Attack logic
            Map.BattleModule.PerformPhysicalAttack(User, Target, 1.0f);
        }
    }

    public abstract class AMagicAttackSkillExecution : AServerEntitySkillExecution
    {
        public override SkillId SkillId => _skillId;
        protected SkillId _skillId;
        protected EntityElement _attackElement;
        protected float _skillFactor;

        protected int Initialize(int skillLvl, BattleEntity user, int spCost, int range, float castTime, float animCd, BattleEntity target, ServerMapInstance map,
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

            Map.BattleModule.PerformMagicalAttack(User, Target, _skillFactor, _attackElement, false, false, false);
        }
    }

    public class FireBoltSkillExecution : AMagicAttackSkillExecution
    {
        public static FireBoltSkillExecution Create(int skillLvl, BattleEntity user, BattleEntity target, ServerMapInstance map)
        {
            FireBoltSkillExecution skill = new();

            if (skill.Initialize(skillLvl, user, skillLvl * 7, 9, 0, user.GetDefaultAnimationCooldown(), target, map, SkillId.FireBolt, skillLvl * 0.5f, (skillLvl - 1) * 0.7f, EntityElement.Fire1) != 0)
                return null;

            return skill;
        }
    }

    public class PlaceWarpSkillExecution : AServerGroundSkillExecution
    {
        public override SkillId SkillId => SkillId.PlaceWarp;
        public Vector2Int WarpTargetCoords;
        public string TargetMap;

        public static PlaceWarpSkillExecution Create(int skillLvl, BattleEntity user, Vector2Int placementCoords, ServerMapInstance map)
        {
            PlaceWarpSkillExecution skill = new();

            float castTime = skillLvl * 2;
            // TODO: This feels like it should be centrally handled, with flags for dfiferent casttime calculations.
            if (user is CharacterRuntimeData character)
            {
                castTime = Mathf.Max(0, castTime * character.CastTime.Total);
            }

            if (skill.Initialize(skillLvl, user, 5 * skillLvl, 10, castTime, user.GetDefaultAnimationCooldown(), placementCoords, map) != 0)
                return null;

            skill.TargetMap = user.MapId;
            skill.WarpTargetCoords = user.Coordinates;
            return skill;
        }

        public override void OnExecute()
        {
            base.OnExecute();

            RectangleCenterGridShape shape = new() { Center = Target, Radius = SkillLvl-1 };
            WarpCellEffectGroup cellEffectGroup = new();
            cellEffectGroup.Create(Map.Grid, shape, TargetMap, WarpTargetCoords, SkillLvl * 10f);
        }
    }
}

