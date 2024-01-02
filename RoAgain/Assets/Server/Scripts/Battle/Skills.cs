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

            AutoAttackSkillExecution skill = new();
            if (skill.Initialize(skillLvl, user, 0, range, 0f, user.GetDefaultAnimationCooldown(), target, map) != 0)
                return null;
            return skill;
        }

        public override void OnExecute()
        {
            base.OnExecute();

            // Attack logic
            Map.BattleModule.PerformPhysicalAttack(User, Target, 1.0f);

            // Indicates an auto-attack with Sticky-Attacking enabled, and last auto-attack succeeded
            if (SkillLvl == 2)
            {
                // Have entity attack again if no other skill has been queued up
                // Depending on system, queueing up skill this early may not even have been possible for the user - which is fine.
                User.QueuedSkill ??= Create(2, User, Target, Map);
            }
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

