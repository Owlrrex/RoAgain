using OwlLogging;
using Shared;

namespace Client
{
    public class ClientBattleEntity : BattleEntity
    {
        public int BaseLvl; // for Aura
        public ASkillExecution QueuedSkill;

        public ClientBattleEntity(Coordinate coordinates, LocalizedStringId locNameId, int modelId, float movespeed, int maxHp, int maxSp, int baseLvl, int id = -1) : base(coordinates, locNameId, modelId, movespeed, maxHp, maxSp, id)
        {
            BaseLvl = baseLvl;
        }

        public void SetData(BattleEntityData data)
        {
            if (Id != 0 && Id != data.EntityId)
            {
                OwlLogger.LogWarning($"Unit Id {Id} has its ID changed to {data.EntityId}!", GameComponent.Other);
            }
            Id = data.EntityId;
            NameOverride = data.NameOverride;
            LocalizedNameId = data.LocalizedNameId;
            MapId = data.MapId;
            if (data.Path != null)
            {
                SetPath(data.Path, data.PathCellIndex);
            }
            Movespeed.Value = data.Movespeed;
            MovementCooldown = data.MovementCooldown;
            Coordinates = data.Coordinates;
            Orientation = data.Orientation;
            ModelId = data.ModelId;

            BaseLvl = data.BaseLvl;
            MaxHp.SetBase(data.MaxHp);
            CurrentHp = data.Hp;
            MaxSp.SetBase(data.MaxSp);
            CurrentSp = data.Sp;
        }
    }
}

