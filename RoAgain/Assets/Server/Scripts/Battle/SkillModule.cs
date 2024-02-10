using OwlLogging;
using Shared;
using System.Collections.Generic;
using UnityEngine;

namespace Server
{
    public class SkillModule
    {
        private ServerMapInstance _mapInstance;

        private static ASkillImpl[] _skillLogicListFast;

        public int Initialize(ServerMapInstance mapInstance)
        {
            if(mapInstance == null)
            {
                OwlLogger.LogError("Can't initialize SkillModule with null mapInstance", GameComponent.Skill);
                return -1;
            }

            if(_skillLogicListFast == null)
                SetupSkillLogicObjects();

            _mapInstance = mapInstance;
            return 0;
        }

        private void SetupSkillLogicObjects()
        {
            _skillLogicListFast = new ASkillImpl[(int)SkillId.END]; // this allocates more memory than needed due to sparse enum - should be fine

            // TODO: Add one object for each skill Id
            _skillLogicListFast[(int)SkillId.AutoAttack] = new AutoAttackSkillImpl();
            _skillLogicListFast[(int)SkillId.PlaceWarp] = new PlaceWarpSkillImpl();

            _skillLogicListFast[(int)SkillId.FirstAid] = new FirstAidSkillImpl();

            _skillLogicListFast[(int)SkillId.Bash] = new BashSkillImpl();
            _skillLogicListFast[(int)SkillId.MagnumBreak] = new MagnumBreakSkillImpl();

            _skillLogicListFast[(int)SkillId.FireBolt] = new FireBoltSkillImpl();
            _skillLogicListFast[(int)SkillId.FireBall] = new FireBallSkillImpl();
        }

        public ASkillImpl GetSkillLogic(SkillId skillId)
        {
            return _skillLogicListFast[(int)skillId];
        }

        public int ReceiveSkillExecutionRequest(SkillId skillId, int skillLvl, ServerBattleEntity user, SkillTarget target)
        {
            if(skillId == SkillId.Unknown)
            {
                OwlLogger.LogError("Can't execute skillId Unknown!", GameComponent.Skill);
                return -2;
            }

            if(skillLvl <= 0)
            {
                OwlLogger.LogError($"Can't execute skill {skillId} on level {skillLvl}", GameComponent.Skill);
                return -1;
            }

            if(user == null)
            {
                OwlLogger.LogError("Can't execute skill with null user!", GameComponent.Skill);
                return -3;
            }

            if (user.MapId != _mapInstance.MapId)
            {
                OwlLogger.LogError($"Received Skill request for userId {user.Id} that's not on map {_mapInstance.MapId}", GameComponent.Skill);
                return -4;
            }

            if(!target.IsSet())
            {
                OwlLogger.LogError($"Can't execute skill {skillId} without setting a target!", GameComponent.Skill);
                return -6;
            }

            if(target.IsEntityTarget())
            {
                if (target.EntityTarget.MapId != _mapInstance.MapId)
                {
                    OwlLogger.LogError($"Received Skill request for targetId {target.EntityTarget.Id} that's not on map {_mapInstance.MapId}", GameComponent.Skill);
                    return -7;
                }
            }
            else
            {
                if(!_mapInstance.Grid.AreCoordinatesValid(target.GroundTarget)
                    || _mapInstance.Grid.GetDataAtCoords(target.GroundTarget).IsVoidCell())
                {
                    OwlLogger.LogError($"Received SkillRequest for targetCoords {target.GroundTarget} that's not valid!", GameComponent.Skill);
                    return 1;
                }
            }

            ASkillImpl logic = GetSkillLogic(skillId);
            if(logic == null)
                return -10; // Already logged in GetSkillLogic

            // Here: Other checks that can be done before allocating a SkillExecution

            ServerSkillExecution skillExec = CreateSkillExecution(skillId, skillLvl, user, target);

            SkillFailReason executeReason = logic.CanBeExecuted(skillExec, user);
            SkillFailReason targetReason = logic.CheckTarget(skillExec);
            
            if(executeReason != SkillFailReason.None
                && executeReason != SkillFailReason.AnimationLocked)
            {
                // TODO: Deny
                return ((int)executeReason) * 1000 + ((int)targetReason * 10); // TODO: Remove hack or wrap in functions
            }

            if(targetReason != SkillFailReason.None
                && targetReason != SkillFailReason.OutOfRange)
            {
                // TODO: Deny
                return ((int)executeReason) * 1000 + ((int)targetReason * 10); // TODO: Remove hack or wrap in functions
            }

            if (executeReason == SkillFailReason.AnimationLocked
                || targetReason == SkillFailReason.OutOfRange)
            {
                // Queue
                EnqueueSkill(skillExec);
            }
            else
            {
                // Start Resolving
                skillExec.User.CurrentlyResolvingSkills.Add(skillExec);

                if (skillExec.HasCastTime())
                    StartCast(skillExec);
            }

            return 0;
        }

