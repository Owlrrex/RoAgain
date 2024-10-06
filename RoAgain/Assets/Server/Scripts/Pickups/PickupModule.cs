using OwlLogging;
using Shared;
using System.Collections.Generic;

namespace Server
{
    public class PickupPathingAction : APathingAction<PickupPathingAction.PickupPathingPayload>
    {
        public class PickupPathingPayload
        {
            public int InventoryId;
            public GridEntity PickupEntity;
            public PickupEntity Pickup;
            public bool OverrideOwner = false;
        }

        public override Coordinate GetTargetCoordinates()
        {
            return Payload.Pickup.Coordinates.ToCoordinate();
        }

        public override bool ShouldCalculateNewPath()
        {
            // User can move - this is fairly aggressive.
            // Ideally, we only calculate a new path if our path is no longer the path we calculated for this action initially,
            // but that's hard to detect
            // Also, we can't use "hasmovedLastUpdate" because that way, we'll never _start_ moving
            return Payload.PickupEntity.CanMove();
        }

        public override IPathingAction.ResultCode ShouldContinuePathing()
        {
            if (Payload.PickupEntity.MapId != Payload.Pickup.MapId)
                return IPathingAction.ResultCode.AbortedSelf;

            if (Payload.PickupEntity.Coordinates == GridData.INVALID_COORDS
                || Payload.Pickup.Coordinates == GridData.INVALID_COORDS)
                return IPathingAction.ResultCode.AbortedSelf;

            if (Payload.PickupEntity.Coordinates.GridDistanceSquare(Payload.Pickup.Coordinates) <= 1) // TODO: Remove magic number "pickup range"
                return IPathingAction.ResultCode.Success;

            return IPathingAction.ResultCode.ContinuePathing;
        }
    }

    // One per map
    public class PickupModule
    {
        private MapInstance _map;
        private InventoryModule _invModule;

        private HashSet<PickupEntity> _currentPickups = new();

        private HashSet<PickupEntity> _visibilityBufferLost = new();
        private HashSet<PickupEntity> _visibilityBufferStill = new();
        private HashSet<PickupEntity> _visibilityBufferGained = new();

        private HashSet<PickupEntity> _toRemoveBuffer = new();

        private List<PickupEntity> _queuedPickups = new();

        public int Initialize(MapInstance map, InventoryModule inventoryModule)
        {
            if (map == null)
            {
                OwlLogger.LogError("Can't initialize PickupModule with null GridData!", GameComponent.Items);
                return -1;
            }

            if (inventoryModule == null)
            {
                OwlLogger.LogError("Can't initialize PickupModule with null InventoryModule!", GameComponent.Items);
                return -2;
            }

            _map = map;
            _invModule = inventoryModule;

            return 0;
        }

        public void Shutdown()
        {
            foreach (PickupEntity pickup in _currentPickups)
            {
                DestroyPickup(pickup);
            }
            _currentPickups.Clear();

            _visibilityBufferLost.Clear();
            _visibilityBufferStill.Clear();
            _visibilityBufferGained.Clear();
            _toRemoveBuffer.Clear();

            _invModule = null;
            _map = null;
        }

        public void UpdatePickups(float deltaTime)
        {
            _toRemoveBuffer.Clear();
            foreach (PickupEntity pickup in _currentPickups)
            {
                // Advance the "dropping" state so pickups reach "on ground" automatically
                if (pickup.State == PickupState.JustDropped)
                    pickup.State = PickupState.OnGround;

                if(pickup.State == PickupState.Unknown)
                {
                    OwlLogger.LogError("Found Pickup in Unknown State - forcing expiry!", GameComponent.Items);
                    pickup.LifeTime.RemainingValue = 0;
                }

                if(pickup.State == PickupState.AboutToDisappear
                    || pickup.State == PickupState.PickedUp)
                {
                    DestroyPickup(pickup);
                    _toRemoveBuffer.Add(pickup);
                }

                pickup.LifeTime.Update(deltaTime);
                if (pickup.LifeTime.IsFinished())
                    pickup.State = PickupState.AboutToDisappear;
            }

            foreach(PickupEntity pickup in _toRemoveBuffer)
                _currentPickups.Remove(pickup);
            _toRemoveBuffer.Clear();

            foreach(PickupEntity pickup in _queuedPickups)
            {
                if (!_map.Grid.PlaceOccupant(pickup, pickup.Coordinates))
                    OwlLogger.LogError($"Placing Queued Pickup Id {pickup.Id} failed!", GameComponent.Items);
                else
                    _currentPickups.Add(pickup);
            }
            _queuedPickups.Clear();
        }

        public void UpdateEntityVisibility(GridEntity observer, ref HashSet<GridEntity> gained, ref HashSet<GridEntity> stayed, ref HashSet<GridEntity> lost)
        {
            _toRemoveBuffer.Clear();
            foreach (GridEntity entity in gained)
            {
                if (entity is not PickupEntity pickup)
                    continue;

                if (!CharacterCanSeePickup(observer, pickup))
                {
                    _toRemoveBuffer.Add(pickup);
                }
                    
            }
            gained.ExceptWith(_toRemoveBuffer);

            _toRemoveBuffer.Clear();
            foreach(GridEntity entity in stayed)
            {
                if (entity is not PickupEntity pickup)
                    continue;

                if(!CharacterCanSeePickup(observer, pickup))
                {
                    _toRemoveBuffer.Add(pickup);
                }
            }
            stayed.ExceptWith(_toRemoveBuffer);
            lost.UnionWith(_toRemoveBuffer);
            _toRemoveBuffer.Clear();
        }

