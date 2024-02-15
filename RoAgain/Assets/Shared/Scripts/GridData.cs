using System.Collections.Generic;
using UnityEngine;
using System;
using OwlLogging;
using System.IO;
using UnityEngine.TextCore.Text;

// This class currently mixes client-only-functions (World Positions, Ray casts, etc) with Shared functionality
public class GridData
{
    // Can't move to Config system - Shared-assembly has no config system
    public const int MAX_VISION_RANGE = 20;

    public enum Direction
    {
        Unknown,
        North,
        NorthEast,
        East,
        SouthEast,
        South,
        SouthWest,
        West,
        NorthWest
    }

    [Serializable]
    public class Path
    {
        public List<Vector2Int> Corners = new();
        public List<Vector2Int> AllCells = new();

        public Vector2Int[] GetPathBounds()
        {
            Vector2Int[] result = new Vector2Int[2];
            result[0] = INVALID_COORDS;
            result[1] = INVALID_COORDS;
            if (Corners.Count == 0)
            {
                OwlLogger.LogWarning("Bounds calculated for empty Path!!", GameComponent.Grid);
                return result;
            }

            result[0] = Corners[0];
            result[1] = Corners[0];

            if (Corners.Count == 1)
            {
                OwlLogger.LogWarning("Bounds calculated for Path with only 1 cell!", GameComponent.Grid);
                return result;
            }

            foreach(Vector2Int c in Corners)
            {
                if (c.x > result[1].x)
                    result[1].x = c.x;
                if (c.x < result[0].x)
                    result[0].x = c.x;
                if (c.y > result[1].y)
                    result[1].y = c.y;
                if (c.y < result[0].y)
                    result[0].y = c.y;
            }
            return result;
        }

        public int ReconstructAllCellsFromCorners()
        {
            throw new NotImplementedException();
        }

        public Path Sanitize()
        {
            if(AllCells.Count == 0 && Corners.Count == 0)
            {
                // Empty path - erase
                // Can't return valid "stationary" format here - can't retrieve coordinates from entity that we don't know
                return null;
            }

            if(AllCells.Count == 1 && Corners.Count == 1)
            {
                // Unused "indicates stationary position" format
                // Could return a valid "stationary path" format here instead
                return null;
            }

            if(AllCells.Count < 2 || Corners.Count < 2)
            {
                // All valid forms of paths with any array shorter than 2 are checked above - this catches all paths where either array is malformed
                OwlLogger.LogError($"Malformed Path found in Sanitize(): {this}", GameComponent.Grid);
                return null;
            }

            return this;
        }
    }

    public static readonly string MAP_FILE_DIR = System.IO.Path.Combine(Application.dataPath, "MapFiles");

    public static readonly Vector2Int INVALID_COORDS = new(-1, -1);

    private GridCellData[] _cellDatas;
    public Vector2Int Bounds { get; private set; }

    private Dictionary<int, GridEntity> _entitiesById = new();

    public Action<GridEntity, Vector2Int, Vector2Int> EntityMoved;
    public Action<GridEntity, Vector2Int> EntityPlaced;
    public Action<GridEntity, Vector2Int> EntityRemoved;

    private List<CellEffectGroup> _cellEffects = new(10);

    public bool Initialize(Vector2Int bounds)
    {
        Bounds = bounds;
        _cellDatas = new GridCellData[bounds.x * bounds.y];
        Array.Fill(_cellDatas, new GridCellData());

        return true;
    }

    public bool Initialize(GridCellData[] cellDatas, Vector2Int bounds)
    {
        if(cellDatas == null || cellDatas.Length == 0)
        {
            OwlLogger.LogError("Can't initialize GridData with empty cellDatas!", GameComponent.Grid);
            return false;
        }

        if(cellDatas.Length != bounds.x * bounds.y)
        {
            OwlLogger.LogError($"Can't initialize GridData when cellData Length {cellDatas.Length} mismatches bounds {bounds}", GameComponent.Grid);
            return false;
        }

        foreach (GridCellData data in cellDatas)
        {
            if(data == null)
            {
                OwlLogger.LogError("Can't initialize GridData with cellDatas containing null!", GameComponent.Grid);
                return false;
            }
        }

        _cellDatas = cellDatas;
        Bounds = bounds;
        return true;
    }