        private ServerSkillExecution CreateSkillExecution(SkillId skillId, int skillLvl, ServerBattleEntity user, SkillTarget target)
        {
            ServerSkillExecution skillExec = new();
            int initError = skillExec.InitializeFromStatic(skillId, skillLvl, user, target, _mapInstance);
            if (initError != 0)
            {
                OwlLogger.LogError($"SkillExecution initialization error {initError}!", GameComponent.Skill);
                return null;
            }

            return skillExec;
        }

        private void EnqueueSkill(ServerSkillExecution skillExec)
        {
            skillExec.User.QueuedSkill = skillExec;

            if (skillExec.User is not CharacterRuntimeData playerUser)
                return;

            if (skillExec.Target.IsGroundTarget())
            {
                LocalPlayerGroundSkillQueuedPacket packet = new()
                {
                    SkillId = skillExec.SkillId,
                    Target = skillExec.Target.GroundTarget
                };
                playerUser.Connection.Send(packet);
            }
            else
            {
                LocalPlayerEntitySkillQueuedPacket packet = new()
                {
                    SkillId = skillExec.SkillId,
                    TargetId = skillExec.Target.EntityTarget.Id
                };
                playerUser.Connection.Send(packet);
            }
        }

        public void UpdateSkillExecutions(float deltaTime)
        {
            if (_mapInstance == null || _mapInstance.Grid == null)
                return;

            foreach(GridEntity entity in _mapInstance.Grid.GetAllOccupants())
            {
                if (entity is not ServerBattleEntity bEntity)
                    continue;

                bEntity.UpdateSkills(deltaTime);

                if (bEntity.QueuedSkill != null)
                {
                    UpdateQueuedSkill(bEntity.QueuedSkill as ServerSkillExecution);
                }

                for(int i = bEntity.CurrentlyResolvingSkills.Count -1; i >= 0; i--)
                {
                    ASkillImpl logic = GetSkillLogic(bEntity.CurrentlyResolvingSkills[i].SkillId);
                    ServerSkillExecution skillExec = bEntity.CurrentlyResolvingSkills[i] as ServerSkillExecution;
                    if (logic.HasFinishedResolving(skillExec))
                    {
                        // Skill completed successfully
                        logic.OnCompleted(skillExec, true);
                        bEntity.CurrentlyResolvingSkills.RemoveAt(i);
                    }
                }

                for(int i = bEntity.CurrentlyResolvingSkills.Count -1; i >= 0; i--)
                {
                    UpdateSkillResolution(bEntity.CurrentlyResolvingSkills[i] as ServerSkillExecution);
                }
                bEntity.UpdateAnimationLockedState();
            }
        }

        private void UpdateQueuedSkill(ServerSkillExecution skillExec)
        {
            ASkillImpl logic = GetSkillLogic(skillExec.SkillId);

            SkillFailReason executeReason = logic.CanBeExecuted(skillExec, skillExec.User);
            SkillFailReason targetReason = logic.CheckTarget(skillExec);

            if (executeReason != SkillFailReason.None
                && executeReason != SkillFailReason.AnimationLocked)
            {
                ClearQueuedSkill(skillExec);
                return;
            }

            if (targetReason != SkillFailReason.None
                && targetReason != SkillFailReason.OutOfRange)
            {
                ClearQueuedSkill(skillExec);
                return;
            }

            if (executeReason == SkillFailReason.AnimationLocked)
                return; // Wait for animation-lock to end

            if(targetReason == SkillFailReason.OutOfRange)
            {
                bool pathingSuccessful = UpdateQueuedSkillPathing(skillExec);
                if(!pathingSuccessful)
                {
                    ClearQueuedSkill(skillExec);
                }
                return;
            }

            // Skill is good to execute, all error cases have been handled
            ClearQueuedSkill(skillExec);
            skillExec.User.CurrentlyResolvingSkills.Add(skillExec);
            if (skillExec.HasCastTime())
                StartCast(skillExec);
        }

