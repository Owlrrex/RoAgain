using OwlLogging;
using System.Collections.Generic;
using UnityEngine;

namespace Shared
{
    /// <summary>
    /// A Criterium that a BattleEntity can either fulfill or not fulfill.
    /// </summary>
    public abstract class ABattleEntityCriterium
    {
        public abstract int Id { get; }

        /// <summary>
        /// Writes this Criterium's Parameters from the given string data
        /// </summary>
        /// <param name="parts">strings that contain the parameters. parts[0] is the CriteriumId</param>
        /// <returns>Were parameters parsed successfully or not?</returns>
        public abstract bool ReadParams(string[] parts);

        /// <summary>
        /// Strongly recommend using BECParser.FormatParams() inside this function to ensure a correctly formatted string.
        /// </summary>
        /// <returns>String representation of this Criterium</returns>
        public abstract string Serialize();
    }

    public abstract class AMinimumLevelBEC : ABattleEntityCriterium
    {
        public override int Id => 1;

        public int MinimumLevel;

        public override bool ReadParams(string[] parts)
        {
            return int.TryParse(parts[1], out MinimumLevel);
        }

        public override string Serialize()
        {
            return BecConditionParser.FormatParams(Id, MinimumLevel);
        }
    }

    public abstract class ARequiredJobsExactBEC : ABattleEntityCriterium
    {
        public override int Id => 2;

