using OwlLogging;
using Shared;
using UnityEngine;

namespace Client
{
    public class SkillSlotInitializer : MonoBehaviour
    {
        [SerializeField]
        private SkillId _skillId = SkillId.Unknown;
        [SerializeField]
        private int _skillParam = 0;

        void Start()
        {
            if (_skillId == SkillId.Unknown)
                return;

            SkillSlot slot = GetComponent<SkillSlot>();
            if (slot == null)
            {
                OwlLogger.LogError($"SkillSlotInitializer can't find slot!", GameComponent.UI);
                return;
            }
            slot.SetSkillId(_skillId);
            slot.SetSkillParam(_skillParam);
        }
    }
}