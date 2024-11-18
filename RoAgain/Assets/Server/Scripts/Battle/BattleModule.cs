using OwlLogging;
using Shared;
using System;

namespace Server
{
    public enum AttackType
    {
        Unknown,
        Physical,
        Magical
    }

    public class AttackParams : IAutoInitPoolObject
    {
        private static readonly EntityElement[] validElements = {
            EntityElement.Unknown,
            EntityElement.Earth1,
            EntityElement.Fire1,
            EntityElement.Ghost1,
            EntityElement.Holy1,
            EntityElement.Neutral1,
            EntityElement.Poison1,
            EntityElement.Shadow1,
            EntityElement.Undead1,
            EntityElement.Water1,
            EntityElement.Wind1
        };

        public ServerSkillExecution SourceSkillExec;
        public ServerBattleEntity Source;
        // SkillFactor is per hit!
        public float SkillFactor = 1.0f;
        public AttackType AttackType;
        public EntityElement AttackElement = EntityElement.Unknown;
        public EquipmentType AttackWeaponType = EquipmentType.Unknown;
        public bool IsTwoHanded;
        public bool CanCrit = false;
        public bool CanMiss = true;
        public bool IgnoreDefense = false;
        public int ChainCount = 0;
        public Action<ServerSkillExecution> PreAttackCallback = null;
        public Action<ServerSkillExecution> PostAttackCallback = null;

        public bool IsValid()
        {
            return Source != null
                && SourceSkillExec != null
                && SkillFactor >= 0.0f
                && AttackType != AttackType.Unknown
                && Array.IndexOf(validElements, AttackElement) != -1
                // All CanCrit values are valid
                // All CanMiss values are valid
                // All IgnoreDefense values are valid
                && ChainCount >= 0;
                // All Callbacks are valid
        }

        public void Reset()
        {
            SourceSkillExec = null;
            SkillFactor = 1.0f;
            AttackType = AttackType.Unknown;
            AttackElement = EntityElement.Unknown;
            CanCrit = false;
            CanMiss = true;
            IgnoreDefense = false;
            ChainCount = 0;
            PreAttackCallback = null;
            PostAttackCallback = null;
        }

        public void InitForPhysicalSkill(ServerSkillExecution skillExec)
        {
            AttackType = AttackType.Physical;
            CanCrit = false; // because only autohits can crit by default // TODO: Config value for skill-crits? Balancing-impact!
            CanMiss = true;
            ChainCount = 0;
            IgnoreDefense = false;
            SkillFactor = skillExec.Var1 / 100.0f;
            Source = skillExec.UserTyped;
            SourceSkillExec = skillExec;
        }

        public void InitForMagicalSkill(ServerSkillExecution skillExec)
        {
            AttackType = AttackType.Magical;
            CanCrit = false;
            CanMiss = false;
            ChainCount = 0;
            IgnoreDefense = false;
            SkillFactor = skillExec.Var1 / 100.0f;
            Source = skillExec.UserTyped;
            SourceSkillExec = skillExec;
        }
    }

    public class BattleModule
    {
        private MapInstance _map;

        private int _hitFleeEqualChance;
        private bool _multiplicativeModifierStacking;

        // These make the BattleModule not thread-safe!
        private Stat battleCalcStat1 = new();
        private Stat battleCalcStat2 = new();

        public int Initialize(MapInstance mapInstance)
        {
            if (mapInstance == null)
            {
                OwlLogger.LogError("Can't initialize BattleModule with null mapInstance", GameComponent.Battle);
                return -1;
            }

            ReadConfig();

            _map = mapInstance;
            return 0;
        }

        private void ReadConfig()
        {
            if(Configuration.Instance == null)
            {
                OwlLogger.LogError("Can't read config - Config not available!", GameComponent.Battle);
                return;
            }

            string configValue = Configuration.Instance.GetMainConfig(ConfigurationKey.HitFleeEqualChance);
            if (!int.TryParse(configValue, out _hitFleeEqualChance))
            {
                int defaultValue = 80;
                OwlLogger.LogError($"Can't parse hit-flee-equal config value: {configValue}, using default value: {defaultValue}", GameComponent.Battle);
                _hitFleeEqualChance = defaultValue;
            }

            configValue = Configuration.Instance.GetMainConfig(ConfigurationKey.BattleMultiplicativeStacking);
            _multiplicativeModifierStacking = configValue == "0";
        }

