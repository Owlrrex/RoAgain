using OwlLogging;
using Shared;

namespace Client
{
    public class ClientBattleEntity : BattleEntity
    {
        public int BaseLvl; // for Aura

        public void SetData(BattleEntityData data)
        {
            if (Id != 0 && Id != data.UnitId)
            {
                OwlLogger.LogWarning($"Unit Id {Id} has its ID changed to {data.UnitId}!", GameComponent.Other);
            }
            Id = data.UnitId;
            Name = data.UnitName;
            if(!string.IsNullOrEmpty(MapId) && MapId != data.MapId)
            {
                OwlLogger.LogWarning($"Unit Id {Id} has its MapId changed from {MapId} to {data.MapId}", GameComponent.Other);
            }
            MapId = data.MapId;
            if (data.Path != null)
            {
                SetPath(data.Path, data.PathCellIndex);
            }
            Movespeed.Value = data.Movespeed;
            MovementCooldown = data.MovementCooldown;
            Coordinates = data.Coordinates;
            Orientation = data.Orientation;

            MaxHp.SetBase(data.MaxHp);
            CurrentHp = data.Hp;
            MaxSp.SetBase(data.MaxSp);
            CurrentSp = data.Sp;

            ModelId = 1; // tmp: hardcoded value for mobs
        }
    }
}

