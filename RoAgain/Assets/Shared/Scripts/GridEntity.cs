using OwlLogging;
using System;
using System.Collections.Generic;
using System.IO;

namespace Shared
{

    // Base class for anything that can be placed on the grid:
    // - NPCs
    // - Monsters
    // - Player Characters
    // - Dropped Items
    // CellEffects are currently handled separately

    public class GridEntity
    {
        private static int _nextGridEntityId = 2001;
        public static int NextEntityId => _nextGridEntityId++;

        public int Id;
        public LocalizedStringId LocalizedNameId = LocalizedStringId.INVALID;
        public string NameOverride;
        public string MapId; // Map-class?
        public Coordinate Coordinates;
        public Coordinate LastUpdateCoordinates;
        public GridData.Direction Orientation;
        // Seconds per Cell
        public WatchableProperty<float, EntityPropertyType> Movespeed = new(EntityPropertyType.Movespeed);

        public GridData.Path Path { get; private set; }
        public int PathCellIndex = -1; // index in the list of all cells that the unit's currently on, for convenience/speed
        public float MovementCooldown;
        public List<GridEntity> VisibleEntities = new();
        public HashSet<CellEffectGroup> VisibleCellEffectGroups = new(); // So various Entities can react to AoEs
        public HashSet<PickupEntity> VisiblePickups = new();
        public int VisionRange = GridData.MAX_VISION_RANGE; // watchable?

        private GridData.Path _lastPath;
        public bool HasNewPath { get; private set; }

        public GridData ParentGrid { get; private set; }

        public Action<GridEntity, GridData.Path, GridData.Path> PathUpdated;
        public Action<GridEntity> PathFinished;

        public int ModelId = -1;

        public IPathingAction CurrentPathingAction;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">Use -1 to auto-generate Id. Caller responsible to avoid collisions!</param>
        public GridEntity(Coordinate coordinates, LocalizedStringId locNameId, int modelId, float movespeed, int id = -1)
        {
            if (id == -1)
                id = _nextGridEntityId++;

            Id = id;
            Coordinates = coordinates;
            LocalizedNameId = locNameId;
            ModelId = modelId;

            if (movespeed <= 0)
            {
                OwlLogger.LogError("Can't have GridEntity with movespeed <= 0!", GameComponent.Grid);
                return;
            }
            Movespeed.Value = movespeed;
        }

        public void ClearPath()
        {
            if (Path != null)
                HasNewPath = true;

            Path = null;
            PathCellIndex = -1;
        }

        public void SetPath(GridData.Path newPath, int currentCellIndex = 0, bool overrideCoordinates = false)
        {
            if (newPath == null)
            {
                OwlLogger.LogError($"SetPath(null) called for Entity {Id} - you have to use ClearPath() instead!", GameComponent.Grid);
                ClearPath();
                return;
            }

            if (newPath == Path)
                return;

            //bool oldPathEmpty = Path == null || Path.AllCells.Count == 0;
            //bool newPathEmpty = newPath == null || newPath.AllCells.Count == 0;
            //if (oldPathEmpty && newPathEmpty)
            //    return;

            bool oldPathSafe = Path != null && Path.Corners.Count > 0;
            bool newPathSafe = newPath != null && newPath.Corners.Count > 0;

            if (Path != null
                && OwlLogger.CurrentLogVerbosity >= LogSeverity.VeryVerbose) // since this function is called ALOT, and the format call is fairly big, make sure none of it gets ran unless necessary
            {
                OwlLogger.LogF("GridEntity {0} is overwriting path to {1} with path to {2}", Id, oldPathSafe ? Path.Corners[^1] : "empty", newPathSafe ? newPath.Corners[^1] : "empty", GameComponent.Grid, LogSeverity.VeryVerbose);
            }

            GridData.Path oldPath = Path;

            Path = newPath;
            PathCellIndex = currentCellIndex;
            HasNewPath = true;

            if (!overrideCoordinates)
            {
                PathUpdated?.Invoke(this, oldPath, Path);
                return;
            }

            if (Path == null || currentCellIndex == -1 || currentCellIndex >= Path.AllCells.Count)
            {
                OwlLogger.Log($"Can't set unit Coordinates for invalid PathCellIndex {currentCellIndex}!", GameComponent.Grid, LogSeverity.Verbose);
                PathUpdated?.Invoke(this, oldPath, Path);
                return;
            }

            Coordinate oldCoordinates = Coordinates;
            if (HasFinishedPath())
            {
                if (Path.AllCells.Count > 0)
                    Coordinates = Path.AllCells[^1];

                PathUpdated?.Invoke(this, oldPath, Path);
                return;
            }

            Coordinates = Path.AllCells[PathCellIndex];
            OwlLogger.LogF("Overriding Coordinates during SetPath for GridEntity {0}: {1} -> {2}", Id, oldCoordinates, Coordinates, GameComponent.Grid, LogSeverity.VeryVerbose);

            PathUpdated?.Invoke(this, oldPath, Path);
        }

