using Shared;
using System.Collections.Generic;

namespace Client
{
    public static class LocalizationHelpers
    {
        public static readonly Dictionary<SkillFailReason, LocalizedStringId> skillFailStrings = new()
        {
            {  SkillFailReason.OutOfRange, new(199) },
            {  SkillFailReason.UserDead, new(200) },
            {  SkillFailReason.AlreadyCasting, new(201) },
            {  SkillFailReason.OnCooldown, new(202) },
            {  SkillFailReason.NotEnoughSp, new(198) },
            {  SkillFailReason.NotEnoughHp, new(203) },
            {  SkillFailReason.NotEnoughAmmo, new(204) },
            {  SkillFailReason.TargetDead, new(205) },
            {  SkillFailReason.TargetInvalid, new(206) }
        };

        public static LocalizedStringId GetErrorMessage(this SkillFailReason reason)
        {
            if (skillFailStrings.ContainsKey(reason))
                return skillFailStrings[reason];
            else
                return LocalizedStringId.INVALID;
        }
    }
}