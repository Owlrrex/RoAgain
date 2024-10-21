using OwlLogging;
using Shared;
using System.Collections.Generic;

namespace Server
{
    // Can't move this to shared - can't use ServerBattleEntity there, and criteriums on Client-side probably also work too differently, even if they hold the same data


    public class MinimumLevelBEC : BattleEntityCriterium
    {
        public override int Id => 1;

        public int MinimumLevel;

        public override bool Evaluate(ServerBattleEntity bEntity)
        {
            return bEntity.BaseLvl.Value >= MinimumLevel;
        }

        public override bool ReadParams(string[] parts)
        {
            return int.TryParse(parts[1], out MinimumLevel);
        }

        public override string Serialize()
        {
            return BECParser.FormatParams(Id, MinimumLevel);
        }
    }

    public class RequiredJobsExactBEC : BattleEntityCriterium
    {
        public override int Id => 2;

        public HashSet<JobId> JobIds = new();

        public override bool Evaluate(ServerBattleEntity bEntity)
        {
            if (bEntity is not CharacterRuntimeData character)
                return false;

            return JobIds.Contains(character.JobId);
        }

        public override bool ReadParams(string[] parts)
        {
            bool success = true;
            for (int i = 1; i < parts.Length; i++)
            {
                success |= int.TryParse(parts[i], out int jobIdInt);
                JobIds.Add((JobId)jobIdInt);
            }
            return success;
        }

        public override string Serialize()
        {
            return BECParser.FormatParams(Id, JobIds);
        }
    }

    public class RequiredJobBaseBEC : BattleEntityCriterium
    {
        public override int Id => 3;
        public HashSet<JobId> JobIds = new();

        public override bool Evaluate(ServerBattleEntity bEntity)
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

        public override bool ReadParams(string[] parts)
        {
            bool success = true;
            for (int i = 1; i < parts.Length; i++)
            {
                success |= int.TryParse(parts[i], out int jobIdInt);
                JobIds.Add((JobId)jobIdInt);
            }
            return success;
        }

        public override string Serialize()
        {
            return BECParser.FormatParams(Id, JobIds);
        }
    }

    public abstract class BattleEntityCriterium
    {
        public abstract int Id { get; }

        /// <summary>
        /// Writes this Criterium's Parameters from the given string data
        /// </summary>
        /// <param name="parts">strings that contain the parameters. parts[0] is the CriteriumId</param>
        /// <returns>Were parameters parsed successfully or not?</returns>
        public abstract bool ReadParams(string[] parts);

        public abstract bool Evaluate(ServerBattleEntity bEntity);

        /// <summary>
        /// Strongly recommend using BECParser.FormatParams() inside this function to ensure a correctly formatted string.
        /// </summary>
        /// <returns>String representation of this Criterium</returns>
        public abstract string Serialize();
    }

    public static class BECParser
    {
        public static string FormatParams(int id, params object[] parameters)
        {
            string result = $"({id}";
            foreach (object param in parameters)
            {
                result += param.ToString();
            }
            result += ")";
            return result;
        }

        public static List<BattleEntityCriterium> ParseCriteriumList(string listString)
        {
            List<BattleEntityCriterium> criteriums = new();
            string[] parts = listString.Split(')');
            foreach (string part in parts)
            {
                if (!part.StartsWith('('))
                {
                    // LogError
                    continue;
                }

                BattleEntityCriterium newCriterium = CreateCriterium(part.TrimStart('('));
                if (newCriterium != null)
                    criteriums.Add(newCriterium);
            }
            return criteriums;
        }

        public static BattleEntityCriterium CreateCriterium(string persistentData)
        {
            // TODO: Error logging
            string[] parts = persistentData.Split(',');
            if (parts.Length < 2)
            {
                return null;
            }

            if (!int.TryParse(parts[0], out int id))
            {
                return null;
            }

            BattleEntityCriterium newCriterum = id switch
            {
                _ => null
            };

            if (newCriterum == null)
            {
                return null;
            }

            newCriterum.ReadParams(parts);
            return newCriterum;
        }
    }
}
