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

        private List<SkillId> _skillIdsFinishedReuse = new(4); // Not predicted that a unit finishes more cooldowns than this on a single tick

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

        // This function should handle a skill-execution once it's been confirmed to actually be executed: 
        // Resolve its actual effect, register it to the relevant entities, etc.
        // If I allow checking here, the checks can be removed from Receive...-functions, where they don't _really_ belong anyway?
        public int HandleSkill(ASkillExecution skill)
        {
            BattleEntity battleUser = skill.User;
            if (battleUser == null)
            {
                OwlLogger.LogError($"Skill use ID {skill.SkillId} handled by wrong grid!", GameComponent.Skill);
                return -4;
            }

            battleUser.CurrentlyExecutingSkills.Add(skill);
            if (skill.CastTime.RemainingValue > 0)
            {
                StartCast(skill);
            }
            else
            {
                ExecuteSkill(skill);
            }

            return 0;
        }

        private bool ShouldQueueSkill(ASkillExecution skill, SkillFailReason executeReason, SkillFailReason targetReason)
        {
            if (executeReason == SkillFailReason.CantAct) // User is animation-locked (attacking or moving)
                return true;

            if (targetReason == SkillFailReason.OutOfRange) // Target is out-of-range: Allow Queueing for autopathing logic
                return true;

            return false;
        }

        private void EnqueueSkill(ASkillExecution skill)
        {
            skill.User.QueuedSkill = skill;

            if (skill.User is not CharacterRuntimeData playerUser)
                return;

            if(skill.Target.IsGroundTarget())
            {
                LocalPlayerGroundSkillQueuedPacket packet = new()
                {
                    SkillId = skill.SkillId,
                    Target = skill.Target.GroundTarget
                };
                playerUser.Connection.Send(packet);
            }
            else
            {
                LocalPlayerEntitySkillQueuedPacket packet = new()
                {
                    SkillId = skill.SkillId,
                    TargetId = skill.Target.EntityTarget.Id
                };
                playerUser.Connection.Send(packet);
            }
        }

        private void ClearQueuedSkill(BattleEntity bEntity)
        {
            bEntity.QueuedSkill = null;

            if (bEntity is not CharacterRuntimeData character)
                return;

            LocalPlayerEntitySkillQueuedPacket packet = new()
            {
                SkillId = SkillId.Unknown,
                TargetId = -1
            };
            character.Connection.Send(packet);
        }

        private void StartCast(ASkillExecution skill)
        {
            // Send packet:
            // Improvement over RO: Also send this packet to players in vision of the _target_,
            // so that people offscreen casting at sth onscreen can be displayed
            // requires casting of AskillExecution to determine target accurately,
            // additional logic required for ground-skills (since aoEs have a "size" that's not accounted for natively)
            CastProgressPacket packet = new()
            {
                CasterId = skill.User.Id,
                SkillId = skill.SkillId,
                CastTimeTotal = skill.CastTime.MaxValue,
                CastTimeRemaining = skill.CastTime.RemainingValue
            };

            List<GridEntity> sent = new();
            List<CharacterRuntimeData> observers = _map.Grid.GetObserversSquare<CharacterRuntimeData>(skill.User.Coordinates);
            foreach (CharacterRuntimeData observer in observers)
            {
                observer.Connection.Send(packet);
                sent.Add(observer);
            }

            if (skill.Target.IsGroundTarget())
            {
                packet.TargetCoords = skill.Target.GroundTarget;
                packet.TargetId = -1;
                observers = _map.Grid.GetObserversSquare<CharacterRuntimeData>(skill.Target.GroundTarget, sent);
                foreach (CharacterRuntimeData observer in observers)
                {
                    observer.Connection.Send(packet);
                }
            }
            else
            {
                packet.TargetId = skill.Target.EntityTarget.Id;
                packet.TargetCoords = GridData.INVALID_COORDS;
                observers = _map.Grid.GetObserversSquare<CharacterRuntimeData>(skill.Target.EntityTarget.Coordinates, sent);
                foreach (CharacterRuntimeData observer in observers)
                {
                    observer.Connection.Send(packet);
                }
            }

            skill.OnCastStart();
        }

        // Ideally this function can be reusable for execution-after-cast
        private void ExecuteSkill(ASkillExecution skill)
        {
            float animCd = skill.User.GetDefaultAnimationCooldown(); // TODO: Custom animCd system

            // Send packet:
            // Improvement over RO: Also send this packet to players in vision of the _target_,
            // so that people offscreen casting at sth onscreen can be displayed        
            Packet packet;
            if (skill.Target.IsGroundTarget())
            {
                packet = new GroundSkillExecutePacket()
                {
                    SkillId = skill.SkillId,
                    UserId = skill.User.Id,
                    TargetCoords = skill.Target.GroundTarget,
                    AnimCd = animCd,
                    Speak = skill.SkillId != SkillId.AutoAttack,
                };

                List<GridEntity> sent = new();
                List<CharacterRuntimeData> observers = _map.Grid.GetObserversSquare<CharacterRuntimeData>(skill.User.Coordinates);
                foreach (CharacterRuntimeData observer in observers)
                {
                    observer.Connection.Send(packet);
                    sent.Add(observer);
                }

                observers = _map.Grid.GetObserversSquare<CharacterRuntimeData>(skill.Target.GroundTarget, sent);
                foreach (CharacterRuntimeData observer in observers)
                {
                    observer.Connection.Send(packet);
                }
            }
            else
            {
                packet = new EntitySkillExecutePacket()
                {
                    SkillId = skill.SkillId,
                    UserId = skill.User.Id,
                    TargetId = skill.Target.EntityTarget.Id,
                    AnimCd = animCd,
                    Speak = skill.SkillId != SkillId.AutoAttack,
                };

                List<GridEntity> sent = new();
                List<CharacterRuntimeData> observers = _map.Grid.GetObserversSquare<CharacterRuntimeData>(skill.User.Coordinates);
                foreach (CharacterRuntimeData observer in observers)
                {
                    observer.Connection.Send(packet);
                    sent.Add(observer);
                }

                observers = _map.Grid.GetObserversSquare<CharacterRuntimeData>(skill.Target.EntityTarget.Coordinates, sent);
                foreach (CharacterRuntimeData observer in observers)
                {
                    observer.Connection.Send(packet);
                }
            }

            // TODO: properly carry correct type
            UpdateSp(skill.User as ServerBattleEntity, -skill.SpCost);

            Dictionary<SkillId, float> skillCooldownsToSet = skill.GetSkillCoolDowns();
            if (skillCooldownsToSet != null)
            {
                foreach (var kvp in skillCooldownsToSet)
                {
                    if (!skill.User.SkillCooldowns.ContainsKey(kvp.Key))
                    {
                        skill.User.SkillCooldowns.Add(kvp.Key, new());
                    }
                    skill.User.SkillCooldowns[kvp.Key].Initialize(kvp.Value);
                }
            }

            skill.OnExecute();
        }

        private void FinishCast(ASkillExecution skill, bool interrupted)
        {
            skill.OnCastEnd(interrupted);

            if(interrupted)
            {
                CastProgressPacket packet = new()
                {
                    CasterId = skill.User.Id,
                    SkillId = skill.SkillId,
                    CastTimeTotal = skill.CastTime.MaxValue,
                    CastTimeRemaining = 0
                };

                List<GridEntity> sent = new();
                List<CharacterRuntimeData> observers = _map.Grid.GetObserversSquare<CharacterRuntimeData>(skill.User.Coordinates);
                foreach (CharacterRuntimeData observer in observers)
                {
                    observer.Connection.Send(packet);
                    sent.Add(observer);
                }

                if(skill.Target.IsGroundTarget())
                {
                    observers = _map.Grid.GetObserversSquare<CharacterRuntimeData>(skill.Target.GroundTarget, sent);
                    foreach (CharacterRuntimeData observer in observers)
                    {
                        observer.Connection.Send(packet);
                    }
                }
                else
                {
                    observers = _map.Grid.GetObserversSquare<CharacterRuntimeData>(skill.Target.EntityTarget.Coordinates, sent);
                    foreach (CharacterRuntimeData observer in observers)
                    {
                        observer.Connection.Send(packet);
                    }
                }
            }
        }

        private void CompleteSkill(ASkillExecution skill)
        {
            skill.OnCompleted();
            skill.User.CurrentlyExecutingSkills.Remove(skill);
        }

        // This function should handle a skill-execution once it's been confirmed to actually be executed: 
        // Resolve its actual effect, register it to the relevant entities, etc.
        // If I allow checking here, the checks can be removed from Receive...-functions, where they don't _really_ belong anyway?
        private int PreHandleSkill(ASkillExecution skill)
        {
            if(skill.Target.IsGroundTarget())
            {
                if (!_map.Grid.AreCoordinatesValid(skill.Target.GroundTarget))
                {
                    OwlLogger.LogError($"Tried to target ground skill on invalid coordinates {skill.Target.GroundTarget}", GameComponent.Skill);
                    return -1;
                }
            }
            else
            {
                BattleEntity targetEntity = skill.Target.EntityTarget;
                if (targetEntity == null)
                {
                    OwlLogger.LogError($"Tried to target entity skill on null entity", GameComponent.Skill);
                    return -1;
                }
            }
            
            // TODO: More checking, maybe draw it in from the Receive-functions?

            HandleSkill(skill);

            return 0;
        }

        private AServerSkillExecution CreateSkillExecution(SkillId skillId, int skillLvl, BattleEntity user, SkillTarget target)
        {
            AServerSkillExecution skill;
            switch (skillId)
            {
                case SkillId.AutoAttack:
                    skill = AutoAttackSkillExecution.Create(skillLvl, user, target, _map);
                    break;
                case SkillId.FireBolt:
                    skill = FireBoltSkillExecution.Create(skillLvl, user, target, _map);
                    break;
                case SkillId.PlaceWarp:
                    skill = PlaceWarpSkillExecution.Create(skillLvl, user, target, _map);
                    break;
                default:
                    OwlLogger.LogError($"Tried to create execution of unknown skillId {skillId}!", GameComponent.Skill);
                    return null;
            }

            return skill;
        }

        public void ReceiveEntitySkillRequest(SkillId skillId, int skillLvl, int userId, int targetId)
        {
            if (_map.Grid.FindOccupant(userId) is not BattleEntity user)
            {
                OwlLogger.LogError($"Received Skill request for invalid userId {userId}", GameComponent.Skill);
                return;
            }

            if (_map.Grid.FindOccupant(targetId) is not BattleEntity targetEntity)
            {
                OwlLogger.LogError($"Received Skill request for invalid targetId {targetId}", GameComponent.Skill);
                return;
            }

            SkillTarget target = new(targetEntity);

            AServerSkillExecution skill = CreateSkillExecution(skillId, skillLvl, user, target);
            if (skill == null)
            {
                OwlLogger.LogError($"Skill Execution creation failed: SkillId {skillId}, SkillLvl {skillLvl}, UserId {user.Id}, TargetId {targetId}", GameComponent.Skill);
                return;
            }

            if (!CheckSkillWithQueue(skill)) // Should this maybe be in the HandleEntitySkill function instead of the Receive-function?
                return;

            int result = PreHandleSkill(skill);
            if (result != 0)
            {
                OwlLogger.LogError($"Skill execution handling failed: SkillId {skillId}, SkillLvl {skillLvl}, UserId {user.Id}, TargetId {targetId}", GameComponent.Skill);
            }
        }

        public void ReceiveGroundSkillRequest(SkillId skillId, int skillLvl, int userId, Vector2Int targetCoords)
        {
            if (!_map.Grid.AreCoordinatesValid(targetCoords))
            {
                OwlLogger.LogError($"Received Skill request for invalid target coordinates {targetCoords}", GameComponent.Skill);
            }

            if (_map.Grid.FindOccupant(userId) is not BattleEntity user)
            {
                OwlLogger.LogError($"Received Skill request for invalid userId {userId}", GameComponent.Skill);
                return;
            }

            SkillTarget target = new(targetCoords);

            AServerSkillExecution skill = CreateSkillExecution(skillId, skillLvl, user, target);
            if (skill == null)
            {
                OwlLogger.LogError($"Skill Execution creation failed: SkillId {skillId}, SkillLvl {skillLvl}, UserId {user.Id}, Target {targetCoords}", GameComponent.Skill);
                return;
            }

            if (!CheckSkillWithQueue(skill)) // Should this maybe be in the HandleGroundSkill function instead of the Receive-function?
                return;

            int result = PreHandleSkill(skill);
            if (result != 0)
            {
                OwlLogger.LogError($"Skill execution handling failed: SkillId {skillId}, SkillLvl {skillLvl}, UserId {user.Id}, Target {targetCoords}", GameComponent.Skill);
            }
        }

        private bool CheckSkillWithQueue(ASkillExecution skill)
        {
            SkillFailReason executeReason = skill.User.CanExecuteSkill(skill);
            SkillFailReason targetReason = skill.CheckTarget();
            if (executeReason != SkillFailReason.None
                || targetReason != SkillFailReason.None)
            {
                if (ShouldQueueSkill(skill, executeReason, targetReason))
                {
                    EnqueueSkill(skill);
                }
                OwlLogger.Log($"Skill execution denied: User {skill.User.Id} can't execute/target {skill.SkillId} level {skill.SkillLvl}", GameComponent.Skill, LogSeverity.Verbose);
                return false;
            }
            return true;
        }

        public void UpdateBattleUnitSkills(float deltaTime)
        {
            if (_map == null || _map.Grid == null)
                return;

            foreach (GridEntity entity in _map.Grid.GetAllOccupants())
            {
                if (entity is not BattleEntity bEntity)
                    continue;

                bEntity.UpdateSkills(deltaTime);

                if (bEntity.QueuedSkill != null)
                {
                    SkillFailReason executeReason = bEntity.CanExecuteSkill(bEntity.QueuedSkill);
                    if (executeReason == SkillFailReason.None)
                    {
                        ASkillExecution skill = bEntity.QueuedSkill;
                        SkillFailReason targetReason = skill.CheckTarget();
                        if (targetReason == SkillFailReason.None) // No matter if we're auto-pathing or not: If we can execute & target, we fire the skill
                        {
                            bEntity.QueuedSkill = null;
                            HandleSkill(skill); // Could instead branch on skillType & use the ReceiveGround/EntitySkill functions to double-check conditions - safer, but more checks & can cause repeated re-queueing
                        }
                        else if (targetReason == SkillFailReason.OutOfRange)
                        {
                            bool pathFound = TrySetPathToSkillTarget(skill);
                            if (!pathFound)
                            {
                                // Some error occured during pathing - abort pathing
                                ClearQueuedSkill(skill.User);
                            }
                        }
                        else if (targetReason == SkillFailReason.Death)
                        {
                            // Discard skill that was queued for dead unit
                            ClearQueuedSkill(bEntity);
                        }
                    }
                }

                for (int i = bEntity.CurrentlyExecutingSkills.Count - 1; i >= 0; i--)
                {
                    ASkillExecution skill = bEntity.CurrentlyExecutingSkills[i];

                    if (!skill.HasExecuted)
                    {
                        if(!skill.User.IsDead())
                        {
                            if (skill.CastTime.MaxValue > 0 && skill.CastTime.RemainingValue > 0)
                            {
                                continue;
                            }

                            if (skill.CastTime.MaxValue > 0 && skill.CastTime.IsFinished())
                            {
                                // If skill had a cast-time preceeding it
                                FinishCast(skill, false);
                            }

                            if (bEntity.CanExecuteSkill(skill) == SkillFailReason.None)
                            {
                                ExecuteSkill(skill);
                                continue;
                            }
                        }

                        OwlLogger.Log($"Entity {bEntity.Id} no longer fulfils conditions to execute skill {skill.SkillId}", GameComponent.Skill);
                        // Forcefully complete/abort skill execution without causing any effect
                        skill.AnimationCooldown.RemainingValue = 0; // Might wanna put this into a function "Abort()" eventually
                        FinishCast(skill, true);
                    }

                    if (skill.IsFinishedExecuting())
                    {
                        CompleteSkill(skill);
                    }
                }

                // Update skill-specific cooldowns
                _skillIdsFinishedReuse.Clear();
                foreach (KeyValuePair<SkillId, TimerFloat> kvp in bEntity.SkillCooldowns)
                {
                    kvp.Value.Update(deltaTime);
                    if (kvp.Value.IsFinished())
                        _skillIdsFinishedReuse.Add(kvp.Key);
                }

                foreach (SkillId skillId in _skillIdsFinishedReuse)
                {
                    bEntity.SkillCooldowns.Remove(skillId);
                }
            }
        }

        // Auto-path towards target if and only if:
        // - The faulting condition is "out of range", execution would otherwise be allowed
        // - Don't calc new path when can't move
        private bool TrySetPathToSkillTarget(ASkillExecution skill)
        {
            Vector2Int targetCoords = GridData.INVALID_COORDS;
            if (skill.Target.IsGroundTarget())
            {
                targetCoords = skill.Target.GroundTarget;
            }
            else
            {
                targetCoords = skill.Target.EntityTarget.Coordinates;
            }

            if (targetCoords == GridData.INVALID_COORDS)
            {
                OwlLogger.LogError($"Error processing queued skill of unknown type: {skill.SkillId}!", GameComponent.Skill);
                return false;
            }

            // Trying to reduce path calculations: Check if current path already leads to target
            if (skill.User.Path != null
                && skill.User.Path.Corners.Count > 0
                && skill.User.Path.Corners[^1] == targetCoords)
            {
                // No path setting required
                return true;
            }

            skill.User.ParentGrid.FindAndSetPathTo(skill.User, targetCoords);
            return true;
        }

        private void HandleEntityDropToZeroHp(ServerBattleEntity bEntity, ServerBattleEntity source)
        {
            // Pre-death effects here

            bEntity.Death?.Invoke(bEntity, source);
            ClearQueuedSkill(bEntity);
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

            return DealPhysicalDamage(source, target, (int)damage);
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

        public int DealPhysicalDamage(ServerBattleEntity source, ServerBattleEntity target, int damage)
        {
            // TODO: Handling of "on physical damage" effects
            ApplyHpDamage(damage, target, source);
            return 0;
        }

        public int PerformMagicalAttack(ServerBattleEntity source, ServerBattleEntity target, float skillFactor, EntityElement overrideElement = EntityElement.Unknown, bool canCrit = false, bool canMiss = true, bool ignoreDefense = false)
        {
            float damage;
            if (canCrit)
            {
                float critChance = source.Crit.Total - target.CritShield.Total;

                if (UnityEngine.Random.Range(0.0f, 1.0f) < critChance)
                {
                    // Crit: Maxi atk, ignore Def
                    damage = source.MatkMax.Total * skillFactor;
                    return DealMagicalDamage(source, target, (int)damage);
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

            return DealMagicalDamage(source, target, (int)damage);
        }

        public int DealMagicalDamage(ServerBattleEntity source, ServerBattleEntity target, int damage)
        {
            // TODO: Handling of "on magical damage" effects
            ApplyHpDamage(damage, target, source);
            return 0;
        }

        public void Shutdown()
        {
            _map = null;
        }
    }
}
