using Shared;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Server
{
    public class SquareWalkerEntity : Mob
    {
        private int _squareSidelength;
        private GridData.Direction _nextDirection = GridData.Direction.North;

        public SquareWalkerEntity(Coordinate coordinates, LocalizedStringId locNameId, int modelId, float movespeed, int maxHp, int maxSp, int id = -1)
            : base(coordinates, locNameId, modelId, movespeed, maxHp, maxSp, id)
        {
        }

        public int Initialize(int squareSidelength, GridData.Direction nextDirection = GridData.Direction.North)
        {
            _squareSidelength = squareSidelength;
            _nextDirection = nextDirection;
            return 0;
        }

        public override void UpdateMovement(float deltaTime)
        {
            base.UpdateMovement(deltaTime);

            if (ParentGrid == null)
                return;

            if (!HasFinishedPath())
                return;

            if (IsDead())
                return;

            Coordinate nextSquareLeg = _nextDirection.ToCoordinateOffset();
            if (nextSquareLeg == Coordinate.INVALID)
            {
                _nextDirection = GridData.Direction.North;
                return;
            }

            if (_nextDirection == GridData.Direction.North)
                _nextDirection = GridData.Direction.West;
            else if (_nextDirection == GridData.Direction.West)
                _nextDirection = GridData.Direction.South;
            else if(_nextDirection == GridData.Direction.South)
                _nextDirection = GridData.Direction.East;
            else if(_nextDirection == GridData.Direction.East)
                _nextDirection = GridData.Direction.North;

            nextSquareLeg.X *= _squareSidelength;
            nextSquareLeg.Y *= _squareSidelength;

            Coordinate nextCoords = new(Coordinates.X + nextSquareLeg.X, Coordinates.Y + nextSquareLeg.Y);
            if(ParentGrid.AreCoordinatesValid(nextCoords))
                ParentGrid.FindAndSetPathTo(this, nextCoords);
        }
    }
}

