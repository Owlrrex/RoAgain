using OwlLogging;
using Shared;
using System.Collections.Generic;
using UnityEngine;

namespace Server
{
    public class CharacterRuntimeData : ServerBattleEntity
    {
        public ClientConnection Connection;
        public ServerMapInstance MapInstance;
        public NetworkQueue NetworkQueue;

        public string AccountId;

        public WatchableProperty<JobId, EntityPropertyType> JobId = new(EntityPropertyType.JobId);
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

        // Inventory reference
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
        public readonly StatFloat PerfectFlee = new();
        public readonly Stat WeightLimit = new();

        public bool IsTranscendent;

        public string SaveMapId = string.Empty;
        public Vector2Int SaveCoords = GridData.INVALID_COORDS;

        public CharacterRuntimeData(ClientConnection connection, int id)
        {
            if(connection == null)
            {
                OwlLogger.LogError("Can't create CharacterRuntimeData with null connection!", GameComponent.Character);
            }

            if (id <= 0)
            {
                OwlLogger.LogError($"Can't create CharacterRuntimeDAta with id {id}", GameComponent.Character);
            }

            Connection = connection;
            Id = id;

            NetworkQueue = new();
            NetworkQueue.Initialize(Connection);
        }

        public CharacterRuntimeData(ClientConnection connection, int charId, string accountId, int baseLvl, JobId jobId, int jobLvl,
            int str, int agi, int vit, int intelligence, int dex, int luk) : this(connection, charId)
        {
            AccountId = accountId;
            BaseLvl.Value = baseLvl;
            JobId.Value = jobId;
            JobLvl.Value = jobLvl;
            Str.SetBase(str);
            Agi.SetBase(agi);
            Vit.SetBase(vit);
            Int.SetBase(intelligence);
            Dex.SetBase(dex);
            Luk.SetBase(luk);

            CalculateAllStats();

            CurrentHp = MaxHp.Total;
            CurrentSp = MaxSp.Total;

            StrIncreaseCost = StatIncreaseCurve(Str.Base);
            AgiIncreaseCost = StatIncreaseCurve(Agi.Base);
            VitIncreaseCost = StatIncreaseCurve(Vit.Base);
            IntIncreaseCost = StatIncreaseCurve(Int.Base);
            DexIncreaseCost = StatIncreaseCurve(Dex.Base);
            LukIncreaseCost = StatIncreaseCurve(Luk.Base);

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

        private int StatIncreaseCurve(int input)
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
            StrIncreaseCost = StatIncreaseCurve(Str.Base);
            NetworkQueue.StatUpdate(EntityPropertyType.Str, Str);
        }

        private void OnAgiChanged(Stat agi)
        {
            CalculateAnimationSpeed();
            CalculateFlee();
            AgiIncreaseCost = StatIncreaseCurve(Agi.Base);
            NetworkQueue.StatUpdate(EntityPropertyType.Agi, Agi);
        }

        private void OnVitChanged(Stat vit)
        {
            CalculateSoftDef();
            CalculateHp();
            CalculateHpReg(); // Have to call it here in case Sp don't change, but vit does have a direct effect
            VitIncreaseCost = StatIncreaseCurve(Vit.Base);
            NetworkQueue.StatUpdate(EntityPropertyType.Vit, Vit);
        }

        private void OnIntChanged(Stat intelligence)
        {
            CalculateMatk();
            CalculateSoftMDef();
            CalculateSp();
            CalculateSpReg(); // Have to call it here in case Sp don't change, but int does have a direct effect
            IntIncreaseCost = StatIncreaseCurve(Int.Base);
            NetworkQueue.StatUpdate(EntityPropertyType.Int, Int);

        }

        private void OnDexChanged(Stat dex)
        {
            CalculateAtk();
            CalculateHit();
            CalculateCastTime();
            CalculateAnimationSpeed();
            DexIncreaseCost = StatIncreaseCurve(Dex.Base);
            NetworkQueue.StatUpdate(EntityPropertyType.Dex, Dex);

        }

        private void OnLukChanged(Stat luk)
        {
            CalculateAtk();
            CalculateCrit();
            CalculateCritShield();
            CalculatePerfectFlee();
            LukIncreaseCost = StatIncreaseCurve(Luk.Base);
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
            MeleeAtkMax.SetBase((int)(baseMelee * 1.1f));
            MeleeAtkMin.SetBase((int)(baseMelee * 0.9f));

            float dexRanged = Dex.Total + (Dex.Total * Dex.Total / 100.0f);
            float strRanged = Str.Total * 0.2f;
            float baseRanged = dexRanged + strRanged + lukAtk;
            // TODO: replace Static variation with dex-based variation
            RangedAtkMax.SetBase((int)(baseRanged * 1.1f));
            RangedAtkMin.SetBase((int)(baseRanged * 0.9f));

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

        public override SkillFailReason CanExecuteSkill(ASkillExecution skill)
        {
            if (GetSkillLevel(skill.SkillId) < skill.SkillLvl)
                return SkillFailReason.NotLearned;

            return base.CanExecuteSkill(skill);
        }

        public RemoteCharacterDataPacket ToRemoteDataPacket()
        {
            return new RemoteCharacterDataPacket()
            {
                UnitId = Id,
                UnitName = Name,
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
                JobId = JobId.Value,
                Gender = Gender.Value,
            };
        }

        public LocalCharacterDataPacket ToLocalDataPacket()
        {
            LocalCharacterDataPacket packet = new()
            {
                UnitId = Id,
                UnitName = Name,
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
                JobId = JobId.Value,
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
                AtkMax = MeleeAtkMax
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