        public void SetParentGrid(GridData grid)
        {
            if (grid == ParentGrid)
                return;

            if (grid == null)
            {
                OwlLogger.Log($"GridEntity {Id} has its grid set to null!", GameComponent.Grid, LogSeverity.VeryVerbose);
            }
            else
            {
                OwlLogger.Log($"GridEntity {Id} has its grid set to new grid of bounds {grid.Bounds}!", GameComponent.Grid, LogSeverity.VeryVerbose);
            }

            ParentGrid = grid;
        }

        public bool HasFinishedPath()
        {
            return PathCellIndex == -1 && MovementCooldown <= 0;
        }

        public void FinishPath()
        {
            PathCellIndex = -1;
            PathFinished?.Invoke(this);
        }

        public void RecalculateVisibleEntities(ref HashSet<GridEntity> newVisibleEntities,
                ref HashSet<GridEntity> stillVisibleEntities, ref HashSet<GridEntity> removedEntities)
        {
            if (ParentGrid == null)
            {
                newVisibleEntities.Clear();
                stillVisibleEntities.Clear();
                removedEntities.Clear();
                return;
            }

            List<GridEntity> totalVisibleNew = ParentGrid.GetOccupantsInRangeSquareLowAlloc<GridEntity>(Coordinates, VisionRange);
            Extensions.DiffArrays(VisibleEntities, totalVisibleNew, ref newVisibleEntities, ref stillVisibleEntities, ref removedEntities);
        }

        public HashSet<CellEffectGroup> RecalculateVisibleCellEffectGroups(ref HashSet<CellEffectGroup> newGroups, ref HashSet<CellEffectGroup> oldGroups, ref HashSet<CellEffectGroup> removedGroups)
        {
            if (ParentGrid == null)
            {
                newGroups?.Clear();
                oldGroups?.Clear();
                removedGroups?.Clear();
                return new();
            }

            HashSet<CellEffectGroup> totalVisibleNew = ParentGrid.GetGroupsInRangeSquare<CellEffectGroup>(Coordinates, VisionRange);
            Extensions.DiffArrays(VisibleCellEffectGroups, totalVisibleNew, ref newGroups, ref oldGroups, ref removedGroups);

            return totalVisibleNew;
        }

        public void UpdateLastPath()
        {
            HasNewPath = _lastPath != Path;
            _lastPath = Path;
        }

        public virtual void UpdateMovement(float deltaTime)
        {

        }

        public bool Equals(GridEntity other)
        {
            return other != null && other.Id == Id;
        }

        public virtual bool IsAnimationLocked()
        {
            return MovementCooldown > 0;
        }

        public virtual bool CanMove()
        {
            return !IsAnimationLocked();
        }

        public bool IsMoving()
        {
            return !HasFinishedPath();
        }

        public bool HasMovedOnLastUpdate()
        {
            return Coordinates != LastUpdateCoordinates;
        }

        public virtual bool BlocksStanding()
        {
            return true;
        }

        public void SetPathingAction(IPathingAction newAction, IPathingAction.ResultCode previousCode = IPathingAction.ResultCode.OtherMovement)
        {
            CurrentPathingAction?.Finish(previousCode);

            CurrentPathingAction = newAction;
        }

        public virtual Packet ToDataPacket()
        {
            return new GridEntityDataPacket()
            {
                EntityId = Id,
                LocalizedNameId = LocalizedNameId,
                NameOverride = NameOverride,
                MapId = MapId,
                Path = Path,
                PathCellIndex = PathCellIndex,
                Movespeed = Movespeed.Value,
                MovementCooldown = MovementCooldown,
                Orientation = Orientation,
                Coordinates = Coordinates,
                ModelId = ModelId,
            };
        }

        public virtual Packet ToRemovedPacket()
        {
            // TODO: Add handling of different remove-reasons to control VFX: Teleport-Away, Death, Hide?
            return new EntityRemovedPacket()
            {
                EntityId = Id,
            };
        }
    }
}