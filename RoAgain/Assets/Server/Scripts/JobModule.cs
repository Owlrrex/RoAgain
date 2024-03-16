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
            // TODO: Apply new job bonuses, overlap with ExperienceModule.UpdateJobBonuses()

            // Apply known passive skills
            foreach (KeyValuePair<SkillId, int> kvp in character.PermanentSkills)
            {
                if (!kvp.Key.IsPassive())
                    continue;

                APassiveSkillImpl impl = character.GetMapInstance().SkillModule.GetPassiveSkillImpl(kvp.Key);
                impl.Apply(character, kvp.Value);
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
                // TODO: Unapply old job bonuses

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
                        impl.Unapply(character, kvp.Value);
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
            int desiredStatPoints = 44;
            if (character.BaseLvl.Value > 1)
                desiredStatPoints += character.StatPointsGainedFromTo(1, character.BaseLvl.Value);

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

        public void Shutdown()
        {

        }
    }
}