        public void UpdateRegenerations(float deltaTime)
        {
            if (_map == null || _map.Grid == null)
                return;

            foreach (GridEntity entity in _map.Grid.GetAllOccupants())
            {
                if (entity is not ServerBattleEntity bEntity)
                    continue;

                if (bEntity.IsDead())
                    continue;

                if (bEntity.HpRegenAmount.Total == 0
                    && bEntity.SpRegenAmount.Total == 0)
                {
                    continue;
                }

                if(bEntity.HpRegenTime > 0)
                {
                    float hpIncreaseAmount = deltaTime;
                    if (bEntity.IsMoving())
                    {
                        if (bEntity is CharacterRuntimeData charEntity
                        && charEntity.HasSkill(SkillId.HpRecWhileMoving))
                            hpIncreaseAmount *= 0.5f;
                        else
                            hpIncreaseAmount = 0;
                    }
                    else
                    {
                        // TODO: if(IsSitting(bEntity)) hpIncreaseAmount *= 2; // Make sitting regen faster, not higher amount
                    }
                    bEntity.HpRegenCounter += hpIncreaseAmount;
                    if (bEntity.HpRegenCounter >= bEntity.HpRegenTime)
                    {
                        ChangeHp(bEntity, bEntity.HpRegenAmount.Total, bEntity);
                        bEntity.HpRegenCounter -= bEntity.HpRegenTime;
                    }
                }

                if(bEntity.SpRegenTime > 0)
                {
                    float spIncreaseAmount = deltaTime;
                    // TODO: if(IsSitting(bEntity)) spIncreaseAmount *= 2; // Make sitting regen faster, not higher amount
                    bEntity.SpRegenCounter += spIncreaseAmount;
                    if (bEntity.SpRegenCounter >= bEntity.SpRegenTime)
                    {
                        ChangeSp(bEntity, bEntity.SpRegenAmount.Total);
                        bEntity.SpRegenCounter -= bEntity.SpRegenTime;
                    }
                }
            }
        }

        // skillRatioPercent is given in percent: 100 = 100%
        // skillRatioPercent is per Hit!
        public int StandardPhysicalAttack(ServerSkillExecution skillExec, int skillRatioPercent, EntityElement overrideElement = EntityElement.Unknown, int hitCount = 0)
        {
            AttackParams parameters = AutoInitResourcePool<AttackParams>.Acquire();

            parameters.InitForPhysicalSkill(skillExec);

            parameters.SkillFactor = skillRatioPercent / 100.0f;
            parameters.AttackElement = overrideElement;
            parameters.ChainCount = hitCount;

            int result = PerformAttack(skillExec.Target.EntityTarget as ServerBattleEntity, parameters);
            AutoInitResourcePool<AttackParams>.Return(parameters);
            return result;
        }

        // skillRatioPercent is given in percent: 100 = 100%
        // skillRatioPercent is per Hit!
        public int StandardMagicAttack(ServerSkillExecution skillExec, int skillRatioPercent, EntityElement overrideElement = EntityElement.Unknown, int hitCount = 0)
        {
            AttackParams parameters = AutoInitResourcePool<AttackParams>.Acquire();

            parameters.InitForMagicalSkill(skillExec);

            parameters.SkillFactor = skillRatioPercent / 100.0f;
            parameters.AttackElement = overrideElement;
            parameters.ChainCount = hitCount;

            int result = PerformAttack(skillExec.Target.EntityTarget as ServerBattleEntity, parameters);
            AutoInitResourcePool<AttackParams>.Return(parameters);
            return result;
        }

