using OwlLogging;
using System.Collections.Generic;
using System.IO;

public class GridCellData
{
    public const float CELL_HEIGHT_VOID = -1;
    private List<GridEntity> _occupants = new();
    public float CellHeight = CELL_HEIGHT_VOID;

    private List<CellEffectGroup> _cellEffects = new(5);

    public List<GridEntity> GetOccupants()
    {
        return _occupants;
    }

    public bool HasOccupants()
    {
        return _occupants.Count > 0;
    }

    public bool PlaceOccupant(GridEntity occupant)
    {
        if(_occupants.Contains(occupant))
        {
            OwlLogger.LogError($"Tried to place occupant {occupant.Id} twice on cell", GameComponent.Grid);
            return false;
        }

        _occupants.Add(occupant);
        return true;
    }

    public bool RemoveOccupant(GridEntity occupant)
    {
        if(!_occupants.Contains(occupant))
        {
            OwlLogger.LogError($"Tried to remove occupant {occupant.Id} from cell its not on", GameComponent.Grid);
            return false;
        }

        return _occupants.Remove(occupant);
    }

    // This describes cells that are "holes" in the walkmesh
    public bool IsVoidCell()
    {
        return CellHeight == CELL_HEIGHT_VOID;
    }

    // Can you walk over this cell?
    // This considers various runtime-considerations like ice walls
    public bool IsWalkable()
    {
        return !IsVoidCell();
    }

    // Is this cell valid to end your move on? (= no other character on there, etc)
    // This implies being Walkable.
    public bool IsStandable()
    {
        return IsWalkable() && _occupants.Count == 0;
    }

    public List<CellEffectGroup> GetCellEffects()
    {
        return _cellEffects;
    }

    public int AddCellEffect(CellEffectGroup cellEffect)
    {
        if (_cellEffects.Contains(cellEffect))
            return -1;

        _cellEffects.Add(cellEffect);
        return 0;
    }

    public int RemoveCellEffect(CellEffectGroup cellEffect)
    {
        return _cellEffects.Remove(cellEffect) ? 0 : -1;
    }

    public bool WriteToBinary(BinaryWriter writer)
    {
        if (writer == null)
            return false;

        writer.Write(CellHeight);
        return true;
    }
}
