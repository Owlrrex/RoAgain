using Shared;
using System.Collections.Generic;

namespace Client
{
    public static class LocalizationHelpers
    {
        public static readonly Dictionary<SkillFailReason, LocalizedStringId> skillFailStrings = new()
        {
            {  SkillFailReason.OutOfRange, new() { Id = 199 } },
            {  SkillFailReason.UserDead, new() { Id = 200 } },
            {  SkillFailReason.AlreadyCasting, new() { Id = 201 } },
            {  SkillFailReason.OnCooldown, new() { Id = 202 } },
            {  SkillFailReason.NotEnoughSp, new() { Id = 198 } },
            {  SkillFailReason.NotEnoughHp, new() { Id = 203 } },
            {  SkillFailReason.NotEnoughAmmo, new() { Id = 204 } },
            {  SkillFailReason.TargetDead, new() { Id = 205 } },
            {  SkillFailReason.TargetInvalid, new() { Id = 206 } }
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