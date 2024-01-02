using Shared;
using UnityEngine;

namespace Client
{
    public class ClientEntitySkill : AEntitySkillExecution
    {
        public override SkillId SkillId => _skillId;
        private SkillId _skillId;
        public new ClientBattleEntity Target => base.Target as ClientBattleEntity;
        
        public void Initialize(SkillId skillId, int skillLvl, ClientBattleEntity user, int spCost, int range, float castTime, float animCd, ClientBattleEntity target)
        {
            _skillId = skillId;
            Initialize(skillLvl, user, spCost, range, castTime, animCd, target);
        }
    }

    public class ClientGroundSkill : AGroundSkillExecution
    {
        public override SkillId SkillId => _skillId;
        private SkillId _skillId;

        public void Initialize(SkillId skillId, int skillLvl, ClientBattleEntity user, int spCost, int range, float castTime, float animCd, Vector2Int target)
        {
            _skillId = skillId;
            Initialize(skillLvl, user, spCost, range, castTime, animCd, target);
        }
    }
}