        // Return values:
        // 3 = Crit
        // 2 = Perfect Dodge
        // 1 = natural Miss
        // 0 = hit
        // -1 = invalid Parameters
        public int PerformAttack(ServerBattleEntity target, AttackParams parameters)
        {
            if (parameters == null || !parameters.IsValid())
            {
                OwlLogger.LogError($"Tried to perform attack at target {target.Id} with invalid Parameters!", GameComponent.Battle);
                return -1;
            }

            CharacterRuntimeData charSource = parameters.Source as CharacterRuntimeData;
            CharacterRuntimeData charTarget = target as CharacterRuntimeData;

            // autofill parameters that don't have to be filled-in externally
            if (parameters.AttackElement == EntityElement.Unknown)
                parameters.AttackElement = parameters.Source.GetOffensiveElement();
            if (parameters.AttackWeaponType == EquipmentType.Unknown)
                parameters.AttackWeaponType = parameters.Source.GetDefaultWeaponType(out _, out parameters.IsTwoHanded);

            parameters.PreAttackCallback?.Invoke(parameters.SourceSkillExec);

            bool isPhysical = parameters.AttackType == AttackType.Physical;

            if (isPhysical && CanPerfectFlee(target, parameters.Source))
            {
                if (UnityEngine.Random.Range(0.0f, 1.0f) < target.PerfectFlee.Total)
                {
                    DamageTakenPacket packet = new()
                    {
                        Damage = DamageTakenPacket.DAMAGE_PDODGE,
                        EntityId = target.Id,
                        IsSpDamage = false,
                        IsCrit = false,
                    };
                    foreach (CharacterRuntimeData observer in _map.Grid.GetObserversSquare<CharacterRuntimeData>(target.Coordinates))
                    {
                        observer.Connection.Send(packet);
                    }
                    return 2;
                }
            }

            bool isCrit = false;
            if (parameters.CanCrit)
            {
                parameters.Source.Crit.CopyTo(battleCalcStat1);
                target.CritShield.CopyTo(battleCalcStat2);
                charSource?.ApplyConditionalStats(EntityPropertyType.Crit, ref battleCalcStat1, parameters);
                charTarget?.ApplyConditionalStats(EntityPropertyType.CritShield, ref battleCalcStat2, parameters);

                float critChance = battleCalcStat1.Total - battleCalcStat2.Total;

                if(critChance > 0)
                    isCrit = UnityEngine.Random.Range(0.0f, 1.0f) < critChance;
            }

            float attackPower;

            // Copy values in case parameters-object is reused between calls & not pass-by-value, since we may change these
            bool canMiss = parameters.CanMiss;
            bool ignoreDefense = parameters.IgnoreDefense;

            float maxAttack;
            float minAttack;
            if (isPhysical)
            {
                parameters.Source.CurrentAtkMax.CopyTo(battleCalcStat1);
                parameters.Source.CurrentAtkMin.CopyTo(battleCalcStat2);

                if(parameters.Source.IsRanged())
                {
                    charSource?.ApplyConditionalStats(EntityPropertyType.RangedAtkMax, ref battleCalcStat1, parameters);
                    charSource?.ApplyConditionalStats(EntityPropertyType.RangedAtkMin, ref battleCalcStat2, parameters);
                    charSource?.ApplyConditionalStats(EntityPropertyType.RangedAtkBoth, ref battleCalcStat1, parameters);
                    charSource?.ApplyConditionalStats(EntityPropertyType.RangedAtkBoth, ref battleCalcStat2, parameters);
                }
                else
                {
                    charSource?.ApplyConditionalStats(EntityPropertyType.MeleeAtkMax, ref battleCalcStat1, parameters);
                    charSource?.ApplyConditionalStats(EntityPropertyType.MeleeAtkMin, ref battleCalcStat2, parameters);
                    charSource?.ApplyConditionalStats(EntityPropertyType.MeleeAtkBoth, ref battleCalcStat1, parameters);
                    charSource?.ApplyConditionalStats(EntityPropertyType.MeleeAtkBoth, ref battleCalcStat2, parameters);
                }
            }
            else
            {
                parameters.Source.MatkMax.CopyTo(battleCalcStat1);
                parameters.Source.MatkMin.CopyTo(battleCalcStat2);

                charSource?.ApplyConditionalStats(EntityPropertyType.MatkMax, ref battleCalcStat1, parameters);
                charSource?.ApplyConditionalStats(EntityPropertyType.MatkMin, ref battleCalcStat2, parameters);
                charSource?.ApplyConditionalStats(EntityPropertyType.MatkBoth, ref battleCalcStat1, parameters);
                charSource?.ApplyConditionalStats(EntityPropertyType.MatkBoth, ref battleCalcStat2, parameters);
            }

            charSource?.ApplyConditionalStats(EntityPropertyType.CurrentAtkMax, ref battleCalcStat1, parameters);
            charSource?.ApplyConditionalStats(EntityPropertyType.CurrentAtkMin, ref battleCalcStat2, parameters);
            charSource?.ApplyConditionalStats(EntityPropertyType.CurrentAtkBoth, ref battleCalcStat1, parameters);
            charSource?.ApplyConditionalStats(EntityPropertyType.CurrentAtkBoth, ref battleCalcStat2, parameters);

            maxAttack = battleCalcStat1.Total;
            minAttack = battleCalcStat2.Total;

            if (isCrit)
            {
                // Crit: Max atk, ignore Def
                canMiss = false;
                ignoreDefense = true;
                attackPower = maxAttack;
            }
            else
            {
                attackPower = UnityEngine.Random.Range(minAttack, maxAttack + 1);
            }

            // Noncrit
            if (canMiss)
            {
                int equalHit = _hitFleeEqualChance;
                float hitChance = (parameters.Source.Hit.Total - target.Flee.Total + equalHit) / 100.0f;
                if (UnityEngine.Random.Range(0.0f, 1.0f) >= hitChance)
                {
                    // Miss
                    DamageTakenPacket packet = new()
                    {
                        Damage = DamageTakenPacket.DAMAGE_MISS,
                        EntityId = target.Id,
                        IsSpDamage = false,
                        IsCrit = false,
                    };
                    foreach (CharacterRuntimeData observer in _map.Grid.GetObserversSquare<CharacterRuntimeData>(target.Coordinates))
                    {
                        observer.Connection.Send(packet);
                    }
                    return 1;
                }
            }

            attackPower *= parameters.SkillFactor;

            float damage;
            float hardDefense;
            float softDefense;
            if (isPhysical)
            {
                hardDefense = target.HardDef.Total;
                softDefense = target.SoftDef.Total;
            }
            else
            {
                hardDefense = target.HardMDef.Total;
                softDefense = target.SoftMDef.Total;
            }

            // Hit
            if (ignoreDefense)
            {
                damage = attackPower;
            }
            else
            {
                // Ensure Defenses don't reduce damage into negative
                damage = Math.Max(attackPower * (1.0f - hardDefense) - softDefense, 0);
            }

            // Cases could be made for or against calculating DamageDealt, DamageReceived & elements before / after Soft-Def
            // Softdef first: Increased SoftDef effect when modifiers overall increase damage
            // Softdef last: Increased SoftDef effect when modifiers overall decrease damage
            float modifier = 1.0f;
            if(charSource?.ConditionalStats?.TryGetValue(EntityPropertyType.DamageDealt, out var dmgList) == true)
            {
                foreach(ConditionalStat stat in dmgList)
                {
                    if (!((ICondition)stat.Condition).Evaluate(parameters))
                        continue;

                    if (_multiplicativeModifierStacking)
                        modifier *= 1 + stat.Value.ModifiersMult;
                    else
                        modifier += stat.Value.ModifiersMult;
                }
            }

            if(charTarget?.ConditionalStats?.TryGetValue(EntityPropertyType.DamageReceived, out var defList) == true)
            {
                float reductionMod = 1.0f;
                foreach(ConditionalStat stat in defList)
                {
                    if (!((ICondition)stat.Condition).Evaluate(parameters))
                        continue;

                    if(_multiplicativeModifierStacking)
                        reductionMod /= 1 + stat.Value.ModifiersMult;
                    else
                        reductionMod -= stat.Value.ModifiersMult;

                    modifier *= reductionMod;
                }
            }

            // In RO, the elemental damage bonus multiplies in at the very end, multiplicative with all other effects.
            // I think that's not too complicated, and doesn't need to be changed,
            // but it's worth noting that this _could_ also be changed via the above config value
            // That would make elemental resistances interact rather oddly with DamageReceived-mods (75% Elemental Resist + 30% DamageReduction = -5% damage absorbed)
            EntityElement attackElement = parameters.AttackElement;
            EntityElement defElement = target.GetDefensiveElement();
            modifier *= GetMultiplierForElementCombination(attackElement, defElement);

            //TODO: Is this how we want Weapon-size modifiers to work?
            if(isPhysical)
                modifier *= GetSizeMultiplierForWeaponType(parameters.AttackWeaponType, target.Size, parameters.IsTwoHanded);

            damage *= modifier;

            int damageInt = (int)damage;

            if (damageInt == 0)
                return 0;

            if(damageInt < 0)
            {
                // TODO: Handle absorb
            }

            // TODO: Make configurable whether or not Soft-Def gets applied before or after multiplying bit HitCount
            if (parameters.ChainCount > 1)
                damageInt *= parameters.ChainCount;

            parameters.PostAttackCallback?.Invoke(parameters.SourceSkillExec);

            if (isPhysical)
                DealPhysicalHitDamage(parameters.Source, target, damageInt, isCrit, parameters.ChainCount);
            else
                DealMagicalHitDamage(parameters.Source, target, damageInt, isCrit, parameters.ChainCount);

            if (isCrit)
                return 3;
            else
                return 0;
        }