        public void ClearQueuedSkill(ServerSkillExecution skillExec)
        {
            if(skillExec.User.QueuedSkill != skillExec)
            {
                OwlLogger.LogError("Tried to clear queue for skill that's not queued on its user!", GameComponent.Skill);
                return;
            }

            skillExec.User.QueuedSkill = null;

            if (skillExec.User is not CharacterRuntimeData character)
                return;

            LocalPlayerEntitySkillQueuedPacket packet = new()
            {
                SkillId = SkillId.Unknown,
                TargetId = -1
            };
            character.Connection.Send(packet);
        }

        private bool UpdateQueuedSkillPathing(ServerSkillExecution skillExec)
        {
            Vector2Int targetCoords = skillExec.Target.GetTargetCoordinates();

            if (targetCoords == GridData.INVALID_COORDS
                || !_mapInstance.Grid.AreCoordinatesValid(targetCoords))
            {
                OwlLogger.LogError($"Invalid Target coordinates for skill {skillExec.SkillId}: {targetCoords}!", GameComponent.Skill);
                return false;
            }

            // Trying to reduce path calculations: Check if current path already leads to target
            if (skillExec.User.Path != null
                && skillExec.User.Path.Corners.Count > 0
                && skillExec.User.Path.Corners[^1] == targetCoords)
            {
                // No path setting required
                return true;
            }

            int pathResult = skillExec.User.ParentGrid.FindAndSetPathTo(skillExec.User, targetCoords);
            return pathResult == 0;
        }

        private void UpdateSkillResolution(ServerSkillExecution skillExec)
        {
            if(skillExec.HasCastTime())
            {
                if (!skillExec.CastTime.IsFinished())
                    return;

                FinishCast(skillExec, false);
            }

            ASkillImpl logic = GetSkillLogic(skillExec.SkillId);

            if (!skillExec.HasExecutionStarted)
            {
                SkillFailReason executeReason = logic.CanBeExecuted(skillExec, skillExec.User);
                if(executeReason != SkillFailReason.None)
                {
                    AbortSkill(skillExec);
                    // TODO: Send SkillFail packet
                    return;
                }

                SkillFailReason targetReason = logic.CheckTarget(skillExec);
                // TODO: Config value that allows out-of-range executes after cast
                bool allowOutOfRangeFinish = false;
                if(targetReason != SkillFailReason.None
                    && !(allowOutOfRangeFinish && targetReason == SkillFailReason.OutOfRange))
                {
                    AbortSkill(skillExec);
                    // TODO: Send SkillFail packet
                    return;
                }

                ExecuteSkill(skillExec);
            }
            else
            {
                if(!logic.IsExecutionFinished(skillExec))
                    logic.OnExecute(skillExec);
            }
        }

        private void StartCast(ServerSkillExecution skillExec)
        {
            // Improvement over RO: Also send this packet to players in vision of the _target_,
            // so that targeting indicators from casters happening offscreen can be displayed

            // TODO: additional logic required for ground-skills (since aoEs have a "size" that's not accounted for natively)
            // requires casting of ASkillExecution to determine target accurately,
            CastProgressPacket packet = new()
            {
                CasterId = skillExec.User.Id,
                SkillId = skillExec.SkillId,
                CastTimeTotal = skillExec.CastTime.MaxValue,
                CastTimeRemaining = skillExec.CastTime.RemainingValue
            };

            if(skillExec.Target.IsEntityTarget())
            {
                packet.TargetId = skillExec.Target.EntityTarget.Id;
            }
            else
            {
                packet.TargetCoords = skillExec.Target.GroundTarget;
            }

            List<GridEntity> sent = new();
            List<CharacterRuntimeData> observers = _mapInstance.Grid.GetObserversSquare<CharacterRuntimeData>(skillExec.User.Coordinates);
            foreach (CharacterRuntimeData observer in observers)
            {
                observer.Connection.Send(packet);
                sent.Add(observer);
            }

            packet.EncodeTarget(skillExec.Target);
            observers = _mapInstance.Grid.GetObserversSquare<CharacterRuntimeData>(skillExec.Target.GetTargetCoordinates(), sent);
            foreach (CharacterRuntimeData observer in observers)
            {
                observer.Connection.Send(packet);
            }

            GetSkillLogic(skillExec.SkillId).OnCastStart(skillExec);
        }

