using OwlLogging;
using Shared;
using System.Collections.Generic;

namespace Server
{
    public class MinimumLevelBEC : AMinimumLevelBEC, IBattleEntityCriterium
    {
        public bool Evaluate(ServerBattleEntity bEntity)
        {
            return bEntity.BaseLvl.Value >= MinimumLevel;
        }
    }

    public class RequiredJobsExactBEC : ARequiredJobsExactBEC, IBattleEntityCriterium
    {
        public bool Evaluate(ServerBattleEntity bEntity)
        {
            if (bEntity is not CharacterRuntimeData character)
                return false;

            return JobIds.Contains(character.JobId);
        }
    }

    public class RequiredJobBaseBEC : ARequiredJobBaseBEC, IBattleEntityCriterium
    {
        public bool Evaluate(ServerBattleEntity bEntity)
        {
            if (bEntity is not CharacterRuntimeData character)
                return false;

            foreach(JobId jobId in JobIds)
            {
                if (character.JobId.IsAdvancementOf(jobId))
                    return true;
            }

            return false;
        }
    }

    public class BelowHpThresholdPercentBEC : ABelowHpThresholdPercentBEC, IBattleEntityCriterium
    {
        public bool Evaluate(ServerBattleEntity bEntity)
        {
            return bEntity.CurrentHp / bEntity.MaxHp.Total <= Percentage;
        }
    }

    public class RaceBEC : ARaceBEC, IBattleEntityCriterium
    {
        public bool Evaluate(ServerBattleEntity bEntity)
        {
            if (bEntity == null)
                return false;
            return Races.Contains(bEntity.Race);
        }
    }

    public class DefaultWeaponTypeBEC : ADefaultWeaponTypeBEC, IBattleEntityCriterium
    {
        public bool Evaluate(ServerBattleEntity bEntity)
        {
            EquipmentType type = bEntity.GetDefaultWeaponType(out EquipmentSlot _, out bool isTwoHanded);
            return type.HasFlag(WeaponType) && isTwoHanded == TwoHanded;
        }
    }

    public class EquipSlotsAreAllTypesBEC : AEquipSlotsAreAllTypesBEC, IBattleEntityCriterium
    {
        public bool Evaluate(ServerBattleEntity bEntity)
        {
            if (bEntity is not CharacterRuntimeData character)
                return false;

            if (!AllowPartialMatch)
            {
                EquipmentSlot groupedSlots = character.EquipSet.GetGroupedSlots(TargetSlots);
                if (groupedSlots != TargetSlots)
                    return false;
            }

            foreach (EquipmentSlot slotToCheck in new EquipmentSlotIterator(TargetSlots))
            {
                EquippableItemType type = character.EquipSet.GetItemType(slotToCheck);
                if (TargetTypes == EquipmentType.Unarmed && type == null)
                    continue; // pass

                if (!type?.EquipmentType.HasFlag(TargetTypes) == true)
                    return false;
            }

            return true;
        }
    }

    public class SlotsAreGroupedBEC : ASlotsAreGroupedBEC, IBattleEntityCriterium
    {
        public bool Evaluate(ServerBattleEntity bEntity)
        {
            if (bEntity is not CharacterRuntimeData character)
                return false;

            EquipmentSlot groupedSlots = character.EquipSet.GetGroupedSlots(TargetSlots);
            if(AllowPartialMatch)
            {
                return groupedSlots.HasFlag(TargetSlots);
            }
            else
            {
                return groupedSlots == TargetSlots;
            }
        }
    }

    public interface IBattleEntityCriterium
    {
        public int Id { get; }
        public bool ReadParams(string[] parts);
        public bool Evaluate(ServerBattleEntity bEntity);
        public string Serialize();
    }

    public static class BECHelper
    {
        public static ABattleEntityCriterium IdResolver(int id)
        {
            return id switch
            {
                1 => new MinimumLevelBEC(),
                2 => new RequiredJobsExactBEC(),
                3 => new RequiredJobBaseBEC(),
                4 => new BelowHpThresholdPercentBEC(),
                5 => new RaceBEC(),
                6 => new DefaultWeaponTypeBEC(),
                7 => new EquipSlotsAreAllTypesBEC(),
                8 => new SlotsAreGroupedBEC(),
                _ => null
            };
        }

        public static List<IBattleEntityCriterium> ParseCriteriumList(string listString)
        {
            return BecConditionParser.ParseCriteriumList<IBattleEntityCriterium>(listString, IdResolver);
        }
    }
}