        private bool CanPerfectFlee(ServerBattleEntity source, ServerBattleEntity target)
        {
            // TODO: Config-value that allows Monsters to perfect-flee
            return target is CharacterRuntimeData;
        }

        private float GetMultiplierForElementCombination(EntityElement attack, EntityElement def)
        {
            float modifier = ElementsDatabase.GetMultiplierForElements(attack, def);
            if (modifier <= -10.0f)
            {
                OwlLogger.LogError($"Can't find size multiplier for elements {attack} vs {def}", GameComponent.Battle);
                return -1.0f;
            }

            return modifier;
        }

        // TODO: Decide how to apply this best
        private float GetSizeMultiplierForWeaponType(EquipmentType weaponType, EntitySize targetSize, bool isTwoHanded)
        {
            float modifier = ASizeDatabase.GetMultiplierForWeaponAndSize(weaponType, targetSize, isTwoHanded);
            if (modifier < 0)
            {
                OwlLogger.LogError($"Can't find size multiplier for size {targetSize}, type {weaponType}", GameComponent.Battle);
                return 1.0f;
            }

            return modifier;
        }

        public int DealPhysicalHitDamage(ServerBattleEntity source, ServerBattleEntity target, int damage, bool isCrit, int chainCount)
        {
            _map.SkillModule.InterruptAnyCasts(target);
            ApplyHpDamage(damage, target, source, isCrit, chainCount);

            // TODO: Handling of "on physical damage" effects
            return 0;
        }