    public static GridData LoadMapFromFiles(string mapId)
    {
        string filepath = System.IO.Path.Combine(MAP_FILE_DIR, $"{mapId}.gatu");
        OwlLogger.Log($"Trying to load map {filepath}", GameComponent.Grid);
        // TODO: All the error catching/checking
        using FileStream fs = new(filepath, FileMode.Open, FileAccess.Read);
        byte[] bytes = new byte[fs.Length];
        int numBytesToRead = (int)fs.Length;
        int numBytesRead = 0;
        while (numBytesToRead > 0)
        {
            // Read may return anything from 0 to numBytesToRead.
            int n = fs.Read(bytes, numBytesRead, numBytesToRead);

            // Break when the end of the file is reached.
            if (n == 0)
                break;

            numBytesRead += n;
            numBytesToRead -= n;
        }

        GridData grid = new();
        grid.DeserializeAndInitialize(bytes);

        return grid;
    }

    public bool PlaceOccupantFromPath(GridEntity occupant)
    {
        if (occupant.HasFinishedPath())
            return PlaceOccupant(occupant, occupant.Path.AllCells[^1]);
        else
            return PlaceOccupant(occupant, occupant.Path.AllCells[occupant.PathCellIndex]);
    }

    public bool PlaceOccupant(GridEntity occupant, Vector2Int coords)
    {
        //if (_entities.Contains(occupant))
        if(_entitiesById.ContainsKey(occupant.Id))
        {
            OwlLogger.LogError($"Tried to place occupant {occupant} on grid twice!", GameComponent.Grid);
            return false;
        }

        GridCellData cell = GetDataAtCoords(coords);
        bool placeResult = cell.PlaceOccupant(occupant);
        if (placeResult)
        {
            occupant.Coordinates = coords;
            //_entities.Add(occupant);
            _entitiesById.Add(occupant.Id, occupant);
            occupant.SetParentGrid(this);
        }

        EntityPlaced?.Invoke(occupant, coords);

        return placeResult;
    }

    public bool RemoveOccupant(GridEntity occupant)
    {
        return RemoveOccupant(occupant, occupant.Coordinates);
    }

    public bool RemoveOccupant(GridEntity occupant, Vector2Int coords)
    {
        // Could check current occupant parentGrid here?
        //if(!_entities.Contains(occupant))
        if(!_entitiesById.ContainsKey(occupant.Id))
        {
            OwlLogger.LogError($"Tried to remove occupant {occupant} from grid it's not on!", GameComponent.Grid);
            return false;
        }

        GridCellData cell = GetDataAtCoords(coords);
        bool removeResult = cell.RemoveOccupant(occupant);
        if(removeResult)
        {
            occupant.Coordinates = INVALID_COORDS;
            //_entities.Remove(occupant);
            _entitiesById.Remove(occupant.Id);
            occupant.SetParentGrid(null);
        }
        else
        {
            OwlLogger.LogError($"Removing occupant {occupant.Id} from its cell failed!", GameComponent.Grid);
        }

        EntityRemoved?.Invoke(occupant, coords);

        return removeResult;
    }