        // Not part of the regular Visibility-updates, since that's covered by GridEntities already, but it may be useful for the AI of Looter-mobs
        // Could also be a useful reference to rewrite how all Visibility-queries work, if it's efficient enough (almost no allocations)
        public void UpdatePickupVisibility(GridEntity entity, ref HashSet<PickupEntity> lost, ref HashSet<PickupEntity> stillVisible, ref HashSet<PickupEntity> gained)
        {
            lost.Clear();
            stillVisible.Clear();
            gained.Clear();

            lost.UnionWith(entity.VisiblePickups);
            entity.VisiblePickups.Clear();
            foreach (PickupEntity pickup in _currentPickups)
            {
                if (CharacterCanSeePickup(entity, pickup))
                {
                    entity.VisiblePickups.Add(pickup);
                    if (lost.Contains(pickup)) // lost also acts as "old" here
                    {
                        lost.Remove(pickup);
                        stillVisible.Add(pickup);
                    }
                    else
                    {
                        lost.Remove(pickup);
                        gained.Add(pickup);
                    }
                }
            }
        }

        private bool CharacterCanSeePickup(GridEntity character, PickupEntity pickup)
        {
            return pickup.State != PickupState.Unknown
                && pickup.State != PickupState.PickedUp // Send Pickup-Removed-packet for getting picked up
                && pickup.State != PickupState.AboutToDisappear // Send Pickup-Removed-packet for timeout
                && character.Coordinates.GridDistanceSquare(pickup.Coordinates) <= GridData.MAX_VISION_RANGE;
        }

        public PickupEntity QueuePickupCreation(long itemTypeId, int count, Coordinate coordinate, int ownerId = 0)
        {
            PickupEntity newPickup = CreatePickup(itemTypeId, count, coordinate, ownerId);
            _queuedPickups.Add(newPickup);
            return newPickup;
        }

        public PickupEntity CreatePickup(long itemTypeId, int count, Coordinate coordinates, int ownerId = 0)
        {
            float lifeTime = 30f; // TODO: Load lifetime from server-config
            PickupEntity pickup = new(coordinates, itemTypeId, count, lifeTime, ownerId);
            return pickup;
        }

        public int PickupPickup(int inventoryId, GridEntity pickupEntity, int pickupId, bool overrideOwner = false)
        {
            PickupEntity pickup = _map.Grid.FindOccupant(pickupId) as PickupEntity;
            if (pickup == null)
            {
                OwlLogger.LogError($"Tried to pick up PickupId {pickupId} that wasn't found on Grid!", GameComponent.Items);
                return -1;
            }

            if (pickup.State != PickupState.OnGround)
            {
                OwlLogger.LogError($"Tried to pick up PickupId {pickupId} that was in invalid state {pickup.State}", GameComponent.Items);
                return -2;
            }

            if (pickupEntity is ServerBattleEntity bEntity)
            {
                if(!bEntity.CanAct())
                {
                    return 2;
                }
            }

            if(!overrideOwner)
            {
                if (pickup.OwnerEntityId != 0 && pickup.OwnerEntityId != pickupEntity.Id)
                {
                    if(pickupEntity is CharacterRuntimeData character)
                    {
                        character.Connection.Send(new LocalizedChatMessagePacket()
                        {
                            ChannelTag = DefaultChannelTags.GENERIC_ERROR,
                            MessageLocId = new(220)
                        });
                    }
                    return 1;
                }
            }

            pickup.State = PickupState.PickedUp;
            pickup.OwnerEntityId = pickupEntity.Id;
            _invModule.AddItemsToInventory(inventoryId, pickup.ItemTypeId, pickup.Count);

            return 0;
        }

        public int CharacterAttemptPickup(CharacterRuntimeData character, int pickupId, bool overrideOwner = false)
        {
            PickupEntity pickup = _map.Grid.FindOccupant(pickupId) as PickupEntity;
            if (pickup == null)
            {
                return -1;
            }

            if (character.Coordinates.GridDistanceSquare(pickup.Coordinates) > 1) // TODO: Remove magic number "pickup range"
            {
                PickupPathingAction pathingAction = new();
                pathingAction.Payload = new()
                {
                    InventoryId = character.InventoryId,
                    OverrideOwner = overrideOwner,
                    Pickup = pickup,
                    PickupEntity = character
                };
                pathingAction.Finished = OnPickupPathingFinished;
                character.SetPathingAction(pathingAction);
                return 1;
            }

            if (!_invModule.HasPlayerSpaceForItemStack(character, pickup.ItemTypeId, pickup.Count))
            {
                return -3;
            }

            return 10 * PickupPickup(character.InventoryId, character, pickupId, overrideOwner);
        }

        private void OnPickupPathingFinished(PickupPathingAction.PickupPathingPayload payload, IPathingAction.ResultCode result)
        {
            if (result != IPathingAction.ResultCode.Success)
                return;

            if (payload.PickupEntity is CharacterRuntimeData character)
                CharacterAttemptPickup(character, payload.Pickup.Id, payload.OverrideOwner);
            else
                PickupPickup(payload.InventoryId, payload.PickupEntity, payload.Pickup.Id, payload.OverrideOwner);
        }

        public int DestroyPickup(PickupEntity pickup)
        {
            if (pickup == null)
            {
                OwlLogger.LogError("Can't destroy null pickup!", GameComponent.Items);
                return -1;
            }

            if (!_map.Grid.RemoveOccupant(pickup))
                return -2;
            
            // Don't remove from _currentPickups so you can call this function while iterating over that list

            return 0;
        }
    }
}