        public int DealMagicalHitDamage(ServerBattleEntity source, ServerBattleEntity target, int damage, bool isCrit, int chainCount)
        {
            _map.SkillModule.InterruptAnyCasts(target);
            ApplyHpDamage(damage, target, source, isCrit, chainCount);

            // TODO: Handling of "on magical damage" effects
            return 0;
        }

        private int ApplyHpDamage(int damage, ServerBattleEntity target, ServerBattleEntity source, bool isCrit, int chainCount)
        {
            if (target == null)
            {
                OwlLogger.LogError("Can't apply Hp Damage to null target!", GameComponent.Battle);
                return -1;
            }

            if (damage <= 0)
            {
                OwlLogger.LogError($"Can't apply invalid hp damage {damage} to entity {target.Id}", GameComponent.Battle);
                return -2;
            }

            float oldValue = target.CurrentHp;
            target.CurrentHp = Math.Clamp(target.CurrentHp - damage, 0, target.MaxHp.Total);
            target.TookDamage?.Invoke(target, damage, false, isCrit, chainCount);

            float contribution = Math.Min(oldValue, damage); // this may not always work?
            if (!target.BattleContributions.ContainsKey(source.Id))
            {
                target.BattleContributions[source.Id] = contribution;
            }
            else
            {
                target.BattleContributions[source.Id] += contribution;
            }

            DamageTakenPacket packet = new()
            {
                EntityId = target.Id,
                Damage = damage,
                IsSpDamage = false,
                IsCrit = isCrit,
                ChainCount = chainCount
            };

            foreach (CharacterRuntimeData character in _map.Grid.GetObserversSquare<CharacterRuntimeData>(target.Coordinates))
            {
                character.Connection.Send(packet);
            }

            if (target.CurrentHp == 0 && oldValue > 0)
            {
                HandleEntityDropToZeroHp(target, source);
            }

            return 0;
        }

