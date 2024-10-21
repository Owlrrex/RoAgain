using OwlLogging;
using Shared;
using System;
using System.Collections.Generic;

namespace Server
{
    public class ExperienceModule
    {
        public int Initialize()
        {            
            return 0;
        }

        public void OnMobDeath(BattleEntity victim)
        {
            if (victim is not Mob mob)
                return;

            foreach (KeyValuePair<int, float> kvp in mob.BattleContributions)
            {
                if (!AServer.Instance.TryGetLoggedInCharacterByEntityId(kvp.Key, out var contributor))
                {
                    // TODO: distinguish between "entity not found" and "contributions from non-character"?
                    // Non-Characters can currently not gain any exp, despite BaseLvls being supported for BattleEntities
                    continue;
                }

                float ratio = kvp.Value / mob.MaxHp.Total;
                bool anyExpChanged = false;

                if (contributor.BaseLvl.Value < GetMaxBaseLevel())
                {
                    int gainedBaseExp = (int)(mob.BaseExpReward * ratio);

                    int newBaseExp = contributor.CurrentBaseExp + gainedBaseExp;
                    while (newBaseExp >= contributor.RequiredBaseExp)
                    {
                        newBaseExp -= contributor.RequiredBaseExp;
                        LevelUpBase(contributor, 1);
                        if(contributor.BaseLvl.Value >= GetMaxBaseLevel())
                        {
                            newBaseExp = 0;
                            break;
                        }
                    }

                    contributor.CurrentBaseExp = newBaseExp;
                    anyExpChanged = true;
                }

                if (contributor.JobLvl.Value < GetMaxJobLevel(contributor.JobId))
                {
                    int gainedJobExp = (int)(mob.JobExpReward * ratio);

                    int newJobExp = contributor.CurrentJobExp + gainedJobExp;

                    while (newJobExp >= contributor.RequiredJobExp)
                    {
                        newJobExp -= contributor.RequiredJobExp;
                        LevelUpJob(contributor, 1);
                        if (contributor.JobLvl.Value >= GetMaxJobLevel(contributor.JobId))
                        {
                            newJobExp = 0;
                            break;
                        }
                    }
                    contributor.CurrentJobExp = newJobExp;
                    anyExpChanged = true;
                }

                if(anyExpChanged)
                {
                    contributor.NetworkQueue.ExpUpdate(contributor);
                }
            }
        }

        public void LevelUpBase(ServerBattleEntity bEntity, int levelChange)
        {
            MapInstance map = bEntity.GetMapInstance();
            if (map == null)
            {
                OwlLogger.LogError($"Expsystem can't find map {bEntity.MapId}!", GameComponent.Battle);
                return;
            }

            int oldLevel = bEntity.BaseLvl.Value;
            int targetLevel = Math.Clamp(oldLevel + levelChange, 1, GetMaxBaseLevel());
            int levelDiff = targetLevel - oldLevel;

            if (levelDiff == 0)
                return;

            bEntity.BaseLvl.Value = targetLevel;
            // Any Non-player Stat-changes go here
            map.BattleModule.ChangeHp(bEntity, bEntity.MaxHp.Total, bEntity);
            map.BattleModule.ChangeSp(bEntity, bEntity.MaxSp.Total);

            if (bEntity is CharacterRuntimeData character)
            {
                int statPointDiff;
                if (levelDiff > 0)
                    statPointDiff = character.StatPointsGainedFromTo(oldLevel, bEntity.BaseLvl.Value);
                else
                    statPointDiff = -character.StatPointsGainedFromTo(bEntity.BaseLvl.Value, oldLevel);

                character.RemainingStatPoints = Math.Max(character.RemainingStatPoints + statPointDiff, 0);
                character.RequiredBaseExp = GetRequiredBaseExpOnLevel(character.BaseLvl.Value, character.IsTranscendent);
                character.CurrentBaseExp = 0;
                // Send characterdata update since many stats change with BaseLvl: Hp, Sp, Hit, Flee, statpoints
                character.NetworkQueue.GridEntityDataUpdate(character);
            }

            foreach (CharacterRuntimeData observer in map.Grid.GetObserversSquare<CharacterRuntimeData>(bEntity.Coordinates))
            {
                observer.NetworkQueue.BaseLevelUp(bEntity);
            }
        }