    public bool MoveOccupant(GridEntity occupant, Vector2Int from, Vector2Int to, bool wantsToStand = false)
    {
        if (from == to)
            return true;

        // Could check current occupant parentGrid here
        //if (!_entities.Contains(occupant))
        if (!_entitiesById.ContainsKey(occupant.Id))
        {
            OwlLogger.LogError($"Tried to move occupant {occupant.Id} who's not on grid!", GameComponent.Grid);
            return false;
        }

        GridCellData fromCell = GetDataAtCoords(from);
        GridCellData toCell = GetDataAtCoords(to);

        bool positionCheck = wantsToStand ? toCell.IsStandable() : toCell.IsWalkable();
        if(!positionCheck)
        {
            OwlLogger.Log($"Tried to move occupant {occupant.Id} to cell {to} that's not walkable/standable!", GameComponent.Grid);
            return false;
        }

        bool removeResult = fromCell.RemoveOccupant(occupant);
        if (!removeResult)
        {
            return false;
        }

        
        bool placeResult = toCell.PlaceOccupant(occupant);
        if (placeResult)
        {
            occupant.Coordinates = to;
        }

        EntityMoved?.Invoke(occupant, from, to);

        return placeResult;
    }

    public List<GridEntity> GetOccupantsOfCell(Vector2Int coords)
    {
        int index = CoordsToIndex(coords);
        GridCellData data = _cellDatas[index];
        return data.GetOccupants();
    }

    public ICollection<GridEntity> GetAllOccupants()
    {
        return _entitiesById.Values;
    }

    public GridEntity GetOccupantFromCell(Vector2Int coords, int entityId)
    {
        List<GridEntity> occupants = GetOccupantsOfCell(coords);
        if (occupants == null || occupants.Count == 0)
            return null;
        return occupants.Find(x => x.Id == entityId);
    }

    public GridEntity FindOccupant(int entityId)
    {
        if(_entitiesById.ContainsKey(entityId))
            return _entitiesById[entityId];
        return null;
    }

    public List<GridCellData> GetCellsInRange(Vector2Int min, Vector2Int max, bool includeVoid = true)
    {
        if (!AreCoordinatesValid(min))
            throw new ArgumentOutOfRangeException("bottomleft");

        if (!AreCoordinatesValid(max))
            throw new ArgumentOutOfRangeException("topright");

        if (min.x > max.x
            || min.y > max.y)
        {
            throw new ArgumentException("bottomleft and topright don't span a proper rectangle!");
        }
            
        int heightspan = max.y - min.y + 1;
        int widthspan = max.x - min.x + 1;

        List<GridCellData> gridCellDatas = new (heightspan * widthspan);
        for(int i = min.y; i <= max.y; ++i)
        {
            for(int j = min.x; j <= max.x; ++j)
            {
                Vector2Int coords = new() { x = j, y = i };
                GridCellData data = GetDataAtCoords(coords);
                if (includeVoid || !data.IsVoidCell())
                    gridCellDatas.Add(data);
                
            }
        }
        return gridCellDatas;
    }

    public List<Vector2Int> GetCoordinatesInRange(Vector2Int min, Vector2Int max, bool includeVoid = true)
    {
        if (!AreCoordinatesValid(min))
            throw new ArgumentOutOfRangeException("bottomleft");

        if (!AreCoordinatesValid(max))
            throw new ArgumentOutOfRangeException("topright");

        int heightspan = max.y - min.y + 1;
        int widthspan = max.x - min.x + 1;

        List<Vector2Int> gridCoords = new(heightspan * widthspan);

        for (int i = min.y; i <= max.y; ++i)
        {
            for (int j = min.x; j <= max.x; ++j)
            {
                Vector2Int coords = new() { x = j, y = i };
                
                if (!includeVoid)
                {
                    GridCellData data = GetDataAtCoords(coords);
                    if (data.IsVoidCell())
                        continue;
                }
                gridCoords.Add(coords);
            }
        }
        return gridCoords;
    }

