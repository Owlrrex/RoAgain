using OwlLogging;
using Server;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TextCore.Text;

// This class manages a single server-side instance of a map
public class ServerMapInstance
{
    public GridData Grid { get; private set; }
    public string MapId { get; private set; }

    public BattleModule BattleModule { get; private set; }

    public MapMobManager MobManager { get; private set; }

    public SkillModule SkillModule { get; private set; }


    // This loads the actual map data in the future, I guess?
    public int Initialize(string mapId, ExperienceModule expModule)
    {
        MapId = mapId;
        int loadError = LoadGridFromFile(MapId);
        if (loadError != 0)
            return loadError;

        BattleModule = new();
        BattleModule.Initialize(this);

        MobManager = new();
        MobManager.Initialize(this, expModule);

        SkillModule = new();
        SkillModule.Initialize(this);

        // Pre-place stuff
        if(mapId == "test_map")
        {
            // Entity test: Place square-walker somewhere
            SquareWalkerEntity walker = new()
            {
                Id = 990,
            };
            walker.Movespeed.Value = 2;
            walker.Initialize(5);
            Grid.PlaceOccupant(walker, new(40, 40));

            // CellEffect test: A warp somewhere
            GridShape shape = new RectangleBoundsGridShape()
            {
                IncludeVoid = false,
                SourceBoundsMin = new(8, 30),
                SourceBoundsMax = new(12, 35)
            };
            WarpCellEffectGroup group = new();
            group.Create(Grid, shape, "test_map2", new(5, 6));
        }
        else
        {
            // Entity test: Place square-walker somewhere
            SquareWalkerEntity walker = new()
            {
                Id = 991,
            };
            walker.Movespeed.Value = 2;
            walker.Initialize(2);
            Grid.PlaceOccupant(walker, new(5, 10));

            // CellEffect test: A warp somewhere
            GridShape hillToBridge = new RectangleBoundsGridShape()
            {
                IncludeVoid = false,
                SourceBoundsMin = new(41, 20),
                SourceBoundsMax = new(43, 21)
            };
            WarpCellEffectGroup group = new();
            group.Create(Grid, hillToBridge, "test_map2", new(10, 41));

            GridShape bridgeToHill = new RectangleBoundsGridShape()
            {
                IncludeVoid = false,
                SourceBoundsMin = new(8, 40),
                SourceBoundsMax = new(9, 42)
            };
            group = new WarpCellEffectGroup();
            group.Create(Grid, bridgeToHill, "test_map2", new(42, 19));

            GridShape bridgeToMap = new RectangleBoundsGridShape()
            {
                IncludeVoid = false,
                SourceBoundsMin = new(46, 40),
                SourceBoundsMax = new(47, 42)
            };
            group = new WarpCellEffectGroup();
            group.Create(Grid, bridgeToMap, "test_map", new(5, 5));
        }

        return loadError;
    }

    private int LoadGridFromFile(string mapid) 
    {
        if (Grid != null)
            DetachFromGrid();

        Grid = GridData.LoadMapFromFiles(mapid);
        if(Grid == null)
        {
            return -1;
        }

        SetupWithGrid();

        return 0;
    }

    private void SetupWithGrid()
    {
        Grid.EntityPlaced += OnEntityPlaced;
        Grid.EntityRemoved += OnEntityRemoved;
    }

    private void DetachFromGrid()
    {
        Grid.EntityPlaced -= OnEntityPlaced;
        Grid.EntityRemoved -= OnEntityRemoved;
    }

    private void OnEntityPlaced(GridEntity entity, Vector2Int coords)
    {
        entity.MapId = MapId;
        if (entity is CharacterRuntimeData data)
        {
            //UpdateVisibleEntities(data);
        }
        // No need to send packet here, will be discovered as part of the VisibleEntities-update automatically
    }

    private void OnEntityRemoved(GridEntity entity, Vector2Int coords)
    {
        EntityRemovedPacket packet = new()
        {
            EntityId = entity.Id,
        };

        List<CharacterRuntimeData> observers = Grid.GetOccupantsInRangeSquare<CharacterRuntimeData>(coords, GridData.MAX_VISION_RANGE);
        foreach(CharacterRuntimeData obs in observers)
        {
            obs.Connection.Send(packet);
        }
    }

    public void Update(float deltaTime)
    {
        UpdateEntityMovementStupid(deltaTime);
        SkillModule?.UpdateSkillExecutions(deltaTime);
        BattleModule?.UpdateRegenerations(deltaTime);
        Grid?.UpdateCellEffects(deltaTime);
        MobManager?.UpdateMobSpawns(deltaTime);
    }