        public void LevelUpJob(CharacterRuntimeData character, int levelChange)
        {
            MapInstance map = character.GetMapInstance();
            if (map == null)
            {
                OwlLogger.LogError($"Expsystem can't find map {character.MapId}!", GameComponent.Battle);
                return;
            }

            int oldLevel = character.JobLvl.Value;
            int targetLevel = Math.Clamp(oldLevel + levelChange, 1, GetMaxJobLevel(character.JobId));
            int levelDiff = targetLevel - oldLevel;
            if (levelDiff == 0)
                return;

            character.JobLvl.Value = targetLevel;
            character.RemainingSkillPoints = Math.Max(character.RemainingSkillPoints + levelDiff, 0);
            UpdateJobBonuses(character, oldLevel);
            character.RequiredJobExp = GetRequiredJobExpOnLevel(character.JobLvl.Value, character.JobId);
            character.CurrentJobExp = 0;

            // This shouldn't be necessary - Stat updates are sent when recalculating job bonuses, and skillpoints & joblevel have their own packet
            //// Send characterdata update since several stats can change with JobLvl: Stat bonuses, Skillpoints
            //character.NetworkQueue.GridEntityDataUpdate(character);

            foreach (CharacterRuntimeData observer in map.Grid.GetObserversSquare<CharacterRuntimeData>(character.Coordinates))
            {
                observer.NetworkQueue.JobLevelUp(character);
            }
            character.Connection.Send(new SkillPointUpdatePacket() { RemainingSkillPoints = character.RemainingSkillPoints });
        }

        public int GetRequiredBaseExpOnLevel(int currentLevel, bool isTrans)
        {
            if (currentLevel >= GetMaxBaseLevel())
                return 9999;

            return currentLevel * 10;
        }

        public int GetRequiredJobExpOnLevel(int currentLevel, JobId jobId)
        {
            if (currentLevel >= GetMaxJobLevel(jobId))
                return 9999;

            return currentLevel * 5;
        }

        private void UpdateJobBonuses(CharacterRuntimeData character, int previousJobLevel)
        {
            int joblvl = character.JobLvl.Value;

            AServer.Instance.JobModule.UpdateJobBonuses(character, previousJobLevel, joblvl);
        }

        // TODO: Better place for this?
        private int GetMaxJobLevel(JobId job)
        {
            int lvl = job switch
            {
                JobId.Novice => 10,
                JobId.Swordman => 50,
                JobId.Mage => 50,
                JobId.Acolyte => 50,
                JobId.Thief => 50,
                JobId.Archer => 50,
                JobId.Merchant => 50,
                JobId.Taekwon => 50,
                JobId.Gunslinger => 70,
                JobId.Ninja => 70,
                JobId.SuperNovice => 99,
                JobId.HighNovice => 10,
                JobId.HighSwordman => 50,
                JobId.HighMage => 50,
                JobId.HighAcolyte => 50,
                JobId.HighThief => 50,
                JobId.HighArcher => 50,
                JobId.HighMerchant => 50,
                JobId.Knight => 50,
                JobId.Crusader => 50,
                JobId.Wizard => 50,
                JobId.Sage => 50,
                JobId.Priest => 50,
                JobId.Monk => 50,
                JobId.Assassin => 50,
                JobId.Rogue => 50,
                JobId.Hunter => 50,
                JobId.Bard => 50,
                JobId.Dancer => 50,
                JobId.Blacksmith => 50,
                JobId.Alchemist => 50,
                JobId.StarGladiator => 50,
                JobId.SoulLinker => 50,
                JobId.LordKnight => 70,
                JobId.Paladin => 70,
                JobId.HighWizard => 70,
                JobId.Professor => 70,
                JobId.HighPriest => 70,
                JobId.Champion => 70,
                JobId.AssassinCross => 70,
                JobId.Stalker => 70,
                JobId.Sniper => 70,
                JobId.Minstrel => 70,
                JobId.Gypsy => 70,
                JobId.Whitesmith => 70,
                JobId.Creator => 70,
                _ => -1
            };

            if(lvl == -1)
                OwlLogger.LogError($"Can't find Max JobLevel for unhandled job {job}!", GameComponent.Battle);

            return lvl;
        }

        private int GetMaxBaseLevel()
        {
            return 99;
        }
    }
}

