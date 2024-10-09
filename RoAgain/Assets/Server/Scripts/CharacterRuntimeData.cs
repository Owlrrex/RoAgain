using OwlLogging;
using Shared;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Server
{
    public class CharacterRuntimeData : ServerBattleEntity
    {
        public ClientConnection Connection;
        // Cached reference removed until needed for optimization
        // public ServerMapInstance MapInstance;
        public NetworkQueue NetworkQueue;

        public string AccountId;
        public int CharacterId;

        public JobId JobId;
        public Action<CharacterRuntimeData> JobChanged;
        public WatchableProperty<int, EntityPropertyType> JobLvl = new(EntityPropertyType.JobLvl);
        public WatchableProperty<int, EntityPropertyType> Gender = new(EntityPropertyType.Gender);

        // these watchable? Or are packets handled by ExpSystem?
        public int CurrentBaseExp;
        public int RequiredBaseExp;
        public int CurrentJobExp;
        public int RequiredJobExp;

        // Skill list
        public Dictionary<SkillId, int> PermanentSkills = new();
        public Dictionary<SkillId, int> TemporarySkills = new();

        // Should this be a direct memory reference? Is there a point to a character being loaded without their inventory?
        // TODO: Persistence & adding to packet
        public int InventoryId;
        // Equip reference
        // Cosmetic references (if not contained in Equipment)
        
        public int RemainingSkillPoints;
        public int RemainingStatPoints;

        public int StrIncreaseCost;
        public int AgiIncreaseCost;
        public int VitIncreaseCost;
        public int IntIncreaseCost;
        public int DexIncreaseCost;
        public int LukIncreaseCost;

        public readonly StatFloat CastTime = new();
        public readonly Stat CritDamage = new();
        public readonly Stat WeightLimit = new();
        public int CurrentWeight;

        public bool IsTranscendent;

        public string SaveMapId = string.Empty;
        public Coordinate SaveCoords = GridData.INVALID_COORDS;

        public Dictionary<EntityPropertyType, List<ConditionalStat>> ConditionalStats;

        public void AddConditionalStat(EntityPropertyType type, ConditionalStat stat)
        {
            ConditionalStats ??= new();

            if(!ConditionalStats.ContainsKey(type))
            {
                ConditionalStats.Add(type, new List<ConditionalStat>());
                ConditionalStats[type].Add(stat);
            }
            else
            {
                bool found = false;
                List<ConditionalStat> sameStatList = ConditionalStats[type];
                foreach(ConditionalStat otherStat in sameStatList)
                {
                    if(stat.Condition.IsMergeable(otherStat.Condition))
                    {
                        otherStat.Value += stat.Value;
                        found = true;
                        break;
                    }
                }

                if(!found)
                {
                    sameStatList.Add(stat);
                }
            }

            // Maybe broadcast event about "conditional stat changed" here
        }

        public void RemoveConditionalStat(EntityPropertyType type, ConditionalStat stat)
        {
            if(ConditionalStats == null
                || !ConditionalStats.ContainsKey(type))
            {
                OwlLogger.LogError($"Can't remove conditional stat of type {type} that's not present!", GameComponent.Other);
                return;
            }

            ConditionalStat foundStat = null;
            List<ConditionalStat> sameTypeStats = ConditionalStats[type];
            foreach(ConditionalStat otherStat in sameTypeStats)
            {
                if(otherStat.Condition.IsMergeable(stat.Condition))
                {
                    otherStat.Value -= stat.Value;
                    foundStat = otherStat;
                    break;
                }
            }

            if(foundStat == null)
            {
                OwlLogger.LogError($"Can't remaove conditional stat of type {type}: No mergeable stat found!", GameComponent.Other);
                return;
            }

            if(foundStat.Value == 0.0f)
            {
                sameTypeStats.Remove(foundStat);
            }

            if (sameTypeStats.Count == 0)
            {
                ConditionalStats.Remove(type);
            }

            // Maybe broadcast "Conditional Stats changed" event here
        }

        public void ApplyModToStatAdd(EntityPropertyType type, ref Stat stat, AttackParams attackParams)
        {
            if(ConditionalStats?.TryGetValue(type, out var statList) == true)
            {
                foreach(ConditionalStat cStat in statList)
                {
                    if (cStat.Condition.Evaluate(attackParams))
                        stat.ModifyAdd((int)cStat.Value);
                }
            }
        }

        public void ApplyModToStatMult(EntityPropertyType type, ref Stat stat, AttackParams attackParams)
        {
            if (ConditionalStats?.TryGetValue(type, out var statList) == true)
            {
                foreach (ConditionalStat cStat in statList)
                {
                    if (cStat.Condition.Evaluate(attackParams))
                        stat.ModifyMult(cStat.Value);
                }
            }
        }

        public void ApplyModToStatFloatAdd(EntityPropertyType type, ref StatFloat stat, AttackParams attackParams)
        {
            if (ConditionalStats?.TryGetValue(type, out var statList) == true)
            {
                foreach (ConditionalStat cStat in statList)
                {
                    if (cStat.Condition.Evaluate(attackParams))
                        stat.ModifyAdd(cStat.Value);
                }
            }
        }

        public void ApplyModToStatFloatMult(EntityPropertyType type, ref StatFloat stat, AttackParams attackParams)
        {
            if (ConditionalStats?.TryGetValue(type, out var statList) == true)
            {
                foreach (ConditionalStat cStat in statList)
                {
                    if (cStat.Condition.Evaluate(attackParams))
                        stat.ModifyMult(cStat.Value);
                }
            }
        }

        public CharacterRuntimeData(ClientConnection connection, CharacterPersistenceData persData, ExperienceModule expModule)
            : base(persData.Coordinates, LocalizedStringId.INVALID, -1, 6, 0, 0, -1)
        {
            if (connection == null)
            {
                OwlLogger.LogError("Can't create CharacterRuntimeData with null connection!", GameComponent.Character);
            }

            Connection = connection;

            NetworkQueue = new();
            NetworkQueue.Initialize(Connection);

            CharacterId = persData.CharacterId;
            AccountId = persData.AccountId;
            Gender.Value = persData.Gender;

            BaseLvl.Value = persData.BaseLevel;
            JobId = persData.JobId;
            JobLvl.Value = persData.JobLevel;
            Str.SetBase(persData.Str);
            Agi.SetBase(persData.Agi);
            Vit.SetBase(persData.Vit);
            Int.SetBase(persData.Int);
            Dex.SetBase(persData.Dex);
            Luk.SetBase(persData.Luk);

            // Values that are always the same (Size?!) and aren't saved
            HpRegenTime = 10;
            SpRegenTime = 5;
            Race = EntityRace.Humanoid;
            Size = EntitySize.Medium; // TODO: Calculation based on mounts
            Element = EntityElement.Neutral1;

            // Fields from the persistentData
            NameOverride = persData.Name;
            MapId = persData.MapId;
            RequiredBaseExp = expModule.GetRequiredBaseExpOnLevel(persData.BaseLevel, false);
            CurrentBaseExp = persData.BaseExp;
            RequiredJobExp = expModule.GetRequiredJobExpOnLevel(persData.JobLevel, persData.JobId);
            CurrentJobExp = persData.JobExp;
            RemainingStatPoints = persData.StatPoints;
            CurrentHp = persData.CurrentHP;
            CurrentSp = persData.CurrentSP;
            RemainingSkillPoints = persData.SkillPoints;
            SaveMapId = persData.SaveMapId;
            SaveCoords = persData.SaveCoords;

            InventoryId = persData.InventoryId;

            CalculateAllStats();

            CurrentHp = MaxHp.Total;
            CurrentSp = MaxSp.Total;

            StrIncreaseCost = GetStatIncreaseCost(Str.Base);
            AgiIncreaseCost = GetStatIncreaseCost(Agi.Base);
            VitIncreaseCost = GetStatIncreaseCost(Vit.Base);
            IntIncreaseCost = GetStatIncreaseCost(Int.Base);
            DexIncreaseCost = GetStatIncreaseCost(Dex.Base);
            LukIncreaseCost = GetStatIncreaseCost(Luk.Base);

            BaseLvl.Changed += OnBaseLvlChanged;
            Str.ValueChanged += OnStrChanged;
            Agi.ValueChanged += OnAgiChanged;
            Vit.ValueChanged += OnVitChanged;
            Int.ValueChanged += OnIntChanged;
            Dex.ValueChanged += OnDexChanged;
            Luk.ValueChanged += OnLukChanged;
            MaxHp.ValueChanged += OnMaxHpChanged;
            MaxSp.ValueChanged += OnMaxSpChanged;
            MeleeAtkMin.ValueChanged += OnMeleeAtkMinChanged;
            MeleeAtkMax.ValueChanged += OnMeleeAtkMaxChanged;
            RangedAtkMin.ValueChanged += OnRangedAtkMinChanged;
            RangedAtkMax.ValueChanged += OnRangedAtkMaxChanged;
            MatkMin.ValueChanged += OnMatkMinChanged;
            MatkMax.ValueChanged += OnMatkMaxChanged;
            HardDef.ValueChanged += OnHardDefChanged;
            SoftDef.ValueChanged += OnSoftDefChanged;
            HardMDef.ValueChanged += OnHardMdefChanged;
            SoftMDef.ValueChanged += OnSoftMdefChanged;
            Crit.ValueChanged += OnCritChanged;
            Flee.ValueChanged += OnFleeChanged;
            PerfectFlee.ValueChanged += OnPerfectFleeChanged;
            Hit.ValueChanged += OnHitChanged;
            WeightLimit.ValueChanged += OnWeightLimitChanged;
        }

        private int GetStatIncreaseCost(int input)
        {
            return Mathf.FloorToInt((input - 1) / 10.0f) + 2;
        }

        public int StatPointsGainedAtLevelUpTo(int newLevel)
        {
            return Mathf.FloorToInt((newLevel - 1) / 5) + 3;
        }

        public int StatPointsGainedFromTo(int from, int to)
        {
            if (from >= to)
                return 0;

            int sum = 0;
            for(int i = from+1; i <= to; i++)
            {
                sum += StatPointsGainedAtLevelUpTo(i);
            }
            return sum;
        }

        public int TotalStatPointsAt(int level)
        {
            // TODO: Make starting-statpoints depend on config-value
            int startingStatPoints = 44;
            int statPoints = startingStatPoints;
            for(int i = 2; i <= level; i++)
            {
                statPoints += StatPointsGainedAtLevelUpTo(i);
            }
            return statPoints;
        }

        private void OnBaseLvlChanged(EntityPropertyType _)
        {
            CalculateHp();
            CalculateSp();
            CalculateHit();
            CalculateFlee();
        }

        private void OnStrChanged(Stat str)
        {
            CalculateAtk();
            CalculateWeightLimit();
            StrIncreaseCost = GetStatIncreaseCost(Str.Base);
            NetworkQueue.StatUpdate(EntityPropertyType.Str, Str);
        }

        private void OnAgiChanged(Stat agi)
        {
            CalculateAnimationSpeed();
            CalculateFlee();
            AgiIncreaseCost = GetStatIncreaseCost(Agi.Base);
            NetworkQueue.StatUpdate(EntityPropertyType.Agi, Agi);
        }

        private void OnVitChanged(Stat vit)
        {
            CalculateSoftDef();
            CalculateHp();
            CalculateHpReg(); // Have to call it here in case Sp don't change, but vit does have a direct effect
            VitIncreaseCost = GetStatIncreaseCost(Vit.Base);
            NetworkQueue.StatUpdate(EntityPropertyType.Vit, Vit);
        }

        private void OnIntChanged(Stat intelligence)
        {
            CalculateMatk();
            CalculateSoftMDef();
            CalculateSp();
            CalculateSpReg(); // Have to call it here in case Sp don't change, but int does have a direct effect
            IntIncreaseCost = GetStatIncreaseCost(Int.Base);
            NetworkQueue.StatUpdate(EntityPropertyType.Int, Int);

        }

        private void OnDexChanged(Stat dex)
        {
            CalculateAtk();
            CalculateHit();
            CalculateCastTime();
            CalculateAnimationSpeed();
            DexIncreaseCost = GetStatIncreaseCost(Dex.Base);
            NetworkQueue.StatUpdate(EntityPropertyType.Dex, Dex);

        }

        private void OnLukChanged(Stat luk)
        {
            CalculateAtk();
            CalculateCrit();
            CalculateCritShield();
            CalculatePerfectFlee();
            // TODO: Status resistances
            LukIncreaseCost = GetStatIncreaseCost(Luk.Base);
            NetworkQueue.StatUpdate(EntityPropertyType.Luk, Luk);
        }

        private void OnMaxHpChanged(Stat maxHp)
        {
            CalculateHpReg();
            NetworkQueue.StatUpdate(EntityPropertyType.MaxHp, MaxHp);
        }

        private void OnMaxSpChanged(Stat maxSp)
        {
            CalculateSpReg();
            NetworkQueue.StatUpdate(EntityPropertyType.MaxSp, MaxSp);
        }

        private void OnMeleeAtkMinChanged(Stat atk)
        {
            // TODO: If weapon is melee
            NetworkQueue.StatUpdate(EntityPropertyType.CurrentAtkMin, atk);
        }

        private void OnMeleeAtkMaxChanged(Stat atk)
        {
            // TODO: If weapon is melee
            NetworkQueue.StatUpdate(EntityPropertyType.CurrentAtkMax, atk);
        }

        private void OnRangedAtkMinChanged(Stat atk)
        {
            // TOOD: If weapon is ranged
            //NetworkQueue.StatUpdate(EntityPropertyType.CurrentAtkMin, atk);
        }

        private void OnRangedAtkMaxChanged(Stat atk)
        {
            // TOOD: If weapon is ranged
            //NetworkQueue.StatUpdate(EntityPropertyType.CurrentAtkMax, atk);
        }

        private void OnMatkMinChanged(Stat matk)
        {
            NetworkQueue.StatUpdate(EntityPropertyType.MatkMin, matk);
        }

        private void OnMatkMaxChanged(Stat matk)
        {
            NetworkQueue.StatUpdate(EntityPropertyType.MatkMax, matk);
        }
        
        // TODO: Figure out Animationspeed system
        //private void OnAnimationSpeedChanged()
        //{
        //
        //}

        private void OnHardDefChanged(StatFloat hardDef)
        {
            NetworkQueue.StatUpdate(EntityPropertyType.HardDef, hardDef);
        }

        private void OnSoftDefChanged(Stat def)
        {
            NetworkQueue.StatUpdate(EntityPropertyType.SoftDef, def);
        }

        private void OnHardMdefChanged(StatFloat hardMdef)
        {
            NetworkQueue.StatUpdate(EntityPropertyType.HardMDef, hardMdef);
        }

        private void OnSoftMdefChanged(Stat mdef)
        {
            NetworkQueue.StatUpdate(EntityPropertyType.SoftMDef, mdef);
        }

        private void OnCritChanged(StatFloat crit)
        {
            NetworkQueue.StatUpdate(EntityPropertyType.Crit, crit);
        }

        private void OnFleeChanged(Stat flee)
        {
            NetworkQueue.StatUpdate(EntityPropertyType.Flee, flee);
        }

        private void OnPerfectFleeChanged(StatFloat pFlee)
        {
            NetworkQueue.StatUpdate(EntityPropertyType.PerfectFlee, pFlee);
        }

        private void OnHitChanged(Stat hit)
        {
            NetworkQueue.StatUpdate(EntityPropertyType.Hit, hit);
        }

        private void OnWeightLimitChanged(Stat weight)
        {
            NetworkQueue.StatUpdate(EntityPropertyType.WeightLimit, weight);
        }

        public void CalculateAllStats()
        {
            Str.Recalculate();
            Vit.Recalculate();
            Agi.Recalculate();
            Int.Recalculate();
            Dex.Recalculate();
            Luk.Recalculate();

            CalculateHp();
            CalculateSp();
            CalculateHpReg();
            CalculateSpReg();
            CalculateAtk();
            CalculateMatk();
            CalculateCastTime();
            CalculateAnimationSpeed();
            CalculateSoftDef();
            CalculateSoftMDef();
            CalculateCrit();
            CalculateCritShield();
            CalculateFlee();
            CalculatePerfectFlee();
            CalculateHit();
            CalculateWeightLimit();
        }

        public void CalculateHp()
        {
            // TODO: Create Job-Db with these values
            float jobValueA = 0.7f;
            float jobValueB = 5.0f;

            // Formula: https://irowiki.org/classic/Max_HP
            // How I changed it: 
            // 1) Remove "round" instruction
            // 2) pull jobValueA out of the sum (constant factor)
            // 3) make sum start at 1 (instead of 2), then replace with "sum of natural number" formula
            float linearTerm = BaseLvl.Value * jobValueB;
            float quadraticTerm = jobValueA * BaseLvl.Value * (BaseLvl.Value+1) / 2.0f;
            float rawHP = 35 + linearTerm + quadraticTerm;
            float baseHp = rawHP * (1 + Vit.Total * 0.01f);
            if (IsTranscendent)
                baseHp *= 1.25f;
            MaxHp.SetBase((int)baseHp);

            
            if (CurrentHp > MaxHp.Total)
                CurrentHp = MaxHp.Total;

            NetworkQueue.StatUpdate(EntityPropertyType.MaxHp, MaxHp);
        }

        public void CalculateHpReg()
        {
            // TODO: Time?, Counter
            float hpbased = MaxHp.Total * 0.005f + Vit.Total * 0.2f;
            HpRegenAmount.SetBase((int)hpbased);
        }

        public void CalculateSp()
        {
            // TODO: Create Job-Db with these values
            float jobValue = 2.0f;

            // Formula https://irowiki.org/classic/Max_SP
            float raw = 10 + (BaseLvl.Value * jobValue);
            MaxSp.SetBase((int)(raw * (1 + Int.Total / 100.0f)));

            if (CurrentSp > MaxSp.Total)
                CurrentSp = MaxSp.Total;

            NetworkQueue.StatUpdate(EntityPropertyType.MaxSp, MaxSp);
        }

        public void CalculateSpReg()
        {
            // TODO: Time, Counter
            float intbased = MaxSp.Total * 0.01f;
            intbased += Int.Total / 6.0f;
            if (Int.Total >= 112)
            {
                intbased += Int.Total * 0.5f - 56;
            }
            SpRegenAmount.SetBase((int)intbased);
        }

        public void CalculateAtk()
        {
            // smoothed formulas
            float lukAtk = Luk.Total * 0.1f;
            float strMelee = Str.Total + (Str.Total * Str.Total / 100.0f);
            float dexMelee = (int)(Str.Total * 0.2f);
            float baseMelee = strMelee + dexMelee + lukAtk;

            // TODO: Explore impact of current modifier-structure on variation being set at base-level (instead of as part of skill)

            // TODO: replace Static variation with dex-based variation
            MeleeAtkMax.SetBase((int)(baseMelee * 1.2f));
            MeleeAtkMin.SetBase((int)(baseMelee * 1.0f));

            float dexRanged = Dex.Total + (Dex.Total * Dex.Total / 100.0f);
            float strRanged = Str.Total * 0.2f;
            float baseRanged = dexRanged + strRanged + lukAtk;
            // TODO: replace Static variation with dex-based variation
            RangedAtkMax.SetBase((int)(baseRanged * 1.2f));
            RangedAtkMin.SetBase((int)(baseRanged * 1.0f));

            // Original AtkDiff is only for weapon-damage (not str-damage), based on dex & weapon level
            // While the concept of "dex & higher level weapons make your damage spread less" is easy to grasp, it's poorly explained and a VERY weird formula. 
            // Notable: The Atk value on paper is the max-atk, not the average atk.
            // The min-atk is given by Dex & weapon level only (for melee) and the same expression _times 1% of atk_ for bow (= at same values, for more than 100 weapon-atk, bows will have better floor, for less worse).
            // For exceedingly high dex, a bow's weapon-atk actually becomes constant Atk*Atk/100, which is totally unintuitive
            // for melee, min-atk can never exceed the weapon's base-atk, no matter how much dex/weapon level
            // I wanna just overhaul that, to make it overall simpler. At least melee & ranged should behave similar.
            // Also, given that Str & Dex have "switched" roles for bow, should bow variance depend on str instead? reducing variance is a huge damage buff since the atk-on-paper is the max.
            // Whatever change to the variance I put in here, also consider making MATK vary by a similar degree for simplicity
        }

        public void CalculateMatk()
        {
            // Starting from the original formulas for max & min matk:
            // max = int + int²/25, min = int + int²/49
            // I formed a formula for their middle value (max+min)/2, and the distance function max - (max+min)/2, both in terms of int.
            // Results: 
            // matk(int) = int²*37/1225 + int
            // diff(int) = int²*12/1225
            // With these, we can have the original spread of matk values, but only store a single value (maybe even display atk & matk the same - both in max-min-form, or both as single value)
            // could even pre-calculate the fractions as floats & then do float-multiplication & cast to int, instead of doing int-mult & int-division. Unclear if that will be faster (since float is slower than int, i guess?) or even relevant
            MatkMin.SetBase((int)(Int.Total + Int.Total * Int.Total / 49.0f));
            MatkMax.SetBase((int)(Int.Total + Int.Total * Int.Total / 25.0f));
        }

        public void CalculateCastTime()
        {
            float fraction = Dex.Total / 150.0f;
            CastTime.SetBase(Mathf.Max(0, 1 - fraction));
        }

        public void CalculateAnimationSpeed()
        {
            // TODO: Research formula & storage format (probably "seconds")
        }

        // CalculateHardDef() not needed - base value is always 0

        public void CalculateSoftDef()
        {
            SoftDef.SetBase(Vit.Total);
        }

        // CalculateHardMDef() not needed - base value is always 0

        public void CalculateSoftMDef()
        {
            SoftMDef.SetBase(Int.Total);
        }

        public void CalculateCrit()
        {
            Crit.SetBase((Luk.Total * 0.003f) + 0.01f);
        }

        // CalculateCritDamage() not needed - base value is always 0

        public void CalculateCritShield()
        {
            CritShield.SetBase(Luk.Total * 0.002f);
        }

        public void CalculateFlee()
        {
            Flee.SetBase(BaseLvl.Value + Agi.Total);
        }

        public void CalculatePerfectFlee()
        {
            PerfectFlee.SetBase(Luk.Total * 0.001f);
        }

        public void CalculateHit()
        {
            Hit.SetBase(BaseLvl.Value + Dex.Total);
        }

        public void CalculateWeightLimit()
        {
            // TODO: Job-specific values from JobDb
            WeightLimit.SetBase(2000 + Str.Base * 20);
        }

        public bool HasSkill(SkillId id)
        {
            return PermanentSkills.ContainsKey(id) || TemporarySkills.ContainsKey(id);
        }

        public bool HasPermanentSkill(SkillId id)
        {
            return PermanentSkills.ContainsKey(id);
        }

        public int GetSkillLevel(SkillId id)
        {
            if(!HasSkill(id))
            {
                OwlLogger.LogError($"Tried to get SkillLevel for unowned Skill {id}! EntityId = {Id}", GameComponent.Character);
                return int.MinValue;
            }

            if (PermanentSkills.ContainsKey(id))
                return PermanentSkills[id];
            else if (TemporarySkills.ContainsKey(id))
                return TemporarySkills[id];

            return int.MinValue;
        }

        public override Packet ToDataPacket()
        {
            return new RemoteCharacterDataPacket()
            {
                EntityId = Id,
                CharacterName = NameOverride,
                MapId = MapId,
                Path = Path,
                PathCellIndex = PathCellIndex,
                Movespeed = Movespeed.Value,
                MovementCooldown = MovementCooldown,
                Orientation = Orientation,
                Coordinates = Coordinates,

                MaxHp = MaxHp.Total,
                Hp = CurrentHp,
                MaxSp = MaxSp.Total,
                Sp = CurrentSp,

                BaseLvl = BaseLvl.Value,
                JobId = JobId,
                Gender = Gender.Value,
            };
        }

        public LocalCharacterDataPacket ToLocalDataPacket()
        {
            LocalCharacterDataPacket packet = new()
            {
                EntityId = Id,
                CharacterId = CharacterId,
                CharacterName = NameOverride,
                MapId = MapId,
                Path = Path,
                PathCellIndex = PathCellIndex,
                Movespeed = Movespeed.Value,
                MovementCooldown = MovementCooldown,
                Orientation = Orientation,
                Coordinates = Coordinates,

                MaxHp = MaxHp.Total,
                Hp = CurrentHp,
                MaxSp = MaxSp.Total,
                Sp = CurrentSp,

                Str = Str,
                Agi = Agi,
                Vit = Vit,
                Int = Int,
                Dex = Dex,
                Luk = Luk,

                BaseLvl = BaseLvl.Value,
                JobId = JobId,
                JobLvl = JobLvl.Value,
                Gender = Gender.Value,

                MatkMin = MatkMin,
                MatkMax = MatkMax,
                HardDef = HardDef,
                SoftDef = SoftDef,
                HardMdef = HardMDef,
                SoftMdef = SoftMDef,
                Hit = Hit,
                Flee = Flee,
                PerfectFlee = PerfectFlee,
                Crit = Crit,
                AttackSpeed = GetDefaultAnimationCooldown(),
                Weightlimit = WeightLimit,
                CurrentWeight = CurrentWeight,

                RemainingSkillPoints = RemainingSkillPoints,
                RemainingStatPoints = RemainingStatPoints,

                StrIncreaseCost = StrIncreaseCost,
                AgiIncreaseCost = AgiIncreaseCost,
                VitIncreaseCost = VitIncreaseCost,
                IntIncreaseCost = IntIncreaseCost,
                DexIncreaseCost = DexIncreaseCost,
                LukIncreaseCost = LukIncreaseCost,

                CurrentBaseExp = CurrentBaseExp,
                RequiredBaseExp = RequiredBaseExp,
                CurrentJobExp = CurrentJobExp,
                RequiredJobExp = RequiredJobExp,
                
                // tmp, until ranged weapons exist.
                AtkMin = MeleeAtkMin,
                AtkMax = MeleeAtkMax,

                InventoryId = InventoryId,
            };

            //if(IsWeaponRanged())
            //{
            //    packet.MinAtk = RangedAtkMax;
            //    packet.MaxAtk = RangedAtkMax;
            //}
            //else
            //{
            //    packet.MinAtk = MeleeAtkMin;
            //    packet.MaxAtk = MeleeAtkMax;
            //}

            return packet;
        }
    }
}