        public HashSet<JobId> JobIds = new();

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
            return BecConditionParser.FormatParams(Id, JobIds);
        }
    }

    public abstract class ARequiredJobBaseBEC : ABattleEntityCriterium
    {
        public override int Id => 3;
        public HashSet<JobId> JobIds = new();

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
            return BecConditionParser.FormatParams(Id, JobIds);
        }
    }

    public abstract class ABelowHpThresholdPercentBEC : ABattleEntityCriterium
    {
        public override int Id => 4;
        public float Percentage;

        public override bool ReadParams(string[] parts)
        {
            return float.TryParse(parts[1], out Percentage);
        }

        public override string Serialize()
        {
            return BecConditionParser.FormatParams(Id, Percentage);
        }
    }

    public abstract class ARaceBEC : ABattleEntityCriterium
    {
        public override int Id => 5;
        public HashSet<EntityRace> Races = new();

        public override bool ReadParams(string[] parts)
        {
            bool success = true;
            for (int i = 1; i < parts.Length; i++)
            {
                success |= int.TryParse(parts[i], out int jobIdInt);
                Races.Add((EntityRace)jobIdInt);
            }
            return success;
        }

        public override string Serialize()
        {
            return BecConditionParser.FormatParams(Id, Races);
        }
    }

    public abstract class ADefaultWeaponTypeBEC : ABattleEntityCriterium
    {
        public override int Id => 6;
        public EquipmentType WeaponType;
        public bool TwoHanded;

        public override bool ReadParams(string[] parts)
        {
            if (!int.TryParse(parts[1], out int typeInt))
                return false;
            WeaponType = (EquipmentType)typeInt;
            return bool.TryParse(parts[2], out TwoHanded);
        }

        public override string Serialize()
        {
            return BecConditionParser.FormatParams(Id, (int)WeaponType, TwoHanded);
        }
    }

    /// <summary>
    /// Checks if all items in the given slot/s have all the given TargetType/s
    /// </summary>
    public abstract class AEquipSlotsAreAllTypesBEC : ABattleEntityCriterium
    {
        public override int Id => 7;

        public EquipmentType TargetTypes;
        public EquipmentSlot TargetSlots;
        /// <summary>
        /// True = The equipped Item/s may occupy more than just the target slot/s, as long as they have the desired type 
        /// False = Item has to occupy exactly the given target slot/s.
        /// </summary>
        public bool AllowPartialMatch;

        public override bool ReadParams(string[] parts)
        {
            if (!int.TryParse(parts[1], out int typeInt))
                return false;
            TargetTypes = (EquipmentType)typeInt;
            if (!int.TryParse(parts[2], out int slotInt))
                return false;
            TargetSlots = (EquipmentSlot)slotInt;
            return bool.TryParse(parts[3], out AllowPartialMatch);
        }

        public override string Serialize()
        {
            return BecConditionParser.FormatParams(Id, (int)TargetTypes, (int)TargetSlots, AllowPartialMatch);
        }
    }

    /// <summary>
    /// Checks whether or not all given slots are covered by the same item
    /// </summary>
    public abstract class ASlotsAreGroupedBEC : ABattleEntityCriterium
    {
        public override int Id => 8;

        public EquipmentSlot TargetSlots;

        /// <summary>
        /// True = The item may cover other slots beyond TargetSlots as well
        /// False = The item must cover exactly TargetSlots
        /// </summary>
        public bool AllowPartialMatch;

        public override bool ReadParams(string[] parts)
        {
            if (!int.TryParse(parts[1], out int slotsInt))
                return false;
            TargetSlots = (EquipmentSlot)slotsInt;
            if (!bool.TryParse(parts[2], out AllowPartialMatch))
                return false;
            return true;
        }

        public override string Serialize()
        {
            return BecConditionParser.FormatParams(Id, (int)TargetSlots, AllowPartialMatch);
        }
    }

    public static class BecConditionParser
    {
        public delegate ABattleEntityCriterium BecIdResolver(int id);
        public delegate ACondition ConditionIdResolver(int id);

        public static string FormatParams(int id, params object[] parameters)
        {
            string result = $"({id}";
            foreach (object param in parameters)
            {
                result += $",{param}";
            }
            result += ")";
            return result;
        }

        public static List<I> ParseCriteriumList<I>(string listString, BecIdResolver idResolver) where I : class
        {
            if (string.IsNullOrEmpty(listString))
                return null;

            if (!listString.StartsWith("(") || !listString.EndsWith(")"))
                return null;

            if (idResolver == null)
                return null;

            List<I> criteriums = new();
            string[] parts = listString.Split(')', System.StringSplitOptions.RemoveEmptyEntries); // Remove empty entries, most importantly the potential whitespace-string after the last closing bracket
            foreach (string part in parts)
            {
                I newCriterium = CreateCriterium(part[1..], idResolver) as I; // Skip the leading '(', the ')' at the end has already been removed by Trim()
                if (newCriterium != null)
                    criteriums.Add(newCriterium);
            }
            return criteriums;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str">Expected Format example: "12,14,15"</param>
        /// <param name="idResolver"></param>
        /// <returns></returns>
        public static ABattleEntityCriterium CreateCriterium(string str, BecIdResolver idResolver)
        {
            if (str == null
                || string.IsNullOrWhiteSpace(str))
                return null;

            if (idResolver == null)
                return null;

            string[] parts = str.Split(',');
            if (parts.Length < 2)
            {
                return null;
            }

            if (!int.TryParse(parts[0], out int id))
            {
                return null;
            }

            ABattleEntityCriterium newCriterium = idResolver(id);

            if (newCriterium == null)
            {
                return null;
            }

            newCriterium.ReadParams(parts);
            return newCriterium;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str">Expected Format example: "(3,12)"</param>
        /// <param name="idResolver"></param>
        /// <returns></returns>
        public static ACondition ParseCondition(string str, ConditionIdResolver idResolver)
        {
            if (string.IsNullOrEmpty(str))
                return null;

            if (idResolver == null)
                return null;

            str = str.TrimStart('(');
            str = str.TrimEnd(')');

            string[] parts = str.Split(',');
            if (parts.Length < 2)
            {
                return null;
            }

            if (!int.TryParse(parts[0], out int idInt))
                return null;

            ACondition newCondition = idResolver(idInt);

            if (newCondition == null)
                return null;

            newCondition.ReadParams(parts);
            return newCondition;
        }

        public static string Serialize(ConditionalStat condStat)
        {
            return $"({condStat.Condition.Serialize()},{JsonUtility.ToJson(condStat.Value, false)})";
        }

        public static ConditionalStat ParseCondStat(string str, ConditionIdResolver idResolver)
        {
            if (string.IsNullOrEmpty(str))
                return null;

            if (!str.StartsWith("(") || !str.EndsWith(")"))
                return null;

            int endOfConditionIdx = str.IndexOf(')');
            string statStr = str[(endOfConditionIdx + 2)..^1];
            string conditionStr = str[1..(endOfConditionIdx + 1)];

            int separatorIdx = str.IndexOf(",");
            return new()
            {
                Value = JsonUtility.FromJson<Stat>(statStr),
                Condition = ParseCondition(conditionStr, idResolver)
            };
        }
    }

    /// <summary>
    /// A condition describes a check performed against certain objects that're more flexible than what BattleEntityCriteriums allow
    /// An example would be to choose a specific BattleEntity in some data object that contains many of them to apply a BEC to.
    /// A condition should always be backed by one (or more) BECs.
    /// </summary>
    public abstract class ACondition
    {
        public abstract int Id { get; }

        /// <summary>
        /// Writes this Conditions's Parameters from the given string data
        /// </summary>
        /// <param name="parts">strings that contain the parameters. parts[0] is the ConditionId</param>
        /// <returns>Were parameters parsed successfully or not?</returns>
        public abstract bool ReadParams(string[] parts);

        /// <summary>
        /// Strongly recommend using BECParser.FormatParams() inside this function to ensure a correctly formatted string.
        /// </summary>
        /// <returns>String representation of this Condition</returns>
        public abstract string Serialize();

        /// <param name="other">The Condition to compare to</param>
        /// <returns>Can the numeric values of this & other Condition be stored in the same Condition object? Do they describe the same Condition?</returns>
        public abstract bool IsMergeable(ACondition other);
    }

    public abstract class ATargetRaceCondition : ACondition
    {
        protected ARaceBEC _bec;
        public HashSet<EntityRace> Races
        {
            get => _bec.Races;
            set => _bec.Races = value;
        }

        public override int Id => 1;

        public override bool ReadParams(string[] parts)
        {
            return _bec.ReadParams(parts);
        }

        public override string Serialize()
        {
            return BecConditionParser.FormatParams(Id, Races);
        }

        public override bool IsMergeable(ACondition other)
        {
            return other is ATargetRaceCondition trc && trc.Races.SetEquals(Races);
        }
    }

    public abstract class AUserAllSlotsAreTypesCondition : ACondition
    {
        protected AEquipSlotsAreAllTypesBEC _bec;
        public EquipmentType EquipmentType { get => _bec.TargetTypes; set => _bec.TargetTypes = value; }
        public EquipmentSlot Slot { get => _bec.TargetSlots; set => _bec.TargetSlots = value; }
        public bool AllowPartialMatch { get => _bec.AllowPartialMatch; set => _bec.AllowPartialMatch = value; }
        public override int Id => 2;

        public override bool ReadParams(string[] parts)
        {
            return _bec.ReadParams(parts);
        }

        public override string Serialize()
        {
            return BecConditionParser.FormatParams(Id, EquipmentType, Slot, AllowPartialMatch);
        }

        public override bool IsMergeable(ACondition other)
        {
            return other is AUserAllSlotsAreTypesCondition wtc
                && wtc.EquipmentType == EquipmentType
                && wtc.Slot == Slot
                && wtc.AllowPartialMatch == AllowPartialMatch;
        }
    }

    public abstract class ABelowHpThresholdPercentCondition : ACondition
    {
        protected ABelowHpThresholdPercentBEC _bec;
        public float Percentage
        {
            get => _bec.Percentage;
            set => _bec.Percentage = value;
        }

        public override int Id => 3;

        public override bool IsMergeable(ACondition other)
        {
            return other is ABelowHpThresholdPercentCondition otherTyped && otherTyped.Percentage == Percentage;
        }

        public override bool ReadParams(string[] parts)
        {
            return _bec.ReadParams(parts);
        }

        public override string Serialize()
        {
            return BecConditionParser.FormatParams(Id, Percentage);
        }
    }

    public abstract class ASkillIdCondition : ACondition
    {
        public SkillId SkillId;

        public override int Id => 4;

        public override bool IsMergeable(ACondition other)
        {
            return other is ASkillIdCondition otherTyped && otherTyped.SkillId == SkillId;
        }

        public override bool ReadParams(string[] parts)
        {
            if (!int.TryParse(parts[1], out int skillInt))
                return false;
            SkillId = (SkillId)skillInt;
            return true;
        }

        public override string Serialize()
        {
            return BecConditionParser.FormatParams(Id, (int)SkillId);
        }
    }

    public abstract class AAttackWeaponTypeCondition : ACondition
    {
        public override int Id => 5;

        // Used only if no AttackParams are given
        protected ADefaultWeaponTypeBEC _bec;

        public bool IsTwoHanded { get => _bec.TwoHanded; set => _bec.TwoHanded = value; }
        public EquipmentType WeaponType { get => _bec.WeaponType; set => _bec.WeaponType = value; }

        public override bool IsMergeable(ACondition other)
        {
            return other is AAttackWeaponTypeCondition otherTyped && otherTyped.WeaponType == WeaponType && otherTyped.IsTwoHanded == IsTwoHanded;
        }

        public override bool ReadParams(string[] parts)
        {
            return _bec.ReadParams(parts);
        }

        public override string Serialize()
        {
            return BecConditionParser.FormatParams(Id, WeaponType, IsTwoHanded);
        }
    }

    public class ConditionalStat
    {
        public Stat Value = new();
        public ACondition Condition;
    }
}