        private void FinishCast(ServerSkillExecution skillExec, bool interrupted)
        {
            GetSkillLogic(skillExec.SkillId).OnCastEnd(skillExec, interrupted);

            if (interrupted)
            {
                CastProgressPacket packet = new()
                {
                    CasterId = skillExec.User.Id,
                    SkillId = skillExec.SkillId,
                    CastTimeTotal = skillExec.CastTime.MaxValue,
                    CastTimeRemaining = 0
                };

                if (skillExec.Target.IsEntityTarget())
                {
                    packet.TargetId = skillExec.Target.EntityTarget.Id;
                }
                else
                {
                    packet.TargetCoords = skillExec.Target.GroundTarget;
                }

                List<GridEntity> sent = new();
                List<CharacterRuntimeData> observers = _mapInstance.Grid.GetObserversSquare<CharacterRuntimeData>(skillExec.User.Coordinates);
                foreach (CharacterRuntimeData observer in observers)
                {
                    observer.Connection.Send(packet);
                    sent.Add(observer);
                }

                Vector2Int targetCoords = skillExec.Target.GetTargetCoordinates();
                observers = _mapInstance.Grid.GetObserversSquare<CharacterRuntimeData>(targetCoords, sent);
                foreach (CharacterRuntimeData observer in observers)
                {
                    observer.Connection.Send(packet);
                }
            }
        }

        private void ExecuteSkill(ServerSkillExecution skill)
        {
            float animCd = skill.User.GetDefaultAnimationCooldown(); // TODO: animCd system

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
                List<CharacterRuntimeData> observers = _mapInstance.Grid.GetObserversSquare<CharacterRuntimeData>(skill.User.Coordinates);
                foreach (CharacterRuntimeData observer in observers)
                {
                    observer.Connection.Send(packet);
                    sent.Add(observer);
                }

                observers = _mapInstance.Grid.GetObserversSquare<CharacterRuntimeData>(skill.Target.GroundTarget, sent);
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
                    Speak = skill.SkillId != SkillId.AutoAttack, // TODO: Account for other skills & conditions that aren't spoken (Double-Attack, autocasts?)
                };

                List<GridEntity> sent = new();
                List<CharacterRuntimeData> observers = _mapInstance.Grid.GetObserversSquare<CharacterRuntimeData>(skill.User.Coordinates);
                foreach (CharacterRuntimeData observer in observers)
                {
                    observer.Connection.Send(packet);
                    sent.Add(observer);
                }

                observers = _mapInstance.Grid.GetObserversSquare<CharacterRuntimeData>(skill.Target.EntityTarget.Coordinates, sent);
                foreach (CharacterRuntimeData observer in observers)
                {
                    observer.Connection.Send(packet);
                }
            }

            // TODO: System for other costs
            _mapInstance.BattleModule.ChangeSp(skill.User as ServerBattleEntity, -skill.SpCost);

            ASkillImpl logic = GetSkillLogic(skill.SkillId);

            Dictionary<SkillId, float> skillCooldownsToSet = logic.GetSkillCoolDowns(skill);
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

            logic.OnExecute(skill);
        }

        /// <summary>
        /// Interrupts any interruptible skills currently being cast by the entity
        /// </summary>
        /// <param name="bEntity">The target of the interruption</param>
        /// <returns>True if any cast was interrupted, false otherwise</returns>
        public bool InterruptAnyCasts(ServerBattleEntity bEntity)
        {
            if (bEntity.CurrentlyResolvingSkills.Count == 0)
                return false;

            bool anyInterrupt = false;
            for(int i = bEntity.CurrentlyResolvingSkills.Count -1; i >= 0;  i--)
            {
                ServerSkillExecution skillExec = bEntity.CurrentlyResolvingSkills[i] as ServerSkillExecution;
                if (skillExec.CastTime.IsFinished())
                    continue;

                if (!skillExec.CanBeInterrupted)
                    continue;

                FinishCast(skillExec, true);
                AbortSkill(skillExec);
                anyInterrupt = true;
            }
            return anyInterrupt;
        }

        private void AbortSkill(ServerSkillExecution skillExec)
        {
            if(skillExec.HasCastTime())
            {
                skillExec.CastTime.RemainingValue = 0;
            }

            skillExec.AnimationCooldown.RemainingValue = 0;
            GetSkillLogic(skillExec.SkillId).OnCompleted(skillExec, false);

            skillExec.User.CurrentlyResolvingSkills.Remove(skillExec);
        }

        public int Shutdown()
        {
            _mapInstance = null;
            return 0;
        }
    }

}
