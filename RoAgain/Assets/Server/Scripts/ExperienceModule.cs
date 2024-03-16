using OwlLogging;
using Shared;
using System.Collections.Generic;

namespace Server
{
    public class ExperienceModule
    {
        ServerMapModule _mapModule;

        public int Initialize(ServerMapModule mapModule)
        {
            if(mapModule == null)
            {
                OwlLogger.LogError($"Can't initialize ExperienceModule with null ServerMapModule!", GameComponent.Other);
                return -1;
            }

            _mapModule = mapModule;
            
            return 0;
        }

        public void OnMobDeath(BattleEntity victim, BattleEntity killer)
        {
            if (victim is not Mob mob)
                return;

            ServerMapInstance map = _mapModule.GetMapInstance(victim.MapId);
            if(map == null)
            {
                OwlLogger.LogError($"Expsystem can't find map {victim.MapId}!", GameComponent.Battle);
                return;
            }

            foreach (KeyValuePair<int, int> kvp in mob.BattleContributions)
            {
                if (map.Grid.FindOccupant(kvp.Key) is not CharacterRuntimeData contributor)
                {
                    // TODO: distinguish between "entity not found" and "contributions from non-character"?
                    // Non-Characters can currently not gain any exp, despite BaseLvls being supported for BattleEntities
                    continue;
                }

                float ratio = kvp.Value / (float)mob.MaxHp.Total;
                bool anyExpChanged = false;

                if (!IsMaxBaseLevel(contributor.BaseLvl.Value))
                {
                    int gainedBaseExp = (int)(mob.BaseExpReward * ratio);

                    int newBaseExp = contributor.CurrentBaseExp + gainedBaseExp;
                    while (newBaseExp >= contributor.RequiredBaseExp)
                    {
                        newBaseExp -= contributor.RequiredBaseExp;
                        LevelUpBase(contributor);
                        if(IsMaxBaseLevel(contributor.BaseLvl.Value))
                        {
                            contributor.CurrentBaseExp = 0;
                            break;
                        }
                    }

                    contributor.CurrentBaseExp = newBaseExp;
                    anyExpChanged = true;
                }

                if(!IsMaxJobLevel(contributor.JobLvl.Value, contributor.JobId))
                {
                    int gainedJobExp = (int)(mob.JobExpReward * ratio);

                    int newJobExp = contributor.CurrentJobExp + gainedJobExp;

                    while (newJobExp >= contributor.RequiredJobExp)
                    {
                        newJobExp -= contributor.RequiredJobExp;
                        LevelUpJob(contributor);
                        if (IsMaxJobLevel(contributor.JobLvl.Value, contributor.JobId))
                        {
                            contributor.CurrentJobExp = 0;
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

        private void LevelUpBase(ServerBattleEntity bEntity)
        {
            ServerMapInstance map = _mapModule.GetMapInstance(bEntity.MapId);
            if (map == null)
            {
                OwlLogger.LogError($"Expsystem can't find map {bEntity.MapId}!", GameComponent.Battle);
                return;
            }

            bEntity.BaseLvl.Value += 1;
            // Non-player Stat-changes here
            map.BattleModule.ChangeHp(bEntity, bEntity.MaxHp.Total, bEntity);
            map.BattleModule.ChangeSp(bEntity, bEntity.MaxSp.Total);

            if (bEntity is CharacterRuntimeData character)
            {
                int statPointGain = character.StatPointsGainedAtLevelUpTo(bEntity.BaseLvl.Value);
                character.RemainingStatPoints += statPointGain;
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

        private void LevelUpJob(CharacterRuntimeData character)
        {
            ServerMapInstance map = _mapModule.GetMapInstance(character.MapId);
            if (map == null)
            {
                OwlLogger.LogError($"Expsystem can't find map {character.MapId}!", GameComponent.Battle);
                return;
            }

            character.JobLvl.Value += 1;
            character.RemainingSkillPoints += 1;
            UpdateJobBonuses(character, character.JobLvl.Value -1);
            character.RequiredJobExp = GetRequiredJobExpOnLevel(character.JobLvl.Value, character.JobId);
            character.CurrentJobExp = 0;

            // Send characterdata update since several stats change with JobLvl: Stat bonuses, Skillpoints
            character.NetworkQueue.GridEntityDataUpdate(character);

            foreach (CharacterRuntimeData observer in map.Grid.GetObserversSquare<CharacterRuntimeData>(character.Coordinates))
            {
                observer.NetworkQueue.JobLevelUp(character);
            }
            character.Connection.Send(new SkillPointUpdatePacket() { RemainingSkillPoints = character.RemainingSkillPoints });
        }

        public int GetRequiredBaseExpOnLevel(int currentLevel, bool isTrans)
        {
            if (IsMaxBaseLevel(currentLevel))
                return 9999;

            return currentLevel * 10;
        }

        public int GetRequiredJobExpOnLevel(int currentLevel, JobId jobId)
        {
            if (IsMaxJobLevel(currentLevel, jobId))
                return 9999;

            return currentLevel * 5;
        }

        private void UpdateJobBonuses(CharacterRuntimeData character, int previousJobLevel)
        {
            int joblvl = character.JobLvl.Value;

            // TODO: Proper JobBonus Database, also needed in JobModule.InitJob()

            int tmpBonusNew = joblvl / 5;
            int tmpBonusOld = previousJobLevel / 5;
            if (tmpBonusNew == tmpBonusOld)
                return;

            character.Str.ModifyAdd(-tmpBonusOld, false);
            character.Str.ModifyAdd(tmpBonusNew);
            character.Agi.ModifyAdd(-tmpBonusOld, false);
            character.Agi.ModifyAdd(tmpBonusNew);
            character.Vit.ModifyAdd(-tmpBonusOld, false);
            character.Vit.ModifyAdd(tmpBonusNew);
            character.Int.ModifyAdd(-tmpBonusOld, false);
            character.Int.ModifyAdd(tmpBonusNew);
            character.Dex.ModifyAdd(-tmpBonusOld, false);
            character.Dex.ModifyAdd(tmpBonusNew);
            character.Luk.ModifyAdd(-tmpBonusOld, false);
            character.Luk.ModifyAdd(tmpBonusNew);
        }

        private bool IsMaxJobLevel(int lvl, JobId job)
        {
            int limit;
            switch (job)
            {
                case JobId.Novice:
                    limit = 10;
                    break;
                case JobId.Swordman:
                case JobId.Mage:
                case JobId.Acolyte:
                case JobId.Thief:
                case JobId.Archer:
                case JobId.Merchant:
                case JobId.Taekwon:
                    limit = 50;
                    break;
                case JobId.Gunslinger:
                case JobId.Ninja:
                    limit = 70;
                    break;
                case JobId.SuperNovice:
                    limit = 99;
                    break;
                case JobId.HighNovice:
                    limit = 10;
                    break;
                case JobId.HighSwordman:
                case JobId.HighMage:
                case JobId.HighAcolyte:
                case JobId.HighThief:
                case JobId.HighArcher:
                case JobId.HighMerchant:
                    limit = 50;
                    break;
                case JobId.Knight:
                case JobId.Crusader:
                case JobId.Wizard:
                case JobId.Sage:
                case JobId.Priest:
                case JobId.Monk:
                case JobId.Assassin:
                case JobId.Rogue:
                case JobId.Hunter:
                case JobId.Bard:
                case JobId.Dancer:
                case JobId.Blacksmith:
                case JobId.Alchemist:
                case JobId.StarGladiator:
                case JobId.SoulLinker:
                    limit = 50;
                    break;
                case JobId.LordKnight:
                case JobId.Paladin:
                case JobId.HighWizard:
                case JobId.Professor:
                case JobId.HighPriest:
                case JobId.Champion:
                case JobId.AssassinCross:
                case JobId.Stalker:
                case JobId.Sniper:
                case JobId.Minstrel:
                case JobId.Gypsy:
                case JobId.Whitesmith:
                case JobId.Creator:
                    limit = 70;
                    break;
                default:
                    OwlLogger.LogError($"Can't find Max JobLevel for unhandled job {job}!", GameComponent.Battle);
                    return true;
            }

            return lvl >= limit;
        }

        private bool IsMaxBaseLevel(int lvl)
        {
            return lvl >= 99;
        }
    }
}