    public List<T> GetOccupantsInRangeSquare<T>(Vector2Int min, Vector2Int max) where T : GridEntity
    {
        List<T> typedOccupants = new();
        // this function could iterate over _entities and do distance-checks
        // or iterate cells & find occupants that way
        // profile regularily which is better & adjust based on common use-cases!
        // could even decide algorithm at runtime based on Grid-occupant-count, since that's available quickly


        //List<GridCellData> cellDatas = GetCellsInRange(min, max);
        //for (int i = 0; i < cellDatas.Count; ++i)
        //{
        //    List<GridEntity> occupants = cellDatas[i].GetOccupants();
        //    foreach (GridEntity occupant in occupants)
        //    {
        //        if (occupant is T t)
        //            typedOccupants.Add(t);
        //    }
        //}

        // ------------------------------------------------------

        foreach (GridEntity entity in GetAllOccupants())
        {
            if (entity is T t)
            {
                if(Overlaps(min, max, entity.Coordinates, entity.Coordinates))
                    typedOccupants.Add(t);
            }
        }

        return typedOccupants;
    }

    private Dictionary<Type, System.Collections.IList> _buffers = new();

    public List<T> GetOccupantsInRangeSquareLowAlloc<T>(Vector2Int min, Vector2Int max) where T : GridEntity
    {
        if(!_buffers.ContainsKey(typeof(T)))
        {
            _buffers.Add(typeof(T), new List<T>());
        }

        List<T> typedOccupants = _buffers[typeof(T)] as List<T>;
        typedOccupants.Clear();
        foreach (GridEntity entity in GetAllOccupants())
        {
            if (entity is T t)
            {
                if (Overlaps(min, max, entity.Coordinates, entity.Coordinates))
                    typedOccupants.Add(t);
            }
        }
        return typedOccupants;
    }

    // Radius refers to the number of cells the square area extends away from the Unit: 
    // 1 = 3x3 area, 3 = 7x7 area, etc.
    public List<T> GetOccupantsInRangeSquare<T>(Vector2Int center, int radius) where T : GridEntity
    {
        if (radius < 0)
        {
            OwlLogger.LogError($"GetEntitiesInRangeSquare called with invalid radius {radius}", GameComponent.Grid);
            return null;
        }

        if (!MakeBounds(center, radius, out Vector2Int min, out Vector2Int max))
        {
            return null;
        }

        return GetOccupantsInRangeSquare<T>(min, max);
    }

    // Radius refers to the number of cells the square area extends away from the Unit: 
    // 1 = 3x3 area, 3 = 7x7 area, etc.
    public List<T> GetOccupantsInRangeSquareLowAlloc<T>(Vector2Int center, int radius) where T : GridEntity
    {
        if (radius < 0)
        {
            OwlLogger.LogError($"GetEntitiesInRangeSquare called with invalid radius {radius}", GameComponent.Grid);
            return null;
        }

        if (!MakeBounds(center, radius, out Vector2Int min, out Vector2Int max))
        {
            return null;
        }

        return GetOccupantsInRangeSquareLowAlloc<T>(min, max);
    }

    public List<T> GetObserversSquare<T>(Vector2Int origin) where T : GridEntity
    {
        List<T> observers = new();
        foreach(GridEntity entity in GetOccupantsInRangeSquareLowAlloc<T>(origin, MAX_VISION_RANGE))
        {
            if (entity is T t && Extensions.GridDistanceSquare(t.Coordinates, origin) <= t.VisionRange)
                observers.Add(t);
        }
        return observers;
    }

    public List<T> GetObserversSquare<T>(Vector2Int origin, List<GridEntity> exclusionList) where T : GridEntity
    {
        List<T> observers = new();
        foreach (GridEntity entity in GetOccupantsInRangeSquareLowAlloc<T>(origin, MAX_VISION_RANGE))
        {
            if (entity is T t && Extensions.GridDistanceSquare(t.Coordinates, origin) <= t.VisionRange
                && !exclusionList.Contains(entity))
                observers.Add(t);
        }
        return observers;
    }

    public HashSet<T> GetGroupsInRangeSquare<T>(Vector2Int center, int radius) where T : CellEffectGroup
    {
        if(radius < 0)
        {
            OwlLogger.LogError($"GetGroupsInRangeSquare called with invalid radius {radius}", GameComponent.Grid);
            return null;
        }

        if (!MakeBounds(center, radius, out Vector2Int min, out Vector2Int max))
        {
            return null;
        }

        return GetGroupsInRangeSquare<T>(min, max);
    }

