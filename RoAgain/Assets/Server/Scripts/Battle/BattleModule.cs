using OwlLogging;
using Shared;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Server
{
    public enum AttackWeaponType
    {
        Unknown,
        Unarmed,
        Dagger,
        OneHandSword,
        TwoHandSword,
        OneHandSpear,
        TwoHandSpear,
        OneHandAxe,
        TwoHandAxe,
        Mace,
        OneHandStaff,
        TwoHandStaff,
        Bow,
        Knuckle,
        Instrument,
        Whip,
        Book,
        Katar,
        Revolver,
        Rifle,
        Shotgun,
        Gatling,
        Grenade,
        FuumaShuriken
    }

    public class BattleModule
    {
        private ServerMapInstance _map;

        public int Initialize(ServerMapInstance mapInstance)
        {
            if (mapInstance == null)
            {
                OwlLogger.LogError("Can't initialize BattleModule with null mapInstance", GameComponent.Battle);
                return -1;
            }

            _map = mapInstance;
            return 0;
        }

        private void HandleEntityDropToZeroHp(ServerBattleEntity bEntity, ServerBattleEntity source)
        {
            // Pre-death effects here

            bEntity.Death?.Invoke(bEntity, source);
            if(bEntity.QueuedSkill != null)
                _map.SkillModule.ClearQueuedSkill(bEntity.QueuedSkill);
            bEntity.ClearPath();
            // TODO: Experience Penalty
        }

        // TODO: Expand handling of damage: Chains, crits, etc
        public int ApplyHpDamage(int damage, ServerBattleEntity target, ServerBattleEntity source)
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

            int oldValue = target.CurrentHp;
            target.CurrentHp = Math.Clamp(target.CurrentHp - damage, 0, target.MaxHp.Total);
            target.TookDamage?.Invoke(target, damage, false);

            int contribution = Math.Min(oldValue, damage); // this may not always work?
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

        public int ApplySpDamage(int damage, ServerBattleEntity target)
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
            target.TookDamage?.Invoke(target, damage, true);

            DamageTakenPacket packet = new()
            {
                EntityId = target.Id,
                Damage = damage,
                IsSpDamage = true,
            };

            foreach (CharacterRuntimeData character in _map.Grid.GetObserversSquare<CharacterRuntimeData>(target.Coordinates))
            {
                character.Connection.Send(packet);
            }

            return 0;
        }

        public int UpdateHp(ServerBattleEntity target, int change, ServerBattleEntity source)
        {
            if (target == null)
            {
                OwlLogger.LogError("Can't update HP of null target!", GameComponent.Battle);
                return -1;
            }

            int newValue = Mathf.Clamp(target.CurrentHp + change, 0, target.MaxHp.Total);
            if (newValue == target.CurrentHp)
                return 0;

            int oldValue = target.CurrentHp;
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

        public int UpdateSp(ServerBattleEntity target, int change)
        {
            if (target == null)
            {
                OwlLogger.LogError("Can't update SP of null target!", GameComponent.Battle);
                return -1;
            }

            int newValue = Mathf.Clamp(target.CurrentSp + change, 0, target.MaxSp.Total);
            if (newValue == target.CurrentSp)
                return 0;

            target.CurrentSp = newValue;

            foreach (CharacterRuntimeData character in _map.Grid.GetObserversSquare<CharacterRuntimeData>(target.Coordinates))
            {
                character.NetworkQueue.SpUpdate(target);
            }
            return 0;
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

                float increaseAmount = deltaTime;
                // TODO: if(IsSitting(bEntity)) increaseAmount *= 2; // Make sitting regen faster, not higher amount

                bEntity.HpRegenCounter += increaseAmount;
                if (bEntity.HpRegenCounter >= bEntity.HpRegenTime)
                {
                    UpdateHp(bEntity, bEntity.HpRegenAmount.Total, bEntity);
                    bEntity.HpRegenCounter -= bEntity.HpRegenTime;
                }

                bEntity.SpRegenCounter += increaseAmount;
                if (bEntity.SpRegenCounter >= bEntity.SpRegenTime)
                {
                    UpdateSp(bEntity, bEntity.SpRegenAmount.Total);
                    bEntity.SpRegenCounter -= bEntity.SpRegenTime;
                }
            }
        }

        // Return values:
        // 2 = Perfect Dodge
        // 1 = natural Miss
        // 0 = hit
        public int PerformPhysicalAttack(ServerBattleEntity source, ServerBattleEntity target, float skillFactor, EntityElement overrideElement = EntityElement.Unknown, bool canCrit = false, bool canMiss = true, bool ignoreDefense = false)
        {
            if (source is CharacterRuntimeData character)
            {
                if (UnityEngine.Random.Range(0.0f, 1.0f) < character.PerfectFlee.Total)
                {
                    DamageTakenPacket packet = new()
                    {
                        Damage = -2,
                        EntityId = target.Id,
                        IsSpDamage = false,
                    };
                    foreach (CharacterRuntimeData observer in _map.Grid.GetObserversSquare<CharacterRuntimeData>(target.Coordinates))
                    {
                        observer.Connection.Send(packet);
                    }
                    return 2;
                }
            }

            float damage;
            float atkRoll = -1;
            bool isCrit = false;
            if (canCrit)
            {
                float critChance = source.Crit.Total - target.CritShield.Total;

                if (UnityEngine.Random.Range(0.0f, 1.0f) < critChance)
                {
                    // Crit: Maxi atk, ignore Def
                    atkRoll = source.CurrentAtkMax.Total * skillFactor;
                    canMiss = false;
                    ignoreDefense = true;
                    isCrit = true;
                }
            }

            // Noncrit
            if (canMiss)
            {
                int equalHit = 80; // TODO: move this value to config
                float hitChance = (source.Hit.Total - target.Flee.Total + equalHit) / 100.0f;
                if (UnityEngine.Random.Range(0.0f, 1.0f) >= hitChance)
                {
                    // Miss
                    DamageTakenPacket packet = new()
                    {
                        Damage = -1,
                        EntityId = target.Id,
                        IsSpDamage = false,
                    };
                    foreach (CharacterRuntimeData observer in _map.Grid.GetObserversSquare<CharacterRuntimeData>(target.Coordinates))
                    {
                        observer.Connection.Send(packet);
                        //observer.NetworkQueue.DamageTaken();
                    }
                    return 1;
                }
            }

            // Hit
            if(!isCrit)
                atkRoll = UnityEngine.Random.Range(source.CurrentAtkMin.Total, source.CurrentAtkMax.Total + 1) * skillFactor;

            if (ignoreDefense)
            {
                damage = atkRoll;
            }
            else
            {
                // This clamp means low defense cannot increase damage
                damage = Math.Clamp(atkRoll * (1.0f - target.HardDef.Total) - target.SoftDef.Total, 0, atkRoll);
            }

            EntityElement attackElement = overrideElement;
            if (attackElement == EntityElement.Unknown)
                attackElement = source.GetOffensiveElement();
            damage = ModifyDamageForElement(source, target, damage, attackElement, false);
            damage = ModifyDamageForRace(source, target, damage, false);
            damage = ModifyDamageForSize(source, target, damage, false);

            // TODO: Handle absorb

            if (damage == 0)
                return 0;

            return DealPhysicalHitDamage(source, target, (int)damage);
        }

        private float ModifyDamageForElement(ServerBattleEntity source, ServerBattleEntity target, float baseDamage, EntityElement attackElement, bool isMagical)
        {
            if (source is CharacterRuntimeData character)
            {
                // TODO: Read player's element-specific bonuses
            }

            EntityElement def = target.GetDefensiveElement();

            return baseDamage * GetMultiplierForElements(attackElement, def);
        }

        private float GetMultiplierForElements(EntityElement attack, EntityElement def)
        {
            float modifier = ElementsDatabase.GetMultiplierForElements(attack, def);
            if (modifier <= -10.0f)
            {
                OwlLogger.LogError($"Can't find size multiplier for elements {attack} vs {def}", GameComponent.Battle);
                return -1.0f;
            }

            return modifier;
        }

        private float ModifyDamageForRace(ServerBattleEntity source, ServerBattleEntity target, float baseDamage, bool isMagical)
        {
            if (source is not CharacterRuntimeData character)
                return baseDamage;

            // TODO: Read player's race-specific bonuses
            return baseDamage;
        }

        private float ModifyDamageForSize(ServerBattleEntity source, ServerBattleEntity target, float baseDamage, bool isMagical)
        {
            if(isMagical)
            {
                // TODO: Player-specific bonuses from equip
                return 1.0f;
            }

            AttackWeaponType weaponType = AttackWeaponType.Unarmed; // TODO: Get weaponType from entity
            float modifier = GetSizeMultiplierForAttackType(weaponType, target.Size);
            // TODO: Player-specific bonuses from equip & such
            return baseDamage * modifier;
        }

        private float GetSizeMultiplierForAttackType(AttackWeaponType weaponType, EntitySize targetSize)
        {
            float modifier = SizeDatabase.GetMultiplierForWeaponAndSize(weaponType, targetSize);
            if (modifier < 0)
            {
                OwlLogger.LogError($"Can't find size multiplier for size {targetSize}, type {weaponType}", GameComponent.Battle);
                return 1.0f;
            }

            return modifier;
        }

        public int DealPhysicalHitDamage(ServerBattleEntity source, ServerBattleEntity target, int damage)
        {
            // TODO: Handling of "on physical damage" effects
            _map.SkillModule.InterruptAnyCasts(target);
            ApplyHpDamage(damage, target, source);
            return 0;
        }

        public int PerformMagicalAttack(ServerBattleEntity source, ServerBattleEntity target, float skillFactor, EntityElement overrideElement = EntityElement.Unknown, bool canCrit = false, bool canMiss = true, bool ignoreDefense = false)
        {
            float damage;
            // Crit
            if (canCrit)
            {
                float critChance = source.Crit.Total - target.CritShield.Total;

                if (UnityEngine.Random.Range(0.0f, 1.0f) < critChance)
                {
                    // Crit: Maxi atk, ignore Def
                    damage = source.MatkMax.Total * skillFactor;
                    return DealMagicalHitDamage(source, target, (int)damage);
                }
            }

            // Noncrit
            if (canMiss)
            {
                int equalHit = 80; // TODO: move this value to config
                float hitChance = (source.Hit.Total - target.Flee.Total + equalHit) / 100.0f;
                if (UnityEngine.Random.Range(0.0f, 1.0f) >= hitChance)
                {
                    // Miss
                    DamageTakenPacket packet = new()
                    {
                        Damage = -1,
                        EntityId = target.Id,
                        IsSpDamage = false,
                    };
                    foreach (CharacterRuntimeData observer in _map.Grid.GetObserversSquare<CharacterRuntimeData>(target.Coordinates))
                    {
                        observer.Connection.Send(packet);
                        //observer.NetworkQueue.DamageTaken();
                    }
                    return 1;
                }
            }

            // Hit
            float matkRoll = UnityEngine.Random.Range(source.MatkMin.Total, source.MatkMax.Total + 1) * skillFactor;
            if (ignoreDefense)
            {
                damage = matkRoll;
            }
            else
            {
                // This clamp means low defense cannot increase damage
                damage = Math.Clamp(matkRoll * (1 - target.HardMDef.Total) - target.SoftMDef.Total, 0, matkRoll);
            }

            EntityElement attackElement = overrideElement;
            if (attackElement == EntityElement.Unknown)
                attackElement = source.GetOffensiveElement();
            damage = ModifyDamageForElement(source, target, damage, attackElement, true);
            damage = ModifyDamageForRace(source, target, damage, true);
            damage = ModifyDamageForSize(source, target, damage, true);

            return DealMagicalHitDamage(source, target, (int)damage);
        }

        public int DealMagicalHitDamage(ServerBattleEntity source, ServerBattleEntity target, int damage)
        {
            // TODO: Handling of "on magical damage" effects
            _map.SkillModule.InterruptAnyCasts(target);
            ApplyHpDamage(damage, target, source);
            return 0;
        }

        public void Shutdown()
        {
            _map = null;
        }
    }
}
