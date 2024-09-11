using Shared;
using OwlLogging;
using System.Collections.Generic;

namespace Server
{
    public class JobModule
    {
        public int Initialize()
        {
            return 0;
        }

        public void InitJob(CharacterRuntimeData character)
        {
            UpdateJobBonuses(character, -1, character.JobLvl.Value);

            // Apply known passive skills
            foreach (KeyValuePair<SkillId, int> kvp in character.PermanentSkills)
            {
                if (!kvp.Key.IsPassive())
                    continue;

                APassiveSkillImpl impl = character.GetMapInstance().SkillModule.GetPassiveSkillImpl(kvp.Key);
                impl?.Apply(character, kvp.Value);
            }
        }

        public void ChangeJob(CharacterRuntimeData character, JobId newJobId, bool wipeLearnedSkills)
        {
            if(character == null)
            {
                OwlLogger.LogError("Can't change job of null character!", GameComponent.Other);
                return;
            }

            if(character.JobId == newJobId)
            {
                OwlLogger.LogWarning($"Can't change Job of character {character.Id} to job {newJobId} - same job!", GameComponent.Other);
                return;
            }

            if(character.JobId != JobId.Unknown)
            {
                UpdateJobBonuses(character, character.JobLvl.Value, -1);

                // TODO: Clear buffs

                if(wipeLearnedSkills)
                {
                    character.GetMapInstance().SkillModule.SkillReset(character);
                }
                else
                {
                    // Unapply all passives in the old tree - they will be reapplied by the InitJob() later
                    foreach (KeyValuePair<SkillId, int> kvp in character.PermanentSkills)
                    {
                        if (!kvp.Key.IsPassive())
                            continue;

                        APassiveSkillImpl impl = character.GetMapInstance().SkillModule.GetPassiveSkillImpl(kvp.Key);
                        impl?.Unapply(character, kvp.Value);
                    }
                }
            }

            List<SkillTreeEntry> oldSkills = SkillTreeDatabase.GetSkillTreeForJob(character.JobId);
            character.JobId = newJobId;
            character.CurrentJobExp = 0;
            character.JobLvl.Value = 1;

            InitJob(character);

            // Skill Tree packets (only for skills that don't exist anymore, or are new to the tree)
            List<SkillTreeEntry> newSkills = SkillTreeDatabase.GetSkillTreeForJob(newJobId);

            List<SkillTreeEntry> removedSkills = new();

            foreach(SkillTreeEntry oldEntry in oldSkills)
            {
                bool stillExists = false;
                foreach(SkillTreeEntry newEntry in newSkills)
                {
                    if (newEntry.Skill == oldEntry.Skill)
                    {
                        newSkills.Remove(newEntry);
                        stillExists = true;
                        break;
                    }
                }
                if(!stillExists)
                    removedSkills.Add(oldEntry);
            }
            // Newskills has now been trimmed of all entries that were in the oldSkills list
            foreach(SkillTreeEntry removedEntry in removedSkills)
            {
                // Always forget skills that aren't in the new tree anymore
                if(character.PermanentSkills.ContainsKey(removedEntry.Skill))
                {
                    character.RemainingSkillPoints += character.PermanentSkills[removedEntry.Skill];
                    character.PermanentSkills.Remove(removedEntry.Skill);
                }

                character.Connection.Send(new SkillTreeRemovePacket()
                {
                    SkillId = removedEntry.Skill
                });
            }

            foreach(SkillTreeEntry newEntry in newSkills)
            {
                character.Connection.Send(newEntry.ToPacket(character));
            }

            foreach (CharacterRuntimeData observer in character.GetMapInstance().Grid.GetObserversSquare<CharacterRuntimeData>(character.Coordinates))
            {
                observer.NetworkQueue.GridEntityDataUpdate(character);
            }

            character.JobChanged?.Invoke(character);
        }

