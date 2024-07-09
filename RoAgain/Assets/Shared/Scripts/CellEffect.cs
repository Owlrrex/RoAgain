using OwlLogging;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[Serializable]
public abstract class GridShape
{
    public bool IncludeVoid;

    public abstract List<GridCellData> GatherCellDatas(GridData grid);

    public abstract List<Vector2Int> GatherCoordinates(GridData grid);

    public abstract void GetBounds(GridData grid, out Vector2Int min, out Vector2Int max);
}

[Serializable]
public class RectangleBoundsGridShape : GridShape
{
    public Vector2Int SourceBoundsMin;
    public Vector2Int SourceBoundsMax;

    public override List<GridCellData> GatherCellDatas(GridData grid)
    {
        return grid.GetCellsInRange(SourceBoundsMin, SourceBoundsMax, IncludeVoid);
    }

    public override List<Vector2Int> GatherCoordinates(GridData grid)
    {
        return grid.GetCoordinatesInRange(SourceBoundsMin, SourceBoundsMax, IncludeVoid);
    }

    public override void GetBounds(GridData grid, out Vector2Int min, out Vector2Int max)
    {
        grid.ClampBounds(SourceBoundsMin, SourceBoundsMax, out min, out max);
    }
}

[Serializable]
public class SquareCenterGridShape : GridShape
{
    public Vector2Int Center;
    public int Radius;

    public override List<GridCellData> GatherCellDatas(GridData grid)
    {
        Vector2Int boundsMin = new(Center.x - Radius, Center.y - Radius);
        Vector2Int boundsMax = new(Center.x + Radius, Center.y + Radius);
        grid.ClampBounds(boundsMin, boundsMax, out boundsMin, out boundsMax);
        return grid.GetCellsInRange(boundsMin, boundsMax, IncludeVoid);
    }

    public override List<Vector2Int> GatherCoordinates(GridData grid)
    {
        Vector2Int boundsMin = new(Center.x - Radius, Center.y - Radius);
        Vector2Int boundsMax = new(Center.x + Radius, Center.y + Radius);
        grid.ClampBounds(boundsMin, boundsMax, out boundsMin, out boundsMax);
        return grid.GetCoordinatesInRange(boundsMin, boundsMax, IncludeVoid);
    }

    public override void GetBounds(GridData grid, out Vector2Int min, out Vector2Int max)
    {
        grid.MakeBounds(Center, Radius, out min, out max);
    }
}

public enum CellEffectType
{
    Unknown = 0,
    Warp = 1,
}

public abstract class CellEffectGroup // maybe instead of having CellEffectCells which do the logic, just have the group itself?
{
    private static int _nextId = 1;
    public abstract CellEffectType Type { get; }
    public int Id;

    public Vector2Int BoundsMin => _boundsMin;
    private Vector2Int _boundsMin;
    public Vector2Int BoundsMax => _boundsMax;
    private Vector2Int _boundsMax;

    public GridShape Shape { get; private set; }

    public List<GridCellData> AffectedCells { get; private set; }

    protected virtual int Create(GridData grid, GridShape shape)
    {
        Id = GetNextGroupId();
        Shape = shape;

        AffectedCells = Shape.GatherCellDatas(grid);
        foreach(GridCellData cellData in AffectedCells)
        {
            cellData.AddCellEffect(this);
        }

        Shape.GetBounds(grid, out _boundsMin, out _boundsMax);

        grid.AddCellEffectGroup(this);

        return 0;
    }

    public static int GetNextGroupId()
    {
        return _nextId++;
    }

    // These callbacks are only ever called on server-side, even if client uses the same classes
    public abstract int EntityEntered(GridEntity entity);

    public abstract int Update(float deltaTime);

    public abstract int EntityLeft(GridEntity entity);
}