    public HashSet<T> GetGroupsInRangeSquare<T>(Vector2Int min, Vector2Int max) where T : CellEffectGroup
    {
        HashSet<T> groups = new();
        // with how few CellGroups are usually on a map, I'm sure distance-checks are usually better than iterating all cells
        foreach (CellEffectGroup group in _cellEffects)
        {
            if (Overlaps(group.BoundsMin, group.BoundsMax, min, max))
            {
                if (group is T t)
                    groups.Add(t);
            }
        }
        return groups;
    }

    public bool AreCoordinatesValid(Vector2Int coords)
    {
        return coords.x > 0 && coords.x <= Bounds.x && coords.y > 0 && coords.y <= Bounds.y;
    }

    public GridCellData GetDataAtCoords(Vector2Int coords)
    {
        return _cellDatas[CoordsToIndex(coords)];
    }

    public int CoordsToIndex(Vector2Int coords)
    {
        if (!AreCoordinatesValid(coords))
            throw new ArgumentOutOfRangeException("coords", $"coords out of bounds for index-conversion: {coords}");

        return (coords.y -1) * Bounds.x + (coords.x -1); // Grid coordinates are 1-based
    }

    #region PathfindingRhombus
    public enum LastDirection
    {
        None,
        Horizontal,
        Diagonal,
        Vertical
    }

    public Path FindPath(Vector2Int startCoords, Vector2Int targetCoords)
    {
        Path path = new();
        
        if (startCoords == targetCoords)
        {
            OwlLogger.LogWarning($"Tried to find path for identical cell {startCoords}!", GameComponent.Grid);
            return path;
        }
            
        if(!AreCoordinatesValid(startCoords))
        {
            OwlLogger.LogError($"Tried to find path for invalid startCoordinates {startCoords}!", GameComponent.Grid);
            return path;
        }

        if(!AreCoordinatesValid(targetCoords))
        {
            OwlLogger.LogError($"Tried to find path for invalid targetCoordinates {targetCoords}!", GameComponent.Grid);
            return path;
        }

        path.AllCells.Add(startCoords);
        path.Corners.Add(startCoords);

        FindPathStep(ref path, targetCoords, LastDirection.None);
        
        if(path.AllCells.Count <= 1)
        {
            // no path found
            OwlLogger.Log("No Path found", GameComponent.Grid);
        }

        return path;
    }

    private bool FindPathStep(ref Path path, Vector2Int targetCoords, LastDirection lastDirection)
    {
        Vector2Int lastCoord = path.AllCells[^1];
        int diffX = targetCoords.x - lastCoord.x;
        int diffY = targetCoords.y - lastCoord.y;
        if (lastCoord == targetCoords)
        {
            // finalize path: Walk back any cells that aren't valid end-cells
            // This may cull the path back to a cell which is further away from the target than a cell on a different path
            GridCellData cellData = GetDataAtCoords(targetCoords);
            while (cellData == null || !cellData.IsStandable())
            {
                if (path.AllCells.Count == 1)
                    break; // don't remove the start-cell
                Vector2Int coordsToRemove = path.AllCells[^1];
                path.AllCells.RemoveAt(path.AllCells.Count - 1);
                path.Corners.Remove(coordsToRemove);
                cellData = GetDataAtCoords(path.AllCells[^1]);
            }

            if(path.AllCells.Count == 0)
            {
                OwlLogger.LogError("All Cells (including start-cell!) have been removed as invalid path!", GameComponent.Grid);
                return false;
            }

            if (!path.Corners.Contains(path.AllCells[^1]))
            {
                path.Corners.Add(path.AllCells[^1]);
            }

            return true; // Target reached as close as it gets (for this algorithm)
        }

        if(diffX != 0 && diffY != 0)
        {
            if (FindPathCheckOneDirection(ref path, targetCoords, lastDirection, LastDirection.Diagonal))
                return true; // a path was found
        }

        if(diffX != 0)
        {
            if (FindPathCheckOneDirection(ref path, targetCoords, lastDirection, LastDirection.Horizontal))
                return true; // a path was found
        }

        if (diffY != 0)
        {
            if (FindPathCheckOneDirection(ref path, targetCoords, lastDirection, LastDirection.Vertical))
                return true; // a path was found
        }

        // none of the steps have reached the target, pathfinding complete
        return false;
    }

