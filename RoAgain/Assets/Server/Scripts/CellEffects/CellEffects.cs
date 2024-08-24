using OwlLogging;
using Server;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WarpCellEffectGroup : CellEffectGroup
{
    public override CellEffectType Type => CellEffectType.Warp;

    private string _targetMap;
    private Vector2Int _targetCoords;
    private TimerFloat _duration = new();
    private GridData _grid;

    public int Create(GridData grid, GridShape shape, string targetMap, Vector2Int targetCoords, float duration = -1)
    {
        if(string.IsNullOrEmpty(targetMap))
        {
            OwlLogger.LogError($"Can't create Warp to empty mapid!", GameComponent.Other);
            return -1;
        }

        if(targetCoords == GridData.INVALID_COORDS)
        {
            OwlLogger.LogError($"Can't create warp to invalid target Coordinates {targetCoords}!", GameComponent.Other);
            return -2;
        }

        _grid = grid;
        _targetMap = targetMap;
        _targetCoords = targetCoords;
        _duration.Initialize(duration);

        return base.Create(grid, shape);
    }

    public override int EntityEntered(GridEntity entity)
    {
        // only warp characters for now
        // implement mob-warping via config in the far, far future
        // Or maybe even make it a configurable property of the Group
        if(entity is CharacterRuntimeData)
        {
            AServer.Instance.MapModule.MoveEntityBetweenMaps(entity.Id, entity.MapId, _targetMap, _targetCoords);
        }
        return 0;
    }

    public override int EntityLeft(GridEntity entity)
    {
        return 0;
    }

    public override int Update(float deltaTime)
    {
        if(_duration.MaxValue > 0)
        {
            _duration.Update(deltaTime);
            if(_duration.IsFinished())
            {
                _grid.RemoveCellEffectGroup(this);
            }
        }

        // Could try to be extra-careful here and warp all occupants that are eligable
        // But then every unwarpable occupant standing in a warp has a performance cost
        return 0;
    }
}