        // TODO: Move to better class
        public void StatReset(CharacterRuntimeData character)
        {
            if(character == null)
            {
                OwlLogger.LogError("Can't stat reset null character!", GameComponent.Other);
                return;
            }

            // TODO: Make starting-statpoints depend on config-value
            int desiredStatPoints = character.TotalStatPointsAt(character.BaseLvl.Value);

            character.Str.SetBase(1, false);
            character.Vit.SetBase(1, false);
            character.Agi.SetBase(1, false);
            character.Int.SetBase(1, false);
            character.Dex.SetBase(1, false);
            character.Luk.SetBase(1, false);
            character.CalculateAllStats();
            
            character.RemainingStatPoints = desiredStatPoints;

            character.Connection.Send(new StatPointUpdatePacket() { NewRemaining = desiredStatPoints });
        }

        public void UpdateJobBonuses(CharacterRuntimeData character, int oldLevel, int newLevel)
        {
            if(character == null)
            {
                OwlLogger.LogError("Can't update JobBonuses on null character!", GameComponent.Other);
                return;
            }

            if(oldLevel <= 0 && newLevel <= 0)
            {
                OwlLogger.LogError($"Can't update JobBonuses with invalid levels provided: old = {oldLevel} new = {newLevel}!", GameComponent.Other);
                return;
            }

            if(oldLevel > 0)
            {
                int strBonusOld = JobDatabase.GetJobData(character.JobId).GetJobBonusAtLevel(EntityPropertyType.Str, oldLevel);
                int agiBonusOld = JobDatabase.GetJobData(character.JobId).GetJobBonusAtLevel(EntityPropertyType.Agi, oldLevel);
                int vitBonusOld = JobDatabase.GetJobData(character.JobId).GetJobBonusAtLevel(EntityPropertyType.Vit, oldLevel);
                int intBonusOld = JobDatabase.GetJobData(character.JobId).GetJobBonusAtLevel(EntityPropertyType.Int, oldLevel);
                int dexBonusOld = JobDatabase.GetJobData(character.JobId).GetJobBonusAtLevel(EntityPropertyType.Dex, oldLevel);
                int lukBonusOld = JobDatabase.GetJobData(character.JobId).GetJobBonusAtLevel(EntityPropertyType.Luk, oldLevel);

                character.Str.ModifyAdd(-strBonusOld, false);
                character.Agi.ModifyAdd(-agiBonusOld, false);
                character.Vit.ModifyAdd(-vitBonusOld, false);
                character.Int.ModifyAdd(-intBonusOld, false);
                character.Dex.ModifyAdd(-dexBonusOld, false);
                character.Luk.ModifyAdd(-lukBonusOld, false);
            }

            if(newLevel > 0)
            {
                int strBonusNew = JobDatabase.GetJobData(character.JobId).GetJobBonusAtLevel(EntityPropertyType.Str, newLevel);
                int agiBonusNew = JobDatabase.GetJobData(character.JobId).GetJobBonusAtLevel(EntityPropertyType.Agi, newLevel);
                int vitBonusNew = JobDatabase.GetJobData(character.JobId).GetJobBonusAtLevel(EntityPropertyType.Vit, newLevel);
                int intBonusNew = JobDatabase.GetJobData(character.JobId).GetJobBonusAtLevel(EntityPropertyType.Int, newLevel);
                int dexBonusNew = JobDatabase.GetJobData(character.JobId).GetJobBonusAtLevel(EntityPropertyType.Dex, newLevel);
                int lukBonusNew = JobDatabase.GetJobData(character.JobId).GetJobBonusAtLevel(EntityPropertyType.Luk, newLevel);

                character.Str.ModifyAdd(strBonusNew, false);
                character.Agi.ModifyAdd(agiBonusNew, false);
                character.Vit.ModifyAdd(vitBonusNew, false);
                character.Int.ModifyAdd(intBonusNew, false);
                character.Dex.ModifyAdd(dexBonusNew, false);
                character.Luk.ModifyAdd(lukBonusNew, false);
            }

            character.CalculateAllStats();
        }

        public void Shutdown()
        {

        }
    }
}