    private bool FindPathCheckOneDirection(ref Path path, Vector2Int targetCoords, LastDirection lastDirection, LastDirection nextDirection)
    {
        Vector2Int lastCoord = path.AllCells[^1];
        int diffX = targetCoords.x - lastCoord.x;
        int diffY = targetCoords.y - lastCoord.y;

        Vector2Int nextStep;
        switch(nextDirection)
        {
            case LastDirection.Diagonal:
                nextStep = new Vector2Int(lastCoord.x + Math.Sign(diffX), lastCoord.y + Math.Sign(diffY));
                break;
            case LastDirection.Horizontal:
                nextStep = new Vector2Int(lastCoord.x + Math.Sign(diffX), lastCoord.y);
                break;
            case LastDirection.Vertical:
                nextStep = new Vector2Int(lastCoord.x, lastCoord.y + Math.Sign(diffY));
                break;
            default:
                return false;
        }

        GridCellData nextCellData = GetDataAtCoords(nextStep);
        if (nextCellData == null
            || nextCellData.IsVoidCell()
            || !nextCellData.IsWalkable())
        {
            return false;
        }

        path.AllCells.Add(nextStep);
        if (lastDirection != LastDirection.None && nextDirection != lastDirection)
            path.Corners.Add(lastCoord);
        if (FindPathStep(ref path, targetCoords, nextDirection))
        {
            return true;
        }
        // this direction didn't reach
        path.AllCells.RemoveAt(path.AllCells.Count - 1);
        if (lastDirection != LastDirection.None && nextDirection != lastDirection)
            path.Corners.RemoveAt(path.Corners.Count - 1);

        return false;
    }
    #endregion

    public int FindAndSetPathTo(GridEntity entity, Vector2Int targetCoords)
    {
        Path path = FindPath(entity.Coordinates, targetCoords);
        if (path == null || path.AllCells.Count == 0 || path.AllCells.Count == 1) // 0 & null are error cases, 1 is "no path found"
        {
            entity.ClearPath();
            return -1;
        }
            

        entity.SetPath(path);
        return 0;
    }

    public List<GridEntity> UpdateEntityMovment(float deltaTime)
    {
        List<GridEntity> movedEntities = new();
        foreach (KeyValuePair<int, GridEntity> kvp in _entitiesById)
        {
            GridEntity entity = kvp.Value;

            entity.UpdateLastPath();

            if (entity.MovementCooldown > 0)
            {
                entity.MovementCooldown -= deltaTime;
                continue;
            }

            entity.LastUpdateCoordinates = entity.Coordinates;

            entity.UpdateMovement(deltaTime);

            if (entity.HasFinishedPath())
                continue;

            while (entity.CanMove())
            {
                ProceedAlongPath(entity);
                movedEntities.Add(entity);
                if (entity.HasFinishedPath())
                    break;
            }
        }
        return movedEntities;
    }

