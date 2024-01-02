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

        public int Initialize(int squareSidelength, GridData.Direction nextDirection = GridData.Direction.North)
        {
            if(MaxHp.Base == 0)
                MaxHp.SetBase(50);
            CurrentHp = MaxHp.Total;
            HpRegenAmount.ModifyAdd(10);
            HpRegenTime = 10;

            if(MaxSp.Base == 0)
                MaxSp.SetBase(30);
            CurrentSp = MaxSp.Total;
            SpRegenAmount.ModifyAdd(15);
            SpRegenTime = 20;

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

            Vector2Int nextSquareLeg = _nextDirection.ToVector();
            if (nextSquareLeg == Vector2Int.zero)
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

            nextSquareLeg *= _squareSidelength;

            Vector2Int nextCoords = Coordinates + nextSquareLeg;
            if(ParentGrid.AreCoordinatesValid(nextCoords))
                ParentGrid.FindAndSetPathTo(this, nextCoords);
        }
    }
}

