using Shared;
using UnityEngine;

namespace Client
{
    public class ClientSkillExecution : ASkillExecution
    {
        public override SkillId SkillId => _skillId;
        private SkillId _skillId;

        public void Initialize(SkillId skillId, int skillLvl, ClientBattleEntity user, int spCost, int range, float castTime, float animCd, SkillTarget target)
        {
            _skillId = skillId;
            Initialize(skillLvl, user, spCost, range, castTime, animCd, target);
        }
    }
}