    public void ProceedAlongPath(GridEntity entity)
    {
        if (entity.HasFinishedPath())
        {
            return;
        }

        if (entity.Path == null)
        {
            OwlLogger.LogWarning($"GridEntity {entity.Id} can't proceed along path: No path given.", GameComponent.Grid);
            entity.ClearPath();
            return;
        }

        if (entity.PathCellIndex < 0 || entity.PathCellIndex >= entity.Path.AllCells.Count)
        {
            OwlLogger.LogError($"GridEntity {entity.Id} can't proceed along path: CurrentPathCellIndex {entity.PathCellIndex} invalid!", GameComponent.Grid);
            entity.ClearPath();
            return;
        }

        if (!entity.HasFinishedPath() && entity.Coordinates == entity.Path.AllCells[^1])
        {
            OwlLogger.Log($"GridEntity {entity.Id} reached end of its path to {entity.Coordinates}", GameComponent.Grid, LogSeverity.VeryVerbose);
            entity.FinishPath();
            return;
        }

        //Vector2Int expectedCoords = entity.Path.AllCells[entity.PathCellIndex];
        //if (entity.Coordinates != expectedCoords)
        //{
        //    // tmp attempt: Correct coordinates mismatches instead of reporting them
        //    OwlLogger.LogWarning($"Correcting coordinates mismatch for GridEntity {entity.Id}: {entity.Coordinates} -> {expectedCoords}", GameComponent.Grid);
        //    MoveOccupant(entity, entity.Coordinates, expectedCoords);

        //    //OwlLogger.LogError($"GridEntity {entity.Id} can't proceed along path: Coordinate mismatch! Actual: {entity.Coordinates}, Expected: {entity.Path.AllCells[entity.PathCellIndex]}", GameComponent.Grid);
        //    //entity.ClearPath();
        //    //return;
        //}

        if (!entity.CanMove())
        {
            OwlLogger.LogError($"GridEntity {entity.Id} can't proceed along path: Can't move!", GameComponent.Grid);
            return;
        }

        // These anomalous movespeed-values may have a meaning later
        if(entity.Movespeed.Value <= 0)
        {
            OwlLogger.LogError($"GridEntity {entity.Id} has Movespeed of zero or lower - can't move!", GameComponent.Grid);
            entity.ClearPath();
            return;
        }

        Vector2Int oldCoords = entity.Coordinates;
        Vector2Int newCoords = entity.Path.AllCells[++entity.PathCellIndex];
        bool wantsToStand = entity.PathCellIndex == entity.Path.AllCells.Count - 1;
        bool moveSuccessful = MoveOccupant(entity, oldCoords, newCoords, wantsToStand);

        if(!moveSuccessful)
        {
            OwlLogger.Log($"GridEntity {entity.Id} can't proceed along path: Moving from {oldCoords} to {newCoords} failed!", GameComponent.Grid);
            entity.ClearPath();
            return;
        }

        float speedFactor = 1.0f;
        if (oldCoords.x != newCoords.x && oldCoords.y != newCoords.y)
            speedFactor = 1.414f; // Account for diagonal movement distance

        entity.MovementCooldown += (1/entity.Movespeed.Value) * speedFactor;
    }

    public int AddCellEffectGroup(CellEffectGroup group)
    {
        if (_cellEffects.Contains(group))
            return -1;
        
        _cellEffects.Add(group);

        foreach(GridCellData cell in group.AffectedCells)
        {
            foreach(GridEntity entity in cell.GetOccupants())
            {
                group.EntityEntered(entity);
            }
        }

        return 0;
    }

    public int RemoveCellEffectGroup(CellEffectGroup group)
    {
        foreach (GridCellData cell in group.AffectedCells)
        {
            foreach (GridEntity entity in cell.GetOccupants())
            {
                group.EntityLeft(entity);
            }
            cell.RemoveCellEffect(group);
        }

        return _cellEffects.Remove(group) ? 0 : -1;
    }

    public void UpdateCellEffects(float deltaTime)
    {
        for(int i = _cellEffects.Count -1;  i >= 0; i--)
        {
            _cellEffects[i].Update(deltaTime);
        }
    }

