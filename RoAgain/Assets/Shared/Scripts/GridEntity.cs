using OwlLogging;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Base class for anything that can be placed on the grid:
// - NPCs
// - Monsters
// - Player Characters
// - Dropped Items
// - CellEffects

public class GridEntity
{
    private static int _nextGridEntityId = 2001;
    public static int NextEntityId => _nextGridEntityId++;

    public int Id;
    public string Name; // watchable?
    public string MapId; // Map-class?
    public Vector2Int Coordinates;
    public Vector2Int LastUpdateCoordinates;
    public GridData.Direction Orientation;
    // Seconds per Cell
    public WatchableProperty<float, EntityPropertyType> Movespeed = new(EntityPropertyType.Movespeed);

    public GridData.Path Path { get; private set; }
    public int PathCellIndex = -1; // index in the list of all cells that the unit's currently on, for convenience/speed
    public float MovementCooldown;
    public List<GridEntity> VisibleEntities = new(20);
    public HashSet<CellEffectGroup> VisibleCellEffectGroups = new(10);
    public int VisionRange = GridData.MAX_VISION_RANGE; // watchable?

    private GridData.Path _lastPath;
    public bool HasNewPath { get; private set; }

    public GridData ParentGrid { get; private set; }

    public Action<GridEntity, GridData.Path, GridData.Path> PathUpdated;
    public Action<GridEntity> PathFinished;

    public void ClearPath()
    {
        if (Path != null)
            HasNewPath = true;

        Path = null;
        PathCellIndex = -1;
    }

    public void SetPath(GridData.Path newPath, int currentCellIndex = 0, bool overrideCoordinates = false)
    {
        if(newPath == null)
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

        if (Path != null)
        {
            OwlLogger.Log($"GridEntity {Id} is overwriting path to {(oldPathSafe ? Path.Corners[^1] : "empty")} with path to {(newPathSafe ? newPath.Corners[^1] : "empty")}", GameComponent.Grid, LogSeverity.VeryVerbose);
        }

        GridData.Path oldPath = Path;

        Path = newPath;
        PathCellIndex = currentCellIndex;
        HasNewPath = true;

        if(!overrideCoordinates)
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

        Vector2Int oldCoordinates = Coordinates;
        if(HasFinishedPath())
        {
            if(Path.AllCells.Count > 0)
                Coordinates = Path.AllCells[^1];

            PathUpdated?.Invoke(this, oldPath, Path);
            return;
        }

        Coordinates = Path.AllCells[PathCellIndex];
        OwlLogger.Log($"Overriding Coordinates during SetPath for GridEntity {Id}. {oldCoordinates} -> {Coordinates}", GameComponent.Grid, LogSeverity.VeryVerbose);            

        PathUpdated?.Invoke(this, oldPath, Path);
    }

    public void SetParentGrid(GridData grid)
    {
        if (grid == ParentGrid)
            return;

        if(grid == null)
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

    public List<GridEntity> RecalculateVisibleEntities(out HashSet<GridEntity> newVisibleEntities,
            out HashSet<GridEntity> stillVisibleEntities, out HashSet<GridEntity> removedEntities)
    {
        newVisibleEntities = new();
        stillVisibleEntities = new();
        removedEntities = new();

        if (ParentGrid == null)
            return new();

        List<GridEntity> totalVisibleNew = ParentGrid.GetOccupantsInRangeSquare<GridEntity>(Coordinates, VisionRange);
        Extensions.DiffArrays(VisibleEntities, totalVisibleNew, out newVisibleEntities, out stillVisibleEntities, out removedEntities);

        return totalVisibleNew;
    }

    public HashSet<CellEffectGroup> RecalculateVisibleCellEffectGroups(out HashSet<CellEffectGroup> newGroups, out HashSet<CellEffectGroup> oldGroups, out HashSet<CellEffectGroup> removedGroups)
    {
        newGroups = new();
        oldGroups = new();
        removedGroups = new();

        if (ParentGrid == null)
            return new();

        HashSet<CellEffectGroup> totalVisibleNew = ParentGrid.GetGroupsInRangeSquare<CellEffectGroup>(Coordinates, VisionRange);
        Extensions.DiffArrays(VisibleCellEffectGroups, totalVisibleNew, out newGroups, out oldGroups, out removedGroups);

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

    public virtual bool CanMove()
    {
        return MovementCooldown <= 0;
    }

    public GridEntityDataPacket ToDataPacket()
    {
        return new GridEntityDataPacket()
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
        };
    }
}