    // We do as little clever "filtering" here as reasonable, leave optimization for later
    private void UpdateEntityMovementStupid(float deltaTime)
    {
        List<GridEntity> movedEntities = Grid.UpdateEntityMovment(deltaTime);
        ICollection<GridEntity> allEntities = Grid.GetAllOccupants();
        List<CharacterRuntimeData> characters = new();
        foreach(GridEntity entity in allEntities)
        {
            if(entity is CharacterRuntimeData)
                characters.Add(entity as CharacterRuntimeData);
        }

        foreach (CharacterRuntimeData character in characters)
        {
            UpdateVisibleEntities(character);
            UpdateVisibleCellGroups(character);
        }

        // Entities in newly placed effects will be handled in AddEffect/UpdateCellEffects
        foreach (GridEntity entity in movedEntities)
        {
            GridCellData oldCell = Grid.GetDataAtCoords(entity.LastUpdateCoordinates);
            GridCellData newCell = Grid.GetDataAtCoords(entity.Coordinates);

            List<CellEffectGroup> oldEffects = oldCell.GetCellEffects();
            List<CellEffectGroup> newEffects = newCell.GetCellEffects();

            List<CellEffectGroup> leftEffects = new();
            List<CellEffectGroup> stayedEffects = new();
            List<CellEffectGroup> enteredEffects = new();
            
            bool[] oldEffectsStayed = new bool[oldEffects.Count];

            for(int i = 0; i < newEffects.Count; i++)
            {
                if (oldEffects.Contains(newEffects[i]))
                {
                    stayedEffects.Add(newEffects[i]);
                    oldEffectsStayed[i] = true;
                }
                else
                    enteredEffects.Add(newEffects[i]);
            }

            for (int i = 0; i < oldEffects.Count; i++)
            {
                if (!oldEffectsStayed[i])
                    leftEffects.Add(oldEffects[i]);
            }

            foreach(CellEffectGroup leftEffect in  leftEffects)
            {
                leftEffect.EntityLeft(entity);
            }

            foreach(CellEffectGroup enteredEffect in enteredEffects)
            {
                enteredEffect.EntityEntered(entity);
            }
        }
    }

    // This function tries to filter beforehand which characters to update visibility for, but it doesn't work well yet
    private void UpdateEntityMovementBrokenFilter(float deltaTime)
    {
        List<GridEntity> movedEntities = Grid.UpdateEntityMovment(deltaTime);
        HashSet<CharacterRuntimeData> charactersToUpdate = new();

        foreach (GridEntity movedEntity in movedEntities)
        {
            var charactersInRange = Grid.GetOccupantsInRangeSquare<CharacterRuntimeData>(movedEntity.Coordinates, GridData.MAX_VISION_RANGE);
            charactersToUpdate.UnionWith(charactersInRange);
        }

        foreach (CharacterRuntimeData character in charactersToUpdate)
        {
            UpdateVisibleEntities(character);
        }
    }

    private void UpdateVisibleEntities(CharacterRuntimeData character)
    {
        List<GridEntity> totalVisible;
        totalVisible = character.RecalculateVisibleEntities(out HashSet<GridEntity> newVisible, out HashSet<GridEntity> remainingVisible, out _);
        character.VisibleEntities = totalVisible;

        foreach (GridEntity newEntity in newVisible)
        {
            // For a unit entering update-range (could be first time): Send full data update
            character.NetworkQueue.GridEntityDataUpdate(newEntity);
        }

        foreach (GridEntity entity in remainingVisible)
        {
            if (entity.HasNewPath)
            {
                // For unit only updating its path: Send only path update
                character.NetworkQueue.GridEntityPathUpdate(entity);
            }
        }
    }

    private void UpdateVisibleCellGroups(CharacterRuntimeData character)
    {
        character.VisibleCellEffectGroups = character.RecalculateVisibleCellEffectGroups(out HashSet<CellEffectGroup> newVisible, out _, out HashSet<CellEffectGroup> removedVisible);

        foreach(CellEffectGroup newGroup in newVisible)
        {
            CellEffectGroupPlacedPacket packet = new()
            {
                GroupId = newGroup.Id,
                Shape = newGroup.Shape,
                Type = newGroup.Type
            };
            character.Connection.Send(packet);
        }

        foreach(CellEffectGroup removedGroup in removedVisible)
        {
            CellEffectGroupRemovedPacket packet = new()
            { GroupId = removedGroup.Id };
            character.Connection.Send(packet);
        }
    }

    public void Shutdown()
    {
        BattleModule?.Shutdown();

        MobManager?.Shutdown();

        BattleModule = null;
        MobManager = null;
        Grid = null;
        MapId = null;
    }
}
