
using Shared;
using System;
using System.Collections.Generic;

namespace Client
{
    public abstract class ACharacterEntity : ClientBattleEntity
    {
        public JobId JobId;
        public Action<ACharacterEntity> JobChanged;
        public int Gender;
        // TODO: Cosmetic info
    }

    public class RemoteCharacterEntity : ACharacterEntity
    {
        public RemoteCharacterEntity(RemoteCharacterData charData)
        {
            SetData(charData);
        }

        public void SetData(RemoteCharacterData charData)
        {
            base.SetData(charData);

            BaseLvl = charData.BaseLvl;

            JobId oldJob = JobId;
            JobId = charData.JobId;
            if (JobId != oldJob)
                JobChanged?.Invoke(this);

            Gender = charData.Gender;
        }
    }

    public class LocalCharacterEntity : ACharacterEntity
    {
        public int JobLvl;

        public Stat Str;
        public Stat Agi;
        public Stat Vit;
        public Stat Int;
        public Stat Dex;
        public Stat Luk;

        public Stat AtkMin;
        public Stat AtkMax;
        public Stat MatkMin;
        public Stat MatkMax;
        public StatFloat HardDef;
        public Stat SoftDef;
        public StatFloat HardMdef;
        public Stat SoftMdef;
        public Stat Hit;
        public StatFloat PerfectHit;
        public Stat Flee;
        public StatFloat PerfectFlee;
        public StatFloat Crit;
        public float AttackSpeed; // What's the format for this gonna be? Attacks/Second? Should I use a Stat for this?
        public int RemainingStatPoints;
        public int RemainingSkillPoints;
        public Stat Weightlimit;

        public int CurrentBaseExp;
        public int RequiredBaseExp;
        public int CurrentJobExp;
        public int RequiredJobExp;

        public int StrIncreaseCost;
        public int AgiIncreaseCost;
        public int VitIncreaseCost;
        public int IntIncreaseCost;
        public int DexIncreaseCost;
        public int LukIncreaseCost;

        // TODO: Accomodate skills existing in the tree multiple times (permanent & temporary)
        public Dictionary<SkillId, SkillTreeEntry> SkillTree = new();
        public Action SkillTreeUpdated;
        // Do we still need these? Leave around for now.
        public Dictionary<SkillId, SkillTreeEntry> PermanentSkillList = new();
        public Dictionary<SkillId, SkillTreeEntry> TemporarySkillList = new();

        public LocalCharacterEntity(LocalCharacterData charData)
        {
            SetData(charData);
        }

        public void SetData(LocalCharacterData charData)
        {
            base.SetData(charData);

            BaseLvl = charData.BaseLvl;

            JobId oldJob = JobId;
            JobId = charData.JobId;
            if (JobId != oldJob)
                JobChanged?.Invoke(this);

            JobLvl = charData.JobLvl;
            Gender = charData.Gender;

            Str = charData.Str;
            Agi = charData.Agi;
            Vit = charData.Vit;
            Int = charData.Int;
            Dex = charData.Dex;
            Luk = charData.Luk;

            MatkMin = charData.MatkMin;
            MatkMax = charData.MatkMax;
            HardDef = charData.HardDef;
            SoftDef = charData.SoftDef;
            HardMdef = charData.HardMdef;
            SoftMdef = charData.SoftMdef;
            Hit = charData.Hit;
            PerfectHit = charData.PerfectHit;
            Flee = charData.Flee;
            PerfectFlee = charData.PerfectFlee;
            Crit = charData.Crit;
            //AttackSpeed = GetDefaultAnimationCooldown(); // TODO: format for this value, overriding of AnimationSpeed, etc.
            RemainingStatPoints = charData.RemainingStatPoints;
            RemainingSkillPoints = charData.RemainingSkillPoints;
            Weightlimit = charData.Weightlimit;

            AtkMin = charData.AtkMin;
            AtkMax = charData.AtkMax;

            CurrentBaseExp = charData.CurrentBaseExp;
            RequiredBaseExp = charData.RequiredBaseExp;
            CurrentJobExp = charData.CurrentJobExp;
            RequiredJobExp = charData.RequiredJobExp;

            StrIncreaseCost = charData.StrIncreaseCost;
            AgiIncreaseCost = charData.AgiIncreaseCost;
            VitIncreaseCost = charData.VitIncreaseCost;
            IntIncreaseCost = charData.IntIncreaseCost;
            DexIncreaseCost = charData.DexIncreaseCost;
            LukIncreaseCost = charData.LukIncreaseCost;
        }
    }
}
