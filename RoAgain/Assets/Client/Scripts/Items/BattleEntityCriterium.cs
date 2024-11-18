using OwlLogging;
using Shared;
using System;
using System.Collections.Generic;

namespace Client
{
    public class MinimumLevelBEC : AMinimumLevelBEC, IBattleEntityCriterium
    {
        public bool Evaluate(LocalCharacterEntity character)
        {
            return character.BaseLvl >= MinimumLevel;
        }

        public string ToDescription()
        {
            throw new NotImplementedException();
        }
    }

    public class RequiredJobsExactBEC : ARequiredJobsExactBEC, IBattleEntityCriterium
    {
        public bool Evaluate(LocalCharacterEntity character)
        {
            return JobIds.Contains(character.JobId);
        }

        public string ToDescription()
        {
            throw new NotImplementedException();
        }
    }

    public class RequiredJobBaseBEC : ARequiredJobBaseBEC, IBattleEntityCriterium
    {
        public bool Evaluate(LocalCharacterEntity character)
        {
            foreach (JobId jobId in JobIds)
            {
                if (character.JobId.IsAdvancementOf(jobId))
                    return true;
            }

            return false;
        }

        public string ToDescription()
        {
            throw new NotImplementedException();
        }
    }

    public class BelowHpThresholdPercentBEC : ABelowHpThresholdPercentBEC, IBattleEntityCriterium
    {
        public bool Evaluate(LocalCharacterEntity character)
        {
            return character.CurrentHp / character.MaxHp.Total <= Percentage;
        }

        public string ToDescription()
        {
            throw new NotImplementedException();
        }
    }

    public class RaceBEC : ARaceBEC, IBattleEntityCriterium
    {
        public bool Evaluate(LocalCharacterEntity character)
        {
            // Currently, race isn't stored for characterEntities clientside since they're always humanoid
            return Races.Contains(EntityRace.Humanoid);
        }

        public string ToDescription()
        {
            throw new NotImplementedException();
        }
    }

    public class DefaultWeaponTypeBEC : ADefaultWeaponTypeBEC, IBattleEntityCriterium
    {
        public bool Evaluate(LocalCharacterEntity character)
        {
            // TODO: Once client has Equipsets stored
            throw new NotImplementedException();
        }

        public string ToDescription()
        {
            throw new NotImplementedException();
        }
    }

    public class EquipSlotsAreAllTypesBEC : AEquipSlotsAreAllTypesBEC, IBattleEntityCriterium
    {
        public bool Evaluate(LocalCharacterEntity character)
        {
            // TODO: Once client has Equipsets stored
            throw new NotImplementedException();
        }

        public string ToDescription()
        {
            throw new NotImplementedException();
        }
    }

    public class SlotsAreGroupedBEC : ASlotsAreGroupedBEC, IBattleEntityCriterium
    {
        public bool Evaluate(LocalCharacterEntity character)
        {
            // TODO: Once client has Equipsets stored
            throw new NotImplementedException();
        }

        public string ToDescription()
        {
            throw new NotImplementedException();
        }
    }

    public interface IBattleEntityCriterium
    {
        public int Id { get; }
        public bool ReadParams(string[] parts);
        public bool Evaluate(LocalCharacterEntity character);

        /// <returns>A formatted string to use in UI that human-readably expresses this Criterium</returns>
        public string ToDescription();
    }

    public static class BecHelper
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

        public static ACondition ConditionIdResolver(int id)
        {
            return id switch
            {
                1 => new TargetRaceCondition(),
                2 => new UserAllSlotsAreTypesCondition(),
                3 => new BelowHpThresholdPercentCondition(),
                4 => new SkillIdCondition(),
                5 => new AttackWeaponTypeCondition(),
                _ => null
            };
        }

        public static List<IBattleEntityCriterium> ParseCriteriumList(string listString)
        {
            return BecConditionParser.ParseCriteriumList<IBattleEntityCriterium>(listString, IdResolver);
        }
    }
}

