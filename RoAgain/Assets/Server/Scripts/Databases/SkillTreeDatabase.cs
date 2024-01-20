using OwlLogging;
using Shared;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Server
{
    [Serializable]
    public class SkillTreeRequirementEntry
    {
        public SkillId RequirementId;
        public int RequirementLevel;
    }

    [Serializable]
    public class SkillTreeEntry
    {
        public SkillId Skill;
        public int MaxLevel;
        public bool CanPointLearn = true;
        public SkillCategory Category;
        public int Tier;
        public int Position;
        public List<SkillTreeRequirementEntry> Requirements;

        public SkillTreeEntryPacket ToPacket(CharacterRuntimeData character)
        {
            SkillTreeEntryPacket packet = new()
            {
                SkillId = Skill,
                MaxSkillLvl = MaxLevel,
                CanPointLearn = CanPointLearn,
                Category = Category,
                Position = Position,
                Tier = Tier,
            };

            if (character.PermanentSkills.ContainsKey(Skill))
            {
                packet.LearnedSkillLvl = character.PermanentSkills[Skill];
            }

            // TODO: Also check Temporary Skills here?

            if (Requirements.Count > 0)
            {
                packet.Requirement1Id = Requirements[0].RequirementId;
                packet.Requirement1Level = Requirements[0].RequirementLevel;
            }

            if (Requirements.Count > 1)
            {
                packet.Requirement2Id = Requirements[1].RequirementId;
                packet.Requirement2Level = Requirements[1].RequirementLevel;
            }

            if (Requirements.Count > 2)
            {
                packet.Requirement3Id = Requirements[2].RequirementId;
                packet.Requirement3Level = Requirements[2].RequirementLevel;
            }

            if(Requirements.Count > 3)
            {
                OwlLogger.LogError($"Requirements for skill {Skill} don't fit into packet - network model or skill tree database need to be adjusted!", GameComponent.Other);
            }

            return packet;
        }
    }

    [Serializable]
    public class SkillTreeDatabaseEntry
    {
        public JobId Job;
        public List<SkillTreeEntry> SkillTree;
    }

    [CreateAssetMenu(fileName = "SkillTreeDatabase", menuName = "ScriptableObjects/SkillTreeDatabase")]
    public class SkillTreeDatabase : ScriptableObject
    {
        private static SkillTreeDatabase _instance;

        [SerializeField]
        private SkillTreeDatabaseEntry[] _entries;

        private Dictionary<JobId, List<SkillTreeEntry>> _data;

        public void Register()
        {
            if (_entries == null)
            {
                OwlLogger.LogError($"Can't register SkillTreeDatabase with null entries!", GameComponent.Other);
                return;
            }

            if (_instance != null)
            {
                OwlLogger.LogError("Duplicate SkillTreeDatabase!", GameComponent.Other);
                return;
            }

            if (_data == null)
            {
                _data = new();
                foreach (SkillTreeDatabaseEntry entry in _entries)
                {
                    _data.Add(entry.Job, entry.SkillTree);
                }
            }

            // TODO: Build tree for every class & keep them in memory for more efficient runtime access?
            // Dictionary<JobId, List<SkillTreeEntry> runtimeDb = new();
            // For each JobId jobid in JobId (except unknown):
            //   runtimeDb[jobId] = GetSkillTreeForJob(jobId);

            _instance = this;
        }

        public static List<SkillTreeEntry> GetSkillTreeForJobExclusive(JobId job)
        {
            if (_instance == null)
            {
                OwlLogger.LogError("Tried to get SkillTree for Job before SkillTreeDatabase was available", GameComponent.Other);
                return null;
            }

            if (job == JobId.Unknown)
            {
                OwlLogger.LogError("Tried to get SkillTree for Unknown Job!", GameComponent.Other);
                return null;
            }

            if (!_instance._data.ContainsKey(job))
            {
                OwlLogger.LogError($"Tried to get SkillTree for Job {job} that wasn't found in Database!", GameComponent.Other);
                return null;
            }

            return _instance._data[job];
        }

        public static List<SkillTreeEntry> GetSkillTreeForJob(JobId job)
        {
            if (_instance == null)
            {
                OwlLogger.LogError("Tried to get SkillTree for Job before SkillTreeDatabase was available", GameComponent.Other);
                return null;
            }

            if (job == JobId.Unknown)
            {
                OwlLogger.LogError("Tried to get SkillTree for Unknown Job!", GameComponent.Other);
                return null;
            }

            if (!_instance._data.ContainsKey(job))
            {
                OwlLogger.LogError($"Tried to get SkillTree for Job {job} that wasn't found in Database!", GameComponent.Other);
                return null;
            }

            // TODO: Caching/reusing lists?
            List<SkillTreeEntry> skillTree = new();

            // Everyone has novice skills
            skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.Novice));

            if (job.IsAdvancementOf(JobId.Swordman))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.Swordman));
            if (job.IsAdvancementOf(JobId.Mage))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.Mage));
            if (job.IsAdvancementOf(JobId.Thief))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.Thief));
            if (job.IsAdvancementOf(JobId.Archer))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.Archer));
            if (job.IsAdvancementOf(JobId.Acolyte))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.Acolyte));
            if (job.IsAdvancementOf(JobId.Merchant))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.Merchant));

            if (job.IsAdvancementOf(JobId.Knight))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.Knight));
            if (job.IsAdvancementOf(JobId.Crusader))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.Crusader));
            if (job.IsAdvancementOf(JobId.Wizard))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.Wizard));
            if (job.IsAdvancementOf(JobId.Sage))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.Sage));
            if (job.IsAdvancementOf(JobId.Assassin))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.Assassin));
            if (job.IsAdvancementOf(JobId.Rogue))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.Rogue));
            if (job.IsAdvancementOf(JobId.Hunter))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.Hunter));
            if (job.IsAdvancementOf(JobId.Bard))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.Bard));
            if (job.IsAdvancementOf(JobId.Dancer))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.Dancer));
            if (job.IsAdvancementOf(JobId.Priest))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.Priest));
            if (job.IsAdvancementOf(JobId.Monk))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.Monk));
            if (job.IsAdvancementOf(JobId.Blacksmith))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.Blacksmith));
            if (job.IsAdvancementOf(JobId.Alchemist))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.Alchemist));

            if (job.IsAdvancementOf(JobId.LordKnight))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.LordKnight));
            if (job.IsAdvancementOf(JobId.Paladin))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.Paladin));
            if (job.IsAdvancementOf(JobId.HighWizard))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.HighWizard));
            if (job.IsAdvancementOf(JobId.Professor))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.Professor));
            if (job.IsAdvancementOf(JobId.AssassinCross))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.AssassinCross));
            if (job.IsAdvancementOf(JobId.Stalker))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.Stalker));
            if (job.IsAdvancementOf(JobId.Sniper))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.Sniper));
            if (job.IsAdvancementOf(JobId.Minstrel))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.Minstrel));
            if (job.IsAdvancementOf(JobId.Gypsy))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.Gypsy));
            if (job.IsAdvancementOf(JobId.HighPriest))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.HighPriest));
            if (job.IsAdvancementOf(JobId.Champion))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.Champion));
            if (job.IsAdvancementOf(JobId.Whitesmith))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.Whitesmith));
            if (job.IsAdvancementOf(JobId.Creator))
                skillTree.AddRange(GetSkillTreeForJobExclusive(JobId.Creator));

            return skillTree;
        }

        public static bool CharacterCanLearnSkill(CharacterRuntimeData character, SkillId skill)
        {
            if (character == null)
            {
                OwlLogger.LogError("Can't check null character for skill learning!", GameComponent.Other);
                return false;
            }
                
            
            if(skill == SkillId.Unknown)
            {
                OwlLogger.LogError("Skill Unknown can never be learned!", GameComponent.Other);
                return false;
            }
                

            if(_instance == null)
            {
                OwlLogger.LogError("Tried to get SkillTree for Job before SkillTreeDatabase was available", GameComponent.Other);
                return false;
            }

            foreach (SkillTreeEntry entry in _instance._data[character.JobId.Value])
            {
                if (entry.Skill != skill)
                    continue;

                if (entry.Requirements.Count == 0)
                    return true;

                foreach (SkillTreeRequirementEntry requirement in entry.Requirements)
                {
                    if (character.PermanentSkills.ContainsKey(requirement.RequirementId)
                        && character.PermanentSkills[requirement.RequirementId] >= requirement.RequirementLevel)
                        return true;
                }
            }

            return false;
        }
    }
}