    public bool MakeBounds(Vector2Int center, int radius, out Vector2Int min, out Vector2Int max)
    {
        min = new(center.x - radius, center.y - radius);
        max = new(center.x + radius, center.y + radius);

        if (min.x > max.x || min.y > max.y)
        {
            OwlLogger.LogError($"GetEntitiesInRangeSquare produced invalid search bounds: {min} to {max}", GameComponent.Grid);
            return false;
        }

        ClampBounds(min, max, out min, out max);

        return true;
    }

    public void ClampBounds(Vector2Int inMin, Vector2Int inMax, out Vector2Int outMin, out Vector2Int outMax)
    {
        outMin = new(Math.Clamp(inMin.x, 1, Bounds.x), Math.Clamp(inMin.y, 1, Bounds.y));
        outMax = new(Math.Clamp(inMax.x, 1, Bounds.x), Mathf.Clamp(inMax.y, 1, Bounds.y));
    }

    public Vector2Int FindRandomPosition(Vector2Int boundsMin, Vector2Int boundsMax, bool allowVoid)
    {
        if (boundsMin == INVALID_COORDS && boundsMax == INVALID_COORDS)
        {
            return FindRandomPosition(allowVoid);
        }

        if(!AreCoordinatesValid(boundsMin) || !AreCoordinatesValid(boundsMax))
        {
            OwlLogger.LogError($"Can't FindRandomPosition with invalid bounds {boundsMin} / {boundsMax}!", GameComponent.Grid);
            return INVALID_COORDS;
        }

        int maxAttempts = (boundsMax.x - boundsMin.x) * (boundsMax.y - boundsMin.y); // Max attempts = number of cells in the area
        for(int attempts = 0; attempts < maxAttempts; attempts++)
        {
            Vector2Int result = new();
            result.x = UnityEngine.Random.Range(boundsMin.x, boundsMax.x + 1);
            result.y = UnityEngine.Random.Range(boundsMin.y, boundsMax.y + 1);

            // Other conditions for cells to find (like player visibility) go here

            if(allowVoid)
            {
                return result;
            }

            GridCellData cellData = GetDataAtCoords(result);
            if (!cellData.IsVoidCell())
                return result;
        }

        OwlLogger.Log($"FindRandomPosition didn't find valid coordinates in range {boundsMin} / {boundsMax}, allowVoid = {allowVoid}!", GameComponent.Grid);
        return INVALID_COORDS;
    }

    public Vector2Int FindRandomPosition(bool allowVoid)
    {
        return FindRandomPosition(new(1, 1), Bounds, allowVoid);
    }

    public byte[] Serialize()
    {
        byte[] result;

        using(MemoryStream memoryStream = new())
        {
            using(BinaryWriter writer = new(memoryStream))
            {
                writer.Write(Bounds.x);
                writer.Write(Bounds.y);
                foreach(GridCellData data in _cellDatas)
                {
                    if(!data.WriteToBinary(writer))
                    {
                        OwlLogger.LogError("Error writing GridCellData!", GameComponent.Grid);
                        return null;
                    }
                }
            }
            result = memoryStream.ToArray();
        }
        return result;
    }

    public bool DeserializeAndInitialize(byte[] rawData)
    {
        using MemoryStream memStream = new(rawData);
        using BinaryReader reader = new(memStream);
        Vector2Int newBounds = new(reader.ReadInt32(), reader.ReadInt32());
        Bounds = newBounds;
        int mapsize = Bounds.x * Bounds.y;
        _cellDatas = new GridCellData[mapsize];
        for (int i = 0; i < mapsize; i++)
        {
            _cellDatas[i] = new()
            {
                CellHeight = reader.ReadSingle()
            };
        }

        // No further init needed right now

        return true;
    }

    // This function can, in theory, check the overlapping area for void-cells and thus create more detailed checks
    // since it has access to the grid topology
    public bool Overlaps(Vector2Int min1, Vector2Int max1, Vector2Int min2, Vector2Int max2)
    {
        return (min1.x <= max2.x && max1.x >= min2.x)
            && (min1.y <= max2.y && max1.y >= min2.y);
    }
}