        private int ApplySpDamage(int damage, ServerBattleEntity target)
        {
            if (target == null)
            {
                OwlLogger.LogError("Can't apply Sp Damage to null target!", GameComponent.Battle);
                return -1;
            }

            if (damage <= 0)
            {
                OwlLogger.LogError($"Can't apply invalid sp damage {damage} to entity {target.Id}", GameComponent.Battle);
                return -2;
            }

            target.CurrentSp = Math.Clamp(target.CurrentSp - damage, 0, target.MaxSp.Total);
            target.TookDamage?.Invoke(target, damage, true, false, 0);

            DamageTakenPacket packet = new()
            {
                EntityId = target.Id,
                Damage = damage,
                IsSpDamage = true,
                IsCrit = false,
                ChainCount = 0
            };

            foreach (CharacterRuntimeData character in _map.Grid.GetObserversSquare<CharacterRuntimeData>(target.Coordinates))
            {
                character.Connection.Send(packet);
            }

            return 0;
        }

        public int ChangeHp(ServerBattleEntity target, float change, ServerBattleEntity source)
        {
            if (target == null)
            {
                OwlLogger.LogError("Can't update HP of null target!", GameComponent.Battle);
                return -1;
            }

            float newValue = Math.Clamp(target.CurrentHp + change, 0.0f, target.MaxHp.Total);
            if (newValue == target.CurrentHp)
                return 0;

            float oldValue = target.CurrentHp;
            target.CurrentHp = newValue;

            if (target.CurrentHp == 0 && oldValue > 0)
            {
                HandleEntityDropToZeroHp(target, source);
            }

            foreach (CharacterRuntimeData character in _map.Grid.GetObserversSquare<CharacterRuntimeData>(target.Coordinates))
            {
                character.NetworkQueue.HpUpdate(target);
            }

            return 0;
        }

        public int ChangeSp(ServerBattleEntity target, float change)
        {
            if (target == null)
            {
                OwlLogger.LogError("Can't update SP of null target!", GameComponent.Battle);
                return -1;
            }

            float newValue = Math.Clamp(target.CurrentSp + change, 0.0f, target.MaxSp.Total);
            if (newValue == target.CurrentSp)
                return 0;

            target.CurrentSp = newValue;

            foreach (CharacterRuntimeData character in _map.Grid.GetObserversSquare<CharacterRuntimeData>(target.Coordinates))
            {
                character.NetworkQueue.SpUpdate(target);
            }
            return 0;
        }

        private void HandleEntityDropToZeroHp(ServerBattleEntity bEntity, ServerBattleEntity source)
        {
            // Pre-death effects here

            bEntity.Death?.Invoke(bEntity, source);

            bEntity.SetPathingAction(null, IPathingAction.ResultCode.EntityDied);

            bEntity.ClearPath();

            // TODO: Experience Penalty
        }

        public void Shutdown()
        {
            _map = null;
        }
    }
}
